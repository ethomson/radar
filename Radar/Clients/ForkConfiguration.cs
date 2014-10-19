using Radar.Util;

namespace Radar.Clients
{
    public class ForkConfiguration
    {
        public string Name
        {
            get;
            private set;
        }

        public string Url
        {
            get;
            private set;
        }

        public static ForkConfiguration LoadFrom(dynamic config)
        {
            ConfigurationParser parser = new ConfigurationParser(config);

            var c = new ForkConfiguration
            {
                Name = parser.Get("name"),
                Url = parser.Get("url"),
            };

            return c;
        }
    }
}
