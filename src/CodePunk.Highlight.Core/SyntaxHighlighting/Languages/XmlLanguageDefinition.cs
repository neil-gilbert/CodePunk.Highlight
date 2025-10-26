using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.Core.SyntaxHighlighting.Languages;

/// <summary>
/// XML language definition for syntax highlighting.
/// Provides a tokenizer for XML markup with support for tags, attributes, and entities.
/// </summary>
public class XmlLanguageDefinition : ILanguageDefinition
{
    public string Name => "xml";
    public string[] Aliases => Array.Empty<string>();

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

        while (pos < source.Length)
        {
            var ch = source[pos];

            // Comments
            if (ch == '<' && pos + 3 < source.Length && source[pos + 1] == '!' &&
                source[pos + 2] == '-' && source[pos + 3] == '-')
            {
                var start = pos;
                pos += 4;
                while (pos < source.Length - 2)
                {
                    if (source[pos] == '-' && source[pos + 1] == '-' && source[pos + 2] == '>')
                    {
                        pos += 3;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // CDATA sections
            if (ch == '<' && pos + 8 < source.Length && source[pos + 1] == '!' &&
                source[pos + 2] == '[' && source[pos + 3] == 'C' && source[pos + 4] == 'D' &&
                source[pos + 5] == 'A' && source[pos + 6] == 'T' && source[pos + 7] == 'A' &&
                source[pos + 8] == '[')
            {
                var start = pos;
                pos += 9;
                while (pos < source.Length - 2)
                {
                    if (source[pos] == ']' && source[pos + 1] == ']' && source[pos + 2] == '>')
                    {
                        pos += 3;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Processing instructions (<?xml ... ?>)
            if (ch == '<' && pos + 1 < source.Length && source[pos + 1] == '?')
            {
                var start = pos;
                pos += 2;
                while (pos < source.Length - 1)
                {
                    if (source[pos] == '?' && source[pos + 1] == '>')
                    {
                        pos += 2;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.Preprocessor, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // DOCTYPE declaration
            if (ch == '<' && pos + 8 < source.Length && source[pos + 1] == '!' &&
                source[pos + 2] == 'D' && source[pos + 3] == 'O' && source[pos + 4] == 'C' &&
                source[pos + 5] == 'T' && source[pos + 6] == 'Y' && source[pos + 7] == 'P' &&
                source[pos + 8] == 'E')
            {
                var start = pos;
                pos += 9;
                var depth = 0;
                while (pos < source.Length)
                {
                    if (source[pos] == '[')
                        depth++;
                    else if (source[pos] == ']')
                        depth--;
                    else if (source[pos] == '>' && depth == 0)
                    {
                        pos++;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.Preprocessor, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Closing tags (</tag>)
            if (ch == '<' && pos + 1 < source.Length && source[pos + 1] == '/')
            {
                tokens.Add(new Token(TokenType.Punctuation, "</"));
                pos += 2;

                // Tag name
                var tagStart = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == ':' || source[pos] == '-' || source[pos] == '_'))
                    pos++;
                if (pos > tagStart)
                    tokens.Add(new Token(TokenType.Keyword, source.Slice(tagStart, pos - tagStart).ToString()));

                // Skip whitespace
                while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                {
                    var wsStart = pos;
                    while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                        pos++;
                    tokens.Add(new Token(TokenType.Text, source.Slice(wsStart, pos - wsStart).ToString()));
                }

                // Closing >
                if (pos < source.Length && source[pos] == '>')
                {
                    tokens.Add(new Token(TokenType.Punctuation, ">"));
                    pos++;
                }
                continue;
            }

            // Opening tags (<tag> or <tag/>)
            if (ch == '<')
            {
                tokens.Add(new Token(TokenType.Punctuation, "<"));
                pos++;

                // Tag name
                var tagStart = pos;
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == ':' || source[pos] == '-' || source[pos] == '_'))
                    pos++;
                if (pos > tagStart)
                    tokens.Add(new Token(TokenType.Keyword, source.Slice(tagStart, pos - tagStart).ToString()));

                // Parse attributes
                while (pos < source.Length && source[pos] != '>' && !(source[pos] == '/' && pos + 1 < source.Length && source[pos + 1] == '>'))
                {
                    // Whitespace
                    if (char.IsWhiteSpace(source[pos]))
                    {
                        var wsStart = pos;
                        while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                            pos++;
                        tokens.Add(new Token(TokenType.Text, source.Slice(wsStart, pos - wsStart).ToString()));
                        continue;
                    }

                    // Attribute name
                    var attrStart = pos;
                    while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == ':' || source[pos] == '-' || source[pos] == '_'))
                        pos++;
                    if (pos > attrStart)
                        tokens.Add(new Token(TokenType.Type, source.Slice(attrStart, pos - attrStart).ToString()));

                    // Skip whitespace around =
                    while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                    {
                        tokens.Add(new Token(TokenType.Text, source[pos].ToString()));
                        pos++;
                    }

                    // =
                    if (pos < source.Length && source[pos] == '=')
                    {
                        tokens.Add(new Token(TokenType.Operator, "="));
                        pos++;
                    }

                    // Skip whitespace after =
                    while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                    {
                        tokens.Add(new Token(TokenType.Text, source[pos].ToString()));
                        pos++;
                    }

                    // Attribute value
                    if (pos < source.Length && (source[pos] == '"' || source[pos] == '\''))
                    {
                        var quote = source[pos];
                        var valueStart = pos;
                        pos++;
                        while (pos < source.Length && source[pos] != quote)
                            pos++;
                        if (pos < source.Length)
                            pos++;
                        tokens.Add(new Token(TokenType.String, source.Slice(valueStart, pos - valueStart).ToString()));
                    }
                }

                // Self-closing tag (/>) or closing >
                if (pos < source.Length && source[pos] == '/' && pos + 1 < source.Length && source[pos + 1] == '>')
                {
                    tokens.Add(new Token(TokenType.Punctuation, "/>"));
                    pos += 2;
                }
                else if (pos < source.Length && source[pos] == '>')
                {
                    tokens.Add(new Token(TokenType.Punctuation, ">"));
                    pos++;
                }
                continue;
            }

            // Entity references (&amp; &lt; etc.)
            if (ch == '&')
            {
                var start = pos;
                pos++;
                while (pos < source.Length && source[pos] != ';' && !char.IsWhiteSpace(source[pos]) && source[pos] != '<')
                    pos++;
                if (pos < source.Length && source[pos] == ';')
                    pos++;
                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Text content
            var textStart = pos;
            while (pos < source.Length && source[pos] != '<' && source[pos] != '&')
                pos++;

            if (pos > textStart)
            {
                tokens.Add(new Token(TokenType.Text, source.Slice(textStart, pos - textStart).ToString()));
            }
        }

        return tokens;
    }
}
