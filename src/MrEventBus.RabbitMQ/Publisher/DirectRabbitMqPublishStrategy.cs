using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MrEventBus.Abstraction.Models;
using MrEventBus.Abstraction.Publisher.Strategies;
using MrEventBus.RabbitMQ.Configurations;
using MrEventBus.RabbitMQ.Publisher;
using RabbitMQ.Client;

namespace EventBus.Publisher.Strategies.RabbitMQ;

public class DirectRabbitMqPublishStrategy : IPublishStrategy
{
    private readonly IRabbitMqPublisherConnectionManager _connectionManager;
    private readonly RabbitMqConfiguration _config;


    public DirectRabbitMqPublishStrategy(IRabbitMqPublisherConnectionManager connectionManager, IOptions<RabbitMqConfiguration> config)
    {
        _connectionManager = connectionManager;
        _config = config.Value;
    }

    public async Task PublishAsync<T>(MessageContext<T> messageContext, string queueName = "")
    {
        using var channel = await _connectionManager.GetChannelAsync();


        string EventData = JsonSerializer.Serialize(messageContext);

        //add message key as param
        string messageKey = messageContext?.Message?.GetType().FullName + ", " + messageContext?.Message?.GetType().Assembly.GetName();
        
        var body=Encoding.UTF8.GetBytes(EventData);
        await channel.BasicPublishAsync(_config.ExchangeName, queueName, body);

    }

}
