using System;

using Radar.Util;

namespace Radar.Notifications
{
    public class NotificationFactory
    {
        public static Notification NewNotification(NotificationConfiguration configuration)
        {
            Assert.NotNull(configuration, "configuration");

            if (configuration.GetType() == typeof(ConsoleNotificationConfiguration))
            {
                return new ConsoleNotification((ConsoleNotificationConfiguration)configuration);
            }

            return null;
        }
    }
}
