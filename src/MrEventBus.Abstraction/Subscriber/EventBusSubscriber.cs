using MrEventBus.Abstraction.Subscriber.Strategies;
using System.Diagnostics;

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
        return _strategy.SubscribeAsync(MessageReceivedAsync, cancellationToken);
    }

    private async Task MessageReceivedAsync(string MessageContextValue, Type messageType)
    {
        var stopWatch = Stopwatch.StartNew();

        try
        {
            await SubscribeMessageAsync(MessageContextValue, messageType);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task SubscribeMessageAsync(string messageContextValue, Type messageType)
    {

        var messageContext = messageContextValue;
        //var consumer = _serviceProvider.GetService<IEventConsumer<Event>>();
        //{
        //    {
        //        return;
        //    }
        //}

        //await consumer.ConsumeAsync(messageContext);

        await Task.CompletedTask;
    }

}

