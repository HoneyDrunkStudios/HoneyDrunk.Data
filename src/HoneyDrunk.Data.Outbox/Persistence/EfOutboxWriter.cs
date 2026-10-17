// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using KernelTenantId = HoneyDrunk.Kernel.Abstractions.Identity.TenantId;

namespace HoneyDrunk.Data.Outbox.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IOutboxWriter"/>.
/// Adds outbox messages to the DbContext change tracker so they are
/// persisted atomically with domain state on the next <c>SaveChangesAsync</c>.
/// </summary>
/// <typeparam name="TContext">The application's DbContext type.</typeparam>
public sealed class EfOutboxWriter<TContext>(
    TContext dbContext,
    IOperationContextAccessor operationContextAccessor,
    IOptions<OutboxOptions> options) : IOutboxWriter
    where TContext : DbContext
{
    private readonly TContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly IOperationContextAccessor _operationContextAccessor = operationContextAccessor ?? throw new ArgumentNullException(nameof(operationContextAccessor));
    private readonly OutboxOptions _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;

    /// <inheritdoc />
    public Task WriteAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ValidatePayloadSize(message);
        EnrichFromContext(message);

        _dbContext.Set<OutboxMessage>().Add(message);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task WriteBatchAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var batch = messages as IReadOnlyList<OutboxMessage> ?? [.. messages];

        foreach (var message in batch)
        {
            ValidatePayloadSize(message);
            EnrichFromContext(message);
        }

        _dbContext.Set<OutboxMessage>().AddRange(batch);

        return Task.CompletedTask;
    }

    private static void ValidateExplicitContext(OutboxMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.CorrelationId) || string.IsNullOrWhiteSpace(message.TenantId))
        {
            throw new InvalidOperationException(
                "Outbox context autopopulation requires a current operation context or explicit correlation and tenant ids.");
        }
    }

    private void EnrichFromContext(OutboxMessage message)
    {
        if (!_options.AutoPopulateFromContext)
            return;

        var context = _operationContextAccessor.Current;
        if (context is null)
        {
            ValidateExplicitContext(message);
            return;
        }

        var correlationId = context.CorrelationId;
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new InvalidOperationException(
                "Outbox context autopopulation requires a current operation context with a non-empty correlation id.");
        }

        if (string.IsNullOrWhiteSpace(message.CorrelationId))
        {
            message.CorrelationId = correlationId;
        }

        KernelTenantId tenantId = context.TenantId;
        if (string.IsNullOrWhiteSpace(message.TenantId))
        {
            message.TenantId = tenantId.ToString();
        }

        ValidateExplicitContext(message);
    }

    private void ValidatePayloadSize(OutboxMessage message)
    {
        if (message.Payload.Length > _options.MaxPayloadSize)
        {
            throw new InvalidOperationException(
                $"Outbox message payload ({message.Payload.Length} chars) exceeds " +
                $"the configured maximum of {_options.MaxPayloadSize} chars.");
        }
    }
}
