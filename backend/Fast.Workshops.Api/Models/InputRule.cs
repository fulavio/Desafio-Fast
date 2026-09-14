using System.Globalization;

namespace Fast.Workshops.Api.Models;

public static class InputRule
{
    /// <summary>Rejects empty text and trims it; e.g. " Ana " becomes "Ana".</summary>
    public static string RequiredText(string? received, string field)
    {
        if (string.IsNullOrWhiteSpace(received))
            throw new InputException($"{field} recebido '{received ?? "null"}'; esperado texto não vazio.");
        return received.Trim();
    }

    /// <summary>Accepts positive identifiers; e.g. 1.</summary>
    public static int PositiveId(int received)
    {
        if (received <= 0)
            throw new InputException($"ID recebido '{received}'; esperado inteiro positivo.");
        return received;
    }

    /// <summary>Parses a timestamp with offset; e.g. 2026-10-08T16:00:00-03:00.</summary>
    public static DateTimeOffset Timestamp(string? received)
    {
        string[] formats = ["yyyy-MM-dd'T'HH:mm:sszzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz",
            "yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"];
        if (!DateTimeOffset.TryParseExact(received, formats, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out var timestamp))
            throw new InputException($"heldAt recebido '{received ?? "null"}'; esperado ISO 8601 com horário e fuso.");
        return timestamp;
    }
}

public sealed class InputException(string message) : Exception(message);
public sealed class MissingResourceException(string message) : Exception(message);
public sealed class DuplicateAttendanceException(string message) : Exception(message);
