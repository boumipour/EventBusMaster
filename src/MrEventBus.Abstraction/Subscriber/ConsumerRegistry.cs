using MrEventBus.Abstraction.Models;

namespace MrEventBus.Abstraction.Subscriber;

public static class ConsumerMessageRegistry
{
    private static Dictionary<string, (Type MessageContextType, Type ConsumerType)> _types = new Dictionary<string, (Type, Type)>();

    public static void RegisterMessageType(Type messageType)
    {
        var messageContextType = typeof(MessageContext<>).MakeGenericType(messageType);
        var consumerType = typeof(IMessageConsumer<>).MakeGenericType(messageType);

        _types.TryAdd(messageType.Name, (MessageContextType: messageContextType, ConsumerType: consumerType));
    }

    public static (Type MessageContextType, Type ConsumerType) GetMessageRelatedType(Type messageType)
    {
        if (_types.TryGetValue(messageType.Name, out var output))
        {
            return output;
        }

        // Return default tuple if not found (you could throw an exception or handle as needed)
        return (MessageContextType: null, ConsumerType: null);
    }
}
