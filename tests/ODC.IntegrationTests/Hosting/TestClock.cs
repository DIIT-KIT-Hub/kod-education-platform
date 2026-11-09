namespace ODC.IntegrationTests.Hosting;

/// <summary>
/// The clock of the application under test: real time plus an offset the tests move forward, so token lifetimes and
/// authenticator time steps can be crossed without waiting. Timers keep running on real time.
/// </summary>
public sealed class TestClock : TimeProvider
{
    /// <summary>
    /// The lock that guards <see cref="_offset"/>.
    /// </summary>
    private readonly Lock _gate = new();

    /// <summary>
    /// How far the clock is ahead of real time.
    /// </summary>
    private TimeSpan _offset;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
        {
            return System.GetUtcNow() + _offset;
        }
    }

    /// <summary>
    /// Moves the clock forward.
    /// </summary>
    /// <param name="duration">How far.</param>
    public void Advance(TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);
        lock (_gate)
        {
            _offset += duration;
        }
    }
}
