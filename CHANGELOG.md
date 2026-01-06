# Changelog

All notable changes to PawSharp will be documented in this file.

## [Unreleased]

### Planned for v0.5.0-alpha4
- REST API endpoints for webhooks, audit logs, scheduled events, and auto moderation
- Gateway event handling for new entity types
- Enhanced WebSocket connection reliability and session resumption
- Comprehensive integration tests for new features

## [0.5.0-alpha3] - 2026-01-05

### Added
- **Webhook Support**: Complete webhook entity models with all webhook types, webhook events, and metadata
- **Audit Log System**: Comprehensive audit log entities with all entry types, change tracking, and optional entries
- **Scheduled Events**: Full guild scheduled event support with all privacy levels, entity types, and user subscriptions
- **Auto Moderation**: Complete auto moderation rules, triggers, actions, and keyword preset types
- **Stage Instances**: Stage instance management with privacy levels and discoverability settings
- **Enhanced Invite System**: Complete invite entities with target types, stage instance data, and scheduled event integration
- **Voice States**: Voice connection state tracking and user voice status management
- **Presence System**: User presence, activities, and client status information with rich activity metadata
- **Application Management**: Discord application entities with team support, install parameters, and OAuth2 integration
- **Guild Templates**: Guild template system with snapshot data and guild configuration preservation
- **Sticker Support**: Sticker and sticker pack entities with all format types and guild sticker management
- **Soundboard Sounds**: Soundboard sound entities with emoji associations and availability tracking
- **Monetization System**: Entitlement, SKU, and subscription entities for Discord marketplace integration
- **OAuth2 Support**: OAuth2 application, token, and authorization information entities
- **Guild Enums**: Missing guild-related enums (VerificationLevel, DefaultMessageNotificationLevel, ExplicitContentFilterLevel, SystemChannelFlags)

### Changed
- Enhanced entity model coverage to ~95% of Discord API v10 entities
- Improved type safety across all new entity models with comprehensive nullable reference types
- Added proper JSON serialization attributes and Snowflake converters for all new entities

### Technical Details
- Added 17 new entity files with complete Discord API v10 coverage
- Implemented comprehensive enum definitions for all Discord API enumerations
- Maintained backwards compatibility with existing v0.5.0-alpha2 features
- All entities include proper XML documentation and serialization support

## [0.5.0-alpha2] - 2026-01-05

### Added
- **Message Components Support**: Full implementation of Discord message components including buttons, select menus, and action rows
- **Slash Commands**: Complete slash command system with command registration, data parsing, and interaction handling
- **Thread & Forum Support**: Comprehensive thread management with creation, joining/leaving, member management, and archived thread retrieval
- **Application Commands API**: Full REST API coverage for global and guild-specific application commands
- **Enhanced Interaction System**: Improved interaction data models with proper type safety and component support
- **Thread Gateway Events**: Added THREAD_CREATE, THREAD_UPDATE, THREAD_DELETE, THREAD_LIST_SYNC, THREAD_MEMBER_UPDATE, and THREAD_MEMBERS_UPDATE events
- **Forum Channel Features**: Support for forum tags, default reactions, and forum-specific channel properties

### Changed
- Updated InteractionCreateEvent to use proper InteractionData model instead of generic object
- Enhanced Channel entity with thread and forum-specific properties
- Improved type safety across all component and interaction models

### Fixed
- Resolved duplicate class definitions between API and Core namespaces
- Fixed Thread entity inheritance issues with proper `new` keyword usage
- Added missing `#nullable enable` directives to interface files

### Technical Details
- Added 15+ new REST API endpoints for application commands and thread management
- Implemented 6 new gateway events for thread operations
- Created comprehensive component models with full type safety
- Maintained backwards compatibility with existing v0.5.0-alpha1 features

## [0.5.0-alpha1] - 2026-01-05

### Added
- Official support declaration for v0.5.0-alpha1 with clear feature set
- Comprehensive documentation of supported REST API endpoints
- Clear distinction between supported and unsupported features
- Updated README with stable alpha release information

### Changed
- Clarified project status as stable alpha release
- Updated feature categorization to distinguish officially supported vs experimental features

### Fixed
- Resolved compilation warnings for nullable reference types
- Improved documentation accuracy

### Known Issues
- 61 nullable reference type warnings remain (mostly in interface files)
- Test coverage is minimal (~15%)
- Some advanced features need more real-world testing

## [0.5.0-alpha] - 2026-01-05

### Initial Alpha Release
- Basic gateway connection and event handling
- REST API client with rate limiting
- Entity caching system
- Core entity models (User, Guild, Channel, Message, Role, Member)
- Dependency injection support
- Sharding support (basic)
- Slash command builders (partial)

---

**Note**: This project is in alpha status. Breaking changes may occur between releases.
