using EventBus.Publisher;
using EventBus.Publisher.Strategies.RabbitMq;
using EventBus.Publisher.Strategies.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using MrEventBus.Abstraction.Publisher;
using MrEventBus.Abstraction.Publisher.Strategies;
using MrEventBus.RabbitMQ.Configurations;
using MrEventBus.RabbitMQ.Publisher;
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


        if (config.Producers.Any())
        {
            services.AddSingleton<IConnection>(provider =>
            {
                var connectionFactory = new ConnectionFactory()
                {
                    UserName = config.UserName,
                    Port = config.Port,
                    Password = config.Password,
                    VirtualHost = config.VirtualHost,
                    ClientProvidedName = config.ExchangeName
                };

                var defaultExchangeName = config.ExchangeName.Trim().ToLower();
                var defaultQname = $"{defaultExchangeName}";

                //todo: handle mulitple host
                var connection= connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
                var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();
                channel.ExchangeDeclareAsync(exchange: defaultExchangeName, type: ExchangeType.Topic).GetAwaiter().GetResult();
                
                //todo:set configs
                channel.QueueDeclareAsync(defaultQname, false,false,false).GetAwaiter().GetResult();
                channel.QueueBindAsync(defaultQname, defaultExchangeName, "").GetAwaiter().GetResult();

                return connection;
            });

            services.AddScoped<IEventBusPublisher, EventBusPublisher>();
            services.AddScoped<IPublishStrategy, DirectRabbitMqPublishStrategy>();
            services.AddScoped<IRabbitMqPublisherConnectionManager, RabbitMqPublisherConnectionManager>();
        }

        return services;
    }
}
