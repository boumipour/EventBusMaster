using RabbitMQ.Client;

namespace MrEventBus.RabbitMQ.Infrastructures;

public interface IRabbitMqConnectionManager : IDisposable
{
    Task<IChannel> GetChannelAsync();
}
