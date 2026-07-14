# Polls

Learn how to create, vote on, and manage Discord polls with PawSharp.

## Table of Contents

1. [Overview](#overview)
2. [Poll Structure](#poll-structure)
3. [Creating Polls](#creating-polls)
4. [Layout Types](#layout-types)
5. [Getting Poll Results](#getting-poll-results)
6. [Ending Polls Early](#ending-polls-early)
7. [Voter Data](#voter-data)
8. [Gateway Events](#gateway-events)
9. [Complete Example](#complete-example)

---

## Overview

Discord polls let users vote on questions directly in chat. Polls are **attached to messages** and support:

- Up to 10 answers
- Duration up to 32 days (768 hours)
- Single or multi-select voting
- Optional emoji per answer
- Results after the poll expires

Polls are created via `CreateMessageRequest.Poll` and managed through dedicated REST endpoints.

---

## Poll Structure

```csharp
public class Poll
{
 public PollMedia Question { get; set; } = null!;
 public List<PollAnswer> Answers { get; set; } = new();
 public DateTimeOffset? Expiry { get; set; }
 public bool AllowMultiselect { get; set; }
 public PollLayoutType LayoutType { get; set; }
 public PollResults? Results { get; set; }
}
```

### Supporting Types

```csharp
public class PollMedia
{
 public string? Text { get; set; } // question: max 300 chars, answer: max 55 chars
 public Emoji? Emoji { get; set; }
}

public class PollAnswer
{
 public int? AnswerId { get; set; } // server-assigned
 public PollMedia PollMedia { get; set; } = null!;
}

public class PollResults
{
 public bool IsFinalized { get; set; }
 public List<PollAnswerCount> AnswerCounts { get; set; } = new();
}

public class PollAnswerCount
{
 public int Id { get; set; }
 public int Count { get; set; }
 public bool MeVoted { get; set; }
}
```

---

## Creating Polls

### Using the Fluent Builder (recommended)

```csharp
using PawSharp.API.Builders;

var createRequest = new CreateMessageRequest
{
 Poll = new PollBuilder()
 .WithQuestion("What is your favourite colour?")
 .AddAnswer("Red", emojiName: "")
 .AddAnswer("Blue", emojiName: "")
 .AddAnswer("Green", emojiName: "")
 .WithDuration(24) // hours
 .AllowMultiselect(false)
 .Build()
};

var msg = await client.Rest.CreateMessageAsync(channelId, createRequest);
Console.WriteLine($"Poll created in message {msg?.Id}");
```

### Using `CreatePollRequest` Directly

```csharp
var pollRequest = new CreatePollRequest
{
 Question = new PollMediaRequest { Text = "Best programming language?" },
 Answers = new List<PollAnswerRequest>
 {
 new() { PollMedia = new() { Text = "C#" } },
 new() { PollMedia = new() { Text = "Python" } },
 new() { PollMedia = new() { Text = "Rust", Emoji = new { name = "" } } },
 },
 Duration = 48,
 AllowMultiselect = true,
 LayoutType = 1,
};

var msg = await client.Rest.CreateMessageAsync(channelId, new()
{
 Content = "Cast your votes!",
 Poll = pollRequest,
});
```

### PollBuilder API

| Method | Description |
|--------|-------------|
| `WithQuestion(string)` | Sets the poll question (max 300 chars) |
| `AddAnswer(text)` | Adds an answer (max 55 chars) |
| `AddAnswer(text, emojiId)` | Adds an answer with a custom emoji |
| `AddAnswer(text, emojiName)` | Adds an answer with a unicode emoji |
| `WithDuration(hours)` | Sets duration (1-768 hours, default 24) |
| `AllowMultiselect(bool)` | Enables multi-select voting |
| `WithLayoutType(int)` | Sets layout type (default 1) |
| `Build()` | Returns the `CreatePollRequest` |

 Validation enforced by the builder:

| Rule | Limit |
|------|-------|
| Question length | 1-300 characters |
| Answer text | 1-55 characters |
| Number of answers | 1-10 |
| Duration | 1-768 hours (32 days) |

---

## Layout Types

```csharp
public enum PollLayoutType
{
 Default = 1
}
```

Currently Discord only supports `Default` (1). The answers are displayed vertically.

---

## Getting Poll Results

Poll results are embedded in the `Poll.Results` property of the **message object** after the poll expires.

### Reading Results After Expiry

```csharp
// Fetch the message after the poll has expired
var msg = await client.Rest.GetMessageAsync(channelId, messageId);

if (msg?.Poll?.Results?.IsFinalized == true)
{
 Console.WriteLine($"Question: {msg.Poll.Question.Text}");
 foreach (var answerCount in msg.Poll.Results.AnswerCounts)
 {
 var answer = msg.Poll.Answers.FirstOrDefault(a => a.AnswerId == answerCount.Id);
 var text = answer?.PollMedia.Text ?? "(unknown)";
 Console.WriteLine($" {text}: {answerCount.Count} votes {(answerCount.MeVoted ? "(you)" : "")}");
 }
}
```

 `Results` is only present when the poll has **finalized** (expired). Active polls have `Results = null`.

---

## Ending Polls Early

You can prematurely end a poll - this immediately finalizes it and makes results available:

```csharp
// Via DiscordClient convenience method
var endedMsg = await client.Rest.EndPollAsync(channelId, messageId);

// Via message extension
var endedMsg = await message.EndPollAsync(client);
```

When a poll is ended early:

- The `Expiry` is updated to the current time
- `Results` becomes available with `IsFinalized = true`
- No further votes are accepted

 Only the bot that created the poll can end it early.

---

## Voter Data

Get a list of users who voted for a specific answer:

```csharp
var voters = await client.Rest.GetAnswerVotersAsync(
 channelId,
 messageId,
 answerId: 1, // the answer_id from the poll
 limit: 25,
 after: null);

foreach (var user in voters ?? new())
{
 Console.WriteLine($"{user.Username} voted for answer 1");
}
```

 This endpoint is not available in all scenarios:

- It returns at most 100 voters per answer
- Pagination via `after` (user ID cursor)
- The bot must have created the poll or have `MESSAGE_HISTORY` permission

---

## Gateway Events

Listen for poll-related events via the low-level dispatcher:

```csharp
// Poll vote added
client.Gateway.Events.On<MessagePollVoteAddEvent>(
 "MESSAGE_POLL_VOTE_ADD", evt =>
{
 Console.WriteLine($"User {evt.UserId} voted on poll {evt.MessageId} answer {evt.AnswerId}");
});

// Poll vote removed
client.Gateway.Events.On<MessagePollVoteRemoveEvent>(
 "MESSAGE_POLL_VOTE_REMOVE", evt =>
{
 Console.WriteLine($"User {evt.UserId} removed vote on poll {evt.MessageId} answer {evt.AnswerId}");
});
```

---

## Complete Example

```csharp
using PawSharp.Client;
using PawSharp.API.Builders;
using PawSharp.API.Models;

var client = new PawSharpClientBuilder()
 .WithToken("Bot YOUR_TOKEN")
 .WithIntents(GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent)
 .Build();

client.OnMessageCreated(async msg =>
{
 if (msg.Content != "!poll") return;

 // Create a poll
 var pollRequest = new CreateMessageRequest
 {
 Content = " Please vote!",
 Poll = new PollBuilder()
 .WithQuestion("Which pet is best?")
 .AddAnswer("Dog", emojiName: "")
 .AddAnswer("Cat", emojiName: "")
 .AddAnswer("Hamster", emojiName: "")
 .WithDuration(1) // expires in 1 hour
 .Build()
 };

 var pollMsg = await client.Rest.CreateMessageAsync(msg.ChannelId, pollRequest);
 Console.WriteLine($"Poll created: {pollMsg?.Id}");
});

// Track votes in real time
client.Gateway.Events.On<MessagePollVoteAddEvent>(
 "MESSAGE_POLL_VOTE_ADD", evt =>
{
 Console.WriteLine($"Vote: user {evt.UserId} answered {evt.AnswerId} on {evt.MessageId}");
});

await client.ConnectAsync();
await Task.Delay(Timeout.Infinite);
```

---

**More guides:** [Messages](../guides/sending-messages.md#messages) | [Gateway](../guides/gateway.md) | [Components](./components.md)
