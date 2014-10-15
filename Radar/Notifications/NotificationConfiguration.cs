using System;

using Radar.Util;

namespace Radar.Notifications
{
    public abstract class NotificationConfiguration
    {
        public abstract string Type { get; }

        public static NotificationConfiguration LoadFrom(dynamic config)
        {
            ConfigurationParser parser = new ConfigurationParser(config);
            string type = parser.Get("type");

            if (type.Equals("console"))
            {
                return ConsoleNotificationConfiguration.LoadFrom(config);
            }

            throw new Exception(String.Format("Configuration error: unknown client type {0}", type));
        }
    }
}
