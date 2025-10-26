using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.Core.SyntaxHighlighting.Languages;

/// <summary>
/// Markdown language definition for syntax highlighting.
/// Provides a tokenizer for Markdown with support for headers, emphasis, links, and code blocks.
/// </summary>
public class MarkdownLanguageDefinition : ILanguageDefinition
{
    public string Name => "markdown";
    public string[] Aliases => new[] { "md" };

    public bool Matches(string languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId)) return false;
        var normalized = languageId.ToLowerInvariant();
        return normalized == Name || Aliases.Contains(normalized);
    }

    public IEnumerable<Token> Tokenize(ReadOnlySpan<char> source)
    {
        var tokens = new List<Token>();
        var pos = 0;
        var lineStart = true;

        while (pos < source.Length)
        {
            var ch = source[pos];

            // Newlines
            if (ch == '\n')
            {
                tokens.Add(new Token(TokenType.Text, "\n"));
                pos++;
                lineStart = true;
                continue;
            }

            if (ch == '\r')
            {
                tokens.Add(new Token(TokenType.Text, "\r"));
                pos++;
                continue;
            }

            // Headers (# at start of line)
            if (lineStart && ch == '#')
            {
                var start = pos;
                while (pos < source.Length && source[pos] == '#')
                    pos++;
                var headerLevel = pos - start;

                // Skip space after #
                if (pos < source.Length && source[pos] == ' ')
                {
                    tokens.Add(new Token(TokenType.Keyword, source.Slice(start, pos - start + 1).ToString()));
                    pos++;
                }
                else
                {
                    tokens.Add(new Token(TokenType.Keyword, source.Slice(start, pos - start).ToString()));
                }

                // Rest of line is header text
                var headerTextStart = pos;
                while (pos < source.Length && source[pos] != '\n' && source[pos] != '\r')
                    pos++;
                if (pos > headerTextStart)
                    tokens.Add(new Token(TokenType.Type, source.Slice(headerTextStart, pos - headerTextStart).ToString()));
                lineStart = false;
                continue;
            }

            // Code blocks (```)
            if (lineStart && ch == '`' && pos + 2 < source.Length && source[pos + 1] == '`' && source[pos + 2] == '`')
            {
                var start = pos;
                pos += 3;

                // Language identifier
                var langStart = pos;
                while (pos < source.Length && source[pos] != '\n' && source[pos] != '\r')
                    pos++;
                var lang = source.Slice(langStart, pos - langStart).ToString().Trim();

                // Add opening fence with language
                tokens.Add(new Token(TokenType.Keyword, source.Slice(start, pos - start).ToString()));

                // Skip newline
                if (pos < source.Length && source[pos] == '\n')
                {
                    tokens.Add(new Token(TokenType.Text, "\n"));
                    pos++;
                }

                // Code content
                var codeStart = pos;
                while (pos < source.Length - 2)
                {
                    if (source[pos] == '`' && source[pos + 1] == '`' && source[pos + 2] == '`')
                        break;
                    pos++;
                }

                if (pos > codeStart)
                    tokens.Add(new Token(TokenType.String, source.Slice(codeStart, pos - codeStart).ToString()));

                // Closing fence
                if (pos < source.Length - 2 && source[pos] == '`' && source[pos + 1] == '`' && source[pos + 2] == '`')
                {
                    tokens.Add(new Token(TokenType.Keyword, "```"));
                    pos += 3;
                }
                lineStart = false;
                continue;
            }

            // Inline code (`code`)
            if (ch == '`')
            {
                var start = pos;
                pos++;
                while (pos < source.Length && source[pos] != '`')
                    pos++;
                if (pos < source.Length)
                    pos++;
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                lineStart = false;
                continue;
            }

            // Links [text](url) or images ![alt](url)
            if (ch == '[' || (ch == '!' && pos + 1 < source.Length && source[pos + 1] == '['))
            {
                var start = pos;
                var isImage = ch == '!';
                if (isImage) pos++;

                // Find closing ]
                var bracketPos = pos;
                pos++;
                while (pos < source.Length && source[pos] != ']')
                    pos++;

                if (pos < source.Length)
                    pos++;

                // Check for (url)
                if (pos < source.Length && source[pos] == '(')
                {
                    while (pos < source.Length && source[pos] != ')')
                        pos++;
                    if (pos < source.Length)
                        pos++;
                    tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                }
                else
                {
                    // Not a complete link, just add as text
                    tokens.Add(new Token(TokenType.Text, source.Slice(start, pos - start).ToString()));
                }
                lineStart = false;
                continue;
            }

            // Bold (**text** or __text__)
            if ((ch == '*' && pos + 1 < source.Length && source[pos + 1] == '*') ||
                (ch == '_' && pos + 1 < source.Length && source[pos + 1] == '_'))
            {
                var marker = ch;
                var start = pos;
                pos += 2;

                // Find closing marker
                while (pos < source.Length - 1)
                {
                    if (source[pos] == marker && source[pos + 1] == marker)
                    {
                        pos += 2;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.Keyword, source.Slice(start, pos - start).ToString()));
                lineStart = false;
                continue;
            }

            // Italic (*text* or _text_)
            if (ch == '*' || ch == '_')
            {
                var marker = ch;
                var start = pos;
                pos++;

                // Find closing marker
                while (pos < source.Length)
                {
                    if (source[pos] == marker)
                    {
                        pos++;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.Keyword, source.Slice(start, pos - start).ToString()));
                lineStart = false;
                continue;
            }

            // Lists (-, *, + at start of line, or numbered)
            if (lineStart && (ch == '-' || ch == '*' || ch == '+'))
            {
                var start = pos;
                pos++;
                if (pos < source.Length && source[pos] == ' ')
                {
                    tokens.Add(new Token(TokenType.Operator, source.Slice(start, pos - start + 1).ToString()));
                    pos++;
                }
                else
                {
                    tokens.Add(new Token(TokenType.Operator, source.Slice(start, pos - start).ToString()));
                }
                lineStart = false;
                continue;
            }

            // Numbered lists (1. at start of line)
            if (lineStart && char.IsDigit(ch))
            {
                var start = pos;
                while (pos < source.Length && char.IsDigit(source[pos]))
                    pos++;
                if (pos < source.Length && source[pos] == '.')
                {
                    pos++;
                    if (pos < source.Length && source[pos] == ' ')
                        pos++;
                    tokens.Add(new Token(TokenType.Operator, source.Slice(start, pos - start).ToString()));
                    lineStart = false;
                    continue;
                }
                else
                {
                    pos = start;
                }
            }

            // Whitespace
            if (char.IsWhiteSpace(ch))
            {
                var start = pos;
                while (pos < source.Length && char.IsWhiteSpace(source[pos]) && source[pos] != '\n' && source[pos] != '\r')
                    pos++;
                tokens.Add(new Token(TokenType.Text, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Regular text
            var textStart = pos;
            while (pos < source.Length && !char.IsWhiteSpace(source[pos]) && source[pos] != '`' &&
                   source[pos] != '[' && source[pos] != '!' && source[pos] != '*' && source[pos] != '_' &&
                   source[pos] != '#')
                pos++;

            if (pos > textStart)
            {
                tokens.Add(new Token(TokenType.Text, source.Slice(textStart, pos - textStart).ToString()));
                lineStart = false;
            }
        }

        return tokens;
    }
}
