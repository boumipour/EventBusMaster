using EventBus.Publisher;
using EventBus.Publisher.Strategies.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using MrEventBus.Abstraction.Publisher;
using MrEventBus.Abstraction.Publisher.Strategies;
using MrEventBus.RabbitMQ.Configurations;
using MrEventBus.RabbitMQ.Infrastructures;
using RabbitMQ.Client;

namespace EventBus;

public static class Registration
{
    public static IServiceCollection AddMrEventBus(this IServiceCollection services, Action<RabbitMqConfiguration>? configurator = null)
    {
        RabbitMqConfiguration config = new();
        configurator?.Invoke(config);

        if (configurator != null)
            services.Configure(configurator);


        var connectionFactory = new ConnectionFactory()
        {
            UserName = config.UserName,
            Port = config.Port,
            Password = config.Password,
            VirtualHost = config.VirtualHost,
            ClientProvidedName = config.ExchangeName
        };
        var connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
        var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

        services.AddSingleton<IConnection>(provider => { return connection; });

        services.AddScoped<IEventBusPublisher, EventBusPublisher>();
        services.AddScoped<IPublishStrategy, DirectRabbitMqPublishStrategy>();
        services.AddScoped<IRabbitMqConnectionManager, RabbitMqConnectionManager>();



        if (config.Producers.Any())
        {
            var defaultExchangeName = config.ExchangeName.Trim().ToLower();
            var defaultQname = $"{defaultExchangeName}.main";

            channel.ExchangeDeclareAsync(exchange: defaultExchangeName, type: ExchangeType.Direct).GetAwaiter().GetResult();
            channel.QueueDeclareAsync(defaultQname, false, false, false).GetAwaiter().GetResult();
            channel.QueueBindAsync(defaultQname, defaultExchangeName, "main").GetAwaiter().GetResult();
        }

        if (config.Consumers.Any())
        {
            foreach (var consumer in config.Consumers) 
            {
                channel.QueueDeclareAsync($"{consumer.ExchangeName}.{consumer.QueueName}", false,false,false).GetAwaiter().GetResult();
                channel.QueueBindAsync($"{consumer.ExchangeName}.{consumer.QueueName}", consumer.ExchangeName, consumer.QueueName).GetAwaiter().GetResult();
            }
        }

        return services;
    }
}
