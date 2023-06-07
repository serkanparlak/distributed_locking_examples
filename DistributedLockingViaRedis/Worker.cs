using StackExchange.Redis;

namespace DistributedLockingViaRedis;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RedisDistributedLockService _distributedLock;
    private readonly ConnectionMultiplexer _redisConnection;
    private readonly string _lockName = "worker1";
    private readonly string _channelName = "lock:worker1";

    public Worker(
        ILogger<Worker> logger,
        RedisDistributedLockService distributedLock,
        ConnectionMultiplexer redisConnection)
    {
        _logger = logger;
        _distributedLock = distributedLock;
        _redisConnection = redisConnection;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await TransferMoney();
            await Task.Delay(10000, stoppingToken); // Her çalışmada 10 saniye beklet
        }
    }

    public async Task TransferMoney()
    {
        // Redis abone nesnesi oluşturun
        var subscriber = _redisConnection.GetSubscriber();

        // Redis kanalını dinleme işlemi
        subscriber.Subscribe(_channelName, (channel, message) =>
        {
            // Kilidi serbest bırakan mesajı kontrol et
            if (message == _distributedLock.GetLockIdentifier(_lockName))
            {
                _logger.LogInformation("Kilidi serbest bırakan mesaj alındı.");

                // Kritik bölgeyi tamamlama
                _logger.LogInformation("Kritik bölgeyi tamamladı.");

                // Redis kanalını dinlemeyi sonlandır
                subscriber.Unsubscribe(_channelName);
            }
        });

        // Kilit almaya çalış
        if (_distributedLock.AcquireLock(_lockName, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30)))
        {
            try
            {
                // Kritik bölgeye gir
                _logger.LogInformation("Kritik bölgeye girdi.");
                Thread.Sleep(5000); // Kilitlenmiş işlemler
            }
            finally
            {
                // Kilidi serbest bırakma mesajını yayınla
                _distributedLock.ReleaseLock(_lockName);
                _logger.LogInformation("Kilit serbest bırakıldı.");
            }
        }
        else
        {
            // Kilit alınamadı
            _logger.LogInformation("Kilit alınamadı.");
        }
    }
}
