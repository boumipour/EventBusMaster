using System.Diagnostics;
using EventBus.Model;
using EventBus.Publisher.Strategies;

namespace EventBus.Publisher;

public class MrEventBusPublisher : IMrEventBusPublisher
{
    private readonly IPublishStrategy _publishStrategy;
    //private readonly ILogger<APEventBusPublisher> _logger;

    public MrEventBusPublisher(IPublishStrategy publishStrategy)
    {
        _publishStrategy = publishStrategy;
    }

    public Task PublishAsync<T>(T message, string shard = "", string queueName = "") where T : class
    {
        var stopWatch = Stopwatch.StartNew();

        try
        {
            Guid messageId = Guid.NewGuid();

            if (string.IsNullOrEmpty(shard))
                shard = messageId.ToString();


            // if (_outboxRepository != null && CheckMessageFullName(message.GetType().FullName))
            // {
            //     return _outboxRepository.CreateAsync(new OutboxMessage(messageId, message, shard));
            // }

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
            // log
            throw;
        }
        finally
        {
            // log
        }
    }

}
