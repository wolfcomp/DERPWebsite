using Newtonsoft.Json;
using StackExchange.Redis;

namespace PDPWebsite.Services;

public class RedisClient
{
    private static readonly TimeSpan expireConstant = new(7, 0, 0, 0);
    private ILogger<RedisClient> _logger;

    public RedisClient(ILogger<RedisClient> logger)
    {
        _logger = logger;
    }

    public ConnectionMultiplexer Connection { get; set; } = ConnectionMultiplexer.Connect("localhost");

    private IDatabase GetDatabase()
    {
        return Connection.GetDatabase();
    }

    public string? Get(string key)
    {
        var db = GetDatabase();
        return db.StringGet(key);
    }

    public void Set(string key, string value, DateTime? expire = null)
    {
        var db = GetDatabase();
        db.StringSet(key, value);
        db.KeyExpire(key, expire ?? DateTime.Now.Add(expireConstant));
    }

    public T? GetObj<T>(string key)
    {
        var k = Get(key);
        return string.IsNullOrWhiteSpace(k) ? default : JsonConvert.DeserializeObject<T>(k);
    }

    public void SetObj<T>(string key, T value, DateTime? expire = null) where T : notnull
    {
        Set(key, JsonConvert.SerializeObject(value), expire);
    }

    public void SetExpire(string key, TimeSpan timeSpan)
    {
        SetExpire(key, DateTime.Now.Add(timeSpan));
    }

    public void SetExpire(string key, DateTime dataTime)
    {
        var db = GetDatabase();
        db.KeyExpire(key, dataTime);
    }
}
