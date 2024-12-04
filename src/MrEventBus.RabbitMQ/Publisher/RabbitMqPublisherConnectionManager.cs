using Microsoft.Extensions.Options;
using MrEventBus.RabbitMQ.Configurations;
using MrEventBus.RabbitMQ.Publisher;
using RabbitMQ.Client;

namespace EventBus.Publisher.Strategies.RabbitMq;

public class RabbitMqPublisherConnectionManager : IRabbitMqPublisherConnectionManager
{
    private readonly RabbitMqConfiguration _config = new();
    private readonly IConnection _connection;
    private readonly ThreadLocal<IChannel?> _threadLocalChannel;

    public RabbitMqPublisherConnectionManager(IConnection connection,IOptions<RabbitMqConfiguration> options)
    {
        _config = options.Value;
        _connection=connection;
        _threadLocalChannel = new ThreadLocal<IChannel?>(() => null);
    }


    public async Task<IChannel> GetChannelAsync()
    {
        if (_threadLocalChannel.Value == null || !_threadLocalChannel.Value.IsOpen)
            _threadLocalChannel.Value = await _connection.CreateChannelAsync();
        
        return _threadLocalChannel.Value!;
    }

    public async Task ReleaseChannelAsync()
    {
        var channel = _threadLocalChannel.Value;
        if (channel != null && channel.IsOpen)
        {
            await channel.CloseAsync();
        }

        channel?.Dispose();
        _threadLocalChannel.Value = null;
    }

    public void Dispose()
    {
        // Dispose the thread-local channel for the current thread
        _threadLocalChannel.Dispose();
    }
}
