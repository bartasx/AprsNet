using System.Text.Json;
using Aprs.Application.Interfaces;
using StackExchange.Redis;

namespace Aprs.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = redis.GetDatabase();
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var json = JsonSerializer.Serialize(value, JsonOptions);
        if (expiry.HasValue)
            await _db.StringSetAsync(key, json, expiry.Value).WaitAsync(cancellationToken);
        else
            await _db.StringSetAsync(key, json).WaitAsync(cancellationToken);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var val = await _db.StringGetAsync(key).WaitAsync(cancellationToken);
        if (val.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(val.ToString()!, JsonOptions);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _db.KeyExistsAsync(key).WaitAsync(cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.KeyDeleteAsync(key).WaitAsync(cancellationToken);
    }
}
