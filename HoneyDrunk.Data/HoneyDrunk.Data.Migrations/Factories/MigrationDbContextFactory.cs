// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HoneyDrunk.Data.Migrations.Factories;

/// <summary>
/// Base class for design-time DbContext factories.
/// Inherit from this class in your migrations project to enable EF Core tooling.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public abstract class MigrationDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    /// <summary>
    /// Gets the migrations assembly name.
    /// Override this if your migrations are in a different assembly.
    /// </summary>
    protected virtual string? MigrationsAssembly => GetType().Assembly.GetName().Name;

    /// <summary>
    /// Creates a new DbContext instance for design-time operations.
    /// </summary>
    /// <param name="args">Command-line arguments (unused).</param>
    /// <returns>A configured DbContext instance.</returns>
    public TContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();

        ConfigureOptions(optionsBuilder);

        return CreateContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Gets the connection string for migrations.
    /// Override this to provide a custom connection string.
    /// </summary>
    /// <remarks>
    /// By default, looks for the HONEYDRUNK_MIGRATION_CONNECTION environment variable.
    /// </remarks>
    /// <returns>The connection string for migrations.</returns>
    protected virtual string GetConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("HONEYDRUNK_MIGRATION_CONNECTION");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Migration connection string not found. " +
                "Set the HONEYDRUNK_MIGRATION_CONNECTION environment variable or override GetConnectionString().");
        }

        return connectionString;
    }

    /// <summary>
    /// Configures the DbContext options.
    /// Override this to customize option configuration.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    protected virtual void ConfigureOptions(DbContextOptionsBuilder<TContext> optionsBuilder)
    {
        var connectionString = GetConnectionString();

        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            if (!string.IsNullOrEmpty(MigrationsAssembly))
            {
                sqlOptions.MigrationsAssembly(MigrationsAssembly);
            }
        });
    }

    /// <summary>
    /// Creates the DbContext instance.
    /// Override this to provide custom instantiation logic.
    /// </summary>
    /// <param name="options">The configured options.</param>
    /// <returns>A new DbContext instance.</returns>
    protected abstract TContext CreateContext(DbContextOptions<TContext> options);
}
