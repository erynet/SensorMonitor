using SensorMonitor.Core;

namespace SensorMonitor.Component.Common.Interface
{
    public interface ITxMessage : IBusMessage
    {
        string ConnectString { get; set; }
    }
}