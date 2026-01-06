# Changelog

All notable changes to PawSharp will be documented in this file.

## [Unreleased]

### Added
- Expanded REST API coverage with 15+ new endpoints for users, messages, channels, and guilds
- Added support for 9 new gateway events (TYPING_START, MESSAGE_REACTION_*, PRESENCE_UPDATE, etc.)
- New entity models for Bans, Invites, and related structures
- Enhanced message operations (pinning, typing indicators, advanced pagination)
- Guild management features (ban/unban users, invite management)
- Channel permission management

### Changed
- Updated feature documentation to reflect expanded API coverage

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
