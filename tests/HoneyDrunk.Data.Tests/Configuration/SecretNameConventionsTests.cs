// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Configuration;

namespace HoneyDrunk.Data.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="SecretNameConventions"/>.
/// </summary>
public sealed class SecretNameConventionsTests
{
    [Fact]
    public void SqlConnection_UsesProviderGroupedConvention()
    {
        Assert.Equal("Sql--TenantConnection", SecretNameConventions.SqlConnection("Tenant"));
    }

    [Fact]
    public void SqlConnection_WithBlankPurpose_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SecretNameConventions.SqlConnection(" "));
    }
}
