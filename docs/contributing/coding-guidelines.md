# Coding Guidelines

Standards and conventions for contributing code to PawSharp.

---

## Coding Conventions

The `.editorconfig` file at the repository root defines all style rules. Key conventions:

### Formatting

- **Indentation**: 4 spaces (no tabs)
- **Line endings**: LF
- **File scoped namespaces**: `namespace PawSharp.Core;` (not block-scoped)
- **Braces**: Always use braces (`csharp_prefer_braces = true:warning`)
- **UTF-8** encoding for all source files

### Naming

- `PascalCase` for public types, methods, properties, constants
- `_camelCase` for private fields (underscore prefix)
- `camelCase` for local variables and parameters
- No Hungarian notation
- `I` prefix for interfaces: `IDiscordClient`, `ICacheProvider`
- Async methods end with `Async` suffix

### var usage

```csharp
//  Avoid when type is not obvious
var result = GetSomething(); // What's the type?

//  Preferred when type is obvious
var client = new PawSharpClientBuilder();
var message = await client.Rest.CreateMessageAsync(id, req);

//  Use explicit type for built-in numeric types
int count = 42;
bool enabled = true;
```

### Modifier order

```
public, private, protected, internal, file, static, extern,
new, virtual, abstract, sealed, override, readonly, unsafe,
required, volatile, async
```

### Modern C# idioms

```csharp
//  Pattern matching
if (obj is Message msg) { /* use msg */ }

//  Null propagation
var content = message?.Content ?? "empty";

//  Switch expressions
var type = channel.Type switch
{
 ChannelType.GuildText => "text",
 ChannelType.GuildVoice => "voice",
 _ => "other"
};

//  Target-typed new
List<Message> messages = [];
Dictionary<ulong, string> dict = [];

//  Collection expressions
var items = new[] { 1, 2, 3 };
```

---

## Async Patterns

### All I/O must be async

```csharp
//  Correct
public async Task<Message> SendMessageAsync(ulong channelId, CreateMessageRequest req)
{
 var response = await _httpClient.PostAsync(...);
 return await DeserializeAsync<Message>(response);
}

//  Wrong - blocks the thread
public Message SendMessage(ulong channelId, CreateMessageRequest req)
{
 var response = _httpClient.PostAsync(...).Result;
 return Deserialize<Message>(response.Result);
}
```

### ConfigureAwait(false)

Every `await` in library code **must** use `.ConfigureAwait(false)`:

```csharp
await _httpClient.PostAsync(...).ConfigureAwait(false);
await _cache.GetAsync(key).ConfigureAwait(false);
```

This prevents deadlocks in synchronization-context-sensitive hosts (ASP.NET, WinForms, WPF).

Exception: test projects should NOT use `ConfigureAwait(false)` unless explicitly testing it.

### CancellationToken

- All async public methods should accept `CancellationToken cancellationToken = default`
- Pass it through to all downstream async calls
- Do not create `CancellationTokenSource` inside hot paths - prefer passing it from the caller

```csharp
public async Task<Message> CreateMessageAsync(
 ulong channelId,
 CreateMessageRequest request,
 CancellationToken ct = default)
{
 ct.ThrowIfCancellationRequested();
 var response = await _httpClient.PostAsync(endpoint, content, ct)
 .ConfigureAwait(false);
 return await DeserializeAsync<Message>(response, ct)
 .ConfigureAwait(false);
}
```

### ValueTask vs Task

Prefer `ValueTask<T>` for:
- Methods that may complete synchronously
- Methods called in tight loops
- Methods where the async path is rare

Prefer `Task<T>` for:
- Most async library methods
- Methods that are always truly async
- Public API surface where consistency matters more than micro-optimization

---

## Nullable Reference Types

The entire codebase uses `#nullable enable` (set project-wide in `Directory.Build.props`).

```csharp
public class User
{
 public string Name { get; set; } // Required - compiler warns if null
 public string? Bio { get; set; } // Optional - may be null
}
```

### Rules

- Always validate non-nullable parameters at public boundaries:

```csharp
public async Task SendMessageAsync(ulong channelId, string content)
{
 ArgumentNullException.ThrowIfNull(content);
 // ...
}
```

