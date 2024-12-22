using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MrEventBus.Abstraction.Models;
using MrEventBus.Abstraction.Subscriber.Inbox.Config;
using MrEventBus.Abstraction.Subscriber.Inbox.Repository;
using MrEventBus.Abstraction.Subscriber.Strategies;
using System.Linq.Expressions;
using System.Text.Json;

namespace MrEventBus.Abstraction.Subscriber;

public class EventBusSubscriber : IEventBusSubscriber
{
    private readonly ISubscribeStrategy _strategy;
    private readonly IServiceProvider _serviceProvider;
    private readonly IInboxRepository? _InboxRepository = null;
    private readonly InboxConfig? _inboxConfig = null;

    public EventBusSubscriber(ISubscribeStrategy strategy, IServiceProvider serviceProvider,IOptions<InboxConfig> config, IInboxRepository? inboxRepository = null)
    {
        _strategy = strategy;
        _serviceProvider = serviceProvider;
        _inboxConfig = config.Value;
        _InboxRepository = inboxRepository;
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


            if (_InboxRepository != null && CheckMessageFullName(messageType.FullName!))
            {
                var accessors = GetPropertyAccessors(types.MessageContextType);

                var message = accessors.GetMessage(deserializedMessageContext!);
                var shard = accessors.GetShard(deserializedMessageContext!);
                var messageId = accessors.GetMessageId(deserializedMessageContext!);
                var publishDateTime = accessors.GetPublishDateTime(deserializedMessageContext!);

                await _InboxRepository!.CreateAsync(new InboxMessage(messageId, message!, shard!,publishDateTime));
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var consumer = scope.ServiceProvider.GetService(types.ConsumerType);

            //todo: fix
            var consumeMethod = types.ConsumerType.GetMethod("ConsumeAsync", new[] { types.MessageContextType });

            Task? task = consumeMethod?.Invoke(consumer, new[] { deserializedMessageContext }) as Task;

            if (task != null)
                await task;

        }
        catch (Exception exceptio)
        {
            Console.WriteLine(exceptio);
            throw;
        }
    }

    private bool CheckMessageFullName(string fullName)
    {
        return _inboxConfig?.EnabledEvents.Any(a => a.FullName == fullName) ?? false;
    }

    private static readonly Dictionary<Type, (Func<object, object> GetMessage, Func<object, string> GetShard, Func<object, Guid> GetMessageId, Func<object, DateTime> GetPublishDateTime)> AccessorCache = new();

    private static (Func<object, object> GetMessage, Func<object, string> GetShard, Func<object, Guid> GetMessageId, Func<object, DateTime> GetPublishDateTime) GetPropertyAccessors(Type messageContextType)
    {
        if (!AccessorCache.TryGetValue(messageContextType, out var accessors))
        {
            var messageParam = Expression.Parameter(typeof(object), "instance");
            var typedMessageParam = Expression.Convert(messageParam, messageContextType);

            var messageProperty = Expression.Property(typedMessageParam, "Message");
            var shardProperty = Expression.Property(typedMessageParam, "Shard");
            var messageIdProperty = Expression.Property(typedMessageParam, "MessageId");
            var publishDateTimeProperty = Expression.Property(typedMessageParam, "publishDateTime");

            accessors = (
                Expression.Lambda<Func<object, object>>(
                    Expression.Convert(messageProperty, typeof(object)),
                    messageParam).Compile(),
                Expression.Lambda<Func<object, string>>(
                    Expression.Convert(shardProperty, typeof(string)),
                    messageParam).Compile(),
                Expression.Lambda<Func<object, Guid>>(
                    Expression.Convert(messageIdProperty, typeof(Guid)),
                    messageParam).Compile(),
                Expression.Lambda<Func<object, DateTime>>(
                    Expression.Convert(publishDateTimeProperty, typeof(DateTime)),
                    messageParam).Compile()

            );

            AccessorCache[messageContextType] = accessors; // Cache the accessors
        }
        return accessors;
    }

}

