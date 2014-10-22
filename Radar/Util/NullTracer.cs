namespace Radar.Util
{
    public class NullTracer : ITracer
    {
        public void WriteInformation(string format, params object[] args)
        {
        }

        public void WriteError(string format, params object[] args)
        {
        }
    }
}
