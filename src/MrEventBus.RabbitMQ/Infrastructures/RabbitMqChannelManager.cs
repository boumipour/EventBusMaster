using Microsoft.Extensions.Options;
using MrEventBus.RabbitMQ.Configurations;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Timers;
using Timer = System.Timers.Timer;

namespace MrEventBus.RabbitMQ.Infrastructures;

public class RabbitMqChannelManager : IRabbitMqChannelManager, IDisposable
{
    private readonly RabbitMqConfiguration _config;
    private readonly IConnection _connection;
    private readonly ConcurrentBag<IChannel> _channelPool;
    private readonly SemaphoreSlim _channelSemaphore;
    private int _activeChannelCount; // Tracks total active channels
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    public RabbitMqChannelManager(IConnection connection, IOptions<RabbitMqConfiguration> options)
    {
        _config = options.Value;
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _channelPool = new ConcurrentBag<IChannel>();
        _channelSemaphore = new SemaphoreSlim(_config.MaxChannelPoolSize, _config.MaxChannelPoolSize);

        _cleanupTimer = new Timer(_config.ChannelPoolCleanupInterval); // Periodic cleanup
        _cleanupTimer.Elapsed += PerformPoolCleanup;
        _cleanupTimer.Start();
    }

    public async Task<IChannel> GetChannelAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMqChannelManager));

        if (await _channelSemaphore.WaitAsync(0))
        {
            if (_channelPool.TryTake(out var channel))
            {
                if (channel.IsOpen)
                    return channel;

                Interlocked.Decrement(ref _activeChannelCount);
                channel.Dispose();
            }
            Interlocked.Increment(ref _activeChannelCount);
            return await CreateChannelAsync();
        }

        // Increase pool size if possible
        if (_activeChannelCount < _config.MaxDynamicChannelPoolSize)
        {
            Interlocked.Increment(ref _activeChannelCount);
            return await CreateChannelAsync();
        }
        else
        {
            Console.WriteLine("Channel pool is exhausted. Consider increasing pool size.");
        }

        // Block if maximum dynamic size is also reached
        await _channelSemaphore.WaitAsync();
        return await GetChannelAsync(); // Retry after acquiring semaphore
    }

    private async Task<IChannel> CreateChannelAsync()
    {
        if (!_connection.IsOpen)
            throw new InvalidOperationException("RabbitMQ connection is closed.");

        try
        {
            var channel = await _connection.CreateChannelAsync();
            return channel;
        }
        catch (Exception ex)
        {
            Interlocked.Decrement(ref _activeChannelCount); // Adjust active count on failure
            Console.WriteLine($"Error creating RabbitMQ channel: {ex.Message}");
            throw;
        }
    }

    public void ReleaseChannel(IChannel channel)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMqChannelManager));

        if (channel != null && channel.IsOpen)
        {
            _channelPool.Add(channel); // Return the channel to the pool
        }
        else
        {
            // Dispose invalid channels
            channel?.Dispose();
            Interlocked.Decrement(ref _activeChannelCount);
        }

        _channelSemaphore.Release();
    }

    private void PerformPoolCleanup(object? sender, ElapsedEventArgs e)
    {
        if (_disposed)
            return;

        // Clean up idle channels if pool size exceeds MinPoolSize
        while (_channelPool.Count > _config.MinChannelPoolSize)
        {
            if (_channelPool.TryTake(out var channel))
            {
                channel.Dispose();
                Interlocked.Decrement(ref _activeChannelCount);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _cleanupTimer.Stop();
        _cleanupTimer.Dispose();

        // Dispose all channels in the pool
        while (_channelPool.TryTake(out var channel))
        {
            channel.Dispose();
        }

        _channelSemaphore.Dispose();
        _disposed = true;
    }
}
