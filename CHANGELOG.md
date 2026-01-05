# Changelog

All notable changes to PawSharp will be documented in this file.

## [Unreleased]

### Added
- Comprehensive README with honest project status assessment
- Nullable reference type support across all projects
- Fixed compilation errors in EventDispatcher

### Changed
- Updated README to reflect actual implementation status
- Clarified which features are working, partial, or not implemented
- Added roadmap for v1.0 release

### Fixed
- Fixed Guild constructor issue (removed unnecessary constructor)
- Fixed EventDispatcher syntax error (duplicate catch block)
- Added #nullable enable to multiple entity and model files
- Resolved 28 compilation errors

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
