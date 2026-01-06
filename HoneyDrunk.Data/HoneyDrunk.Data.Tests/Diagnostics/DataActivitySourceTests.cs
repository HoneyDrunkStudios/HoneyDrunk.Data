// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Diagnostics;
using System.Diagnostics;

namespace HoneyDrunk.Data.Tests.Diagnostics;

/// <summary>
/// Unit tests for <see cref="DataActivitySource"/>.
/// </summary>
public sealed class DataActivitySourceTests : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly List<Activity> _capturedActivities = [];

    public DataActivitySourceTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == DataActivitySource.DefaultSourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => _capturedActivities.Add(activity),
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
        foreach (var activity in _capturedActivities)
        {
            activity.Dispose();
        }
    }

    [Fact]
    public void DefaultSourceName_HasExpectedValue()
    {
        Assert.Equal("HoneyDrunk.Data", DataActivitySource.DefaultSourceName);
    }

    [Fact]
    public void StartActivity_CreatesActivityWithName()
    {
        using var activity = DataActivitySource.StartActivity("TestOperation");

        Assert.NotNull(activity);
        Assert.Equal("TestOperation", activity.OperationName);
    }

    [Fact]
    public void StartActivity_WithKind_SetsActivityKind()
    {
        using var activity = DataActivitySource.StartActivity("TestOperation", ActivityKind.Server);

        Assert.NotNull(activity);
        Assert.Equal(ActivityKind.Server, activity.Kind);
    }

    [Fact]
    public void StartActivity_DefaultKind_IsClient()
    {
        using var activity = DataActivitySource.StartActivity("TestOperation");

        Assert.NotNull(activity);
        Assert.Equal(ActivityKind.Client, activity.Kind);
    }

    [Fact]
    public void StartRepositoryActivity_CreatesActivityWithCorrectName()
    {
        using var activity = DataActivitySource.StartRepositoryActivity("Order", "FindById");

        Assert.NotNull(activity);
        Assert.Equal("Repository.Order.FindById", activity.OperationName);
    }

    [Fact]
    public void StartRepositoryActivity_SetsEntityTag()
    {
        using var activity = DataActivitySource.StartRepositoryActivity("Order", "FindById");

        Assert.NotNull(activity);
        Assert.Equal("Order", activity.GetTagItem("db.entity"));
    }

    [Fact]
    public void StartRepositoryActivity_SetsOperationTag()
    {
        using var activity = DataActivitySource.StartRepositoryActivity("Order", "FindById");

        Assert.NotNull(activity);
        Assert.Equal("FindById", activity.GetTagItem("db.operation"));
    }

    [Fact]
    public void StartSaveChangesActivity_CreatesActivityWithCorrectName()
    {
        using var activity = DataActivitySource.StartSaveChangesActivity();

        Assert.NotNull(activity);
        Assert.Equal("UnitOfWork.SaveChanges", activity.OperationName);
    }

    [Fact]
    public void StartTransactionActivity_CreatesActivityWithCorrectName()
    {
        using var activity = DataActivitySource.StartTransactionActivity("Begin");

        Assert.NotNull(activity);
        Assert.Equal("Transaction.Begin", activity.OperationName);
    }

    [Theory]
    [InlineData("Begin")]
    [InlineData("Commit")]
    [InlineData("Rollback")]
    public void StartTransactionActivity_SupportsVariousOperations(string operation)
    {
        using var activity = DataActivitySource.StartTransactionActivity(operation);

        Assert.NotNull(activity);
        Assert.Equal($"Transaction.{operation}", activity.OperationName);
    }
}
