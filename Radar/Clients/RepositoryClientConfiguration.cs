using System.Collections.Generic;
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

        public ICollection<ForkConfiguration> Forks
        {
            get;
            private set;
        }

        public string[] Snoozed
        {
            get;
            private set;
        }

        public static new RepositoryClientConfiguration LoadFrom(dynamic config)
        {
            ConfigurationParser parser = new ConfigurationParser(config);

            var c = new RepositoryClientConfiguration
            {
                Path = parser.Get("path"),
            };

            parser.Execute("snoozed", (v) => { c.Snoozed = v.ToObject<string[]>(); } );
            parser.Execute("forks", (v) => { c.Forks = LoadForks(v); });

            return c;
        }

        private static IEnumerable<ForkConfiguration> LoadForks(dynamic configurations)
        {
            List<ForkConfiguration> result = new List<ForkConfiguration>();

            foreach (dynamic c in configurations)
            {
                result.Add(ForkConfiguration.LoadFrom(c));
            }

            return result;
        }
    }
}
