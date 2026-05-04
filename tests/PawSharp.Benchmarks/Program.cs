using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using PawSharp.Cache.Providers;
using PawSharp.Core.Entities;
using PawSharp.Core.Serialization;
using PawSharp.API.RateLimit;
using System.Text.Json;

BenchmarkRunner.Run<SerializationBenchmarks>();
BenchmarkRunner.Run<CoreBenchmarks>();

[MemoryDiagnoser]
public class SerializationBenchmarks
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

    private const string GuildJson = """
    {
        "id": "123456789012345678",
        "name": "Test Guild",
        "owner_id": "123456789012345679",
        "permissions": "8",
        "member_count": 100
    }
    """;

    private const string ComponentJson = """
    {
        "type": 1,
        "components": [
            {
                "type": 2,
                "style": 1,
                "label": "Click Me",
                "custom_id": "btn_1"
            },
            {
                "type": 3,
                "custom_id": "select_1",
                "placeholder": "Choose an option",
                "options": [
                    {
                        "label": "Option 1",
                        "value": "val_1"
                    },
                    {
                        "label": "Option 2",
                        "value": "val_2"
                    }
                ]
            }
        ]
    }
    """;

    private const string SnowflakeDictionaryJson = """
    {
        "123456789012345678": { "id": "123456789012345678", "username": "user1" },
        "123456789012345679": { "id": "123456789012345679", "username": "user2" }
    }
    """;

    [Benchmark]
    public Message DeserializeMessage()
    {
        return JsonSerializer.Deserialize<Message>(MessageJson)!;
    }

    [Benchmark]
    public Guild DeserializeGuild()
    {
        return JsonSerializer.Deserialize<Guild>(GuildJson)!;
    }

    [Benchmark]
    public MessageComponent DeserializeComponent()
    {
        return JsonSerializer.Deserialize<MessageComponent>(ComponentJson)!;
    }

    [Benchmark]
    public string SerializeMessage()
    {
        var message = new Message
        {
            Id = 123456789012345678,
            ChannelId = 123456789012345679,
            Content = "Hello world",
            Timestamp = DateTimeOffset.UtcNow
        };
        return JsonSerializer.Serialize(message);
    }

    [Benchmark]
    public string SerializeComponent()
    {
        var component = new ActionRow
        {
            Components = new List<MessageComponent>
            {
                new Button
                {
                    Style = ButtonStyle.Primary,
                    Label = "Click Me",
                    CustomId = "btn_1"
                }
            }
        };
        return JsonSerializer.Serialize(component);
    }

    [Benchmark]
    public Dictionary<ulong, User> DeserializeSnowflakeDictionary()
    {
        return JsonSerializer.Deserialize<Dictionary<ulong, User>>(SnowflakeDictionaryJson)!;
    }

    [Benchmark]
    public string SerializeSnowflakeDictionary()
    {
        var dict = new Dictionary<ulong, User>
        {
            [123456789012345678] = new User { Id = 123456789012345678, Username = "user1" },
            [123456789012345679] = new User { Id = 123456789012345679, Username = "user2" }
        };
        return JsonSerializer.Serialize(dict);
    }

    [Benchmark]
    public string SerializeSnowflakeAsString()
    {
        ulong snowflake = 123456789012345678;
        return snowflake.ToString();
    }

    [Benchmark]
    public ulong ParseSnowflakeFromString()
    {
        string snowflakeStr = "123456789012345678";
        return ulong.Parse(snowflakeStr);
    }
}

[MemoryDiagnoser]
public class CoreBenchmarks
{
    private readonly MemoryCacheProvider _cache;
    private readonly AdvancedRateLimiter _rateLimiter;

    public CoreBenchmarks()
    {
        _cache = new MemoryCacheProvider();
        _rateLimiter = new AdvancedRateLimiter();

        // Setup cache with some data
        var user = new User { Id = 123456789012345680, Username = "testuser" };
        _cache.CacheUser(user);
    }

    [Benchmark]
    public User CacheLookupUser()
    {
        return _cache.GetUser(123456789012345680)!;
    }

    [Benchmark]
    public async Task RateLimitWaitAsync()
    {
        await _rateLimiter.WaitForRateLimitAsync("channels/123456789012345679/messages");
    }
}
