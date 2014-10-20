namespace Radar.Clients
{
    public class DummyClientConfiguration : ClientConfiguration
    {
        public override string Type
        {
            get
            {
                return "dummy";
            }
        }

        public static new DummyClientConfiguration LoadFrom(dynamic config)
        {
            return new DummyClientConfiguration();
        }
    }
}
