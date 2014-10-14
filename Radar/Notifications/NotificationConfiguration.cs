using System;

namespace Radar.Notifications
{
    public interface NotificationConfiguration
    {
        string Type { get; }
        NotificationConfiguration Duplicate();
    }
}
