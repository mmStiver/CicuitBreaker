using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    [ProtoContract]
    public class CircuitState {
        public CircuitState()
        {
            this.State = CircuitStateKind.Closed;
            this.Failures = 0;
            this.Success = 0;
            this.TimeSinceLastError = 0;
            this.MaxFailures = 20;
            this.FaultTimeout = DateTime.MinValue.Ticks;
            this.SuccessThreshold= 20;
            this.RestartTimeout = DateTime.MinValue.Ticks;
        }
        public CircuitState(CircuitStateKind state,int Failures, int Success, long TimeSinceLastError, int MaxFailures, long FaultTimeout, int SuccessThreshold, long RestartTimeout)
        {
            this.State = state;
            this.Failures = Failures;
            this.Success = Success;
            this.TimeSinceLastError = TimeSinceLastError;
            this.MaxFailures = MaxFailures;
            this.FaultTimeout = FaultTimeout;
            this.SuccessThreshold = SuccessThreshold;
            this.RestartTimeout = RestartTimeout;
        }
        [ProtoMember(1)]
        public CircuitStateKind State { get; set; }
        [ProtoMember(2)]
        public int Failures { get; set; }
        [ProtoMember(3)]
        public int Success { get; set; }
        [ProtoMember(4)]
        public long TimeSinceLastError { get; set; }
        [ProtoMember(5)]
        public int MaxFailures { get; set; }
        [ProtoMember(6)]
        public long FaultTimeout { get; set; }
        [ProtoMember(7)]
        public int SuccessThreshold { get; set; }
        [ProtoMember(8)]
        public long RestartTimeout { get; set; }

        public CircuitState Clone()
        {
            return new CircuitState(State, Failures, Success, TimeSinceLastError, MaxFailures, FaultTimeout, SuccessThreshold, RestartTimeout);
        }

        public bool Equals(CircuitState other)
        {
            if (other is null) return false;
            if (this.State != other.State) return false;
            if (this.Failures != other.Failures) return false;
            if (this.Success != other.Success) return false;
            if (!this.TimeSinceLastError.Equals(other.TimeSinceLastError)) return false;
            if (!this.MaxFailures.Equals(other.MaxFailures)) return false;
            if (!this.FaultTimeout.Equals(other.FaultTimeout)) return false;
            if (!this.SuccessThreshold.Equals(other.SuccessThreshold)) return false;
            if (!this.RestartTimeout.Equals(other.RestartTimeout)) return false;
            return true;
        }

        public static bool operator ==(CircuitState lhv, CircuitState rhv) => (lhv is not null && rhv is not null) ? lhv.Equals(rhv) : (lhv is null && rhv is null);
        public static bool operator !=(CircuitState lhv, CircuitState rhv) => (lhv is null || rhv is null) ? true : !lhv.Equals(rhv);

    }
}
