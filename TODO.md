# PawSharp v0.5.0-alpha4 Development Todo List

## Completed Tasks
- [x] Audited repository for version strings and updated to alpha4
- [x] Updated CHANGELOG.md with planned header and detailed items for alpha4
- [x] Updated README.md status to Alpha 4
- [x] Updated src/Directory.Build.props version to 0.1.0-alpha4
- [x] Mapped implemented features and defined stable feature list in changelog
- [x] Scaffolded REST client methods for Webhooks and Scheduled Events
- [x] Scaffolded placeholder tests for Webhooks and Scheduled Events
- [x] Validated builds multiple times (all clean with 0 warnings/errors)

## In Progress
- [ ] Defining stable feature list (drafted in changelog, but may need refinement)

## Pending Tasks

### High Priority (Immediate Next Steps)
- [x] Implement CreateWebhookAsync method in PawSharp.API/Clients/RestClient.cs
- [x] Implement GetChannelWebhooksAsync method in PawSharp.API/Clients/RestClient.cs
- [x] Implement GetGuildWebhooksAsync method in PawSharp.API/Clients/RestClient.cs
- [x] Implement GetWebhookAsync method in PawSharp.API/Clients/RestClient.cs
- [x] Implement ModifyWebhookAsync method in PawSharp.API/Clients/RestClient.cs
- [x] Implement DeleteWebhookAsync method in PawSharp.API/Clients/RestClient.cs
- [x] Implement ExecuteWebhookAsync method in PawSharp.API/Clients/RestClient.cs
- [x] Add real test logic to Webhooks tests in tests/PawSharp.API.Tests/
- [x] Implement CreateScheduledEventAsync method in PawSharp.API/Clients/RestClient.cs
- [x] Implement GetScheduledEventsAsync method in PawSharp.API/Clients/RestClient.cs
- [x] Implement GetScheduledEventAsync method in PawSharp.API/Clients/RestClient.cs
- [x] Implement ModifyScheduledEventAsync method in PawSharp.API/Clients/RestClient.cs
- [x] Implement DeleteScheduledEventAsync method in PawSharp.API/Clients/RestClient.cs
- [x] Implement GetScheduledEventUsersAsync method in PawSharp.API/Clients/RestClient.cs
- [x] Add real test logic to Scheduled Events tests in tests/PawSharp.API.Tests/

### Medium Priority
- [x] Implement Audit Logs REST methods (GetGuildAuditLogsAsync)
- [x] Implement Auto Moderation REST methods (CRUD operations)
- [ ] Add Gateway event support for scheduled events (GUILD_SCHEDULED_EVENT_* events)
- [ ] Add Gateway event support for auto-moderation actions
- [ ] Improve WebSocket reliability (Identify/Resume handling, heartbeat ACK, invalid_session, exponential backoff)
- [ ] Enhance rate limiting with route-specific buckets and 429 Retry-After handling
- [ ] Add opt-in integration tests for live Discord calls
- [ ] Add additional unit tests for new models and clients
- [ ] Update CI to run unit tests by default

### Low Priority
- [ ] Update documentation in docs/ for new features
- [ ] Update examples/ with end-to-end examples for Webhooks, Scheduled Events, and Auto Moderation
- [ ] Run full unit and integration tests
- [ ] Prepare final changelog entry for alpha4 release
- [ ] Update README.md with new feature documentation