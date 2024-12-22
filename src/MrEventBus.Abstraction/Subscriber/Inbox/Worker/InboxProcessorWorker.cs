using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MrEventBus.Abstraction.Models;
using MrEventBus.Abstraction.Subscriber.Inbox.Config;
using MrEventBus.Abstraction.Subscriber.Inbox.Repository;
using System.Diagnostics;

namespace MrEventBus.Abstraction.Subscriber.Inbox.Worker
{
    public class InboxProcessorWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly InboxConfig _InboxingConfig;
        private readonly ILogger<InboxProcessorWorker> _logger;

        public InboxProcessorWorker(IServiceProvider serviceProvider, IOptions<InboxConfig> InboxingConfig, ILogger<InboxProcessorWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _InboxingConfig = InboxingConfig.Value;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Factory.StartNew(async () =>
            {
                using var scope = _serviceProvider.CreateScope();

                while (!stoppingToken.IsCancellationRequested)
                {
                    var stopWatch = Stopwatch.StartNew();
                    try
                    {
                        var repository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();

                        var messages = (await repository.GetAsync()).ToList();


                        ParallelOptions option = new()
                        {
                            CancellationToken = stoppingToken,
                            MaxDegreeOfParallelism = _InboxingConfig.Concurrency
                        };

                        Parallel.ForEach(messages, option, async (message) =>
                        {
                            {
                                await ConsumeMessageAsync(scope, repository, message);
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
                        await Task.Delay(_InboxingConfig.ReaderInterval, stoppingToken);
                    }
                }
            }, stoppingToken, TaskCreationOptions.None, TaskScheduler.Default);
        }

        private async Task ConsumeMessageAsync(IServiceScope serviceScope, IInboxRepository repository, InboxMessage message)
        {
            try
            {
                var messagePayload = message.RecreateMessage();
                if (messagePayload == null)
                    return;

                var messageType = Type.GetType(message.Type);
                if (messageType == null)
                {
                    Console.WriteLine($"message type {message.Type} not found");
                    return;
                }

                var types = ConsumerMessageRegistry.GetMessageRelatedType(messageType);

                var consumer = serviceScope.ServiceProvider.GetRequiredService(types.ConsumerType);

                //todo: fix
                var consumeMethod = types.ConsumerType.GetMethod("ConsumeAsync", new[] { types.MessageContextType });

                Task? task = consumeMethod?.Invoke(consumer, new[] { messagePayload }) as Task;

                if (task != null)
                    await task;

                message.Consumed();
                await repository.UpdateAsync(message);
            }
            catch(Exception exception) 
            {
               Console.WriteLine(exception);
               return;
            }
        }
    }


}
