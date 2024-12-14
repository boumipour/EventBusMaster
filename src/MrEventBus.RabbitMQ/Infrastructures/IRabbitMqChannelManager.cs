using RabbitMQ.Client;

namespace MrEventBus.RabbitMQ.Infrastructures;

public interface IRabbitMqChannelManager : IDisposable
{
    Task<IChannel> GetChannelAsync();
    void ReleaseChannel(IChannel channel);
}
