using StackExchange.Redis;

public class RedisDistributedLockService
{
    private readonly ConnectionMultiplexer _redisConnection;

    public RedisDistributedLockService(ConnectionMultiplexer redisConnection)
    {
        _redisConnection = redisConnection;
    }

    public bool AcquireLock(string lockName, TimeSpan acquireTimeout, TimeSpan lockTimeout)
    {
        var redisDb = _redisConnection.GetDatabase();
        var identifier = Guid.NewGuid().ToString();
        var lockKey = $"lock:{lockName}";

        var endtime = DateTime.Now + acquireTimeout;

        while (DateTime.Now < endtime)
        {
            if (redisDb.LockTake(lockKey, identifier, lockTimeout))
            {
                // Kilidi başarıyla aldık, işlem devam ediyor
                return true;
            }

            Thread.Sleep(50); // Kilit alınamazsa bekleme süresi
        }

        return false; // Kilit alınamadı
    }

    public void ReleaseLock(string lockName)
    {
        var redisDb = _redisConnection.GetDatabase();
        var lockKey = $"lock:{lockName}";
        var identifier = GetLockIdentifier(lockName);

        // Kilidi yayınla (publish) ve serbest bırakma mesajını gönder
        redisDb.Publish(lockKey, identifier);
        redisDb.LockRelease(lockKey, identifier);
    }

    public string GetLockIdentifier(string lockName)
    {
        // Kilit kimliğini özelleştirmek için gerektiğiniz şekilde oluşturabilirsiniz
        return $"lock_identifier:{lockName}";
    }
}
