using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace MrEventBus.Abstraction.Subscriber.Workers;
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
            var subscriber = scope.ServiceProvider.GetRequiredService<IEventBusSubscriber>();

            while (!stoppingToken.IsCancellationRequested)
            {
                var stopWatch = Stopwatch.StartNew();

                try
                {
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
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
            }
        }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
}
