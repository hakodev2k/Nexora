namespace Nexora.UnitTests;

internal sealed class TestRunner
{
    private readonly List<TestCase> _tests = new();

    public void Add(string name, Action test) => _tests.Add(new(name, test, null, null));

    public void AddWindows(string name, Action test) => AddForPlatform(name, "windows", OperatingSystem.IsWindows(), test);

    public void AddLinux(string name, Action test) => AddForPlatform(name, "linux", OperatingSystem.IsLinux(), test);

    public void Skip(string name, string reason, string? platform = null) =>
        _tests.Add(new(name, null, reason, platform));

    public int Run(string[]? args = null)
    {
        var requiredPlatform = ReadRequiredPlatform(args ?? Array.Empty<string>());
        if (requiredPlatform is not (null or "windows" or "linux"))
        {
            Console.Error.WriteLine($"[fail] unsupported --require-os value '{requiredPlatform}'. Use windows or linux.");
            return 2;
        }

        var failed = 0;
        var passed = 0;
        var skipped = 0;
        var requiredPlatformSkips = 0;
        foreach (var testCase in _tests)
        {
            if (testCase.SkipReason is not null)
            {
                skipped++;
                if (requiredPlatform is not null &&
                    string.Equals(requiredPlatform, testCase.Platform, StringComparison.Ordinal))
                {
                    requiredPlatformSkips++;
                }

                Console.WriteLine($"[skip] {testCase.Name}: {testCase.SkipReason}");
                continue;
            }

            try
            {
                testCase.Test!();
                passed++;
                Console.WriteLine($"[pass] {testCase.Name}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.Error.WriteLine($"[fail] {testCase.Name}: {ex.Message}");
            }
        }

        Console.WriteLine($"Discovered {_tests.Count} tests; passed {passed}; failed {failed}; skipped {skipped}.");
        if (requiredPlatformSkips > 0)
        {
            Console.Error.WriteLine($"Required platform '{requiredPlatform}' has {requiredPlatformSkips} skipped test(s); the suite is not executable on this runner.");
            return 1;
        }

        return failed == 0 ? 0 : 1;
    }

    private void AddForPlatform(string name, string platform, bool available, Action test)
    {
        if (available)
        {
            _tests.Add(new(name, test, null, platform));
            return;
        }

        Skip(name, $"requires an actual {platform} process; current OS is '{Environment.OSVersion.Platform}'.", platform);
    }

    private static string? ReadRequiredPlatform(string[] args)
    {
        for (var index = 0; index < args.Length; index++)
        {
            if (string.Equals(args[index], "--require-os", StringComparison.Ordinal) && index + 1 < args.Length)
            {
                return args[index + 1].Trim().ToLowerInvariant();
            }

            if (args[index].StartsWith("--require-os=", StringComparison.Ordinal))
            {
                return args[index]["--require-os=".Length..].Trim().ToLowerInvariant();
            }
        }

        return null;
    }

    private sealed record TestCase(string Name, Action? Test, string? SkipReason, string? Platform);
}

internal static class AssertEx
{
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void False(bool condition, string message) => True(!condition, message);

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}. Expected '{expected}', got '{actual}'.");
        }
    }

    public static void Throws<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }
}
