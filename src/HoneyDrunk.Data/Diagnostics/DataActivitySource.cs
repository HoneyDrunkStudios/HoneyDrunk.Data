// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;

namespace HoneyDrunk.Data.Diagnostics;

/// <summary>
/// Provides activity source and naming conventions for data layer telemetry.
/// </summary>
public static class DataActivitySource
{
    /// <summary>
    /// The default activity source name for data operations.
    /// </summary>
    public const string DefaultSourceName = "HoneyDrunk.Data";

    private static readonly ActivitySource Source = new(DefaultSourceName);

    /// <summary>
    /// Starts an activity for a database operation.
    /// </summary>
    /// <param name="operationName">The name of the database operation.</param>
    /// <param name="kind">The activity kind.</param>
    /// <returns>The started activity, or <c>null</c> if there are no listeners.</returns>
    public static Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Client)
    {
        return Source.StartActivity(operationName, kind);
    }

    /// <summary>
    /// Starts an activity for a repository operation.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="operation">The repository operation (e.g., "FindById", "Add").</param>
    /// <returns>The started activity, or <c>null</c> if there are no listeners.</returns>
    public static Activity? StartRepositoryActivity(string entityType, string operation)
    {
        var activity = Source.StartActivity($"Repository.{entityType}.{operation}", ActivityKind.Client);
        activity?.SetTag("db.operation", operation);
        activity?.SetTag("db.entity", entityType);
        return activity;
    }

    /// <summary>
    /// Starts an activity for a unit of work save operation.
    /// </summary>
    /// <returns>The started activity, or <c>null</c> if there are no listeners.</returns>
    public static Activity? StartSaveChangesActivity()
    {
        return Source.StartActivity("UnitOfWork.SaveChanges", ActivityKind.Client);
    }

    /// <summary>
    /// Starts an activity for a transaction operation.
    /// </summary>
    /// <param name="operation">The transaction operation (e.g., "Begin", "Commit", "Rollback").</param>
    /// <returns>The started activity, or <c>null</c> if there are no listeners.</returns>
    public static Activity? StartTransactionActivity(string operation)
    {
        return Source.StartActivity($"Transaction.{operation}", ActivityKind.Client);
    }
}
