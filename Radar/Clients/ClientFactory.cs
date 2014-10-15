using System;

using Radar.Util;

namespace Radar.Clients
{
    public class ClientFactory
    {
        public static Client NewClient(ClientConfiguration configuration)
        {
            Assert.NotNull(configuration, "configuration");

            if (configuration.GetType() == typeof(RepositoryClientConfiguration))
            {
                return new RepositoryClient((RepositoryClientConfiguration)configuration);
            }

            return null;
        }
    }
}
