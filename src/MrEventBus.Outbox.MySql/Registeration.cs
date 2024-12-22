using Dapper;
using Microsoft.Extensions.DependencyInjection;
using MrEventBus.Abstraction.Producer.Outbox.Config;
using MrEventBus.Abstraction.Producer.Outbox.Repository;
using MrEventBus.Abstraction.Producer.Outbox.Worker;
using MrEventBus.Abstraction.Subscriber.Inbox.Config;
using MrEventBus.Abstraction.Subscriber.Inbox.Repository;
using MrEventBus.Abstraction.Subscriber.Inbox.Worker;
using MrEventBus.Boxing.MySql.InBox;
using MrEventBus.Boxing.MySql.Infrastructure;
using MrEventBus.Boxing.MySql.OutBox;
using MrEventBus.Boxing.MySql.Utilities;

namespace MrEventBus.Boxing.MySql;

public static class Registeration
{
    public static IServiceCollection AddMySqlOutBoxing(this IServiceCollection services, Action<MySqlOutboxConfiguration>? configurator = null)
    {
        MySqlOutboxConfiguration conf = new();
        configurator?.Invoke(conf);

        if (configurator != null)
            services.Configure(configurator);

        services.Configure<OutboxConfig>(options =>
        {
            options.Concurrency = conf.Concurrency;
            options.ReaderInterval = conf.ReaderInterval;
            options.EnabledProcessor = conf.EnabledProcessor;
            options.PersistenceDuration = conf.PersistenceDuration;
            options.EnabledEvents = conf.EnabledEvents;
        });


        services.AddSingleton<IMySqlConnectionFactory>(new MySqlConnectionFactory(conf.MySqlConnectionString));
        services.AddScoped<IOutboxRepository, OutBoxMySqlRepository>();
        services.AddScoped<OutBoxDbInitializer>();

        SqlMapper.AddTypeHandler(new GuidHandler());

        if (conf.EnabledProcessor)
        {
            services.AddHostedService<OutboxProcessorWorker>();

            if (conf.PersistenceDuration.TotalSeconds > 0)
                services.AddHostedService<OutboxClearWorker>();
        }

        return services;
    }


    public static IServiceCollection AddMySqlInBoxing(this IServiceCollection services, Action<MySqlInboxConfiguration>? configurator = null)
    {
        MySqlInboxConfiguration conf = new();
        configurator?.Invoke(conf);

        if (configurator != null)
            services.Configure(configurator);


        services.Configure<InboxConfig>(options =>
        {
            options.EnabledProcessor = conf.EnabledProcessor;
            options.Concurrency = conf.Concurrency;
            options.ReaderInterval = conf.ReaderInterval;
            options.PersistenceDuration = conf.PersistenceDuration;
            options.EnabledEvents = conf.EnabledEvents;
        });


        services.AddSingleton<IMySqlConnectionFactory>(new MySqlConnectionFactory(conf.MySqlConnectionString));
        services.AddScoped<IInboxRepository, InBoxMySqlRepository>();
        services.AddScoped<InBoxDbInitializer>();

        SqlMapper.AddTypeHandler(new GuidHandler());

        if (conf.EnabledProcessor)
        {
            services.AddHostedService<InboxProcessorWorker>();

            if (conf.PersistenceDuration.TotalSeconds > 0)
                services.AddHostedService<InboxClearWorker>();
        }

        return services;
    }
}
