using DistributedLockingViaRedis;
using StackExchange.Redis;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        
        // Redis bağlantısını oluşturun
        services.AddSingleton(ConnectionMultiplexer.Connect("localhost:6379"));
        services.AddSingleton<RedisDistributedLockService>();
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
