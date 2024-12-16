using RabbitMQ.Client;

namespace MrEventBus.RabbitMQ.Infrastructures;

public interface IRabbitMqChannelManager
{ 
    ValueTask<IChannel> GetChannelAsync(string queueName);
}
