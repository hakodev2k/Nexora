using System.Security.Cryptography;
using System.Text;

namespace Nexora.Api.Security;

/// <summary>
/// Bounded local-process admission for the contracted authentication limits.
/// In-flight credential checks reserve failure capacity until their result is
/// known, preventing concurrent requests from bypassing the failure threshold.
/// No raw subject, address, session or credential is retained in counter keys.
/// </summary>
public sealed class AuthenticationRequestThrottle(TimeProvider clock)
{
    private const int MaximumCounters = 4096;
    private readonly object _gate = new();
    private readonly Dictionary<string, Counter> _counters = new(StringComparer.Ordinal);
    private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);

    public Admission? TryAcquire(string path, string subject, string source, out int retryAfterSeconds)
    {
        var policy = PolicyFor(path);
        retryAfterSeconds = 0;
        if (policy is null) return new Admission(null, null, false);

        var selected = policy.Value;
        var now = clock.GetUtcNow();
        var sourceKey = Digest($"{selected.Family}:ip:{source}");
        var subjectKey = selected.SubjectLimit > 0
            ? Digest($"{selected.Family}:subject:{subject}|ip:{(selected.FailuresOnly ? source : string.Empty)}")
            : null;

        lock (_gate)
        {
            // Bounded storage, including arbitrary invalid/unknown subjects and
            // clients. Saturation fails closed instead of evicting live limits.
            foreach (var key in _counters.Where(pair => pair.Value.Pending == 0 &&
                         now >= pair.Value.ExpiresAt).Select(pair => pair.Key).ToArray())
                _counters.Remove(key);

            var missing = (_counters.ContainsKey(sourceKey) ? 0 : 1) +
                          (subjectKey is not null && !_counters.ContainsKey(subjectKey) ? 1 : 0);
            if (_counters.Count + missing > MaximumCounters)
            {
                retryAfterSeconds = 60;
                return null;
            }

            var sourceCounter = GetCounter(sourceKey, now, selected.Window);
            var subjectCounter = subjectKey is null ? null : GetCounter(subjectKey, now, selected.Window);
            if (sourceCounter.Count >= selected.SourceLimit ||
                subjectCounter is not null && subjectCounter.Count + subjectCounter.Pending >= selected.SubjectLimit)
            {
                var expiry = sourceCounter.Count >= selected.SourceLimit ? sourceCounter.ExpiresAt : subjectCounter!.ExpiresAt;
                retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((expiry - now).TotalSeconds));
                return null;
            }

            sourceCounter.Count++;
            if (subjectCounter is not null)
            {
                if (selected.FailuresOnly) subjectCounter.Pending++;
                else subjectCounter.Count++;
            }

            return new Admission(this, subjectCounter, selected.FailuresOnly);
        }
    }

    private Counter GetCounter(string key, DateTimeOffset now, TimeSpan window)
    {
        if (!_counters.TryGetValue(key, out var value))
            _counters.Add(key, value = new Counter(now.Add(window)));
        return value;
    }

    private string Digest(string value) => Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(value)));

    private static Policy? PolicyFor(string path)
    {
        path = path.TrimEnd('/');
        if (path.EndsWith("/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("/auth/reauth", StringComparison.OrdinalIgnoreCase))
            return new("credentials", 100, 10, TimeSpan.FromMinutes(15), true);
        if (path.EndsWith("/auth/registrations", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("/auth/verifications/resend", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("/auth/password-resets", StringComparison.OrdinalIgnoreCase))
            return new("submissions", 20, 5, TimeSpan.FromMinutes(15), false);
        // Retain the additional existing proof-endpoint guard, distinct from
        // the email-submission and credential-failure contract above.
        if (path.EndsWith("/auth/verifications", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("/auth/password-resets/confirm", StringComparison.OrdinalIgnoreCase))
            return new("proof:" + path.ToUpperInvariant(), 5, 0, TimeSpan.FromMinutes(5), false);
        return null;
    }

    private readonly record struct Policy(string Family, int SourceLimit, int SubjectLimit, TimeSpan Window, bool FailuresOnly);
    internal sealed class Counter(DateTimeOffset expiresAt)
    {
        public DateTimeOffset ExpiresAt { get; } = expiresAt;
        public int Count { get; set; }
        public int Pending { get; set; }
    }

    public sealed class Admission : IDisposable
    {
        private readonly AuthenticationRequestThrottle? _owner;
        private readonly Counter? _subject;
        private readonly bool _tracksFailures;
        private bool _completed;

        internal Admission(AuthenticationRequestThrottle? owner, Counter? subject, bool tracksFailures)
        {
            _owner = owner;
            _subject = subject;
            _tracksFailures = tracksFailures;
        }

        public void Complete(int? statusCode)
        {
            if (_owner is null) return;
            lock (_owner._gate)
            {
                if (_completed) return;
                _completed = true;
                if (_tracksFailures && _subject is not null)
                {
                    _subject.Pending--;
                    if (statusCode == StatusCodes.Status401Unauthorized) _subject.Count++;
                }
            }
        }

        public void Dispose() => Complete(null);
    }
}
