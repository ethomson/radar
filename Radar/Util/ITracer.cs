namespace Radar.Util
{
    public interface ITracer
    {
        void WriteInformation(string format, params object[] args);
        void WriteError(string format, params object[] args);
    }
}
