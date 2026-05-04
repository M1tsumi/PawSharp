#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PawSharp.Client;
using PawSharp.Core.Entities;
using PawSharp.Interactivity.Extensions;

namespace PawSharp.Interactivity.Builders;

/// <summary>
/// Builder for creating complex interactivity flows with chained operations.
/// </summary>
public class InteractivityFlowBuilder
{
    private readonly DiscordClient _client;
    private readonly Channel _channel;
    private readonly User _user;
    private readonly TimeSpan? _timeout;
    private readonly CancellationToken _cancellationToken;
    private readonly List<Func<Task<InteractivityResult<object>>>> _steps;

    /// <summary>
    /// Initializes a new instance of the <see cref="InteractivityFlowBuilder"/> class.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="channel">The channel to use for the flow.</param>
    /// <param name="user">The user to interact with.</param>
    /// <param name="timeout">Optional timeout for the flow.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public InteractivityFlowBuilder(
        DiscordClient client,
        Channel channel,
        User user,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        _client = client;
        _channel = channel;
        _user = user;
        _timeout = timeout;
        _cancellationToken = cancellationToken;
        _steps = new List<Func<Task<InteractivityResult<object>>>>();
    }

    /// <summary>
    /// Adds a message input step to the flow.
    /// </summary>
    /// <param name="prompt">The prompt message to display.</param>
    /// <param name="validator">Optional validator function for the input.</param>
    /// <param name="errorMessage">Error message to show on validation failure.</param>
    /// <param name="maxAttempts">Maximum number of attempts for validation.</param>
    /// <returns>The builder for chaining.</returns>
    public InteractivityFlowBuilder WithMessageInput(
        string prompt,
        Func<string, bool>? validator = null,
        string? errorMessage = null,
        int maxAttempts = 3)
    {
        _steps.Add(async () =>
        {
            if (validator == null)
            {
                var result = await _channel.GetInputAsync(_client, _user, prompt, _timeout, _cancellationToken);
                return new InteractivityResult<object> { TimedOut = result.TimedOut, Result = result.Result };
            }
            else
            {
                var result = await _channel.GetValidInputAsync(
                    _client,
                    _user,
                    prompt,
                    validator,
                    errorMessage ?? "Invalid input. Please try again.",
                    maxAttempts,
                    _timeout,
                    _cancellationToken);
                return new InteractivityResult<object> { TimedOut = result.TimedOut, Result = result.Result };
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a confirmation step to the flow.
    /// </summary>
    /// <param name="question">The question to ask.</param>
    /// <param name="yesLabel">Label for the Yes button.</param>
    /// <param name="noLabel">Label for the No button.</param>
    /// <returns>The builder for chaining.</returns>
    public InteractivityFlowBuilder WithConfirmation(
        string question,
        string yesLabel = "Yes",
        string noLabel = "No")
    {
        _steps.Add(async () =>
        {
            var result = await _channel.ConfirmAsync(_client, question, _user, yesLabel, noLabel, _timeout, _cancellationToken);
            return new InteractivityResult<object> { TimedOut = result.TimedOut, Result = result.Result };
        });
        return this;
    }

    /// <summary>
    /// Adds a custom step to the flow.
    /// </summary>
    /// <param name="step">The custom step function.</param>
    /// <returns>The builder for chaining.</returns>
    public InteractivityFlowBuilder WithCustomStep(Func<Task<InteractivityResult<object>>> step)
    {
        _steps.Add(step);
        return this;
    }

    /// <summary>
    /// Executes the flow and returns the results of all steps.
    /// </summary>
    /// <returns>A list of results from each step.</returns>
    public async Task<List<InteractivityResult<object>>> ExecuteAsync()
    {
        var results = new List<InteractivityResult<object>>();

        foreach (var step in _steps)
        {
            var result = await step();
            results.Add(result);

            // Stop if a step times out
            if (result.TimedOut)
                break;
        }

        return results;
    }

    /// <summary>
    /// Executes the flow and returns the results of all steps with typed values.
    /// </summary>
    /// <typeparam name="T">The type of results.</typeparam>
    /// <returns>A list of typed results from each step.</returns>
    public async Task<List<InteractivityResult<T>>> ExecuteAsync<T>()
    {
        var results = new List<InteractivityResult<T>>();

        foreach (var step in _steps)
        {
            var result = await step();
            results.Add(new InteractivityResult<T>
            {
                TimedOut = result.TimedOut,
                Result = result.Result is T t ? t : default
            });

            // Stop if a step times out
            if (result.TimedOut)
                break;
        }

        return results;
    }
}

/// <summary>
/// Extension methods for creating interactivity flows.
/// </summary>
public static class InteractivityFlowExtensions
{
    /// <summary>
    /// Creates a new interactivity flow builder.
    /// </summary>
    /// <param name="channel">The channel to use for the flow.</param>
    /// <param name="client">The Discord client.</param>
    /// <param name="user">The user to interact with.</param>
    /// <param name="timeout">Optional timeout for the flow.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new interactivity flow builder.</returns>
    public static InteractivityFlowBuilder CreateFlow(
        this Channel channel,
        DiscordClient client,
        User user,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        return new InteractivityFlowBuilder(client, channel, user, timeout, cancellationToken);
    }
}
