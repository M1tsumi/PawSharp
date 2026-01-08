# PawSharp.Client

High-level Discord client for the PawSharp API wrapper.

## Features

- Unified client interface
- Combines REST API and Gateway functionality
- Dependency injection support
- Easy-to-use bot development

## Installation

```bash
dotnet add package PawSharp.Client --version 0.1.0-alpha4
```

## Usage

```csharp
var services = new ServiceCollection();
services.AddPawSharp(options => {
    options.Token = "your-bot-token";
});

var client = services.BuildServiceProvider().GetRequiredService<DiscordClient>();
await client.ConnectAsync();
```