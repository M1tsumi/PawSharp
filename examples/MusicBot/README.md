# Music Bot Example

A basic music bot example demonstrating PawSharp's command framework and interactivity features.

## Features

- **Play Command**: Play music from URLs or search queries
- **Queue Management**: View and manage the music queue
- **Volume Control**: Adjust playback volume
- **Now Playing**: Display current track information
- **Stop Command**: Stop music playback

## Setup

1. Set your bot token as an environment variable:
   ```bash
   export DISCORD_TOKEN=your-bot-token-here
   ```

2. Run the bot:
   ```bash
   dotnet run
   ```

## Commands

- `!music play <query>` - Play music
- `!music stop` - Stop playback
- `!music queue` - Show queue
- `!music volume <0-100>` - Set volume
- `!music np` or `!music nowplaying` - Show current track

## Note

This example demonstrates the command structure and UI. Actual audio playback requires implementing the voice features, which are currently experimental in PawSharp.

## Extending

To add real music functionality:
1. Implement voice connections using `PawSharp.Voice`
2. Add audio streaming and decoding
3. Integrate with a music service API (YouTube, Spotify, etc.)
4. Add queue management and audio mixing