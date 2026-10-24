using Nexora.Api.Security;
using Xunit;

namespace Nexora.UnitTests;

public sealed class AuthenticationThrottlePolicyTests
{
    [Fact]
    public void Credential_failures_are_subject_and_source_scoped_with_exact_expiry()
    {
        var clock = new ManualClock();
        var throttle = new AuthenticationRequestThrottle(clock);
        for (var index = 0; index < 10; index++)
            throttle.TryAcquire("/auth/login", "subject-a", "source-a", out _)!.Complete(401);
        Assert.Null(throttle.TryAcquire("/auth/login", "subject-a", "source-a", out var retry));
        Assert.Equal(900, retry);
        using var anotherSubject = throttle.TryAcquire("/auth/login", "subject-b", "source-a", out _);
        using var anotherSource = throttle.TryAcquire("/auth/login", "subject-a", "source-b", out _);
        Assert.NotNull(anotherSubject);
        Assert.NotNull(anotherSource);
        clock.Advance(TimeSpan.FromMinutes(15) - TimeSpan.FromTicks(1));
        Assert.Null(throttle.TryAcquire("/auth/login", "subject-a", "source-a", out _));
        clock.Advance(TimeSpan.FromTicks(1));
        using var renewed = throttle.TryAcquire("/auth/login", "subject-a", "source-a", out _);
        Assert.NotNull(renewed);
    }

    [Fact]
    public void Successful_checks_release_failure_slots_but_keep_the_hundred_per_source_cap()
    {
        var throttle = new AuthenticationRequestThrottle(new ManualClock());
        for (var index = 0; index < 100; index++)
        {
            using var admission = throttle.TryAcquire("/auth/login", "subject", "source", out _);
            Assert.NotNull(admission);
            admission.Complete(200);
        }
        Assert.Null(throttle.TryAcquire("/auth/login", "other-subject", "source", out _));
    }

    [Fact]
    public void Concurrent_credential_requests_reserve_capacity_and_exception_disposal_releases_it()
    {
        var throttle = new AuthenticationRequestThrottle(new ManualClock());
        var admissions = Enumerable.Range(0, 10).AsParallel()
            .Select(_ => throttle.TryAcquire("/auth/login", "subject", "source", out _)).ToArray();
        Assert.All(admissions, admission => Assert.NotNull(admission));
        Assert.Null(throttle.TryAcquire("/auth/login", "subject", "source", out _));
        foreach (var admission in admissions) admission!.Dispose();
        using var released = throttle.TryAcquire("/auth/login", "subject", "source", out _);
        Assert.NotNull(released);
    }

    [Theory]
    [InlineData("/auth/registrations")]
    [InlineData("/auth/verifications/resend/")]
    [InlineData("/AUTH/PASSWORD-RESETS")]
    public void Email_submissions_have_five_per_subject_and_twenty_per_source_limits(string path)
    {
        var throttle = new AuthenticationRequestThrottle(new ManualClock());
        for (var index = 0; index < 5; index++)
            throttle.TryAcquire(path, "subject", "source-a", out _)!.Complete(202);
        Assert.Null(throttle.TryAcquire(path, "subject", "source-b", out _));
        for (var index = 0; index < 15; index++)
            throttle.TryAcquire(path, $"subject-{index}", "source-a", out _)!.Complete(202);
        Assert.Null(throttle.TryAcquire(path, "new-subject", "source-a", out var retry));
        Assert.Equal(900, retry);
    }

    [Fact]
    public void Unknown_subjects_and_counter_saturation_fail_closed_and_expired_capacity_recovers()
    {
        var clock = new ManualClock();
        var throttle = new AuthenticationRequestThrottle(clock);
        for (var index = 0; index < 2048; index++)
            throttle.TryAcquire("/auth/login", $"unknown-{index}", $"source-{index}", out _)!.Complete(401);
        Assert.Null(throttle.TryAcquire("/auth/login", "new-unknown", "new-source", out var retry));
        Assert.Equal(60, retry);
        clock.Advance(TimeSpan.FromMinutes(15));
        using var recovered = throttle.TryAcquire("/auth/login", "new-unknown", "new-source", out _);
        Assert.NotNull(recovered);
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 25, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }
}
