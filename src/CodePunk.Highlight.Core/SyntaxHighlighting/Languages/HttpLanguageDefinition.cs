using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.Core.SyntaxHighlighting.Languages;

/// <summary>
/// HTTP language definition for syntax highlighting.
/// Provides a tokenizer for HTTP requests and responses with support for methods, headers, and status codes.
/// </summary>
public class HttpLanguageDefinition : ILanguageDefinition
{
    public string Name => "http";
    public string[] Aliases => Array.Empty<string>();

    private static readonly HashSet<string> Methods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS", "CONNECT", "TRACE"
    };

    private static readonly HashSet<string> CommonHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accept", "Accept-Encoding", "Accept-Language", "Authorization", "Cache-Control",
        "Content-Type", "Content-Length", "Content-Encoding", "Cookie", "Host", "User-Agent",
        "Referer", "Connection", "Date", "ETag", "Expires", "Last-Modified", "Location",
        "Server", "Set-Cookie", "Transfer-Encoding", "Vary", "WWW-Authenticate"
    };

    public bool Matches(string languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId)) return false;
        var normalized = languageId.ToLowerInvariant();
        return normalized == Name;
    }

    public IEnumerable<Token> Tokenize(ReadOnlySpan<char> source)
    {
        var tokens = new List<Token>();
        var pos = 0;
        var isFirstLine = true;
        var inHeaders = true;

        while (pos < source.Length)
        {
            var ch = source[pos];

            // Handle line breaks
            if (ch == '\n')
            {
                tokens.Add(new Token(TokenType.Text, "\n"));
                pos++;
                isFirstLine = false;

                // Check if we're entering body (empty line after headers)
                if (inHeaders && pos < source.Length && source[pos] == '\n')
                {
                    tokens.Add(new Token(TokenType.Text, "\n"));
                    pos++;
                    inHeaders = false;
                }
                continue;
            }

            if (ch == '\r')
            {
                tokens.Add(new Token(TokenType.Text, "\r"));
                pos++;
                continue;
            }

            // Whitespace (except newlines)
            if (char.IsWhiteSpace(ch))
            {
                var wsStart = pos;
                while (pos < source.Length && char.IsWhiteSpace(source[pos]) && source[pos] != '\n' && source[pos] != '\r')
                    pos++;
                tokens.Add(new Token(TokenType.Text, source.Slice(wsStart, pos - wsStart).ToString()));
                continue;
            }

            // First line: HTTP method or HTTP version
            if (isFirstLine && inHeaders)
            {
                var lineStart = pos;
                var lineEnd = pos;
                while (lineEnd < source.Length && source[lineEnd] != '\n' && source[lineEnd] != '\r')
                    lineEnd++;

                var line = source.Slice(lineStart, lineEnd - lineStart);

                // Check for HTTP method (request line)
                var spaceIdx = line.IndexOf(' ');
                if (spaceIdx > 0)
                {
                    var firstWord = line.Slice(0, spaceIdx).ToString();

                    if (Methods.Contains(firstWord))
                    {
                        // HTTP Request: METHOD /path HTTP/version
                        tokens.Add(new Token(TokenType.Keyword, firstWord));
                        tokens.Add(new Token(TokenType.Text, " "));
                        pos += spaceIdx + 1;

                        // Parse URL path
                        var pathStart = pos;
                        while (pos < lineEnd && source[pos] != ' ')
                            pos++;
                        if (pos > pathStart)
                            tokens.Add(new Token(TokenType.String, source.Slice(pathStart, pos - pathStart).ToString()));

                        // Parse HTTP version
                        if (pos < lineEnd)
                        {
                            tokens.Add(new Token(TokenType.Text, source.Slice(pos, lineEnd - pos).ToString()));
                        }
                        pos = lineEnd;
                        continue;
                    }
                    else if (firstWord.StartsWith("HTTP/"))
                    {
                        // HTTP Response: HTTP/version status-code reason
                        var httpVersionEnd = spaceIdx;
                        tokens.Add(new Token(TokenType.Type, line.Slice(0, httpVersionEnd).ToString()));
                        tokens.Add(new Token(TokenType.Text, " "));
                        pos += httpVersionEnd + 1;

                        // Parse status code
                        var statusStart = pos;
                        while (pos < lineEnd && char.IsDigit(source[pos]))
                            pos++;
                        if (pos > statusStart)
                            tokens.Add(new Token(TokenType.Number, source.Slice(statusStart, pos - statusStart).ToString()));

                        // Parse reason phrase
                        if (pos < lineEnd)
                        {
                            tokens.Add(new Token(TokenType.Text, source.Slice(pos, lineEnd - pos).ToString()));
                        }
                        pos = lineEnd;
                        continue;
                    }
                }

                // Fallback: treat entire line as text
                tokens.Add(new Token(TokenType.Text, line.ToString()));
                pos = lineEnd;
                continue;
            }

            // Headers: Header-Name: value
            if (inHeaders)
            {
                var lineStart = pos;
                var colonIdx = -1;

                // Find colon
                for (var i = pos; i < source.Length && source[i] != '\n' && source[i] != '\r'; i++)
                {
                    if (source[i] == ':')
                    {
                        colonIdx = i;
                        break;
                    }
                }

                if (colonIdx > 0)
                {
                    // Header name
                    var headerName = source.Slice(lineStart, colonIdx - lineStart).ToString();
                    tokens.Add(new Token(TokenType.Type, headerName));
                    tokens.Add(new Token(TokenType.Punctuation, ":"));
                    pos = colonIdx + 1;

                    // Skip whitespace after colon
                    while (pos < source.Length && (source[pos] == ' ' || source[pos] == '\t'))
                    {
                        tokens.Add(new Token(TokenType.Text, source[pos].ToString()));
                        pos++;
                    }

                    // Header value (rest of line)
                    var valueStart = pos;
                    while (pos < source.Length && source[pos] != '\n' && source[pos] != '\r')
                        pos++;

                    if (pos > valueStart)
                        tokens.Add(new Token(TokenType.String, source.Slice(valueStart, pos - valueStart).ToString()));
                    continue;
                }
            }

            // Body content (after headers)
            if (!inHeaders)
            {
                var textStart = pos;
                while (pos < source.Length)
                    pos++;
                tokens.Add(new Token(TokenType.Text, source.Slice(textStart, pos - textStart).ToString()));
                continue;
            }

            // Fallback: treat as text
            var start = pos;
            while (pos < source.Length && source[pos] != '\n' && source[pos] != '\r')
                pos++;
            tokens.Add(new Token(TokenType.Text, source.Slice(start, pos - start).ToString()));
        }

        return tokens;
    }
}
