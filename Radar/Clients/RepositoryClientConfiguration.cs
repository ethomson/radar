using Radar.Util;

namespace Radar.Clients
{
    public class RepositoryClientConfiguration : ClientConfiguration
    {
        public override string Type
        {
            get
            {
                return "repository";
            }
        }

        public string Path
        {
            get;
            private set;
        }

        public static new RepositoryClientConfiguration LoadFrom(dynamic config)
        {
            ConfigurationParser parser = new ConfigurationParser(config);

            return new RepositoryClientConfiguration
            {
                Path = parser.Get("path")
            };
        }
    }
}
