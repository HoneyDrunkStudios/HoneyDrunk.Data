// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Outbox.Serialization;
using HoneyDrunk.Transport.Abstractions;
using HoneyDrunk.Transport.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace HoneyDrunk.Data.Outbox.Dispatcher;

/// <summary>
/// Background service that polls the outbox for pending messages and publishes
/// them through HoneyDrunk.Transport abstractions.
/// </summary>
/// <remarks>
/// <para>
/// Each polling cycle creates a new DI scope, resolves <see cref="IOutboxReader"/>
/// and <see cref="ITransportPublisher"/>, processes a batch, then disposes the scope.
/// </para>
/// <para>
/// Messages are claimed under a time-bound lease. If this service crashes, the
/// lease expires and another instance reclaims the messages on the next poll cycle.
/// </para>
/// <para>
/// Failed messages are retried with exponential backoff up to
/// <see cref="OutboxDispatcherOptions.MaxRetryAttempts"/>. Messages exceeding
/// the retry limit are moved to <see cref="OutboxMessageStatus.DeadLetter"/>.
/// </para>
/// </remarks>
public sealed class OutboxDispatcherService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxDispatcherOptions> options,
    ILogger<OutboxDispatcherService> logger) : BackgroundService, IOutboxDispatcher
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly OutboxDispatcherOptions _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
    private readonly ILogger<OutboxDispatcherService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task DispatchPendingAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var reader = scope.ServiceProvider.GetRequiredService<IOutboxReader>();
        var publisher = scope.ServiceProvider.GetRequiredService<ITransportPublisher>();

        var messages = await reader.ClaimBatchAsync(_options.BatchSize, _options.LeaseDuration, cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        Log.ProcessingBatch(_logger, messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var envelope = BuildEnvelope(message);
                var destination = ResolveDestination(message);

                await publisher.PublishAsync(envelope, destination, cancellationToken);
                await reader.MarkDispatchedAsync(message.Id, cancellationToken);

                Log.MessageDispatched(_logger, message.Id, destination.Address);
            }
            catch (Exception ex)
            {
                await HandleFailureAsync(reader, message, ex, cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.StartupDelay > TimeSpan.Zero)
        {
            Log.WaitingBeforeFirstCycle(_logger, _options.StartupDelay);
            await Task.Delay(_options.StartupDelay, stoppingToken);
        }

        Log.DispatcherStarted(
            _logger,
            _options.PollInterval,
            _options.BatchSize,
            _options.MaxRetryAttempts);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
                await Task.Delay(_options.PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.DispatchCycleFailed(_logger, ex, _options.ErrorDelay);
                await SafeDelay(_options.ErrorDelay, stoppingToken);
            }
        }

        Log.DispatcherStopped(_logger);
    }

    private static TransportEnvelope BuildEnvelope(OutboxMessage message)
    {
        var headers = OutboxHeaderSerializer.Deserialize(message.Headers)
            ?? [];

        // Strip outbox-internal headers before forwarding to Transport
        headers.Remove(OutboxHeaderNames.Destination);

        return new TransportEnvelope
        {
            MessageId = message.Id.ToString(),
            MessageType = message.Type,
            CorrelationId = message.CorrelationId,
            TenantId = message.TenantId,
            Timestamp = message.OccurredAt,
            Payload = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(message.Payload)),
            Headers = headers,
        };
    }

    private static async Task SafeDelay(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Swallow — host is shutting down
        }
    }

    private IEndpointAddress ResolveDestination(OutboxMessage message)
    {
        var headers = OutboxHeaderSerializer.Deserialize(message.Headers);
        var destination = headers?.GetValueOrDefault(OutboxHeaderNames.Destination)
            ?? _options.DefaultDestination;

        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new InvalidOperationException(
                $"No destination for outbox message {message.Id}. " +
                $"Set the '{OutboxHeaderNames.Destination}' header or configure " +
                $"{nameof(OutboxDispatcherOptions)}.{nameof(OutboxDispatcherOptions.DefaultDestination)}.");
        }

        return EndpointAddress.Create(name: destination, address: destination);
    }

    private async Task HandleFailureAsync(
        IOutboxReader reader,
        OutboxMessage message,
        Exception ex,
        CancellationToken cancellationToken)
    {
        var newRetryCount = message.RetryCount + 1;

        if (newRetryCount >= _options.MaxRetryAttempts)
        {
            Log.MessageDeadLettered(
                _logger,
                ex,
                message.Id,
                newRetryCount);

            await reader.DeadLetterAsync(message.Id, ex.Message, cancellationToken);
            return;
        }

        var delay = CalculateBackoff(newRetryCount);
        var nextAttemptAt = DateTimeOffset.UtcNow.Add(delay);

        Log.MessageFailed(
            _logger,
            ex,
            message.Id,
            newRetryCount,
            _options.MaxRetryAttempts,
            nextAttemptAt);

        await reader.ReleaseForRetryAsync(message.Id, newRetryCount, nextAttemptAt, ex.Message, cancellationToken);
    }

    private TimeSpan CalculateBackoff(int retryCount)
    {
        var seconds = _options.BaseRetryDelay.TotalSeconds * Math.Pow(2, retryCount - 1);
        var capped = Math.Min(seconds, _options.MaxRetryDelay.TotalSeconds);

        return TimeSpan.FromSeconds(capped);
    }
}
