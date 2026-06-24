# PollyBackoff

<img src="icon.png" width="100" align="right" />

[![NuGet](https://img.shields.io/nuget/v/PollyBackoff.svg)](https://www.nuget.org/packages/PollyBackoff)
[![NuGet Downloads](https://img.shields.io/nuget/dt/PollyBackoff.svg)](https://www.nuget.org/packages/PollyBackoff)
[![CI](https://github.com/Swevo/PollyBackoff/actions/workflows/build.yml/badge.svg)](https://github.com/Swevo/PollyBackoff/actions/workflows/build.yml)

Backoff delay strategies for **Polly v8** resilience pipelines.

`Polly.Contrib.WaitAndRetry` was built for Polly v7's `WaitAndRetry()` API. Polly v8 uses a `DelayGenerator` delegate — this package provides the same beloved strategies in the new API.

## Install

```
dotnet add package PollyBackoff
```

## Usage

### Fluent extension on `RetryStrategyOptions`

```csharp
using PollyBackoff;

var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
    .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 5,
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .HandleResult(r => !r.IsSuccessStatusCode)
    }
    .UseDecorrelatedJitter(baseDelay: TimeSpan.FromMilliseconds(100)))
    .Build();
```

### Direct `Backoff` factory

```csharp
var backoff = Backoff.DecorrelatedJitter(baseDelay: TimeSpan.FromMilliseconds(100));

var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 5,
        DelayGenerator = args => new ValueTask<TimeSpan?>(backoff(args.AttemptNumber))
    })
    .Build();
```

## Strategies

### Decorrelated Jitter (recommended)

Based on the algorithm from [Marc Brooker's blog](https://brooker.co.za/blog/2015/03/21/backoff.html) and AWS guidance. Each delay is randomly chosen from `[baseDelay, previous × factor]`, capped at `maxDelay`. Avoids retry storms by spreading attempts across time.

```csharp
options.UseDecorrelatedJitter(
    baseDelay: TimeSpan.FromMilliseconds(100),
    factor: 3.0,              // default
    maxDelay: TimeSpan.FromSeconds(30));  // default
```

### Exponential Backoff

`delay = min(maxDelay, baseDelay × factor^attempt)`, with optional full jitter.

```csharp
options.UseExponentialBackoff(
    baseDelay: TimeSpan.FromMilliseconds(100),
    factor: 2.0,              // default
    maxDelay: TimeSpan.FromSeconds(30),
    addJitter: true);
```

### Linear Backoff

`delay = baseDelay + increment × attempt`, capped at `maxDelay`.

```csharp
options.UseLinearBackoff(
    baseDelay: TimeSpan.FromMilliseconds(100),
    increment: TimeSpan.FromMilliseconds(100),  // defaults to baseDelay
    maxDelay: TimeSpan.FromSeconds(10),
    addJitter: false);
```

### Constant Backoff

Fixed delay every attempt, with optional ±jitter.

```csharp
options.UseConstantBackoff(
    delay: TimeSpan.FromSeconds(1),
    addJitter: true,
    jitterFactor: 0.1);  // ±10%
```

## Composing with existing `DelayGenerator`

Each strategy also exposes a `Func<int, TimeSpan>` you can use directly:

```csharp
var backoff = Backoff.ExponentialBackoff(TimeSpan.FromMilliseconds(100), addJitter: true);

// attempt 0 → ~100ms, attempt 1 → ~200ms, attempt 2 → ~400ms (with jitter)
TimeSpan delay = backoff(attemptNumber);
```

## Support

If PollyBackoff saves you time — especially if you're migrating from Polly.Contrib.WaitAndRetry — consider supporting the project:

[![Sponsor](https://img.shields.io/badge/Sponsor-%E2%9D%A4-pink?logo=github)](https://github.com/sponsors/Swevo)

> 💼 **Need .NET resilience help?** Visit [solidqualitysolutions.com](https://solidqualitysolutions.com/) for consulting and architecture services.

## Related packages

| Package | Description |
|---|---|
| [PollyChaos](https://www.nuget.org/packages/PollyChaos) | Chaos engineering — inject faults & latency (Simmy for v8) |
| [PollyMediatR](https://www.nuget.org/packages/PollyMediatR) | Polly v8 pipelines for MediatR request handlers |
| [PollyEFCore](https://www.nuget.org/packages/PollyEFCore) | Polly v8 resilience for EF Core queries and SaveChanges |
| [PollyHealthChecks](https://www.nuget.org/packages/PollyHealthChecks) | [![Downloads](https://img.shields.io/nuget/dt/PollyHealthChecks.svg)](https://www.nuget.org/packages/PollyHealthChecks) | ASP.NET Core health checks for Polly v8 circuit breakers |
| [PollyOpenAI](https://www.nuget.org/packages/PollyOpenAI) | [![Downloads](https://img.shields.io/nuget/dt/PollyOpenAI.svg)](https://www.nuget.org/packages/PollyOpenAI) | Polly v8 resilience for OpenAI and Azure OpenAI — retry on 429, Retry-After, circuit breaker |
| [PollyRedis](https://www.nuget.org/packages/PollyRedis) | [![Downloads](https://img.shields.io/nuget/dt/PollyRedis.svg)](https://www.nuget.org/packages/PollyRedis) | Polly v8 resilience for StackExchange.Redis — retry, circuit breaker, timeout |
| [PollySignalR](https://www.nuget.org/packages/PollySignalR) | [![Downloads](https://img.shields.io/nuget/dt/PollySignalR.svg)](https://www.nuget.org/packages/PollySignalR) | Polly v8 exponential back-off reconnect policy for SignalR HubConnection |
| [PollyGrpc](https://www.nuget.org/packages/PollyGrpc) | Polly v8 resilience (retry, CB, timeout) for gRPC .NET clients via Interceptor |
| [PollyKafka](https://www.nuget.org/packages/PollyKafka) | Polly v8 resilience (retry, CB, timeout) for Confluent.Kafka producers and consumers |
| [PollyAzureEventHub](https://github.com/Swevo/PollyAzureEventHub) | Polly v8 for Azure Event Hubs |
| [PollyAzureServiceBus](https://www.nuget.org/packages/PollyAzureServiceBus) | Polly v8 resilience (retry, CB, timeout) for Azure Service Bus senders and receivers |
| [PollyCaching](https://www.nuget.org/packages/PollyCaching) | Caching resilience strategy |
| [PollyBulkhead](https://www.nuget.org/packages/PollyBulkhead) | Bulkhead isolation |
| [PollyRateLimiter](https://www.nuget.org/packages/PollyRateLimiter) | Rate limiting strategies |
| [PollyOpenTelemetry](https://www.nuget.org/packages/PollyOpenTelemetry) | OpenTelemetry metrics & tracing |

| [PollyRabbitMQ](https://www.nuget.org/packages/PollyRabbitMQ) | Polly v8 resilience for RabbitMQ.Client channels |

| [PollyElasticsearch](https://github.com/Swevo/PollyElasticsearch) | Polly v8 for Elastic.Clients.Elasticsearch |

| [PollyAzureKeyVault](https://github.com/Swevo/PollyAzureKeyVault) | Polly v8 for Azure Key Vault |

| [PollySendGrid](https://github.com/Swevo/PollySendGrid) | Polly v8 for SendGrid |

| [PollyMassTransit](https://github.com/Swevo/PollyMassTransit) | Polly v8 for MassTransit |

| [PollyAzureTableStorage](https://github.com/Swevo/PollyAzureTableStorage) | Polly v8 for Azure Table Storage |

| [PollyMailKit](https://github.com/Swevo/PollyMailKit) | MailKit SMTP email client |
| [PollyAzureQueueStorage](https://github.com/Swevo/PollyAzureQueueStorage) | Azure Queue Storage QueueClient |
| [PollyHangfire](https://github.com/Swevo/PollyHangfire) | Hangfire IBackgroundJobClient |
## License

MIT
