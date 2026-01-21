# Dashboard Bot Example

A comprehensive dashboard bot example showcasing PawSharp's interaction system, slash commands, and embed building.

## Features

- **Server Info**: Display detailed server statistics
- **User Info**: Show user profiles with server-specific information
- **Ping Command**: Measure bot latency
- **Bot Stats**: Display bot-wide statistics
- **Interactive Embeds**: Rich embeds with colors and thumbnails

## Setup

1. Set your bot token as an environment variable:
   ```bash
   export DISCORD_TOKEN=your-bot-token-here
   ```

2. Run the bot:
   ```bash
   dotnet run
   ```

3. Register slash commands (run once):
   The bot will automatically register slash commands when it starts.

## Commands

- `/serverinfo` - Server information dashboard
- `/userinfo [user]` - User profile (defaults to command user)
- `/ping` - Bot latency check
- `/stats` - Bot statistics overview

## Features Demonstrated

- **Slash Commands**: Modern Discord command interface
- **Embed Building**: Rich message formatting with colors and images
- **User Resolution**: Handling optional user parameters
- **Guild Context**: Server-specific information and member data
- **Ephemeral Responses**: Private command responses
- **Follow-up Messages**: Multi-message interactions

## Extending

Add more dashboard features:
- Channel statistics and management
- Role management interface
- Moderation logs and actions
- Custom welcome messages
- Server configuration panels
- User activity tracking