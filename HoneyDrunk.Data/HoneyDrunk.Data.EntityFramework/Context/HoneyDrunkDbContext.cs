// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;
using HoneyDrunk.Data.Abstractions.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.EntityFramework.Context;

/// <summary>
/// Base DbContext that integrates with HoneyDrunk Grid context for tenant awareness,
/// correlation tracking, and telemetry enrichment.
/// </summary>
public abstract class HoneyDrunkDbContext : DbContext
{
    private readonly ITenantAccessor? _tenantAccessor;
    private readonly IDataDiagnosticsContext? _diagnosticsContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="HoneyDrunkDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options.</param>
    protected HoneyDrunkDbContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HoneyDrunkDbContext"/> class with Grid integration.
    /// </summary>
    /// <param name="options">The context options.</param>
    /// <param name="tenantAccessor">The tenant accessor for multi-tenant resolution.</param>
    /// <param name="diagnosticsContext">The diagnostics context for telemetry.</param>
    protected HoneyDrunkDbContext(
        DbContextOptions options,
        ITenantAccessor tenantAccessor,
        IDataDiagnosticsContext diagnosticsContext)
        : base(options)
    {
        _tenantAccessor = tenantAccessor;
        _diagnosticsContext = diagnosticsContext;
    }

    /// <summary>
    /// Gets the current tenant identifier.
    /// </summary>
    protected TenantId CurrentTenantId => _tenantAccessor?.GetCurrentTenantId() ?? default;

    /// <summary>
    /// Gets the current diagnostics context for telemetry enrichment.
    /// </summary>
    protected IDataDiagnosticsContext? DiagnosticsContext => _diagnosticsContext;

    /// <summary>
    /// Gets the schema name to use for this context, based on tenant resolution.
    /// Override this to implement schema-based multi-tenancy.
    /// </summary>
    protected virtual string? Schema => null;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        if (!string.IsNullOrEmpty(Schema))
        {
            modelBuilder.HasDefaultSchema(Schema);
        }

        ApplyConfigurations(modelBuilder);
        ApplyConventions(modelBuilder);
    }

    /// <summary>
    /// Applies entity type configurations. Override to register IEntityTypeConfiguration implementations.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected virtual void ApplyConfigurations(ModelBuilder modelBuilder)
    {
        // Default implementation does nothing.
        // Derived contexts should call modelBuilder.ApplyConfigurationsFromAssembly()
    }

    /// <summary>
    /// Applies model conventions. Override to customize naming or other conventions.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected virtual void ApplyConventions(ModelBuilder modelBuilder)
    {
        // Default implementation does nothing.
    }

    /// <summary>
    /// Gets the query tag to apply to commands for correlation tracking.
    /// </summary>
    /// <returns>A query tag string with correlation information, or <c>null</c> if no context is available.</returns>
    protected string? GetQueryTag()
    {
        if (_diagnosticsContext is null)
        {
            return null;
        }

        var parts = new List<string>();

        if (!string.IsNullOrEmpty(_diagnosticsContext.CorrelationId))
        {
            parts.Add($"correlation:{_diagnosticsContext.CorrelationId}");
        }

        if (!string.IsNullOrEmpty(_diagnosticsContext.OperationId))
        {
            parts.Add($"operation:{_diagnosticsContext.OperationId}");
        }

        return parts.Count > 0 ? string.Join(" ", parts) : null;
    }
}
