using System;

namespace Radar
{
    public class Event
    {
        public Identity Identity { get; internal set; }
        public DateTime Time { get; internal set; }
        public string Content { get; internal set; }
        public EventKind Kind { get; internal set; }
        public string RepositoryFriendlyName { get; internal set; }
        public string BranchName { get; internal set; }
        public string[] Shas { get; internal set; }
    }
}
