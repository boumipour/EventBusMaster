using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MrEventBus.Abstraction.Models;
using MrEventBus.Abstraction.Producer.Outbox.Config;
using MrEventBus.Abstraction.Producer.Outbox.Repository;
using MrEventBus.Abstraction.Producer.Strategies;
using System.Threading;

namespace MrEventBus.Abstraction.Producer.Outbox.Worker
{
    public class OutboxProcessorWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly OutboxConfig _outboxingConfig;

        public OutboxProcessorWorker(IServiceProvider serviceProvider, IOptions<OutboxConfig> outboxingConfig)
        {
            _serviceProvider = serviceProvider;
            _outboxingConfig = outboxingConfig.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Factory.StartNew(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var semaphore = new SemaphoreSlim(_outboxingConfig.Concurrency);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                        var publisher = scope.ServiceProvider.GetRequiredService<IProduceStrategy>();

                        var messages = (await repository.GetAsync()).ToList();

                        var tasks = messages.Select(async message =>
                        {
                            await semaphore.WaitAsync(stoppingToken);
                            try
                            {
                                await PublishMessageAsync(publisher, repository, message, stoppingToken);
                            }
                            finally
                            {
                                semaphore.Release(); 
                            }
                        });

                        await Task.WhenAll(tasks);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        continue;
                    }

                    finally
                    {
                        await Task.Delay(_outboxingConfig.ReaderInterval, stoppingToken);
                    }
                }
            }, stoppingToken, TaskCreationOptions.None, TaskScheduler.Default);
        }



        private async Task PublishMessageAsync(IProduceStrategy publisher, IOutboxRepository repository, OutboxMessage message, CancellationToken cancellation = default)
        {
            var messagePayload = message.RecreateMessage();
            if (messagePayload == null)
                return;

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
