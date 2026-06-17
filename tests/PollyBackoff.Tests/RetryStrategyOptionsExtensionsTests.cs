// <copyright file="RetryStrategyOptionsExtensionsTests.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

using FluentAssertions;
using NUnit.Framework;
using Polly;
using Polly.Retry;

namespace PollyBackoff.Tests;

[TestFixture]
public class RetryStrategyOptionsExtensionsTests
{
    [Test]
    public void UseBackoff_SetsDelayGenerator()
    {
        var options = new RetryStrategyOptions<string>();
        var backoff = Backoff.ConstantBackoff(TimeSpan.FromSeconds(1));

        options.UseBackoff(backoff);

        options.DelayGenerator.Should().NotBeNull();
    }

    [Test]
    public void UseBackoff_DelayGenerator_ReturnsBackoffValue()
    {
        var options = new RetryStrategyOptions<string>();
        var expected = TimeSpan.FromSeconds(2);
        options.UseBackoff(_ => expected);

        var context = ResilienceContextPool.Shared.Get();
        var args = new RetryDelayGeneratorArguments<string>(
            context, Outcome.FromResult("x"), 0);
        var result = options.DelayGenerator!(args).GetAwaiter().GetResult();
        ResilienceContextPool.Shared.Return(context);

        result.Should().Be(expected);
    }

    [Test]
    public void UseBackoff_NullOptions_Throws()
    {
        RetryStrategyOptions<string> options = null!;
        var act = () => options.UseBackoff(_ => TimeSpan.FromSeconds(1));

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void UseBackoff_NullBackoff_Throws()
    {
        var options = new RetryStrategyOptions<string>();
        var act = () => options.UseBackoff(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void UseDecorrelatedJitter_SetsDelayGenerator()
    {
        var options = new RetryStrategyOptions<string>();

        options.UseDecorrelatedJitter(TimeSpan.FromMilliseconds(100));

        options.DelayGenerator.Should().NotBeNull();
    }

    [Test]
    public void UseExponentialBackoff_SetsDelayGenerator()
    {
        var options = new RetryStrategyOptions<string>();

        options.UseExponentialBackoff(TimeSpan.FromMilliseconds(100));

        options.DelayGenerator.Should().NotBeNull();
    }

    [Test]
    public void UseLinearBackoff_SetsDelayGenerator()
    {
        var options = new RetryStrategyOptions<string>();

        options.UseLinearBackoff(TimeSpan.FromMilliseconds(100));

        options.DelayGenerator.Should().NotBeNull();
    }

    [Test]
    public void UseConstantBackoff_SetsDelayGenerator()
    {
        var options = new RetryStrategyOptions<string>();

        options.UseConstantBackoff(TimeSpan.FromMilliseconds(500));

        options.DelayGenerator.Should().NotBeNull();
    }

    [Test]
    public void UseBackoff_ReturnsOptionsForFluentChaining()
    {
        var options = new RetryStrategyOptions<string>();

        var result = options.UseBackoff(_ => TimeSpan.FromSeconds(1));

        result.Should().BeSameAs(options);
    }

    [Test]
    public void Pipeline_WithDecorrelatedJitter_BuildsSuccessfully()
    {
        var act = () => new ResiliencePipelineBuilder<string>()
            .AddRetry(new RetryStrategyOptions<string>()
                .UseDecorrelatedJitter(TimeSpan.FromMilliseconds(100), seed: 1))
            .Build();

        act.Should().NotThrow();
    }
}
