using System;

namespace Radar.Tracking
{
    public class RepositoryEvent : IEvent
    {
        public Identity Identity { get; internal set; }
        public DateTime Time { get; internal set; }
        public string Content { get; internal set; }
        public string ShortReferenceName { get; internal set; }
        public string[] Shas { get; internal set; }
        public EventKind Kind { get; internal set; }
    }
}
