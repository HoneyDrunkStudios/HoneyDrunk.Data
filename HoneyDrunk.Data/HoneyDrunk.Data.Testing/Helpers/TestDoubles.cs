// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;
using HoneyDrunk.Data.Abstractions.Tenancy;

namespace HoneyDrunk.Data.Testing.Helpers;

/// <summary>
/// Test doubles for data abstractions.
/// </summary>
public static class TestDoubles
{
    /// <summary>
    /// Creates a test tenant accessor that returns the specified tenant ID.
    /// </summary>
    /// <param name="tenantId">The tenant ID to return.</param>
    /// <returns>A configured tenant accessor.</returns>
    public static ITenantAccessor CreateTenantAccessor(string tenantId)
    {
        return new TestTenantAccessor(TenantId.FromString(tenantId));
    }

    /// <summary>
    /// Creates a test tenant accessor that returns an empty tenant ID.
    /// </summary>
    /// <returns>A configured tenant accessor.</returns>
    public static ITenantAccessor CreateEmptyTenantAccessor()
    {
        return new TestTenantAccessor(default);
    }

    /// <summary>
    /// Creates a test diagnostics context with the specified values.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="operationId">The operation ID.</param>
    /// <param name="nodeId">The node ID.</param>
    /// <returns>A configured diagnostics context.</returns>
    public static IDataDiagnosticsContext CreateDiagnosticsContext(
        string? correlationId = null,
        string? operationId = null,
        string? nodeId = null)
    {
        return new TestDataDiagnosticsContext(correlationId, operationId, nodeId);
    }

    private sealed class TestTenantAccessor : ITenantAccessor
    {
        private readonly TenantId _tenantId;

        public TestTenantAccessor(TenantId tenantId)
        {
            _tenantId = tenantId;
        }

        public TenantId GetCurrentTenantId() => _tenantId;
    }

    private sealed class TestDataDiagnosticsContext : IDataDiagnosticsContext
    {
        private readonly Dictionary<string, string> _tags;

        public TestDataDiagnosticsContext(string? correlationId, string? operationId, string? nodeId)
        {
            CorrelationId = correlationId;
            OperationId = operationId;
            NodeId = nodeId;

            _tags = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(correlationId))
            {
                _tags["correlation.id"] = correlationId;
            }

            if (!string.IsNullOrEmpty(operationId))
            {
                _tags["operation.id"] = operationId;
            }

            if (!string.IsNullOrEmpty(nodeId))
            {
                _tags["node.id"] = nodeId;
            }
        }

        public string? CorrelationId { get; }

        public string? OperationId { get; }

        public string? NodeId { get; }

        public IReadOnlyDictionary<string, string> Tags => _tags.AsReadOnly();
    }
}
