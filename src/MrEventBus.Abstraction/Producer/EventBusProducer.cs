using Microsoft.Extensions.Options;
using MrEventBus.Abstraction.Models;
using MrEventBus.Abstraction.Producer.Outbox.Config;
using MrEventBus.Abstraction.Producer.Outbox.Repository;
using MrEventBus.Abstraction.Producer.Strategies;

namespace MrEventBus.Abstraction.Producer;

public class EventBusProducer : IEventBusProducer
{
    private readonly IProduceStrategy _publishStrategy;

    private readonly IOutboxRepository? _outboxRepository = null;
    private readonly OutboxConfig? _outboxConfig = null;

    public EventBusProducer(IProduceStrategy publishStrategy, IOutboxRepository? outboxRepository = null, IOptions<OutboxConfig>? outboxConfig = null)
    {
        _publishStrategy = publishStrategy;
        _outboxRepository = outboxRepository;
        _outboxConfig = outboxConfig?.Value;

    }

    public  async Task PublishAsync<T>(T message, string shard = "", string queueName = "main") where T : class
    {
        try
        {
            Guid messageId = Guid.NewGuid();

            if (string.IsNullOrEmpty(shard))
                shard = messageId.ToString();

            if (_outboxRepository != null && CheckMessageFullName(message!.GetType().FullName))
            {
                await _outboxRepository!.CreateAsync(new OutboxMessage(messageId, message, shard, queueName));
                return;
            }

            await _publishStrategy.PublishAsync(new MessageContext<T>
            {
                Message = message,
                MessageId = messageId,
                Shard = shard,
                PublishDateTime = DateTime.Now
            }
            , queueName);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }


    private bool CheckMessageFullName(string fullName)
    {
        return _outboxConfig?.EnabledEvents.Any(a => a.FullName == fullName) ?? false;
    }

}
