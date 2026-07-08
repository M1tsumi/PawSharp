# Benchmarks

PawSharp includes a BenchmarkDotNet project for measuring performance. Run it to verify performance regressions and understand throughput characteristics.

---

## Running Benchmarks

```bash
# From the repository root
dotnet run -c Release -p tools/Benchmarks/
```

Example output:

```
BenchmarkDotNet v0.14.0
// * Summary *

BenchmarkDotNet: serialization/deserialization of Discord entities
| Method                    | Mean     | Error    | Allocated |
|---------------------------|----------|----------|-----------|
| SerializeGuild            | 1.234 us | 0.012 us | 2.45 KB   |
| DeserializeGuild          | 2.345 us | 0.023 us | 3.12 KB   |
| SerializeMessage          | 0.987 us | 0.009 us | 1.89 KB   |
| DeserializeMessage        | 1.876 us | 0.018 us | 2.56 KB   |

Cache throughput:
| Method                    | Mean     | Error    | Allocated |
|---------------------------|----------|----------|-----------|
| MemoryCacheGetGuild       | 0.045 us | 0.001 us | 0 B       |
| MemoryCacheSetGuild       | 0.067 us | 0.002 us | 0 B       |
| RedisCacheGetUser         | 1.234 us | 0.015 us | 1.2 KB    |
| RedisCacheSetUser         | 1.567 us | 0.021 us | 1.5 KB    |

REST client throughput:
| Method                    | Mean     | Error    | Allocated |
|---------------------------|----------|----------|-----------|
| BuildAndSerializeRequest  | 0.567 us | 0.008 us | 1.2 KB    |
| ParseResponseHeaders      | 0.123 us | 0.002 us | 0 B       |
```

---

## Key Metrics

| Metric | Expected Range | Notes |
|--------|---------------|-------|
| Cache get (memory) | < 0.1 µs | ConcurrentDictionary lookup |
| Cache set (memory) | < 0.1 µs | Dictionary insert |
| Cache get (Redis) | 1-2 ms | Network round-trip |
| JSON serialize | 1-5 µs | Source-generated contexts |
| JSON deserialize | 2-10 µs | Source-generated contexts |
| Event dispatch | 1-5 µs per handler | Parallel if `EnableParallelDispatch` |
| Heartbeat latency | 10-100 ms | Network to Discord |

---

## Interpreting Results

- **Mean** — average execution time in microseconds
- **Error** — statistical error margin
- **Allocated** — memory allocated per operation

Track these across commits to catch performance regressions.

---

## Performance Regression Testing

```bash
# Before making changes
dotnet run -c Release -p tools/Benchmarks/ -- --exporters json --output baseline.json

# After making changes
dotnet run -c Release -p tools/Benchmarks/ -- --exporters json --output current.json

# Compare
# (Manual comparison or use a CI tool like Ben.Demystifier)
```

Check for:
- >10% increase in mean execution time
- >10% increase in allocation
- New allocations on hot paths (cache lookups, message deserialization)

---

## Common Mistakes

| Mistake | Impact |
|---------|--------|
| Running benchmarks in Debug mode | Results not representative |
| Running on throttled CPU | Higher variance; use `--job medium` |
| Ignoring Allocation column | Allocation spikes cause GC pressure |
| Not comparing against baseline | Can't detect regressions |
| Running with attached debugger | Invalidates all measurements |
