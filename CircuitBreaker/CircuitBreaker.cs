using Core.Contract;
using Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace Core
{
    public class CircuitBreaker : ICircuitBreaker
    {
        ICircuitStateSynchronization _state;
        SemaphoreSlim _semaphore;
        SemaphoreSlim _tripLock;
        ILogger<CircuitBreaker> _logger;

        #region ctor
        public CircuitBreaker(ICircuitStateSynchronization state)
        {
            _state = state;
            _semaphore = new SemaphoreSlim(1);
            _tripLock = new SemaphoreSlim(1);
            _logger = new NullLogger<CircuitBreaker>();
        }
        public CircuitBreaker(ICircuitStateSynchronization state, ILogger<CircuitBreaker> logger)
        {
            _state = state;
            _semaphore = new SemaphoreSlim(1);
            _tripLock = new SemaphoreSlim(1);
            _logger = logger;
        }
        #endregion

        #region Sync
        public void Execute(Action action) =>
            Execute(action, () => { });

        public void Execute(Action action, Action fallback) {
            if (State == CircuitStateKind.Open)
                throw new CircuitBreakerOpenException();

            if (action == null)
                throw new ArgumentNullException();

            if (State == CircuitStateKind.Closed)
                ActionInvoke(action, fallback);

            else if (State == CircuitStateKind.HalfOpen)
                ActionInvoke(action, fallback);

        }

        private void ActionInvoke(Action action, Action fallback)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                fallback?.Invoke();
                IncrementFailure(1);
            }
        }

        private void ThrottledAction(Action action, Action fallback)
        {
            _semaphore.Wait();
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                Trip();
                _logger.LogError(ex.Message, ex);
                fallback?.Invoke();
            }
            finally
            {
                _semaphore.Release();
            }
        }
        #endregion

        #region Async
        public Task ExecuteAsync(Func<Task> action) =>
            ExecuteAsync(action, () => Task.CompletedTask);
         
        public Task ExecuteAsync(Func<Task> action, Func<Task> fallback)
        {
            if (State == CircuitStateKind.Open)
                throw new CircuitBreakerOpenException();

            if (action == null)
                throw new ArgumentNullException();

            if (State == CircuitStateKind.HalfOpen)
                return ThrottledActionAsync(action, fallback);

            return ActionInvokeAsync(action, fallback);
        }

        private async Task ActionInvokeAsync(Func<Task> action, Func<Task> fallback)
        {
            try
            {
                await action.Invoke();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                ///Server error. Treat as a normal error
                HandleClosedCircuitException(ex as Exception);
                await fallback.Invoke();
            }
            catch (HttpRequestException ex)
            {
                //Something Bad. Hard Stop
                HandleClosedCircuitException(ex);
                await fallback.Invoke();
                throw;
            }
            catch (Exception ex)
            {
                HandleClosedCircuitException(ex);
                await fallback.Invoke();
            }
        }

        private async Task ThrottledActionAsync(Func<Task> action, Func<Task> fallback)
        {
            await _semaphore.WaitAsync(1);
            try
            {
                await action.Invoke();
                IncrementSuccess(1);
            }
            catch (Exception ex)
            {

                Trip();
                _logger.LogError(ex.Message, ex);
                await fallback.Invoke();
            }
            finally
            {
                _semaphore.Release();
            }
        }
        #endregion

        /// <summary>
        /// Base exception. Treats as a general failure.
        /// </summary>
        /// <param name="ex"></param>
        private void HandleClosedCircuitException(Exception ex)
        {
            _logger.LogError(ex.Message, ex);
            (int failures, int max) = IncrementFailure(1);
            if (failures >= max)
                Trip();
        }
        /// <summary>
        /// Network error. Treat as a hard stop.
        /// </summary>
        /// <param name="ex"></param>
        private void HandleClosedCircuitException(HttpRequestException ex)
        {
            _logger.LogError(ex.Message, ex);
            Trip();
        }

        private CircuitStateKind State => _state.Get<CircuitStateKind>((CircuitState status) => status.State);
        private int GetFailures() => _state.Get<int>((CircuitState status) => status.Failures);
        private long GetTicks() => _state.Get<long>((CircuitState status) => status.TimeSinceLastError);

        private (int, int) IncrementFailure(int increase) =>
            _state.Write<(int, int)>((CircuitState status) => {
                status.Failures += increase;
                status.TimeSinceLastError = DateTime.UtcNow.Ticks;
                return (status.Failures, status.MaxFailures);
            });
        private void Trip()
        {
            _tripLock.Wait(1);
            try {
                if(this.State != CircuitStateKind.Open)
                    _state.Write<CircuitState>((CircuitState status) => 
                    { 
                        status.State = CircuitStateKind.Open; 
                        status.FaultTimeout = DateTime.UtcNow.AddMinutes(2).Ticks; 
                    });
            } finally {
                _tripLock.Release();
            }
        }
    
        private int IncrementSuccess(int increase) =>
            _state.Write<int>((CircuitState status) => {
                status.Success += increase;
                return status.Success;
            }); 
        private void Reset() => _state.Write<CircuitState>((CircuitState status) => status.State = CircuitStateKind.Closed);

    }
}