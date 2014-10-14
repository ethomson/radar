using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;

using Newtonsoft.Json;

using Radar.Clients;
using Radar.Notifications;

namespace Radar
{
    public class Configuration
    {
        private Configuration()
        {
            PollInterval = DefaultPollInterval;

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

            string configString = System.IO.File.ReadAllText(path);
            dynamic configJson = JsonConvert.DeserializeObject(configString);

            foreach (var entry in configJson)
            {
                if (entry.Name.Equals("poll_interval"))
                {
                    configuration.PollInterval = (int)entry.Value;
                }
                else if (entry.Name.Equals("clients"))
                {
                    configuration.Clients = LoadClients(configuration, entry.Value);
                }
                else if (entry.Name.Equals("notifications"))
                {
                    configuration.Notifications = LoadNotifications(configuration, entry.Value);
                }
            }

            return configuration;
        }

        private static IEnumerable<ClientConfiguration> LoadClients(Configuration parent, dynamic input)
        {
            List<ClientConfiguration> result = new List<ClientConfiguration>();

            foreach (dynamic json in input)
            {
                string type = json["type"];

                throw new Exception(String.Format("Unknown client type {0}", type));
            }

            return result;
        }

        private static IEnumerable<NotificationConfiguration> LoadNotifications(Configuration parent, dynamic input)
        {
            List<NotificationConfiguration> result = new List<NotificationConfiguration>();

            foreach (dynamic json in input)
            {
                string type = json["type"];

                throw new Exception(String.Format("Unknown notification type {0}", type));
            }

            return result;
        }
    }
}
