using System;

using Radar.Util;

namespace Radar.Notifications
{
    public class MetroNotificationConfiguration : NotificationConfiguration
    {
        public MetroNotificationConfiguration()
        {
            Audio = true;
        }

        public override string Type
        {
            get
            {
                return "metro";
            }
        }

        public bool Audio { get; private set; }
        public bool DebugInstall { get; private set; }

        public static new MetroNotificationConfiguration LoadFrom(dynamic input)
        {
            MetroNotificationConfiguration config = new MetroNotificationConfiguration();
            ConfigurationParser parser = new ConfigurationParser(input);

            parser.TryExecute("audio", (v) => { config.Audio = v; });
            parser.TryExecute("debugInstall", (v) => { config.DebugInstall = v; });

            return config;
        }
    }
}
