// <copyright file="Backoff.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

using Polly.Retry;

namespace PollyBackoff;

/// <summary>
/// Provides backoff delay strategies for use with Polly v8 <see cref="RetryStrategyOptions{TResult}.DelayGenerator"/>.
/// </summary>
public static class Backoff
{
    /// <summary>
    /// Decorrelated jitter backoff V2 (Marc Brooker / AWS recommendation).
    /// Each delay is randomly chosen from [<paramref name="baseDelay"/>, previous * <paramref name="factor"/>],
    /// capped at <paramref name="maxDelay"/>. Avoids retry storms by spreading attempts across time.
    /// </summary>
    public static Func<int, TimeSpan> DecorrelatedJitter(
        TimeSpan baseDelay,
        double factor = 3.0,
        TimeSpan? maxDelay = null,
        int? seed = null)
    {
        if (baseDelay <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(baseDelay), "Must be positive.");
        if (factor <= 1.0) throw new ArgumentOutOfRangeException(nameof(factor), "Must be greater than 1.");

        var cap = maxDelay ?? TimeSpan.FromSeconds(30);
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        var previous = baseDelay;
        var syncRoot = new object();

        return attempt =>
        {
            lock (syncRoot)
            {
                var low = baseDelay.TotalMilliseconds;
                var high = Math.Min(cap.TotalMilliseconds, previous.TotalMilliseconds * factor);
                if (high < low) high = low;
                var next = TimeSpan.FromMilliseconds(rng.NextDouble() * (high - low) + low);
                previous = next;
                return next;
            }
        };
    }

    /// <summary>
    /// Exponential backoff: delay = <paramref name="baseDelay"/> * <paramref name="factor"/>^attempt,
    /// capped at <paramref name="maxDelay"/>, with optional full jitter.
    /// </summary>
    public static Func<int, TimeSpan> ExponentialBackoff(
        TimeSpan baseDelay,
        double factor = 2.0,
        TimeSpan? maxDelay = null,
        bool addJitter = false,
        int? seed = null)
    {
        if (baseDelay <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(baseDelay), "Must be positive.");
        if (factor <= 1.0) throw new ArgumentOutOfRangeException(nameof(factor), "Must be greater than 1.");

        var cap = maxDelay ?? TimeSpan.FromSeconds(30);
        var rng = addJitter ? (seed.HasValue ? new Random(seed.Value) : new Random()) : null;

        return attempt =>
        {
            var exp = Math.Min(cap.TotalMilliseconds, baseDelay.TotalMilliseconds * Math.Pow(factor, attempt));
            if (rng != null)
                exp = rng.NextDouble() * exp;
            return TimeSpan.FromMilliseconds(exp);
        };
    }

    /// <summary>
    /// Linear backoff: delay = <paramref name="baseDelay"/> + <paramref name="increment"/> * attempt,
    /// capped at <paramref name="maxDelay"/>, with optional jitter.
    /// </summary>
    public static Func<int, TimeSpan> LinearBackoff(
        TimeSpan baseDelay,
        TimeSpan? increment = null,
        TimeSpan? maxDelay = null,
        bool addJitter = false,
        int? seed = null)
    {
        if (baseDelay <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(baseDelay), "Must be positive.");

        var step = increment ?? baseDelay;
        var cap = maxDelay ?? TimeSpan.MaxValue;
        var rng = addJitter ? (seed.HasValue ? new Random(seed.Value) : new Random()) : null;

        return attempt =>
        {
            var ms = baseDelay.TotalMilliseconds + step.TotalMilliseconds * attempt;
            ms = Math.Min(ms, cap == TimeSpan.MaxValue ? double.MaxValue : cap.TotalMilliseconds);
            if (rng != null)
                ms += rng.NextDouble() * step.TotalMilliseconds;
            return TimeSpan.FromMilliseconds(ms);
        };
    }

    /// <summary>
    /// Constant backoff: every retry waits the same <paramref name="delay"/>, with optional jitter of ±<paramref name="jitterFactor"/>.
    /// </summary>
    public static Func<int, TimeSpan> ConstantBackoff(
        TimeSpan delay,
        bool addJitter = false,
        double jitterFactor = 0.1,
        int? seed = null)
    {
        if (delay <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delay), "Must be positive.");
        if (jitterFactor is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(jitterFactor), "Must be between 0 and 1.");

        var rng = addJitter ? (seed.HasValue ? new Random(seed.Value) : new Random()) : null;

        return _ =>
        {
            if (rng == null) return delay;
            var jitter = delay.TotalMilliseconds * jitterFactor * (rng.NextDouble() * 2 - 1);
            return TimeSpan.FromMilliseconds(Math.Max(0, delay.TotalMilliseconds + jitter));
        };
    }
}
