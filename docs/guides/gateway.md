# Gateway Connection

The Gateway provides a persistent WebSocket connection for real-time events from Discord.

## Connecting

```csharp
try
{
    await client.ConnectAsync();
    await Task.Delay(Timeout.Infinite);
}
catch (Exception ex)
{
    Console.WriteLine($"Connection failed: {ex.Message}");
}
finally
{
    await client.DisconnectAsync();
}
```

## Connection Lifecycle

```
Connecting -> Connected -> Ready -> Events Flow -> Disconnect -> Reconnecting
```

## Reconnection

PawSharp automatically handles reconnection with exponential backoff. Subscribe to `OnReady` to detect session restoration:

```csharp
client.OnReady(ready =>
{
    Console.WriteLine($"Session ready: {ready.User.Username} in {ready.Guilds.Count} guilds");
    return Task.CompletedTask;
});

// Low-level: detect resumed sessions
client.Gateway.Events.On<ResumedEvent>("RESUMED", resumed =>
{
    Console.WriteLine("Session resumed after reconnect");
    return Task.CompletedTask;
});
```

## Manual Reconnection

```csharp
if (!client.Gateway.IsConnected)
    await client.Gateway.ConnectAsync();
```

## Graceful Shutdown

```csharp
Console.CancelKeyPress += async (s, e) =>
{
    e.Cancel = true;
    await client.DisconnectAsync();
};
```

## Sharding

For bots in 2500+ servers, enable auto-sharding:

```csharp
var options = new PawSharpOptions
{
    Token = token,
    Shards = ShardingStrategy.Auto,
};

var shardManager = provider.GetRequiredService<ShardManager>();
await shardManager.ConnectAllAsync();

// Events fire for all shards automatically
client.OnMessageCreated(HandleMessageAsync);
```

Access a specific shard:

```csharp
var shard0Gateway = shardManager.GetShard(0)?.Gateway;
shard0Gateway?.Events.On<ReadyEvent>("READY", ready =>
{
    Console.WriteLine($"Shard 0 ready");
    return Task.CompletedTask;
});
```
