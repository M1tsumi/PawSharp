using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using PawSharp.Cache.Providers;
using PawSharp.Core.Entities;
using PawSharp.API.RateLimit;
using System.Text.Json;

BenchmarkRunner.Run<Benchmarks>();

[MemoryDiagnoser]
public class Benchmarks
{
    private const string MessageJson = """
    {
        "id": "123456789012345678",
        "channel_id": "123456789012345679",
        "author": {
            "id": "123456789012345680",
            "username": "testuser",
            "discriminator": "1234",
            "avatar": null
        },
        "content": "Hello world",
        "timestamp": "2023-01-01T00:00:00.000Z"
    }
    """;

    private readonly MemoryCacheProvider _cache;
    private readonly RateLimiter _rateLimiter;

    public Benchmarks()
    {
        _cache = new MemoryCacheProvider();
        _rateLimiter = new RateLimiter(10, TimeSpan.FromSeconds(1));

        // Setup cache with some data
        var user = new User { Id = 123456789012345680, Username = "testuser" };
        _cache.CacheUser(user);
    }

    [Benchmark]
    public Message DeserializeMessage()
    {
        return JsonSerializer.Deserialize<Message>(MessageJson)!;
    }

    [Benchmark]
    public User CacheLookupUser()
    {
        return _cache.GetUser(123456789012345680)!;
    }

    [Benchmark]
    public bool RateLimitTryAcquire()
    {
        return _rateLimiter.TryAcquire();
    }
}
