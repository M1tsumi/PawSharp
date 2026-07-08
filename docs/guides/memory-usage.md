# Memory Usage

PawSharp's memory profile is dominated by the entity cache, gateway message buffers, and string allocations from JSON parsing.

---

## Cache Memory Management

### MemoryCacheProvider

Each entity type is stored in a `ConcurrentDictionary` with estimated per-entity sizes:

| Entity | Est. Size | Default Max | Est. Total |
|--------|-----------|-------------|------------|
| User | ~1 KB | 20,000 | ~20 MB |
| Guild | ~2 KB | 1,000 | ~2 MB |
| Channel | ~1 KB | 5,000 | ~5 MB |
| Message | ~2 KB | 10,000 | ~20 MB |
| GuildMember | ~1 KB | 50,000 | ~50 MB |
| Role | ~0.5 KB | 10,000 | ~5 MB |
| Emoji | ~0.5 KB | 5,000 | ~2.5 MB |

**Maximum in-memory cache ~104 MB** at default settings.

```csharp
public long GetMemoryUsage()
{
 return (_users.Count * 1024L) +
 (_guilds.Count * 2048L) +
 (_channels.Count * 1024L) +
 (_messages.Count * 2048L) +
 (_members.Count * 1024L) +
 (_roles.Count * 512L) +
 (_emojis.Count * 512L);
}
```

---

## Eviction Policies

Two eviction mechanisms protect against unbounded growth:

### LRU Eviction

Triggered when any per-type dictionary exceeds its `Max*` limit:

```csharp
var keysToRemove = keysWithAccess
 .OrderBy(k => k.access)
 .Take(cache.Count - maxSize);
// Least recently accessed entries evicted first
```

### TTL-based Expiration

Background timer (`CleanupExpiredEntries`) runs every 60 seconds:

```csharp
var expired = _users.Where(kvp =>
 (now - _lastAccess[kvp.Key]) > _userExpiration.Value);
```

---

## LRU Tracking Overhead

Each cache entry adds a `_lastAccess` dictionary entry (`ulong -> DateTime`, ~16 bytes per entry). For 90,000 entities across all types, this adds ~1.4 MB overhead.

---

## String Allocation in Gateway Messages

The gateway receive loop allocates strings for every message:

1. Raw JSON string from WebSocket
2. `JsonDocument.Parse(message)` with `IDisposable` lifetime
3. Event data extracted via `GetRawText()`
4. String passed to event handlers

To minimize overhead:
- Use `JsonDocument` (not `JObject`) for pooled document parsing
- Event dispatcher processes strings, not streams

---

## Buffer Management

`WebSocketConnection` rents buffers from `ArrayPool<byte>.Shared`:

```csharp
byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
// bufferSize = WebSocketBufferSizeKb * 1024 (default: 16 KB)
```

---

## Monitoring Memory

### IPerformanceMetrics

```csharp
public interface IPerformanceMetrics
{
 MetricsSummary GetSummary();
 void RecordCacheOperation(string entityType, bool isHit);
 void RecordEventDispatch(string eventName, long durationMs);
 void RecordQueueDepth(int depth);
}
```

```csharp
var summary = metrics.GetSummary();
Console.WriteLine($"Cache hit rate: {summary.CacheHitRate:P2}");
Console.WriteLine($"Queue depth: {summary.CurrentQueueDepth}/{summary.MaxQueueDepth}");
```

### Direct memory monitoring

```csharp
var cache = provider.GetRequiredService<IEntityCache>();
var stats = cache.GetCacheStats();

Console.WriteLine($"Total entities: {cache.GetEntityCount()}");
Console.WriteLine($"Memory: {cache.GetMemoryUsage() / 1024 / 1024} MB");
Console.WriteLine($"Guilds: {stats.GuildCount}");
Console.WriteLine($"Messages: {stats.MessageCount}");
```

---

## Best Practices for Memory-Constrained Environments

1. **Reduce cache bounds**:
```csharp
var cacheOptions = new CacheOptions
{
 MaxMessages = 1000,
 MaxUsers = 5000,
 MaxMembers = 10000
};
```

2. **Set TTL aggressively**:
```csharp
cacheOptions.MessageExpiration = TimeSpan.FromMinutes(5);
cacheOptions.UserExpiration = TimeSpan.FromHours(1);
```

3. **Use Redis for persistence** - offload memory to external store.

4. **Disable message caching** if not needed:
```csharp
// In CacheManager, skip Message subscription:
// Don't call gateway.Events.On("MESSAGE_CREATE", ...)
```

5. **Monitor with `IMemoryMetrics`**:
```csharp
var process = Process.GetCurrentProcess();
Console.WriteLine($"Working set: {process.WorkingSet64 / 1024 / 1024} MB");
Console.WriteLine($"GC: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
```

---

## Common Mistakes

| Mistake | Impact |
|---------|--------|
| Default cache settings for large bots | May exceed available RAM |
| Not setting message TTL | Messages accumulate indefinitely |
| Storing large object references in cache | Prevents GC collection |
| Ignoring `_genericCache` max size (10,000) | Entries evicted silently |
| Holding `JsonDocument` longer than needed | Keeps memory allocated |
