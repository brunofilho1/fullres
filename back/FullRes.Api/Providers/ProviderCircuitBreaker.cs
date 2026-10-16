using FullRes.Api.Options;

namespace FullRes.Api.Providers;

/// <summary>
/// Minimal in-memory circuit breaker for the reverse image search provider.
///
/// States:
/// - Closed: normal operation.
/// - Open: after <c>FailureThreshold</c> consecutive transient failures (timeout /
///   unavailability); calls fail fast for <c>OpenSeconds</c> without touching the provider.
/// - Half-open: after the open period, a single probe request is allowed; other concurrent
///   requests still fail fast. A successful probe closes the circuit, a failing one re-opens it.
///
/// A provider quota exhaustion (HTTP 429 "out of searches") is tracked separately: calls are
/// suspended until the announced/cooldown period elapses, then the provider is re-evaluated
/// with a normal request. Configuration errors (missing/invalid key) never open the circuit.
///
/// The state lives in process memory: with multiple API instances each instance keeps its own
/// circuit. That is acceptable for this project; a shared store would be needed only if the
/// deployment requires coordinated tripping across instances.
/// </summary>
public sealed class ProviderCircuitBreaker
{
    private static readonly TimeSpan MaxQuotaCooldown = TimeSpan.FromHours(6);
    private static readonly TimeSpan MinQuotaCooldown = TimeSpan.FromSeconds(30);

    private readonly object _lock = new();
    private readonly FullResOptions _options;
    private readonly TimeProvider _clock;

    private int _consecutiveFailures;
    private DateTimeOffset? _openUntil;
    private DateTimeOffset? _quotaUntil;
    private bool _probeInFlight;

    public ProviderCircuitBreaker(FullResOptions options, TimeProvider? clock = null)
    {
        _options = options;
        _clock = clock ?? TimeProvider.System;
    }

    public ProviderCircuitState State
    {
        get
        {
            lock (_lock)
                return SnapshotState();
        }
    }

    /// <summary>Throws a <see cref="SearchProviderException"/> when the provider must not be called.</summary>
    public void EnsureAllowed()
    {
        var resilience = _options.Resilience;
        if (!resilience.CircuitBreakerEnabled)
            return;

        lock (_lock)
        {
            var now = _clock.GetUtcNow();

            if (_quotaUntil is { } quotaUntil && now < quotaUntil)
                throw new SearchProviderException(
                    ProviderErrorKind.QuotaExceeded,
                    "The search provider quota is exhausted; calls are suspended until the cooldown elapses.",
                    retryAfterSeconds: SecondsUntil(quotaUntil, now));

            if (_openUntil is { } openUntil)
            {
                if (now < openUntil)
                    throw new SearchProviderException(
                        ProviderErrorKind.Unavailable,
                        "The search provider circuit breaker is open after repeated failures.",
                        retryAfterSeconds: SecondsUntil(openUntil, now));

                // Open period elapsed: half-open, allow a single probe.
                if (_probeInFlight)
                    throw new SearchProviderException(
                        ProviderErrorKind.Unavailable,
                        "The search provider circuit breaker is half-open; a probe request is already in flight.",
                        retryAfterSeconds: 1);
                _probeInFlight = true;
            }
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            _consecutiveFailures = 0;
            _openUntil = null;
            _quotaUntil = null;
            _probeInFlight = false;
        }
    }

    public void RecordFailure(ProviderErrorKind kind, int? retryAfterSeconds)
    {
        var resilience = _options.Resilience;
        lock (_lock)
        {
            var wasProbing = _probeInFlight;
            _probeInFlight = false;

            switch (kind)
            {
                case ProviderErrorKind.QuotaExceeded:
                    var cooldown = ClampQuotaCooldown(retryAfterSeconds);
                    _quotaUntil = _clock.GetUtcNow() + cooldown;
                    _consecutiveFailures = 0;
                    _openUntil = null;
                    break;

                case ProviderErrorKind.Timeout:
                case ProviderErrorKind.Unavailable:
                    if (!resilience.CircuitBreakerEnabled)
                        break;
                    if (wasProbing || ++_consecutiveFailures >= resilience.CircuitBreakerFailureThreshold)
                    {
                        _consecutiveFailures = 0;
                        _openUntil = _clock.GetUtcNow() + TimeSpan.FromSeconds(resilience.CircuitBreakerOpenSeconds);
                    }
                    break;

                default:
                    // Validation, authentication and rejected-image failures are not transient
                    // provider outages; they must not trip the circuit.
                    break;
            }
        }
    }

    private ProviderCircuitState SnapshotState()
    {
        if (_quotaUntil is { } quotaUntil && _clock.GetUtcNow() < quotaUntil)
            return ProviderCircuitState.QuotaExhausted;
        if (_openUntil is { } openUntil)
            return _clock.GetUtcNow() < openUntil ? ProviderCircuitState.Open : ProviderCircuitState.HalfOpen;
        return ProviderCircuitState.Closed;
    }

    private TimeSpan ClampQuotaCooldown(int? retryAfterSeconds)
    {
        var fallback = TimeSpan.FromSeconds(Math.Max(30, _options.Resilience.QuotaCooldownSeconds));
        if (retryAfterSeconds is not { } seconds || seconds <= 0)
            return fallback;
        var announced = TimeSpan.FromSeconds(seconds);
        if (announced < MinQuotaCooldown)
            return MinQuotaCooldown;
        return announced > MaxQuotaCooldown ? MaxQuotaCooldown : announced;
    }

    private static int SecondsUntil(DateTimeOffset target, DateTimeOffset now)
    {
        var seconds = (int)Math.Ceiling((target - now).TotalSeconds);
        return Math.Max(1, seconds);
    }
}

public enum ProviderCircuitState
{
    Closed,
    Open,
    HalfOpen,
    QuotaExhausted
}
