namespace Nexora.UnitTests;

internal sealed class TestRunner
{
    private readonly List<(string Name, Action Test)> _tests = new();

    public void Add(string name, Action test) => _tests.Add((name, test));

    public int Run()
    {
        var failed = 0;
        foreach (var (name, test) in _tests)
        {
            try
            {
                test();
                Console.WriteLine($"[pass] {name}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.Error.WriteLine($"[fail] {name}: {ex.Message}");
            }
        }

        Console.WriteLine($"Executed {_tests.Count} unit tests; failed {failed}.");
        return failed == 0 ? 0 : 1;
    }
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
