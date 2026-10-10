namespace FixtureHub.Infrastructure.Reading;

internal static class SqlLike
{
    public static string Contains(string text) => $"%{Escape(text.Trim())}%";

    private static string Escape(string text) =>
        text.Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);
}
