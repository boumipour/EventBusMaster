using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using MrEventBus.Abstraction.Models;
using MrEventBus.Abstraction.Producer.Strategies;
using MrEventBus.RabbitMQ.Configurations;
using MrEventBus.RabbitMQ.Infrastructures;
using RabbitMQ.Client;

namespace MrEventBus.RabbitMQ.Producer;

public class DirectRabbitMqProduceStrategy : IProduceStrategy
{
    private readonly IRabbitMqConnectionManager _connectionManager;
    private readonly RabbitMqConfiguration _config;


    public DirectRabbitMqProduceStrategy(IRabbitMqConnectionManager connectionManager, IOptions<RabbitMqConfiguration> config)
    {
        _connectionManager = connectionManager;
        _config = config.Value;
    }

    public async Task PublishAsync<T>(MessageContext<T> messageContext, string queueName = "", CancellationToken cancellationToken = default)
    {
        using var channel = await _connectionManager.GetChannelAsync();


        //string EventData = JsonSerializer.Serialize(messageContext);
        //add message key as param

        string messageKey = messageContext?.Message?.GetType().FullName + ", " + messageContext?.Message?.GetType().Assembly.GetName();



        var message = JsonSerializer.SerializeToUtf8Bytes(messageContext, typeof(T));


        // Set the message properties
        BasicProperties properties = new()
        {
            Persistent = true,
            Headers = new Dictionary<string, object>
            {
                { "messageKey", messageKey }
            }

        };


        await channel.BasicPublishAsync(exchange: _config.ExchangeName, routingKey: queueName, body: message, cancellationToken: cancellationToken);

    }

}
