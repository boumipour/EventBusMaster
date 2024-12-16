using Microsoft.Extensions.Options;
using MrEventBus.RabbitMQ.Configurations;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace MrEventBus.RabbitMQ.Infrastructures;

public class RabbitMqChannelManager : IRabbitMqChannelManager
{
    private readonly RabbitMqConfiguration _config;
    private readonly IConnection _connection;

    private readonly ConcurrentDictionary<string, List<IChannel>> channelPools;
    private readonly ConcurrentDictionary<string, int> queueIndex;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> initializationLocks;

    private bool _disposed;

    public RabbitMqChannelManager(IConnection connection, IOptions<RabbitMqConfiguration> options)
    {
        _config = options.Value;
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

        channelPools = new ConcurrentDictionary<string, List<IChannel>>();
        queueIndex = new ConcurrentDictionary<string, int>();
        initializationLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

    }

    public async ValueTask<IChannel> GetChannelAsync(string queueName)
    {
        if (!channelPools.ContainsKey(queueName))
            await InitializePoolAsync(queueName, _config.PoolSizePerQueue);
        

        if (!channelPools.TryGetValue(queueName, out var pool))
            throw new Exception($"Queue {queueName} has no initialized channel pool.");
        
        int poolSize = pool.Count;

        // round robin
        // Atomically update the index and wrap it around the pool size
        int nextIndex = queueIndex.AddOrUpdate(
            queueName,                     // Key: queue name
            0,                             // Initial value
            (key, currentIndex) =>         // Update function
                (currentIndex + 1) % poolSize // Increment and wrap
        );

        var channel = pool[nextIndex];

        // Replace channel if unhealthy
        if (!IsChannelHealthy(channel))
        {
            var newChannel = await _connection.CreateChannelAsync();
            pool[nextIndex] = newChannel;
            await channel.CloseAsync();
            channel = newChannel;
        }

        return channel;
    }

    private async Task InitializePoolAsync(string queueName, int poolSize)
    {
        var lockForQueue = initializationLocks.GetOrAdd(queueName, _ => new SemaphoreSlim(1, 1));
        await lockForQueue.WaitAsync();

        try
        {
            if (!channelPools.ContainsKey(queueName))
            {
                var pool = new List<IChannel>();

                for (int i = 0; i < poolSize; i++)
                {
                    var channel = await _connection.CreateChannelAsync();
                    pool.Add(channel);
                }

                channelPools.TryAdd(queueName, pool);
            }
        }
        finally
        {
            lockForQueue.Release();
        }
    }

    private bool IsChannelHealthy(IChannel channel)
    {
        return channel != null && channel.IsOpen;
    }
}
