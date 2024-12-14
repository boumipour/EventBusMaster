using Microsoft.Extensions.Options;
using MrEventBus.RabbitMQ.Configurations;
using RabbitMQ.Client;
using System.Collections.Concurrent;


namespace MrEventBus.RabbitMQ.Infrastructures;

public class RabbitMqChannelManager : IRabbitMqChannelManager, IDisposable
{
    private readonly RabbitMqConfiguration _config;
    private readonly IConnection _connection;
    private readonly ConcurrentBag<IChannel> _channelPool;

    private bool _disposed;

    public RabbitMqChannelManager(IConnection connection, IOptions<RabbitMqConfiguration> options)
    {
        _config = options.Value;
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _channelPool = new ConcurrentBag<IChannel>();
    }


    public async Task<IChannel> GetChannelAsync()
    {
        if (_channelPool.TryTake(out var channel))
        {
            return channel;
        }

        return await _connection.CreateChannelAsync();
    }

    public void ReleaseChannel(IChannel channel)
    {
        _channelPool.Add(channel);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        // Dispose all channels in the pool
        while (_channelPool.TryTake(out var channel))
        {
            channel.Dispose();
        }

        _disposed = true;
    }
}
