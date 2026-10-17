// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.SqlServer.Conventions;

/// <summary>
/// SQL Server-specific model conventions.
/// </summary>
public static class SqlServerModelConventions
{
    /// <summary>
    /// Applies SQL Server-specific index conventions.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ApplySqlServerIndexConventions(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        return modelBuilder;
    }

    /// <summary>
    /// Configures all DateTime properties to use datetime2 column type.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder UseDateTime2ForAllDateTimeProperties(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
        {
            property.SetColumnType("datetime2");
        }

        return modelBuilder;
    }

    /// <summary>
    /// Configures all decimal properties to use a specific precision and scale.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="precision">The precision (total digits). Defaults to 18.</param>
    /// <param name="scale">The scale (decimal places). Defaults to 2.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ConfigureDecimalPrecision(
        this ModelBuilder modelBuilder,
        int precision = 18,
        int scale = 2)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(precision);
            property.SetScale(scale);
        }

        return modelBuilder;
    }
}
