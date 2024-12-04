using RabbitMQ.Client;

namespace MrEventBus.RabbitMQ.Publisher;

public interface IRabbitMqPublisherConnectionManager : IDisposable
{
    Task<IChannel> GetChannelAsync();
}
