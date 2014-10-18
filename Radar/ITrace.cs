namespace Radar
{
    public interface ITrace
    {
        void WriteInformation(string format, params object[] args);
    }
}