- Use nullable annotations honestly. If a property may be null, mark it `?`
- Avoid null-forgiving operator (`!`) except in interop or test code
- Prefer `ArgumentNullException.ThrowIfNull()` over manual null checks

### Exception handling for null

```csharp
//  Good
if (member.User is null) return;

//  Good - throw typed exceptions
if (string.IsNullOrWhiteSpace(content))
 throw new ValidationException("Content cannot be empty");
```

---

## XML Documentation Requirements

All **public** APIs must have XML doc comments. Internal/private APIs are encouraged but not required.

```csharp
/// <summary>
/// Sends a message to a Discord channel.
/// </summary>
/// <param name="channelId">The ID of the channel to send to.</param>
/// <param name="request">The message creation request.</param>
/// <param name="ct">Cancellation token.</param>
/// <returns>The created <see cref="Message"/>.</returns>
/// <exception cref="ValidationException">Content is empty or too long.</exception>
/// <exception cref="DiscordApiException">API returned an error.</exception>
/// <exception cref="RateLimitException">Rate limit exceeded.</exception>
/// <example>
/// <code>
/// var msg = await client.SendMessageAsync(channelId, "Hello!", ct);
/// Console.WriteLine(msg.Id);
/// </code>
/// </example>
public async Task<Message> SendMessageAsync(
 ulong channelId,
 string content,
 CancellationToken ct = default)
```

### Patterns

| Element | When to use |
|---|---|
| `<summary>` | Always - describes what the method/property does |
| `<param>` | Every parameter |
| `<returns>` | Every non-void method |
| `<exception>` | Every documented exception the caller should handle |
| `<example>` | Complex APIs, especially builder and configuration APIs |
| `<see cref="..."/>` | Cross-reference other types |
| `<c>...</c>` | Inline code references (when `<see>` doesn't resolve) |
| `<inheritdoc/>` | When overriding/implementing a documented member |

---

## Testing Requirements

- All new features must include tests
- Test both success and failure paths
- Follow AAA pattern (Arrange, Act, Assert)
- Name tests: `MethodName_Condition_ExpectedResult`

```csharp
[Fact]
public async Task CreateMessageAsync_ValidRequest_ReturnsMessage()
{
 // Arrange
 var request = new CreateMessageRequest { Content = "Test" };

 // Act
 var result = await _client.CreateMessageAsync(channelId, request);

 // Assert
 result.Should().NotBeNull();
 result.Content.Should().Be("Test");
}
```

### Test categories

```csharp
[Fact, TestCategory("Unit")]
public void Snowflake_ValidId_ReturnsTimestamp() { /* ... */ }

[Fact, TestCategory("Integration")]
public async Task RestClient_GetGateway_ReturnsUrl() { /* ... */ }
```

See [running-tests.md](running-tests.md) for detailed guidance.

---

## PR Process

1. **Create a feature/fix branch** from `main`:

 ```
 feat/your-feature
 fix/your-bug-fix
 docs/your-doc-update
 ```

2. **Commit** using [Conventional Commits](https://www.conventionalcommits.org/):

 ```
 feat(api): add GetGuildPreviewAsync endpoint
 fix(gateway): handle WebSocket reconnect race
 docs: update migration guide for alpha.5
 ```

3. **Open a pull request** targeting `main`:

 - Descriptive title matching the commit format
 - Summary of changes and motivation
 - Reference related issues: `Closes #42`
 - Checklist of verification steps

4. **CI must pass** - builds, tests, and hygiene checks

5. **Code review** - at least one maintainer must approve

6. **Merge** - squash or merge commit (maintainer's choice)

---

## Code review checklist

Reviewers check for:

- [ ] Follows `.editorconfig` style and naming conventions
- [ ] Async patterns correct (`ConfigureAwait(false)`, `CancellationToken`)
- [ ] Nullable annotations correct, no unnecessary `!`
- [ ] Public APIs have XML documentation
- [ ] Tests cover success and failure paths
- [ ] No blocking calls (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`)
- [ ] Thread safety for shared state (volatile, locks, ConcurrentDictionary)
- [ ] No hardcoded secrets, tokens, or credentials
- [ ] New dependencies justified and added to `Directory.Packages.props`
- [ ] No dead/unused code
- [ ] Changes are backward compatible (or documented as breaking)
