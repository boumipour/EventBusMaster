using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MrEventBus.Abstraction.Consumer.Workers
{
    public sealed class SubscribeWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public SubscribeWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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
                        var subscriber = scope.ServiceProvider.GetRequiredService<IEventBusSubscriber>();
                        await subscriber.SubscribeAsync(stoppingToken);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        continue;
                    }
                    finally
                    {
                        Console.WriteLine("Subscribed");
                    }
                }
            }, stoppingToken, TaskCreationOptions.None, TaskScheduler.Default);
        }
    }
}
