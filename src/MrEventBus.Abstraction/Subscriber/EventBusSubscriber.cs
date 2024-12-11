using MrEventBus.Abstraction.Models;
using MrEventBus.Abstraction.Subscriber.Strategies;
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
            var messageContextGenericType = typeof(MessageContext<>).MakeGenericType(messageType);
            var deserializedMessageContext = JsonSerializer.Deserialize(messageContextValue, messageContextGenericType);


            var consumerType = typeof(IMessageConsumer<>).MakeGenericType(messageType);
            var consumer = _serviceProvider.GetService(consumerType);
            var consumeMethod = consumerType.GetMethod("ConsumeAsync");


            await (Task)consumeMethod?.Invoke(consumer, new[] { deserializedMessageContext });

        }
        catch (Exception)
        {
            throw;
        }
    }

}

