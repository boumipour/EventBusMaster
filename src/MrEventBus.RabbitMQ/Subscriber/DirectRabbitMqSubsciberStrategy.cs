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

    public DirectRabbitMqSubsciberStrategy(IRabbitMqConnectionManager connectionManager, IOptions<RabbitMqConfiguration> config)
    {
        _connectionManager = connectionManager;
        _config = config.Value;
    }

    public async Task SubscribeAsync(Func<string, Type, Task> messageRecieved, CancellationToken cancellationToken = default)
    {
        if (!_config.Consumers.Any())
        {
            return;
        }

        // Start consuming
        foreach (var consumer in _config.Consumers)
        {
            var channel = await _connectionManager.GetChannelAsync();
            var listener = CreateListener(channel, messageRecieved);

            await channel.BasicConsumeAsync(queue: $"{consumer.ExchangeName}.{consumer.QueueName}", autoAck: true, consumer: listener);
        }
    }

    private AsyncEventingBasicConsumer CreateListener(IChannel channel, Func<string, Type, Task> messageRecieved, CancellationToken cancellationToken = default)
    {
        var listener = new AsyncEventingBasicConsumer(channel);
        listener.ReceivedAsync += async (model, ea) =>
        {
            byte[] body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            await messageRecieved!.Invoke(message, typeof(DirectRabbitMqSubsciberStrategy)).ContinueWith(action =>
            {
                if (action.IsCompletedSuccessfully)
                {
                    //act
                }
                else
                {
                    throw action.Exception ?? new Exception("action was failed");
                }
            }, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);


        };

        return listener;
    }
}
