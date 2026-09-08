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

            var end = IndexOfUnescapedQuote(remaining[1..]);

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

    /// <summary>
    ///   Finds the first quote that is not escaped by a backslash. A quote is escaped only when it is preceded by an
    ///   odd amount of backslashes, as an even amount means the backslashes escape each other.
    /// </summary>
    private static int IndexOfUnescapedQuote(ReadOnlySpan<char> content)
    {
        int searchStart = 0;

        while (searchStart < content.Length)
        {
            var index = content[searchStart..].IndexOf('"');

            if (index == -1)
                return -1;

            index += searchStart;

            int backslashes = 0;
            while (index - backslashes > 0 && content[index - backslashes - 1] == '\\')
                ++backslashes;

            if (backslashes % 2 == 0)
                return index;

            searchStart = index + 1;
        }

        return -1;
    }
}
