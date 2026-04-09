# Contributing to PawSharp

Use this guide to set up the repository, make changes, run tests, and open a pull request.

## Development Environment Setup

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Git](https://git-scm.com/)
- IDE: [Visual Studio 2022](https://visualstudio.microsoft.com/), [VS Code](https://code.visualstudio.com/), or [Rider](https://www.jetbrains.com/rider/)

### Getting Started
1. **Clone the repository**:
   ```bash
    git clone https://github.com/M1tsumi/PawSharp.git
   cd PawSharp
   ```
2. **Create a feature branch**:
   ```bash
    git checkout -b feature/add-message-cache
   ```
3. **Build the solution**:
   ```bash
   dotnet build
   ```
4. **Run tests**:
   ```bash
   dotnet test
   ```

## Code Style & Standards

### .NET Conventions
- Follow the provided `.editorconfig` file
- Use meaningful variable and method names
- Keep methods focused on single responsibilities
- Prefer async/await over synchronous operations
- Use `var` when the type is obvious

### PawSharp-Specific Guidelines
- **Always throw exceptions** instead of returning null/empty collections
- **Use dependency injection** for all services and components
- **Implement proper logging** with structured logging
- **Add XML documentation** to all public APIs
- **Write comprehensive unit tests** for new features
- **Handle errors gracefully** with typed exceptions

### Code Structure
```
src/
├── PawSharp.Core/          # Core entities, exceptions, validation
├── PawSharp.API/           # REST client and rate limiting
├── PawSharp.Gateway/       # WebSocket gateway and events
├── PawSharp.Cache/         # Caching providers
├── PawSharp.Client/        # High-level client
├── PawSharp.Interactions/  # Slash commands and components
├── PawSharp.Commands/      # Command framework
├── PawSharp.Interactivity/ # Interactive features
└── PawSharp.Voice/         # Voice support (experimental)
```

## Development Workflow

### 1. Choose an Issue
- Check [GitHub Issues](https://github.com/M1tsumi/PawSharp/issues) for open tasks
- Look for "good first issue" or "help wanted" labels
- Comment on the issue to indicate you're working on it

### 2. Implement Changes
- Write clean, testable code
- Add unit tests for new functionality
- Update documentation as needed
- Ensure all tests pass: `dotnet test`

### 3. Testing
- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test component interactions
- **Manual Testing**: Test with a real Discord bot
- **Performance Testing**: Use benchmarks for performance-critical code

### 4. Documentation
- Update XML comments for public APIs
- Add examples for new features
- Update README and docs as needed
- Ensure code samples compile and work

### 5. Commit & Push
```bash
git add .
git commit -m "feat: add new feature

- Description of changes
- Related issue: #123"
git push origin feature/add-message-cache
```

### 6. Create Pull Request
- Use a descriptive title and detailed description
- Reference related issues
- Request review from maintainers
- Address review feedback

## Architectural Guidelines

### Dependency Injection
All components should support DI:

```csharp
// ✅ Good
public class MyService
{
    private readonly ILogger<MyService> _logger;
    private readonly ICacheProvider _cache;

    public MyService(ILogger<MyService> logger, ICacheProvider cache)
    {
        _logger = logger;
        _cache = cache;
    }
}
```

### Error Handling
Use typed exceptions and handle them appropriately:

```csharp
// ✅ Good
public async Task SendMessageAsync(ulong channelId, string content)
{
    if (string.IsNullOrWhiteSpace(content))
        throw new ValidationException("Content cannot be empty");

    if (content.Length > 2000)
        throw new ValidationException("Content exceeds 2000 characters");

    // Implementation
}
```

### Logging
Use structured logging with appropriate levels:

```csharp
// ✅ Good
_logger.LogInformation("Bot connected to {GuildCount} guilds", guilds.Count);
_logger.LogWarning("Rate limit hit for endpoint {Endpoint}, retrying in {RetryAfter}s",
    endpoint, retryAfter);
_logger.LogError(ex, "Failed to process message {MessageId}", messageId);
```

### Async Programming
All I/O operations should be async:

```csharp
// ✅ Good
public async Task<Message> SendMessageAsync(CreateMessageRequest request)
{
    var response = await _httpClient.PostAsync(endpoint, content);
    return await DeserializeAsync<Message>(response);
}

// ❌ Bad
public Message SendMessage(CreateMessageRequest request)
{
    var response = _httpClient.PostAsync(endpoint, content).Result;
    return Deserialize<Message>(response.Result);
}
```

## Testing Guidelines

### Unit Tests
- Test public APIs and error conditions
- Use mocking for external dependencies
- Follow AAA pattern (Arrange, Act, Assert)
- Name tests descriptively: `MethodName_Condition_ExpectedResult`

```csharp
[Fact]
public async Task CreateMessageAsync_ValidRequest_ReturnsMessage()
{
    // Arrange
    var request = new CreateMessageRequest { Content = "Test" };
    var mockRestClient = new Mock<IDiscordRestClient>();
    mockRestClient.Setup(x => x.CreateMessageAsync(It.IsAny<ulong>(), It.IsAny<CreateMessageRequest>()))
        .ReturnsAsync(new Message { Id = 123, Content = "Test" });

    // Act
    var result = await mockRestClient.Object.CreateMessageAsync(456, request);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test", result.Content);
}
```

### Integration Tests
- Test real component interactions
- Use test databases/cache providers
- Clean up after tests

## Documentation Standards

### XML Comments
```csharp
/// <summary>
/// Sends a message to a Discord channel.
/// </summary>
/// <param name="channelId">The ID of the channel to send to.</param>
/// <param name="request">The message creation request.</param>
/// <returns>The created message.</returns>
/// <exception cref="ValidationException">Thrown when request is invalid.</exception>
/// <exception cref="RateLimitException">Thrown when rate limited.</exception>
public async Task<Message> CreateMessageAsync(ulong channelId, CreateMessageRequest request)
```

### README Updates
- Keep installation instructions current
- Update feature lists for new capabilities
- Add migration notes for breaking changes

## Performance Considerations

- **Caching**: Use appropriate cache strategies
- **Memory Management**: Avoid memory leaks in long-running bots
- **Rate Limiting**: Respect Discord's limits and implement backoff
- **Concurrency**: Use thread-safe collections and proper locking
- **Serialization**: Optimize JSON serialization for large payloads

## Security Best Practices

- **Token Handling**: Never log or expose bot tokens
- **Input Validation**: Validate all user input before processing
- **Permission Checks**: Verify bot permissions before API calls
- **Error Messages**: Don't expose sensitive information in errors
- **Dependencies**: Keep NuGet packages updated and secure

## Release Process

### Versioning
PawSharp follows [Semantic Versioning](https://semver.org/):
- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

### Alpha/Beta Releases
- Use pre-release tags: `1.0.0-alpha1`, `1.0.0-beta1`
- Clearly document experimental features
- Provide migration guides for breaking changes

## Getting Help

- **Issues**: For bugs and feature requests
- **Discussions**: For questions and ideas
- **Discord**: Community chat
- **Documentation**: Check docs/ and examples/

## Recognition

Contributors are recognized in:
- CHANGELOG.md for significant contributions
- GitHub's contributor insights
- Future contributor acknowledgments

---

We appreciate your help in making PawSharp better for everyone! Happy coding! 🚀
