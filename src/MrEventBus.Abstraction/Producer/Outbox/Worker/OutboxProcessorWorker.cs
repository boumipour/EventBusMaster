using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MrEventBus.Abstraction.Models;
using MrEventBus.Abstraction.Producer.Outbox.Config;
using MrEventBus.Abstraction.Producer.Outbox.Repository;
using MrEventBus.Abstraction.Producer.Strategies;

namespace MrEventBus.Abstraction.Producer.Outbox.Worker
{
    public class OutboxProcessorWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly OutboxConfiguration _outboxingConfig;

        public OutboxProcessorWorker(IServiceProvider serviceProvider, IOptions<OutboxConfiguration> outboxingConfig)
        {
            _serviceProvider = serviceProvider;
            _outboxingConfig = outboxingConfig.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Factory.StartNew(async () =>
            {
                using var scope = _serviceProvider.CreateScope();

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                        var publisher = scope.ServiceProvider.GetRequiredService<IProduceStrategy>();
                        var messages = (await repository.GetAsync()).ToList();


                        ParallelOptions option = new()
                        {
                            CancellationToken = stoppingToken,
                            MaxDegreeOfParallelism = 10
                        };

                        Parallel.ForEach(messages, option, async (message) =>
                        {
                            {
                                await PublishMessageAsync(publisher, repository, message, stoppingToken);
                            }
                        });
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        continue;
                    }

                    finally
                    {
                        await Task.Delay(((int)_outboxingConfig.OutboxReaderInterval.TotalMilliseconds), stoppingToken);
                    }
                }
            }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }



        private async Task PublishMessageAsync(IProduceStrategy publisher, IOutboxRepository repository, OutboxMessage message, CancellationToken cancellation = default)
        {
            var messagePayload = message.RecreateMessage();
            if (messagePayload == null)
                return;

            //var messageType = Type.GetType(message.Type);

            await publisher.PublishAsync(new MessageContext<object>()
            {
                Message = messagePayload,
                MessageId = message.MessageId,
                PublishDateTime = message.CreateDateTime,
                Shard = message.Shard,
            },
            queueName: message.QueueName,
            cancellationToken: cancellation
            );

            message.DeliveredToBus();
            await repository.UpdateAsync(message);
        }
    }
}
