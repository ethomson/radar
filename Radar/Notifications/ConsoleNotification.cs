using System;
using System.IO;

using Radar.Clients;
using Radar.Util;

namespace Radar.Notifications
{
    public class ConsoleNotification : Notification
    {
        private ConsoleNotificationConfiguration configuration;

        public ConsoleNotification(ConsoleNotificationConfiguration configuration)
        {
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

            string content = e.Content ??
                String.Format("{0} {1} {2} [{3}]",
                e.RepositoryFriendlyName, e.Kind, e.BranchName, string.Join(", ", e.Shas));

            fh.WriteLine("{0}: {1} {2} <{3}>: {4}",
                client.Name, e.Time, e.Identity.Name, e.Identity.Email, content);
        }

        public void Stop()
        {
        }
    }
}
