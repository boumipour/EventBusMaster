﻿using Microsoft.Extensions.DependencyInjection;
using MrEventBus.Abstraction.Subscriber.Strategies;
using System;
using System.Text.Json;

namespace MrEventBus.Abstraction.Subscriber;

public class EventBusSubscriber : IEventBusSubscriber
{
    private readonly ISubscribeStrategy _strategy;
    private readonly IServiceProvider _serviceProvider;

    public EventBusSubscriber(ISubscribeStrategy strategy, IServiceProvider serviceProvider)
    {
        _strategy = strategy;
        _serviceProvider = serviceProvider;
    }

    public Task SubscribeAsync(CancellationToken cancellationToken = default)
    {
        return _strategy.SubscribeAsync(ConsumeMessageAsync, cancellationToken);
    }

    private async Task ConsumeMessageAsync(string messageContextValue, Type messageType)
    {
        try
        {
            var types = ConsumerMessageRegistry.GetMessageRelatedType(messageType);

            var deserializedMessageContext = JsonSerializer.Deserialize(messageContextValue, types.MessageContextType);

            using var scope = _serviceProvider.CreateScope();
            var consumer = scope.ServiceProvider.GetService(types.ConsumerType);


            //todo: fix
            //var consumeMethod = types.ConsumerType.GetMethod("ConsumeAsync");
            var consumeMethod = types.ConsumerType.GetMethod("ConsumeAsync", new[] { types.MessageContextType });

            Task? task = consumeMethod?.Invoke(consumer, new[] { deserializedMessageContext }) as Task;

            if (task != null)
                await task;

        }
        catch (Exception)
        {
            throw;
        }
    }

}

