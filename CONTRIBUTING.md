# Contributing to PawSharp

Thank you for your interest in contributing to PawSharp! This document provides guidelines and information for contributors.

## 🎯 Priority Areas

We especially welcome contributions in these areas:

### High Priority
1. **Testing** - Write unit and integration tests (current coverage: ~15%)
2. **Bug Fixes** - Identify and fix issues in existing code
3. **Documentation** - Improve XML docs and add usage examples
4. **Error Handling** - Add comprehensive error handling and logging

### Medium Priority
5. **Feature Completion** - Complete partially-implemented features (interactions, components)
6. **Performance** - Optimize memory usage and response times
7. **Examples** - Create real-world bot examples

### Low Priority
8. **New Features** - Implement planned but not-started features

## 🛠️ Development Setup

1. **Prerequisites**
   - .NET 8.0 SDK or later
   - Git
   - A code editor (VS Code, Visual Studio, or Rider recommended)

2. **Clone and Build**
   ```bash
   git clone <repository-url>
   cd PawSharp
   dotnet restore
   dotnet build
   ```

3. **Run Tests**
   ```bash
   dotnet test
   ```

4. **Verify Your Environment**
   - Ensure all projects compile successfully
   - Check that existing tests pass

## 📝 Code Style

- Follow C# naming conventions (PascalCase for public members, camelCase for private fields)
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Enable nullable reference types (`#nullable enable`) in all new files
- Keep methods focused and reasonably sized (< 50 lines when possible)
- Use async/await consistently - don't block async calls

## 🔍 Pull Request Process

1. **Fork the Repository**
   - Create a fork and clone it to your machine

2. **Create a Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```
   or
   ```bash
   git checkout -b fix/bug-description
   ```

3. **Make Your Changes**
   - Write clean, well-documented code
   - Add tests for new functionality
   - Ensure all existing tests still pass

4. **Commit Your Changes**
   - Write clear, descriptive commit messages
   - Use conventional commit format when possible:
     ```
     feat: add support for forum channels
     fix: resolve rate limit issue in REST client
     docs: improve README examples
     test: add tests for message caching
     ```

5. **Push and Create PR**
   ```bash
   git push origin feature/your-feature-name
   ```
   - Open a pull request with a clear description
   - Reference any related issues
   - Be responsive to review feedback

## 🧪 Testing Guidelines

- **Unit Tests**: Test individual methods and classes in isolation
- **Integration Tests**: Test interactions between components
- **Example**: Place test files in appropriate test projects:
  - `tests/PawSharp.Core.Tests/` for core functionality
  - `tests/PawSharp.API.Tests/` for REST API tests
  - etc.

Example test structure:
```csharp
[TestClass]
public class MessageCacheTests
{
    [TestMethod]
    public void CacheMessage_StoresMessage_Successfully()
    {
        // Arrange
        var cache = new MemoryCacheProvider();
        var message = new Message { Id = 12345, Content = "Test" };
        
        // Act
        cache.CacheMessage(message);
        var retrieved = cache.GetMessage(12345);
        
        // Assert
        Assert.IsNotNull(retrieved);
        Assert.AreEqual("Test", retrieved.Content);
    }
}
```

## 📚 Documentation

- Add XML documentation to all public APIs:
  ```csharp
  /// <summary>
  /// Retrieves a cached message by its ID.
  /// </summary>
  /// <param name="messageId">The snowflake ID of the message.</param>
  /// <returns>The cached message, or null if not found.</returns>
  public Message? GetMessage(ulong messageId)
  ```

- Update the README if you add new features
- Add examples in the `examples/` directory for complex features

## ⚠️ Important Notes

- **Breaking Changes**: Avoid breaking changes unless absolutely necessary (we're in alpha, but still be mindful)
- **Discord API**: Always reference the official [Discord API docs](https://discord.com/developers/docs) for accuracy
- **Dependencies**: Minimize new dependencies - discuss in an issue first
- **License**: All contributions will be licensed under Apache 2.0

## 🐛 Bug Reports

When reporting bugs, please include:
- A clear, descriptive title
- Steps to reproduce the issue
- Expected behavior vs. actual behavior
- Code samples if applicable
- .NET version and operating system
- Any relevant logs or error messages

## 💡 Feature Requests

For new features:
- Check if it's already on the roadmap in README.md
- Explain the use case and why it's valuable
- Provide examples of how it would be used
- Consider whether it fits with PawSharp's philosophy (modular, transparent, efficient)

## 🤔 Questions?

- Open a GitHub Discussion for general questions
- Open an issue for bugs or feature requests
- Tag maintainers in PRs if you need feedback

## 📜 Code of Conduct

- Be respectful and constructive
- Help others learn and grow
- Focus on what's best for the project
- Assume good intentions

---

Thank you for contributing to PawSharp! 🐾
