// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Outbox;
using HoneyDrunk.Data.Outbox.Dispatcher;
using HoneyDrunk.Data.Outbox.Serialization;
using HoneyDrunk.Transport.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute.ExceptionExtensions;

namespace HoneyDrunk.Data.Tests.Outbox.Dispatcher;

public sealed class OutboxDispatcherServiceTests : IDisposable
{
    private readonly IOutboxReader _reader;
    private readonly ITransportPublisher _publisher;
    private readonly OutboxDispatcherOptions _options;
    private readonly OutboxDispatcherService _sut;

    public OutboxDispatcherServiceTests()
    {
        _reader = Substitute.For<IOutboxReader>();
        _publisher = Substitute.For<ITransportPublisher>();
        _options = new OutboxDispatcherOptions
        {
            DefaultDestination = "default-topic",
            LeaseDuration = TimeSpan.FromMinutes(5),
            MaxRetryAttempts = 3,
            BaseRetryDelay = TimeSpan.FromSeconds(1),
            MaxRetryDelay = TimeSpan.FromMinutes(5),
        };

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IOutboxReader)).Returns(_reader);
        serviceProvider.GetService(typeof(ITransportPublisher)).Returns(_publisher);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        _sut = new OutboxDispatcherService(
            scopeFactory,
            Options.Create(_options),
            NullLogger<OutboxDispatcherService>.Instance);
    }

    public void Dispose() => _sut.Dispose();

    [Fact]
    public async Task DispatchPendingAsync_WithNoMessages_DoesNotPublish()
    {
        _reader.ClaimBatchAsync(Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await _sut.DispatchPendingAsync();

        await _publisher.DidNotReceive().PublishAsync(
            Arg.Any<ITransportEnvelope>(),
            Arg.Any<IEndpointAddress>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchPendingAsync_PublishesMessageAndMarksDispatched()
    {
        var message = CreatePendingMessage();
        _reader.ClaimBatchAsync(Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns([message]);

        await _sut.DispatchPendingAsync();

        await _publisher.Received(1).PublishAsync(
            Arg.Is<ITransportEnvelope>(e => e.MessageType == "OrderPlaced"),
            Arg.Any<IEndpointAddress>(),
            Arg.Any<CancellationToken>());

        await _reader.Received(1).MarkDispatchedAsync(message.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchPendingAsync_UsesDestinationFromHeaders()
    {
        var message = CreatePendingMessage(destination: "orders-topic");
        _reader.ClaimBatchAsync(Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns([message]);

        await _sut.DispatchPendingAsync();

        await _publisher.Received(1).PublishAsync(
            Arg.Any<ITransportEnvelope>(),
            Arg.Is<IEndpointAddress>(d => d.Address == "orders-topic"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchPendingAsync_FallsBackToDefaultDestination()
    {
        var message = CreatePendingMessage();
        _reader.ClaimBatchAsync(Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns([message]);

        await _sut.DispatchPendingAsync();

        await _publisher.Received(1).PublishAsync(
            Arg.Any<ITransportEnvelope>(),
            Arg.Is<IEndpointAddress>(d => d.Address == "default-topic"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchPendingAsync_WhenPublishFails_ReleasesForRetry()
    {
        var message = CreatePendingMessage(retryCount: 0);
        _reader.ClaimBatchAsync(Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns([message]);
        _publisher.PublishAsync(Arg.Any<ITransportEnvelope>(), Arg.Any<IEndpointAddress>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Broker down"));

        await _sut.DispatchPendingAsync();

        await _reader.Received(1).ReleaseForRetryAsync(
            message.Id,
            1,
            Arg.Any<DateTimeOffset>(),
            Arg.Is<string?>(s => s != null && s.Contains("Broker down")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchPendingAsync_WhenRetriesExhausted_DeadLetters()
    {
        var message = CreatePendingMessage(retryCount: 2);
        _reader.ClaimBatchAsync(Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns([message]);
        _publisher.PublishAsync(Arg.Any<ITransportEnvelope>(), Arg.Any<IEndpointAddress>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("Timed out"));

        await _sut.DispatchPendingAsync();

        await _reader.Received(1).DeadLetterAsync(
            message.Id,
            Arg.Is<string?>(s => s != null && s.Contains("Timed out")),
            Arg.Any<CancellationToken>());
        await _reader.DidNotReceive().ReleaseForRetryAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<DateTimeOffset>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchPendingAsync_PassesBatchSizeToReader()
    {
        _options.BatchSize = 25;
        _reader.ClaimBatchAsync(Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await _sut.DispatchPendingAsync();

        await _reader.Received(1).ClaimBatchAsync(25, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchPendingAsync_PassesLeaseDurationToReader()
    {
        _reader.ClaimBatchAsync(Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await _sut.DispatchPendingAsync();

        await _reader.Received(1).ClaimBatchAsync(
            Arg.Any<int>(),
            TimeSpan.FromMinutes(5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchPendingAsync_SetsEnvelopeCorrelationIdFromMessage()
    {
        var message = CreatePendingMessage();
        message.CorrelationId = "corr-xyz";
        _reader.ClaimBatchAsync(Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns([message]);

        await _sut.DispatchPendingAsync();

        await _publisher.Received(1).PublishAsync(
            Arg.Is<ITransportEnvelope>(e => e.CorrelationId == "corr-xyz"),
            Arg.Any<IEndpointAddress>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchPendingAsync_StripsDestinationHeaderFromEnvelope()
    {
        var message = CreatePendingMessage(destination: "some-topic");
        _reader.ClaimBatchAsync(Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns([message]);

        await _sut.DispatchPendingAsync();

        await _publisher.Received(1).PublishAsync(
            Arg.Is<ITransportEnvelope>(e =>
                e.Headers == null ||
                !e.Headers.ContainsKey(OutboxHeaderNames.Destination)),
            Arg.Any<IEndpointAddress>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchPendingAsync_WithNoDestinationAndNoDefault_ThrowsInvalidOperation()
    {
        _options.DefaultDestination = string.Empty;
        var message = CreatePendingMessage();
        _reader.ClaimBatchAsync(Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns([message]);

        // HandleFailureAsync catches the InvalidOperationException from ResolveDestination
        // and dead-letters or retries it, so we check that the failure path ran
        await _sut.DispatchPendingAsync();

        await _reader.Received(1).ReleaseForRetryAsync(
            message.Id,
            Arg.Any<int>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Is<string?>(s => s != null && s.Contains("No destination")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchPendingAsync_ProcessesMultipleMessages()
    {
        var messages = new[]
        {
            CreatePendingMessage(type: "Event1"),
            CreatePendingMessage(type: "Event2"),
        };
        _reader.ClaimBatchAsync(Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(messages);

        await _sut.DispatchPendingAsync();

        await _publisher.Received(2).PublishAsync(
            Arg.Any<ITransportEnvelope>(),
            Arg.Any<IEndpointAddress>(),
            Arg.Any<CancellationToken>());
        await _reader.Received(1).MarkDispatchedAsync(messages[0].Id, Arg.Any<CancellationToken>());
        await _reader.Received(1).MarkDispatchedAsync(messages[1].Id, Arg.Any<CancellationToken>());
    }

    private static OutboxMessage CreatePendingMessage(
        string type = "OrderPlaced",
        string payload = "{}",
        string? destination = null,
        int retryCount = 0)
    {
        var headers = destination is not null
            ? new Dictionary<string, string> { [OutboxHeaderNames.Destination] = destination }
            : null;

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            OccurredAt = DateTimeOffset.UtcNow.AddSeconds(-10),
            Status = OutboxMessageStatus.Leased,
            Headers = OutboxHeaderSerializer.Serialize(headers),
            RetryCount = retryCount,
        };
    }
}
