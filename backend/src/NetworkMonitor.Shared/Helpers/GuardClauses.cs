namespace NetworkMonitor.Shared.Helpers;

/// <summary>Small argument-guard helpers to keep constructors/methods terse across layers.</summary>
public static class GuardClauses
{
    public static string NotNullOrEmpty(string? value, string paramName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"'{paramName}' cannot be null or empty.", paramName)
            : value;

    public static T NotNull<T>(T? value, string paramName) where T : class =>
        value ?? throw new ArgumentNullException(paramName);
}
