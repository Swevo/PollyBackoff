// <copyright file="RetryStrategyOptionsExtensions.cs" company="Justin Bannister">
// Copyright (c) Justin Bannister. All rights reserved.
// </copyright>

using Polly.Retry;

namespace PollyBackoff;

/// <summary>
/// Extension methods for <see cref="RetryStrategyOptions{TResult}"/> to apply backoff strategies.
/// </summary>
public static class RetryStrategyOptionsExtensions
{
    /// <summary>
    /// Sets <see cref="RetryStrategyOptions{TResult}.DelayGenerator"/> from a backoff function
    /// produced by <see cref="Backoff"/> (e.g. <see cref="Backoff.DecorrelatedJitter"/>).
    /// </summary>
    public static RetryStrategyOptions<TResult> UseBackoff<TResult>(
        this RetryStrategyOptions<TResult> options,
        Func<int, TimeSpan> backoff)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(backoff);

        options.DelayGenerator = args => new ValueTask<TimeSpan?>(backoff(args.AttemptNumber));
        return options;
    }

    /// <summary>
    /// Applies decorrelated jitter backoff to this retry strategy.
    /// </summary>
    public static RetryStrategyOptions<TResult> UseDecorrelatedJitter<TResult>(
        this RetryStrategyOptions<TResult> options,
        TimeSpan baseDelay,
        double factor = 3.0,
        TimeSpan? maxDelay = null,
        int? seed = null) =>
        options.UseBackoff(Backoff.DecorrelatedJitter(baseDelay, factor, maxDelay, seed));

    /// <summary>
    /// Applies exponential backoff to this retry strategy.
    /// </summary>
    public static RetryStrategyOptions<TResult> UseExponentialBackoff<TResult>(
        this RetryStrategyOptions<TResult> options,
        TimeSpan baseDelay,
        double factor = 2.0,
        TimeSpan? maxDelay = null,
        bool addJitter = false,
        int? seed = null) =>
        options.UseBackoff(Backoff.ExponentialBackoff(baseDelay, factor, maxDelay, addJitter, seed));

    /// <summary>
    /// Applies linear backoff to this retry strategy.
    /// </summary>
    public static RetryStrategyOptions<TResult> UseLinearBackoff<TResult>(
        this RetryStrategyOptions<TResult> options,
        TimeSpan baseDelay,
        TimeSpan? increment = null,
        TimeSpan? maxDelay = null,
        bool addJitter = false,
        int? seed = null) =>
        options.UseBackoff(Backoff.LinearBackoff(baseDelay, increment, maxDelay, addJitter, seed));

    /// <summary>
    /// Applies constant backoff to this retry strategy.
    /// </summary>
    public static RetryStrategyOptions<TResult> UseConstantBackoff<TResult>(
        this RetryStrategyOptions<TResult> options,
        TimeSpan delay,
        bool addJitter = false,
        double jitterFactor = 0.1,
        int? seed = null) =>
        options.UseBackoff(Backoff.ConstantBackoff(delay, addJitter, jitterFactor, seed));
}
