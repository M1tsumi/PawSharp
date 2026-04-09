# Moderation Bot Example

This example demonstrates how to build a Discord moderation bot using PawSharp. The bot includes features like automatic content filtering, user warnings, muting, kicking, and banning.

## Features

- **Content Filtering**: Automatically detects and removes messages containing banned words
- **Spam Detection**: Basic rule-based spam detection that can be extended with message-frequency checks or custom heuristics
- **Moderation Commands**:
  - `!mod warn <user>` - Warn a user
  - `!mod mute <user>` - Mute a user temporarily
  - `!mod kick <user>` - Kick a user from the server
  - `!mod ban <user>` - Ban a user from the server
  - `!mod warnings <user>` - Show warnings for a user
- **Auto-Moderation**: Users with too many warnings are automatically banned
- **Welcome Messages**: Greets new members when they join

## Setup

1. Create a Discord application and bot at https://discord.com/developers/applications
2. Copy the bot token
3. Set the `DISCORD_TOKEN` environment variable:
   ```bash
   export DISCORD_TOKEN="your_bot_token_here"
   ```
4. Invite the bot to your server with the following permissions:
   - Send Messages
   - Read Messages
   - Manage Messages
   - Kick Members
   - Ban Members
   - Manage Roles (for muting functionality)

## Running the Bot

```bash
dotnet run
```

## Configuration

The bot includes several configurable settings in the `ModerationSystem` class:

- `BannedWords`: Array of words to filter
- `MaxWarnings`: Number of warnings before auto-ban
- `MuteDuration`: How long mutes last

## Extending the Bot

This example can be extended with:

- More sophisticated spam detection using message frequency analysis
- Custom role-based permissions
- Audit logging to external databases
- Integration with external moderation services
- Scheduled tasks for cleaning up old data

## Security Notes

- In a production bot, implement proper permission checking
- Store sensitive data securely (use environment variables, not hardcoded values)
- Rate limit your own API calls to avoid hitting Discord's limits
- Log moderation actions for accountability

## Related Examples

- [Basic Bot](../BasicBot/) - Simple bot with basic commands
- [Advanced Bot](../AdvancedBot/) - Bot with caching and metrics