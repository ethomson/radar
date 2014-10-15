using System;

namespace Radar
{
    public class Event
    {
        public Identity Identity { get; internal set; }
        public DateTime Time { get; internal set; }
        public string Content { get; internal set; }
    }
}
