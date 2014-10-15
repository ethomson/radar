using System;

using Radar.Util;

namespace Radar.Clients
{
    public abstract class ClientConfiguration
    {
        public abstract string Type { get; }

        public static ClientConfiguration LoadFrom(dynamic config)
        {
            ConfigurationParser parser = new ConfigurationParser(config);
            string type = parser.Get("type");

            if (type.Equals("repository"))
            {
                return RepositoryClientConfiguration.LoadFrom(config);
            }

            throw new Exception(String.Format("Configuration error: unknown client type {0}", type));
        }
    }
}
