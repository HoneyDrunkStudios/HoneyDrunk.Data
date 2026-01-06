// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;
using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Data.Diagnostics;

/// <summary>
/// Default implementation of <see cref="IDataDiagnosticsContext"/> that extracts
/// diagnostic information from the Kernel context.
/// </summary>
public sealed class KernelDataDiagnosticsContext : IDataDiagnosticsContext
{
    private static readonly IReadOnlyDictionary<string, string> EmptyTags =
        new Dictionary<string, string>().AsReadOnly();

    private readonly IOperationContextAccessor _operationContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="KernelDataDiagnosticsContext"/> class.
    /// </summary>
    /// <param name="operationContextAccessor">The operation context accessor.</param>
    public KernelDataDiagnosticsContext(IOperationContextAccessor operationContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(operationContextAccessor);
        _operationContextAccessor = operationContextAccessor;
    }

    /// <inheritdoc />
    public string? CorrelationId => _operationContextAccessor.Current?.CorrelationId;

    /// <inheritdoc />
    public string? OperationId => _operationContextAccessor.Current?.OperationId;

    /// <inheritdoc />
    public string? NodeId => _operationContextAccessor.Current?.GridContext?.NodeId;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Tags
    {
        get
        {
            var context = _operationContextAccessor.Current;
            if (context is null)
            {
                return EmptyTags;
            }

            var tags = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(context.CorrelationId))
            {
                tags["correlation.id"] = context.CorrelationId;
            }

            if (!string.IsNullOrEmpty(context.OperationId))
            {
                tags["operation.id"] = context.OperationId;
            }

            var nodeId = context.GridContext?.NodeId;
            if (!string.IsNullOrEmpty(nodeId))
            {
                tags["node.id"] = nodeId;
            }

            if (!string.IsNullOrEmpty(context.TenantId))
            {
                tags["tenant.id"] = context.TenantId;
            }

            return tags.AsReadOnly();
        }
    }
}
