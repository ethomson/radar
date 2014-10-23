using Radar.Util;

namespace Radar.Clients
{
    public class ClientFactory
    {
        public static Client NewClient(Radar radar, ClientConfiguration configuration)
        {
            Assert.NotNull(radar, "radar");
            Assert.NotNull(configuration, "configuration");

            if (configuration.GetType() == typeof(RepositoryClientConfiguration))
            {
                return new RepositoryClient(radar, (RepositoryClientConfiguration)configuration);
            }
            else if (configuration.GetType() == typeof(DummyClientConfiguration))
            {
                return new DummyClient(radar, (DummyClientConfiguration)configuration);
            }

            return null;
        }
    }
}
