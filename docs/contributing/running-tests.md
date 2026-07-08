# Running Tests

How to run, add, and manage tests in the PawSharp project.

---

## Test projects overview

Each library project has a corresponding test project. All tests use **xUnit** with **FluentAssertions** and **Moq**.

| Test project | Tests for | Tests |
|---|---|---|
| `PawSharp.Core.Tests` | Entities, validation, utilities | Unit |
| `PawSharp.API.Tests` | REST client, rate limiting, serialization | Unit |
| `PawSharp.Gateway.Tests` | WebSocket, sharding, event dispatcher | Unit |
| `PawSharp.Cache.Tests` | MemoryCache, RedisCache, CacheSwapper | Unit + Integration |
| `PawSharp.Client.Tests` | DiscordClient, builder, DI | Unit |
| `PawSharp.Interactions.Tests` | InteractionHandler, modals, components | Unit |
| `PawSharp.Commands.Tests` | Command framework, type converters, preconditions | Unit |
| `PawSharp.Interactivity.Tests` | Pagination, polls, confirmation dialogs | Unit |
| `PawSharp.Voice.Tests` | Opus, RTP, DAVE E2EE, MLS | Unit + Integration |

Additional project:
- `PawSharp.Benchmarks` — performance benchmarks (BenchmarkDotNet)

---

## Running unit tests

**All unit tests:**

```bash
dotnet test PawSharp.sln
```

**Specific project:**

```bash
dotnet test tests/PawSharp.Core.Tests/PawSharp.Core.Tests.csproj
```

**With category filter (recommended for quick feedback):**

```bash
dotnet test --filter "Category=Unit"
```

**Verbose output:**

```bash
dotnet test -v d
```

**With test results file:**

```bash
dotnet test --logger "trx;LogFileName=results.trx" --results-directory ./test-results
```

---

## Running integration tests

Integration tests require a **real Discord bot token** and sometimes a running Redis instance or voice channel access.

### Discord token

Set the `DISCORD_TOKEN` environment variable:

```powershell
$env:DISCORD_TOKEN = "your-bot-token"
```

Or create a `test-settings.json` file (gitignored):

```json
{
  "DiscordToken": "your-bot-token"
}
```

### Run integration tests

```bash
dotnet test --filter "Category=Integration"
```

### Required infrastructure

| Test project | Requires |
|---|---|
| `PawSharp.Cache.Tests` (Redis integration) | Redis on `localhost:6379` |
| `PawSharp.Voice.Tests` (DAVE integration) | Discord token + voice channel |
| `PawSharp.Gateway.Tests` (live tests) | Discord token |

⚠️ Integration tests are excluded from the default CI run unless the token is present.

---

## Test categories

Tests are categorized using `[TestCategory]` or `[Trait]` attributes:

```csharp
[Fact, TestCategory("Unit")]
public void ParsesValidSnowflake()
{
    // No network or external dependencies
}

[Fact, TestCategory("Integration")]
public async Task ConnectsAndDisconnects()
{
    // Requires a real Discord connection
}
```

Filters:

| Filter | Use case |
|---|---|
| `Category=Unit` | Fast, no external dependencies |
| `Category=Integration` | Requires Discord token or infrastructure |
| `Category=Voice` | Voice-specific integration tests |
| `FullyQualifiedName~DAVE` | DAVE E2EE specific tests |

---

## Adding new tests

### 1. Choose the right project

Add test files to the test project that mirrors the source project:
- Source: `src/PawSharp.Core/Validation/SnowflakeValidator.cs`
- Test: `tests/PawSharp.Core.Tests/Validation/SnowflakeValidatorTests.cs`

### 2. Test structure

```csharp
using FluentAssertions;
using Moq;
using Xunit;

namespace PawSharp.Core.Tests.Validation;

public class SnowflakeValidatorTests
{
    [Fact, TestCategory("Unit")]
    public void Validate_ValidSnowflake_ReturnsTrue()
    {
        // Arrange
        var sut = new SnowflakeValidator();

        // Act
        var result = sut.Validate(123456789012345678ul);

        // Assert
        result.Should().BeTrue();
    }

    [Theory, TestCategory("Unit")]
    [InlineData(0)]
    [InlineData(1)]
    public void Validate_ReservedIds_ReturnsFalse(ulong id)
    {
        var sut = new SnowflakeValidator();
        sut.Validate(id).Should().BeFalse();
    }
}
```

### 3. Mock external dependencies

Use Moq for interfaces:

```csharp
[Fact, TestCategory("Unit")]
public async Task CreateMessageAsync_ApiFails_ThrowsDiscordApiException()
{
    var mockRest = new Mock<IDiscordRestClient>();
    mockRest.Setup(x => x.CreateMessageAsync(
            It.IsAny<ulong>(),
            It.IsAny<CreateMessageRequest>(),
            It.IsAny<CancellationToken>()))
        .ThrowsAsync(new DiscordApiException("Bad request"));

    var client = new DiscordClient(mockRest.Object);
    var act = () => client.SendMessageAsync(1, "test");

    await act.Should().ThrowAsync<DiscordApiException>();
}
```

### 4. Naming convention

```
<MethodName>_<Condition>_<ExpectedResult>
```

Examples:
- `CreateMessageAsync_ValidRequest_ReturnsMessage`
- `Validate_NullContent_ThrowsValidationException`
- `ConnectAsync_DoubleConnect_ThrowsInvalidOperationException`

---

## Benchmarking

### Running benchmarks

```bash
dotnet run -c Release --project tests/PawSharp.Benchmarks/PawSharp.Benchmarks.csproj
```

### Adding benchmarks

```csharp
[MemoryDiagnoser]
public class JsonSerializationBenchmarks
{
    private Message _message = null!;
    private byte[] _json = null!;

    [GlobalSetup]
    public void Setup()
    {
        _message = new Message { Id = 1, Content = "Hello" };
        _json = JsonSerializer.SerializeToUtf8Bytes(_message);
    }

    [Benchmark]
    public byte[] Serialize()
    {
        return JsonSerializer.SerializeToUtf8Bytes(_message);
    }

    [Benchmark]
    public Message? Deserialize()
    {
        return JsonSerializer.Deserialize<Message>(_json);
    }
}
```

---

## Tips

- **Run unit tests before pushing** — they should complete in under 30 seconds
- **Use `dotnet test --no-build`** after the first build to skip recompilation
- **Add `[Collection("Non-Parallel")]`** to integration tests that share state
- **Keep tests deterministic** — no sleeps, no network calls in unit tests
- **Clean up** disposal and database state in `Dispose` or `IAsyncLifetime`
- **Use `Should().ThrowExactly<T>()`** from FluentAssertions for precise exception type checks
