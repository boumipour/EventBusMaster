﻿using Dapper;
using Microsoft.Extensions.DependencyInjection;
using MrEventBus.Abstraction.Producer.Outbox.Config;
using MrEventBus.Abstraction.Producer.Outbox.Repository;
using MrEventBus.Abstraction.Producer.Outbox.Worker;
using MrEventBus.Abstraction.Subscriber.Inbox.Config;
using MrEventBus.Abstraction.Subscriber.Inbox.Repository;
using MrEventBus.Abstraction.Subscriber.Inbox.Worker;
using MrEventBus.Storage.MySql.DatabaseMigrator;
using MrEventBus.Storage.MySql.InBox;
using MrEventBus.Storage.MySql.Infrastructure;
using MrEventBus.Storage.MySql.OutBox;
using MrEventBus.Storage.MySql.Utilities;

namespace MrEventBus.Storage.MySql;

public static class Registeration
{
    public static IServiceCollection AddMySqlOutBoxing(this IServiceCollection services, Action<MySqlOutboxConfig>? configurator = null)
    {
        MySqlOutboxConfig conf = new();
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
        services.AddScoped<IOutboxRepository, MySqlOutBoxRepository>();

        if (conf.DBInitializer)
            services.AddScoped<MySqlDbMigrator>();

        SqlMapper.AddTypeHandler(new GuidHandler());

        if (conf.EnabledProcessor)
        {
            services.AddHostedService<OutboxProcessorWorker>();

            if (conf.PersistenceDuration.TotalSeconds > 0)
                services.AddHostedService<OutboxClearWorker>();
        }

        return services;
    }


    public static IServiceCollection AddMySqlInBoxing(this IServiceCollection services, Action<MySqlInboxConfig>? configurator = null)
    {
        MySqlInboxConfig conf = new();
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
        services.AddScoped<IInboxRepository, MySqlInBoxRepository>();

        if (conf.DBInitializer)
            services.AddScoped<MySqlDbMigrator>();

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