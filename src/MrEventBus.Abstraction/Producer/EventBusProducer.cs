using MrEventBus.Abstraction.Models;
using MrEventBus.Abstraction.Producer.Strategies;
using System.Diagnostics;

namespace MrEventBus.Abstraction.Producer;

public class EventBusProducer : IEventBusProducer
{
    private readonly IProduceStrategy _publishStrategy;

    public EventBusProducer(IProduceStrategy publishStrategy)
    {
        _publishStrategy = publishStrategy;
    }

    public Task PublishAsync<T>(T message, string shard = "", string queueName = "main") where T : class
    {
        try
        {
            Guid messageId = Guid.NewGuid();

            if (string.IsNullOrEmpty(shard))
                shard = messageId.ToString();

            return _publishStrategy.PublishAsync(new MessageContext<T>
            {
                Message = message,
                MessageId = messageId,
                Shard = shard,
                PublishDateTime = DateTime.Now
            }
            , queueName);
        }
        catch (Exception)
        {

            throw;
        }
    }

}
