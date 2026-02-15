// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Outbox.Configuration;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.Outbox;

/// <summary>
/// Extension methods for applying outbox entity configuration to a <see cref="ModelBuilder"/>.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies the <see cref="OutboxMessage"/> entity configuration.
    /// Call this from your DbContext's <c>OnModelCreating</c> method.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    /// <param name="schema">Database schema. Defaults to <c>"outbox"</c>.</param>
    /// <param name="tableName">Table name. Defaults to <c>"OutboxMessages"</c>.</param>
    /// <returns>The model builder for chaining.</returns>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     base.OnModelCreating(modelBuilder);
    ///     modelBuilder.ApplyOutboxConfiguration();
    /// }
    /// </code>
    /// </example>
    public static ModelBuilder ApplyOutboxConfiguration(
        this ModelBuilder builder,
        string schema = "outbox",
        string tableName = "OutboxMessages")
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ApplyConfiguration(new OutboxMessageConfiguration(schema, tableName));

        return builder;
    }
}
