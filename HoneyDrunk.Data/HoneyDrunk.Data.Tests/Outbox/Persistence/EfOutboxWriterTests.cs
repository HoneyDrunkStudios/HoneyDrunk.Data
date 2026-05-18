// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Outbox;
using HoneyDrunk.Data.Outbox.Persistence;
using HoneyDrunk.Data.Testing.Factories;
using HoneyDrunk.Data.Tests.TestFixtures;
using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.Extensions.Options;
using KernelTenantId = HoneyDrunk.Kernel.Abstractions.Identity.TenantId;

namespace HoneyDrunk.Data.Tests.Outbox.Persistence;

public sealed class EfOutboxWriterTests : IAsyncDisposable
{
    private readonly SqliteTestDbContextFactory<OutboxTestDbContext> _factory;
    private readonly OutboxTestDbContext _context;

    public EfOutboxWriterTests()
    {
        _factory = new SqliteTestDbContextFactory<OutboxTestDbContext>(
            options => new OutboxTestDbContext(options));
        _context = _factory.Create();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task WriteAsync_AddsMessageToChangeTracker()
    {
        var writer = CreateWriter(_context);
        var message = CreateMessage();

        await writer.WriteAsync(message);
        await _context.SaveChangesAsync();

        Assert.Single(_context.OutboxMessages);
    }

    [Fact]
    public async Task WriteAsync_SetsStatusToPending()
    {
        var writer = CreateWriter(_context);
        var message = CreateMessage();

        await writer.WriteAsync(message);
        await _context.SaveChangesAsync();

        var saved = _context.OutboxMessages.Single();
        Assert.Equal(OutboxMessageStatus.Pending, saved.Status);
    }

    [Fact]
    public async Task WriteAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        var writer = CreateWriter(_context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteAsync(null!));
    }

