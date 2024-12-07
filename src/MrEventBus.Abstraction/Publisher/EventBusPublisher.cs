using MrEventBus.Abstraction.Models;
using MrEventBus.Abstraction.Publisher;
using MrEventBus.Abstraction.Publisher.Strategies;
using System.Diagnostics;

namespace EventBus.Publisher;

public class EventBusPublisher : IEventBusPublisher
{
    private readonly IPublishStrategy _publishStrategy;

    public EventBusPublisher(IPublishStrategy publishStrategy)
    {
        _publishStrategy = publishStrategy;
    }

    public Task PublishAsync<T>(T message, string shard = "", string queueName = "main") where T : class
    {
        var stopWatch = Stopwatch.StartNew();

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
