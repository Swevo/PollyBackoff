// <copyright file="BackoffTests.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

using FluentAssertions;
using NUnit.Framework;

namespace PollyBackoff.Tests;

[TestFixture]
public class BackoffTests
{
    [Test]
    public void DecorrelatedJitter_FirstDelay_IsAtLeastBaseDelay()
    {
        var backoff = Backoff.DecorrelatedJitter(TimeSpan.FromMilliseconds(100), seed: 42);

        var delay = backoff(0);

        delay.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(100));
    }

    [Test]
    public void DecorrelatedJitter_Delays_DoNotExceedMaxDelay()
    {
        var max = TimeSpan.FromSeconds(1);
        var backoff = Backoff.DecorrelatedJitter(TimeSpan.FromMilliseconds(100), maxDelay: max, seed: 1);

        var delays = Enumerable.Range(0, 20).Select(backoff).ToList();

        delays.Should().AllSatisfy(d => d.Should().BeLessThanOrEqualTo(max));
    }

    [Test]
    public void DecorrelatedJitter_Delays_AreAlwaysPositive()
    {
        var backoff = Backoff.DecorrelatedJitter(TimeSpan.FromMilliseconds(50), seed: 99);

        var delays = Enumerable.Range(0, 10).Select(backoff).ToList();

        delays.Should().AllSatisfy(d => d.Should().BePositive());
    }

    [Test]
    public void DecorrelatedJitter_InvalidBaseDelay_Throws()
    {
        var act = () => Backoff.DecorrelatedJitter(TimeSpan.Zero);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("baseDelay");
    }

    [Test]
    public void DecorrelatedJitter_FactorTooLow_Throws()
    {
        var act = () => Backoff.DecorrelatedJitter(TimeSpan.FromSeconds(1), factor: 1.0);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("factor");
    }

    [Test]
    public void ExponentialBackoff_Attempt0_ReturnsBaseDelay()
    {
        var backoff = Backoff.ExponentialBackoff(TimeSpan.FromMilliseconds(100), factor: 2.0);

        var delay = backoff(0);

        delay.Should().Be(TimeSpan.FromMilliseconds(100));
    }

    [Test]
    public void ExponentialBackoff_Delays_GrowExponentially()
    {
        var backoff = Backoff.ExponentialBackoff(TimeSpan.FromMilliseconds(100), factor: 2.0);

        var d0 = backoff(0);
        var d1 = backoff(1);
        var d2 = backoff(2);

        d1.Should().BeCloseTo(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(1));
        d2.Should().BeCloseTo(TimeSpan.FromMilliseconds(400), TimeSpan.FromMilliseconds(1));
        d1.Should().BeGreaterThan(d0);
        d2.Should().BeGreaterThan(d1);
    }

    [Test]
    public void ExponentialBackoff_Delays_DoNotExceedMaxDelay()
    {
        var max = TimeSpan.FromSeconds(1);
        var backoff = Backoff.ExponentialBackoff(TimeSpan.FromMilliseconds(100), factor: 2.0, maxDelay: max);

        var delays = Enumerable.Range(0, 20).Select(backoff).ToList();

        delays.Should().AllSatisfy(d => d.Should().BeLessThanOrEqualTo(max));
    }

    [Test]
    public void ExponentialBackoff_WithJitter_DelaysAreLessOrEqualToNonJittered()
    {
        var backoff = Backoff.ExponentialBackoff(TimeSpan.FromMilliseconds(100), factor: 2.0, addJitter: true, seed: 42);
        var noJitter = Backoff.ExponentialBackoff(TimeSpan.FromMilliseconds(100), factor: 2.0, addJitter: false);

        for (var i = 0; i < 10; i++)
        {
            backoff(i).Should().BeLessThanOrEqualTo(noJitter(i));
        }
    }

    [Test]
    public void LinearBackoff_Attempt0_ReturnsBaseDelay()
    {
        var backoff = Backoff.LinearBackoff(TimeSpan.FromMilliseconds(100));

        var delay = backoff(0);

        delay.Should().Be(TimeSpan.FromMilliseconds(100));
    }

    [Test]
    public void LinearBackoff_Delays_GrowLinearly()
    {
        var base_ = TimeSpan.FromMilliseconds(100);
        var backoff = Backoff.LinearBackoff(base_);

        var d0 = backoff(0);
        var d1 = backoff(1);
        var d2 = backoff(2);

        d1.Should().BeCloseTo(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(1));
        d2.Should().BeCloseTo(TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(1));
    }

    [Test]
    public void LinearBackoff_CustomIncrement_UsesIncrement()
    {
        var backoff = Backoff.LinearBackoff(TimeSpan.FromMilliseconds(100), increment: TimeSpan.FromMilliseconds(50));

        backoff(0).Should().BeCloseTo(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(1));
        backoff(1).Should().BeCloseTo(TimeSpan.FromMilliseconds(150), TimeSpan.FromMilliseconds(1));
        backoff(2).Should().BeCloseTo(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(1));
    }

    [Test]
    public void LinearBackoff_Delays_DoNotExceedMaxDelay()
    {
        var max = TimeSpan.FromSeconds(1);
        var backoff = Backoff.LinearBackoff(TimeSpan.FromMilliseconds(100), maxDelay: max);

        var delays = Enumerable.Range(0, 20).Select(backoff).ToList();

        delays.Should().AllSatisfy(d => d.Should().BeLessThanOrEqualTo(max));
    }

    [Test]
    public void ConstantBackoff_AllDelays_AreEqual()
    {
        var expected = TimeSpan.FromSeconds(1);
        var backoff = Backoff.ConstantBackoff(expected);

        var delays = Enumerable.Range(0, 10).Select(backoff).ToList();

        delays.Should().AllBeEquivalentTo(expected);
    }

    [Test]
    public void ConstantBackoff_WithJitter_DelaysVaryAroundBase()
    {
        var delay = TimeSpan.FromSeconds(1);
        var backoff = Backoff.ConstantBackoff(delay, addJitter: true, jitterFactor: 0.1, seed: 7);

        var delays = Enumerable.Range(0, 20).Select(backoff).ToList();

        delays.Should().AllSatisfy(d => d.Should().BePositive());
        delays.Should().AllSatisfy(d => d.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(1.1)));
        delays.Distinct().Should().HaveCountGreaterThan(1);
    }

    [Test]
    public void ConstantBackoff_InvalidDelay_Throws()
    {
        var act = () => Backoff.ConstantBackoff(TimeSpan.Zero);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("delay");
    }

    [Test]
    public void ConstantBackoff_InvalidJitterFactor_Throws()
    {
        var act = () => Backoff.ConstantBackoff(TimeSpan.FromSeconds(1), addJitter: true, jitterFactor: 1.5);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("jitterFactor");
    }
}
