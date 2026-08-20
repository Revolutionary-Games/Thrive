using System;

/// <summary>
///   A custom command parser.
/// </summary>
public ref struct SpanTokenizer(ReadOnlySpan<char> input)
{
    private ReadOnlySpan<char> remaining = input.Trim();

    /// <summary>
    ///   Moves to the next token.
    /// </summary>
    /// <returns>false iff there are no more tokens.</returns>
    public bool MoveNext(out ReadOnlySpan<char> token, out bool isQuoted)
    {
        remaining = remaining.TrimStart();
        if (remaining.IsEmpty)
        {
            token = default;
            isQuoted = false;
            return false;
        }

        // handles strings parsing quotes
        if (remaining[0] == '"')
        {
            isQuoted = true;

            var end = remaining[1..].IndexOf('"');

            // unclosed quote, take everything
            if (end == -1)
            {
                token = remaining[1..];
                remaining = default;
            }
            else
            {
                token = remaining.Slice(1, end);
                remaining = remaining[(end + 2)..]; // skip quote + closing quote
            }
        }
        else
        {
            isQuoted = false;
            var space = remaining.IndexOf(' ');
            if (space == -1)
            {
                token = remaining;
                remaining = default;
            }
            else
            {
                token = remaining[..space];
                remaining = remaining[space..];
            }
        }

        return true;
    }
}
