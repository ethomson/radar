using Radar.Util;

namespace Radar.Notifications
{
    public class NotificationFactory
    {
        public static Notification NewNotification(Radar radar, NotificationConfiguration configuration)
        {
            Assert.NotNull(radar, "radar");
            Assert.NotNull(configuration, "configuration");

            if (configuration.GetType() == typeof(ConsoleNotificationConfiguration))
            {
                return new ConsoleNotification(radar, (ConsoleNotificationConfiguration)configuration);
            }
            else if (configuration.GetType() == typeof(MetroNotificationConfiguration))
            {
                return new MetroNotification(radar, (MetroNotificationConfiguration)configuration);
            }

            return null;
        }
    }
}