    [Fact]
    public async Task WriteAsync_ExceedingMaxPayloadSize_ThrowsInvalidOperationException()
    {
        var options = new OutboxOptions { MaxPayloadSize = 10 };
        var writer = CreateWriter(_context, options: options);
        var message = CreateMessage(payload: new string('x', 11));

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteAsync(message));
    }

    [Fact]
    public async Task WriteAsync_WithinMaxPayloadSize_Succeeds()
    {
        var options = new OutboxOptions { MaxPayloadSize = 10 };
        var writer = CreateWriter(_context, options: options);
        var message = CreateMessage(payload: new string('x', 10));

        await writer.WriteAsync(message);
        await _context.SaveChangesAsync();

        Assert.Single(_context.OutboxMessages);
    }

    [Fact]
    public async Task WriteAsync_EnrichesCorrelationIdFromContext()
    {
        var accessor = Substitute.For<IOperationContextAccessor>();
        var opContext = Substitute.For<IOperationContext>();
        opContext.CorrelationId.Returns("corr-123");
        opContext.TenantId.Returns(KernelTenantId.Internal);
        accessor.Current.Returns(opContext);

        var writer = CreateWriter(_context, accessor);
        var message = CreateMessage();

        await writer.WriteAsync(message);
        await _context.SaveChangesAsync();

        var saved = _context.OutboxMessages.Single();
        Assert.Equal("corr-123", saved.CorrelationId);
    }

    [Fact]
    public async Task WriteAsync_EnrichesTenantIdFromContext()
    {
        var accessor = Substitute.For<IOperationContextAccessor>();
        var opContext = Substitute.For<IOperationContext>();
        var tenantId = KernelTenantId.NewId();
        opContext.CorrelationId.Returns("corr-456");
        opContext.TenantId.Returns(tenantId);
        accessor.Current.Returns(opContext);

        var writer = CreateWriter(_context, accessor);
        var message = CreateMessage();

        await writer.WriteAsync(message);
        await _context.SaveChangesAsync();

        var saved = _context.OutboxMessages.Single();
        Assert.Equal(tenantId.ToString(), saved.TenantId);
    }

    [Fact]
    public async Task WriteAsync_DoesNotOverrideExistingCorrelationId()
    {
        var accessor = Substitute.For<IOperationContextAccessor>();
        var opContext = Substitute.For<IOperationContext>();
        opContext.CorrelationId.Returns("from-context");
        accessor.Current.Returns(opContext);

        var writer = CreateWriter(_context, accessor);
        var message = CreateMessage();
        message.CorrelationId = "already-set";

        await writer.WriteAsync(message);
        await _context.SaveChangesAsync();

        var saved = _context.OutboxMessages.Single();
        Assert.Equal("already-set", saved.CorrelationId);
    }

    [Fact]
    public async Task WriteAsync_WhenAutoPopulateDisabled_DoesNotEnrich()
    {
        var accessor = Substitute.For<IOperationContextAccessor>();
        var opContext = Substitute.For<IOperationContext>();
        opContext.CorrelationId.Returns("should-not-appear");
        opContext.TenantId.Returns(KernelTenantId.NewId());
        accessor.Current.Returns(opContext);

        var options = new OutboxOptions { AutoPopulateFromContext = false };
        var writer = CreateWriter(_context, accessor, options);
        var message = CreateMessage();

        await writer.WriteAsync(message);
        await _context.SaveChangesAsync();

        var saved = _context.OutboxMessages.Single();
        Assert.Null(saved.CorrelationId);
        Assert.Null(saved.TenantId);
    }

    [Fact]
    public async Task WriteAsync_WhenContextAccessorReturnsNull_ThrowsInvalidOperationException()
    {
        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns((IOperationContext)null!);

        var writer = CreateWriter(_context, accessor);
        var message = CreateMessage();

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteAsync(message));
    }

    [Fact]
    public async Task WriteAsync_WhenContextAccessorReturnsNull_WithExplicitContext_Succeeds()
    {
        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns((IOperationContext)null!);

        var writer = CreateWriter(_context, accessor);
        var message = CreateMessage();
        message.CorrelationId = "explicit-correlation";
        message.TenantId = KernelTenantId.Internal.ToString();

        await writer.WriteAsync(message);
        await _context.SaveChangesAsync();

        var saved = _context.OutboxMessages.Single();
        Assert.Equal("explicit-correlation", saved.CorrelationId);
        Assert.Equal(KernelTenantId.Internal.ToString(), saved.TenantId);
    }

    [Fact]
    public async Task WriteAsync_WhenContextCorrelationIdMissing_ThrowsInvalidOperationException()
    {
        var accessor = Substitute.For<IOperationContextAccessor>();
        var opContext = Substitute.For<IOperationContext>();
        opContext.CorrelationId.Returns(string.Empty);
        opContext.TenantId.Returns(KernelTenantId.Internal);
        accessor.Current.Returns(opContext);

        var writer = CreateWriter(_context, accessor);
        var message = CreateMessage();

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteAsync(message));
    }

    [Fact]
    public async Task WriteBatchAsync_AddsMultipleMessages()
    {
        var writer = CreateWriter(_context);
        var messages = new[]
        {
            CreateMessage(type: "Event1"),
            CreateMessage(type: "Event2"),
            CreateMessage(type: "Event3"),
        };

        await writer.WriteBatchAsync(messages);
        await _context.SaveChangesAsync();

        Assert.Equal(3, _context.OutboxMessages.Count());
    }

    [Fact]
    public async Task WriteBatchAsync_WithNullMessages_ThrowsArgumentNullException()
    {
        var writer = CreateWriter(_context);

        await Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteBatchAsync(null!));
    }

    [Fact]
    public async Task WriteBatchAsync_WithOversizedPayload_ThrowsForFirstViolation()
    {
        var options = new OutboxOptions { MaxPayloadSize = 10 };
        var writer = CreateWriter(_context, options: options);
        var messages = new[]
        {
            CreateMessage(payload: "ok"),
            CreateMessage(payload: new string('x', 20)),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.WriteBatchAsync(messages));
    }

    private static EfOutboxWriter<OutboxTestDbContext> CreateWriter(
        OutboxTestDbContext context,
        IOperationContextAccessor? accessor = null,
        OutboxOptions? options = null)
    {
        accessor ??= CreateAccessor();
        var opts = Options.Create(options ?? new OutboxOptions());
        return new EfOutboxWriter<OutboxTestDbContext>(context, accessor, opts);
    }

    private static IOperationContextAccessor CreateAccessor()
    {
        var accessor = Substitute.For<IOperationContextAccessor>();
        var opContext = Substitute.For<IOperationContext>();
        opContext.CorrelationId.Returns("corr-default");
        opContext.TenantId.Returns(KernelTenantId.Internal);
        accessor.Current.Returns(opContext);
        return accessor;
    }

    private static OutboxMessage CreateMessage(string type = "TestEvent", string payload = "{}")
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            OccurredAt = DateTimeOffset.UtcNow,
        };
    }
}
