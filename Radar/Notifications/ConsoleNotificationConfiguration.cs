using System;

using Radar.Util;

namespace Radar.Notifications
{
    public class ConsoleNotificationConfiguration : NotificationConfiguration
    {
        public enum ConsoleStream
        {
            Output,
            Error
        }

        public ConsoleNotificationConfiguration()
        {
            Stream = ConsoleStream.Output;
        }

        public override string Type
        {
            get
            {
                return "console";
            }
        }

        public ConsoleStream Stream { get; private set; }

        public static new ConsoleNotificationConfiguration LoadFrom(dynamic input)
        {
            ConsoleNotificationConfiguration config = new ConsoleNotificationConfiguration();
            ConfigurationParser parser = new ConfigurationParser(input);

            parser.TryExecute("stream", (c) => {
                if ("stdout".Equals(c))
                {
                    config.Stream = ConsoleStream.Output;
                }
                else if ("stderr".Equals(c))
                {
                    config.Stream = ConsoleStream.Error;
                }
                else
                {
                    throw new ConfigurationException(String.Format("unknown stream type '{0}'", c));
                }
            });

            return config;
        }
    }
}
