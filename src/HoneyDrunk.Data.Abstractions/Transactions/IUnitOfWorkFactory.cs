// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Abstractions.Transactions;

/// <summary>
/// Factory for creating unit of work instances.
/// </summary>
public interface IUnitOfWorkFactory
{
    /// <summary>
    /// Creates a new unit of work instance.
    /// </summary>
    /// <returns>A new unit of work.</returns>
    IUnitOfWork Create();
}
