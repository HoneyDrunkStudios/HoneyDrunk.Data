// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HoneyDrunk.Data.EntityFramework.Modeling;

/// <summary>
/// Provides model building conventions for HoneyDrunk data contexts.
/// </summary>
public static class ModelBuilderConventions
{
    /// <summary>
    /// Applies standard naming conventions to the model.
    /// Converts entity names to snake_case for table names.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ApplySnakeCaseNamingConvention(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            ApplySnakeCaseToEntity(entity);
        }

        return modelBuilder;
    }

    /// <summary>
    /// Applies default string length to all string properties without explicit configuration.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="maxLength">The maximum string length. Defaults to 256.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ApplyDefaultStringLength(this ModelBuilder modelBuilder, int maxLength = 256)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(string) && p.GetMaxLength() is null))
        {
            property.SetMaxLength(maxLength);
        }

        return modelBuilder;
    }

    private static void ApplySnakeCaseToEntity(IMutableEntityType entity)
    {
        entity.SetTableName(ToSnakeCase(entity.GetTableName() ?? entity.ClrType.Name));

        foreach (var property in entity.GetProperties())
        {
            property.SetColumnName(ToSnakeCase(property.GetColumnName()));
        }

        foreach (var key in entity.GetKeys())
        {
            RenameIfPresent(key.GetName(), name => key.SetName(name));
        }

        foreach (var foreignKey in entity.GetForeignKeys())
        {
            RenameIfPresent(foreignKey.GetConstraintName(), name => foreignKey.SetConstraintName(name));
        }

        foreach (var index in entity.GetIndexes())
        {
            RenameIfPresent(index.GetDatabaseName(), name => index.SetDatabaseName(name));
        }
    }

    private static void RenameIfPresent(string? originalName, Action<string> setter)
    {
        if (!string.IsNullOrEmpty(originalName))
        {
            setter(ToSnakeCase(originalName));
        }
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var result = new System.Text.StringBuilder();
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    result.Append('_');
                }

                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }
}
