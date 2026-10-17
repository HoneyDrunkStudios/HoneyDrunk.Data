// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Tenancy;
using HoneyDrunk.Kernel.Abstractions.Context;
using KernelTenantId = HoneyDrunk.Kernel.Abstractions.Identity.TenantId;

namespace HoneyDrunk.Data.Tenancy;

/// <summary>
/// Default implementation of <see cref="ITenantAccessor"/> that extracts tenant information
/// from the Kernel operation context.
/// </summary>
public sealed class KernelTenantAccessor : ITenantAccessor
{
    private readonly IOperationContextAccessor _operationContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="KernelTenantAccessor"/> class.
    /// </summary>
    /// <param name="operationContextAccessor">The operation context accessor.</param>
    public KernelTenantAccessor(IOperationContextAccessor operationContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(operationContextAccessor);
        _operationContextAccessor = operationContextAccessor;
    }

    /// <inheritdoc />
    public TenantId GetCurrentTenantId()
    {
        var context = _operationContextAccessor.Current;
        if (context is null)
        {
            return default;
        }

        KernelTenantId kernelTenantId = context.TenantId;
        return kernelTenantId.IsInternal ? default : TenantId.FromString(kernelTenantId.ToString());
    }
}
