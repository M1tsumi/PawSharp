# Changelog

All notable changes to PawSharp will be documented in this file.

## [Unreleased]

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
