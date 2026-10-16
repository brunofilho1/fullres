using FullRes.Api.Options;
using FullRes.Api.Providers;

namespace FullRes.Tests.Providers;

public class ProviderCircuitBreakerTests
{
    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }

    private static (ProviderCircuitBreaker Breaker, FakeTimeProvider Clock, FullResOptions Options) Create(
        Action<ResilienceOptions>? configure = null)
    {
        var options = new FullResOptions();
        configure?.Invoke(options.Resilience);
        var clock = new FakeTimeProvider();
        return (new ProviderCircuitBreaker(options, clock), clock, options);
    }

    [Fact]
    public void OpensAfterThresholdAndRejectsCalls()
    {
        var (breaker, _, _) = Create(r =>
        {
            r.CircuitBreakerFailureThreshold = 2;
            r.CircuitBreakerOpenSeconds = 30;
        });

        breaker.RecordFailure(ProviderErrorKind.Unavailable, null);
        breaker.EnsureAllowed();

        breaker.RecordFailure(ProviderErrorKind.Unavailable, null);

        Assert.Equal(ProviderCircuitState.Open, breaker.State);
        var ex = Assert.Throws<SearchProviderException>(() => breaker.EnsureAllowed());
        Assert.Equal(ProviderErrorKind.Unavailable, ex.Kind);
        Assert.True(ex.RetryAfterSeconds is > 0);
    }

    [Fact]
    public void RecoversAfterOpenPeriod()
    {
        var (breaker, clock, _) = Create(r =>
        {
            r.CircuitBreakerFailureThreshold = 1;
            r.CircuitBreakerOpenSeconds = 30;
        });

        breaker.RecordFailure(ProviderErrorKind.Timeout, null);
        Assert.Throws<SearchProviderException>(() => breaker.EnsureAllowed());

        clock.Advance(TimeSpan.FromSeconds(31));

        breaker.EnsureAllowed();
        breaker.RecordSuccess();
        Assert.Equal(ProviderCircuitState.Closed, breaker.State);
    }

    [Fact]
    public void HalfOpenAllowsOnlyOneProbe()
    {
        var (breaker, clock, _) = Create(r =>
        {
            r.CircuitBreakerFailureThreshold = 1;
            r.CircuitBreakerOpenSeconds = 10;
        });

        breaker.RecordFailure(ProviderErrorKind.Unavailable, null);
        clock.Advance(TimeSpan.FromSeconds(11));

        breaker.EnsureAllowed();
        Assert.Throws<SearchProviderException>(() => breaker.EnsureAllowed());

        breaker.RecordFailure(ProviderErrorKind.Unavailable, null);
        Assert.Equal(ProviderCircuitState.Open, breaker.State);
    }

    [Fact]
    public void QuotaExhaustionSuspendsCallsForCooldown()
    {
        var (breaker, clock, _) = Create();

        breaker.RecordFailure(ProviderErrorKind.QuotaExceeded, retryAfterSeconds: 120);
        Assert.Equal(ProviderCircuitState.QuotaExhausted, breaker.State);

        var ex = Assert.Throws<SearchProviderException>(() => breaker.EnsureAllowed());
        Assert.Equal(ProviderErrorKind.QuotaExceeded, ex.Kind);
        Assert.True(ex.RetryAfterSeconds is > 100 and <= 120);

        clock.Advance(TimeSpan.FromSeconds(121));
        breaker.EnsureAllowed();
        breaker.RecordSuccess();
        Assert.Equal(ProviderCircuitState.Closed, breaker.State);
    }

    [Fact]
    public void ValidationFailuresDoNotOpenCircuit()
    {
        var (breaker, _, _) = Create(r => r.CircuitBreakerFailureThreshold = 2);

        for (var i = 0; i < 10; i++)
        {
            breaker.RecordFailure(ProviderErrorKind.Unauthorized, null);
            breaker.RecordFailure(ProviderErrorKind.RejectedImage, null);
            breaker.RecordFailure(ProviderErrorKind.BadRequest, null);
        }

        Assert.Equal(ProviderCircuitState.Closed, breaker.State);
        breaker.EnsureAllowed();
    }

    [Fact]
    public void DisabledBreakerNeverBlocks()
    {
        var (breaker, _, _) = Create(r =>
        {
            r.CircuitBreakerEnabled = false;
            r.CircuitBreakerFailureThreshold = 1;
        });

        for (var i = 0; i < 5; i++)
            breaker.RecordFailure(ProviderErrorKind.Unavailable, null);

        breaker.EnsureAllowed();
        Assert.Equal(ProviderCircuitState.Closed, breaker.State);
    }
}
