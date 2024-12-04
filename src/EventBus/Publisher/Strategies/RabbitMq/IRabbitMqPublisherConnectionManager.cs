using RabbitMQ.Client;

namespace EventBus.Publisher.Strategies.RabbitMq;

public interface IRabbitMqPublisherConnectionManager:IDisposable
{
     Task<IChannel> GetChannelAsync();
}
