using System;

namespace Radar
{
    public interface IEvent
    {
        Identity Identity { get; }
        DateTime Time { get; }
        string Content { get; }
    }
}
