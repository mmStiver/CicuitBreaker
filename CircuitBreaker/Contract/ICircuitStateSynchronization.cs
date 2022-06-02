namespace Core.Contract
{
    public interface ICircuitStateSynchronization
    {
        TResult Get<TResult>(Func<CircuitState, TResult> func);
        void Write<TResult>(Action<CircuitState> acton);
        TResult Write<TResult>(Func<CircuitState, TResult> acton);

    }
}
