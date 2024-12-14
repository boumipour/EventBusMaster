using Microsoft.Extensions.DependencyInjection;
using MrEventBus.Abstraction.Producer;
using MrEventBus.Abstraction.Producer.Strategies;
using MrEventBus.Abstraction.Subscriber;
using MrEventBus.Abstraction.Subscriber.Strategies;
using MrEventBus.Abstraction.Subscriber.Workers;
using MrEventBus.RabbitMQ.Configurations;
using MrEventBus.RabbitMQ.Infrastructures;
using MrEventBus.RabbitMQ.Producer;
using MrEventBus.RabbitMQ.Subscriber;
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
        services.AddScoped<IRabbitMqChannelManager, RabbitMqChannelManager>();


        if (config.Producers.Any())
        {
            var defaultExchangeName = config.ExchangeName.Trim().ToLower();
            var defaultQname = $"{defaultExchangeName}.main";

            channel.ExchangeDeclareAsync(exchange: defaultExchangeName, type: ExchangeType.Direct).GetAwaiter().GetResult();
            channel.QueueDeclareAsync(defaultQname, false, false, false).GetAwaiter().GetResult();
            channel.QueueBindAsync(defaultQname, defaultExchangeName, "main").GetAwaiter().GetResult();

            services.AddScoped<IEventBusProducer, EventBusProducer>();
            services.AddScoped<IProduceStrategy, DirectRabbitMqProduceStrategy>();
        }

        if (config.Consumers.Any())
        {
            foreach (var consumer in config.Consumers) 
            {
                foreach (var consumerType in consumer.ConsumerTypes)
                {
                    ConsumerMessageRegistry.RegisterMessageType(consumerType);
                }

                channel.QueueDeclareAsync($"{consumer.ExchangeName}.{consumer.QueueName}", false,false,false).GetAwaiter().GetResult();
                channel.QueueBindAsync($"{consumer.ExchangeName}.{consumer.QueueName}", consumer.ExchangeName, consumer.QueueName).GetAwaiter().GetResult();
            }

            services.AddScoped<IEventBusSubscriber, EventBusSubscriber>();
            services.AddScoped<ISubscribeStrategy, DirectRabbitMqSubsciberStrategy>();
            
            services.AddHostedService<SubscribeWorker>();
        }

        return services;
    }
}
