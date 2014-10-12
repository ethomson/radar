namespace Radar
{
    public class ForkEvent
    {
        private readonly MonitoredRepository _monitoredRepository;
        private readonly ForkEventKind _kind;

        public ForkEvent(MonitoredRepository monitoredRepository, ForkEventKind kind)
        {
            _monitoredRepository = monitoredRepository;
            _kind = kind;
        }

        public MonitoredRepository MonitoredRepository
        {
            get { return _monitoredRepository; }
        }

        public ForkEventKind Kind
        {
            get { return _kind; }
        }
    }
}
