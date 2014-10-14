using System;

using Radar.Clients;

namespace Radar.Notifications
{
    public interface Notification
    {
        NotificationConfiguration Configuration { get; }
        void Notify(Client client, Event e);
        void Stop();
    }
}
