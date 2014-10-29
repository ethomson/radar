using System;
using System.IO;

using Radar.Clients;
using Radar.Util;

namespace Radar.Notifications
{
    public class ConsoleNotification : Notification
    {
        private ConsoleNotificationConfiguration configuration;

        public ConsoleNotification(Radar radar, ConsoleNotificationConfiguration configuration)
        {
            Assert.NotNull(radar, "radar");
            Assert.NotNull(configuration, "configuration");

            this.configuration = configuration;
        }

        public NotificationConfiguration Configuration
        {
            get
            {
                return configuration;
            }
        }

        public void Notify(Client client, Event e)
        {
            TextWriter fh = configuration.Stream == ConsoleNotificationConfiguration.ConsoleStream.Output ?
                Console.Out : Console.Error;

            if (e.Identity is NullIdentity)
            {
                fh.WriteLine("{0} - {1} : {2}",
                client.Name, e.Time, e.Content);
            }
            else
            {
                fh.WriteLine("{0} - {1} : ({2} <{3}>) {4}",
                client.Name, e.Time, e.Identity.Name, e.Identity.Email, e.Content);
            }
        }

        public void Stop()
        {
        }
    }
}
