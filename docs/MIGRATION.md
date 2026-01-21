# PawSharp Migration Guide

This guide helps you upgrade between major PawSharp versions, highlighting breaking changes and migration steps.

## 0.5.x → 1.0.0 (planned)
- **API surface will stabilize**: Expect fewer breaking changes after 1.0.0.
- **Sharding and distributed caching**: New features may require configuration changes.
- **Exception handling**: All REST methods throw exceptions instead of returning null.
- **DI and configuration**: Use .NET DI for all services; see updated examples.

## 0.5.0-alpha7 Breaking Changes
- `HeartbeatManager` constructor now requires `ILogger` parameter (can be null).
- `GatewayClient.ConnectAsync()` now validates state and prevents reconnection while already connected.

## General Migration Tips
- Review the [CHANGELOG.md](../CHANGELOG.md) for all breaking changes.
- Update your code to use new exception types and DI patterns.
- Test your bot thoroughly after upgrading.

---

For help, open an issue or discussion.