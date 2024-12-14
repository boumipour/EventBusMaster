using Microsoft.Extensions.Options;
using MrEventBus.Abstraction.Subscriber.Strategies;
using MrEventBus.RabbitMQ.Configurations;
using MrEventBus.RabbitMQ.Infrastructures;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace MrEventBus.RabbitMQ.Subscriber;

public class DirectRabbitMqSubsciberStrategy : ISubscribeStrategy
{
    private readonly IRabbitMqConnectionManager _connectionManager;
    private readonly RabbitMqConfiguration _config;

    private bool _isSubscribed = false;

    public DirectRabbitMqSubsciberStrategy(IRabbitMqConnectionManager connectionManager, IOptions<RabbitMqConfiguration> config)
    {
        _connectionManager = connectionManager;
        _config = config.Value;
    }

    public async Task SubscribeAsync(Func<string, Type, Task> messageRecieved, CancellationToken cancellationToken = default)
    {
        if (_isSubscribed) return;
        _isSubscribed = true;

        if (!_config.Consumers.Any())
            return;

        foreach (var consumer in _config.Consumers)
        {
            var semaphore = new SemaphoreSlim(consumer.ConcurrencyLevel);

            for (int i = 0; i < consumer.ConcurrencyLevel; i++)
            {
                var channel = await _connectionManager.GetChannelAsync();
                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: consumer.PrefetchCount, global: false);

                var listener = CreateListener(channel, messageRecieved, semaphore, cancellationToken);

                await channel.BasicConsumeAsync(queue: $"{consumer.ExchangeName}.{consumer.QueueName}", autoAck: false, consumer: listener);
            }

            // Dispose semaphore when no longer needed
            cancellationToken.Register(() => semaphore.Dispose());
        }
    }

    private AsyncEventingBasicConsumer CreateListener(IChannel channel, Func<string, Type, Task> messageRecieved, SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        var listener = new AsyncEventingBasicConsumer(channel);
        listener.ReceivedAsync += async (model, ea) =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Cancellation requested. Stopping consumer.");
                return;
            }

            await semaphore.WaitAsync(cancellationToken);

            try
            {
                byte[] body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                object? messageTypeNameRaw = string.Empty;
                ea.BasicProperties?.Headers?.TryGetValue("messageKey", out messageTypeNameRaw);


                var messageTypeName = Encoding.UTF8.GetString((messageTypeNameRaw as byte[]) ?? new byte[] { });

                var messageType = Type.GetType(messageTypeName);
                if (messageType == null)
                {
                    Console.WriteLine("can't find message type in consumer side");
                    return;
                }

                await messageRecieved(message, messageType);

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch
            {
                await channel.BasicNackAsync(ea.DeliveryTag, false, true);

            }
            finally
            {
                semaphore.Release();
            }
        };

        return listener;
    }
}
