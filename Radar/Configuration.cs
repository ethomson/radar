using System;
using System.Collections.Generic;
using System.IO;

using Radar.Clients;
using Radar.Images;
using Radar.Notifications;
using Radar.Util;

namespace Radar
{
    public class Configuration : ImageManagerConfiguration
    {
        private Configuration()
        {
            PollInterval = DefaultPollInterval;
            ImageCacheDir = DefaultImageCacheDir;

            Clients = new List<ClientConfiguration>();
            Notifications = new List<NotificationConfiguration>();
        }

        public string ImageCacheDir
        {
            get;
            private set;
        }

        public int PollInterval
        {
            get;
            private set;
        }

        public static string UserAgent
        {
            get
            {
                return string.Format("{0} ({1})", Constants.ApplicationName, Constants.InformationUrl);
            }
        }

        public IEnumerable<ClientConfiguration> Clients
        {
            get;
            private set;
        }

        public IEnumerable<NotificationConfiguration> Notifications
        {
            get;
            private set;
        }

        public static int DefaultPollInterval
        {
            get
            {
                return 60;
            }
        }

        public static string DefaultConfigurationPath
        {
            get
            {
                string exe = System.Reflection.Assembly.GetEntryAssembly().Location;
                string configpath;

                if (exe.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
                {
                    configpath = exe.Substring(0, exe.Length - 4) + ".config";
                }
                else
                {
                    configpath = exe + ".config";
                }

                return configpath;
            }
        }

        private static string DefaultImageCacheDir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Path.Combine(Constants.ApplicationName, "Images"));
            }
        }

        public static Configuration LoadFrom(string path)
        {
            Configuration configuration = new Configuration();
            ConfigurationParser parser = new ConfigurationParser(System.IO.File.ReadAllText(path));

            parser.TryExecute("poll_interval", (v) => { configuration.PollInterval = (int)v; });
            parser.TryExecute("imageCacheDir", (v) => { configuration.ImageCacheDir = v; });
            parser.Execute("clients", (v) => { configuration.Clients = LoadClients(v); });
            parser.Execute("notifications", (v) => { configuration.Notifications = LoadNotifications(v); });

            return configuration;
        }

        private static IEnumerable<ClientConfiguration> LoadClients(dynamic configurations)
        {
            List<ClientConfiguration> result = new List<ClientConfiguration>();

            foreach (dynamic c in configurations)
            {
                result.Add(ClientConfiguration.LoadFrom(c));
            }

            return result;
        }

        private static IEnumerable<NotificationConfiguration> LoadNotifications(dynamic notifications)
        {
            List<NotificationConfiguration> result = new List<NotificationConfiguration>();

            foreach (dynamic n in notifications)
            {
                result.Add(NotificationConfiguration.LoadFrom(n));
            }

            return result;
        }
    }
}
