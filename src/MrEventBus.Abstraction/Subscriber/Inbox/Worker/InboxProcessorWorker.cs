using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MrEventBus.Abstraction.Models;
using MrEventBus.Abstraction.Subscriber.Inbox.Config;
using MrEventBus.Abstraction.Subscriber.Inbox.Repository;
using System.Diagnostics;
using System.Threading;

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
                var semaphore = new SemaphoreSlim(_InboxingConfig.Concurrency);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var stopWatch = Stopwatch.StartNew();
                    try
                    {
                        var repository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();

                        var messages = (await repository.GetAsync()).ToList();

                        var tasks = messages.Select(async message =>
                        {
                            await semaphore.WaitAsync(stoppingToken);
                            try
                            {
                                await ConsumeMessageAsync(scope, repository, message);
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
                        await Task.Delay(_InboxingConfig.ReaderInterval, stoppingToken);
                    }
                }
            }, stoppingToken, TaskCreationOptions.None, TaskScheduler.Default);
        }

        //todo: review performance issue
        private async Task ConsumeMessageAsync(IServiceScope serviceScope, IInboxRepository repository, InboxMessage message)
        {
            try
            {
                var messageType = Type.GetType(message.Type);
                if (messageType == null)
                {
                    Console.WriteLine($"message type {message.Type} not found");
                    return;
                }

                var types = ConsumerMessageRegistry.GetMessageRelatedType(messageType);
   
                var messageContext = Activator.CreateInstance(types.MessageContextType);
                types.MessageContextType.GetProperty("Message")?.SetValue(messageContext, message.RecreateMessage());
                types.MessageContextType.GetProperty("MessageId")?.SetValue(messageContext, message.MessageId);
                types.MessageContextType.GetProperty("PublishDateTime")?.SetValue(messageContext, message.PublishDateTime);
                types.MessageContextType.GetProperty("Shard")?.SetValue(messageContext, message.Shard);

                var consumer = serviceScope.ServiceProvider.GetRequiredService(types.ConsumerType);

                var consumeMethod = types.ConsumerType.GetMethod("ConsumeAsync");

                Task? task = consumeMethod?.Invoke(consumer, new[] { messageContext }) as Task;

                if (task != null)
                    await task;

                message.Consumed();
                await repository.UpdateAsync(message);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return;
            }
        }
    }


}
