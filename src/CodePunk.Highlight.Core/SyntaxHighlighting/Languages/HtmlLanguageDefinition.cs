using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.Core.SyntaxHighlighting.Languages;

/// <summary>
/// HTML language definition for syntax highlighting.
/// Provides a tokenizer for HTML markup with support for tags, attributes, and entities.
/// </summary>
public class HtmlLanguageDefinition : ILanguageDefinition
{
    public string Name => "html";
    public string[] Aliases => new[] { "htm", "xhtml" };

    private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input",
        "link", "meta", "param", "source", "track", "wbr"
    };

    private static readonly HashSet<string> CommonTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "html", "head", "body", "title", "meta", "link", "script", "style",
        "div", "span", "p", "a", "img", "ul", "ol", "li", "table", "tr", "td", "th",
        "form", "input", "button", "select", "option", "textarea", "label",
        "h1", "h2", "h3", "h4", "h5", "h6", "header", "footer", "nav", "section",
        "article", "aside", "main", "figure", "figcaption", "details", "summary"
    };

    private static readonly HashSet<string> CommonAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "class", "style", "src", "href", "alt", "title", "width", "height",
        "type", "name", "value", "placeholder", "required", "disabled", "checked",
        "data-", "aria-", "role", "rel", "target", "method", "action"
    };

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

        while (pos < source.Length)
        {
            var ch = source[pos];

            // HTML Comments
            if (ch == '<' && pos + 3 < source.Length &&
                source[pos + 1] == '!' && source[pos + 2] == '-' && source[pos + 3] == '-')
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

            // HTML Tags
            if (ch == '<')
            {
                var start = pos;
                pos++;

                // Check for closing tag
                var isClosingTag = pos < source.Length && source[pos] == '/';
                if (isClosingTag) pos++;

                // Add opening bracket
                tokens.Add(new Token(TokenType.Punctuation, source.Slice(start, pos - start).ToString()));

                // Skip whitespace
                while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                {
                    tokens.Add(new Token(TokenType.Text, source[pos].ToString()));
                    pos++;
                }

                // Parse tag name
                if (pos < source.Length && (char.IsLetter(source[pos]) || source[pos] == '!'))
                {
                    var tagStart = pos;
                    while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '-' || source[pos] == ':'))
                        pos++;

                    var tagName = source.Slice(tagStart, pos - tagStart).ToString();
                    tokens.Add(new Token(TokenType.Keyword, tagName));

                    // Parse attributes (only for opening tags)
                    if (!isClosingTag)
                    {
                        while (pos < source.Length && source[pos] != '>')
                        {
                            // Skip whitespace
                            if (char.IsWhiteSpace(source[pos]))
                            {
                                tokens.Add(new Token(TokenType.Text, source[pos].ToString()));
                                pos++;
                                continue;
                            }

                            // Check for self-closing tag
                            if (source[pos] == '/')
                            {
                                tokens.Add(new Token(TokenType.Punctuation, "/"));
                                pos++;
                                continue;
                            }

                            // Parse attribute name
                            if (char.IsLetter(source[pos]) || source[pos] == '-' || source[pos] == ':')
                            {
                                var attrStart = pos;
                                while (pos < source.Length &&
                                       (char.IsLetterOrDigit(source[pos]) || source[pos] == '-' || source[pos] == ':' || source[pos] == '_'))
                                    pos++;

                                tokens.Add(new Token(TokenType.Type, source.Slice(attrStart, pos - attrStart).ToString()));

                                // Skip whitespace around equals sign
                                while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                                {
                                    tokens.Add(new Token(TokenType.Text, source[pos].ToString()));
                                    pos++;
                                }

                                // Parse equals sign and value
                                if (pos < source.Length && source[pos] == '=')
                                {
                                    tokens.Add(new Token(TokenType.Operator, "="));
                                    pos++;

                                    // Skip whitespace after equals
                                    while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                                    {
                                        tokens.Add(new Token(TokenType.Text, source[pos].ToString()));
                                        pos++;
                                    }

                                    // Parse attribute value
                                    if (pos < source.Length && (source[pos] == '"' || source[pos] == '\''))
                                    {
                                        var quote = source[pos];
                                        var valueStart = pos;
                                        pos++;
                                        while (pos < source.Length && source[pos] != quote)
                                            pos++;
                                        if (pos < source.Length) pos++; // Include closing quote

                                        tokens.Add(new Token(TokenType.String, source.Slice(valueStart, pos - valueStart).ToString()));
                                    }
                                }
                                continue;
                            }

                            // Unknown character, just add it
                            tokens.Add(new Token(TokenType.Text, source[pos].ToString()));
                            pos++;
                        }
                    }
                }

                // Closing bracket
                if (pos < source.Length && source[pos] == '>')
                {
                    tokens.Add(new Token(TokenType.Punctuation, ">"));
                    pos++;
                }
                continue;
            }

            // HTML Entities
            if (ch == '&')
            {
                var start = pos;
                pos++;

                // Named entity (&nbsp;) or numeric entity (&#123; or &#x1F4A9;)
                if (pos < source.Length)
                {
                    if (source[pos] == '#')
                    {
                        pos++;
                        if (pos < source.Length && (source[pos] == 'x' || source[pos] == 'X'))
                            pos++;

                        while (pos < source.Length && (char.IsLetterOrDigit(source[pos])))
                            pos++;
                    }
                    else
                    {
                        while (pos < source.Length && char.IsLetterOrDigit(source[pos]))
                            pos++;
                    }

                    if (pos < source.Length && source[pos] == ';')
                        pos++;

                    tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                    continue;
                }
            }

            // Whitespace
            if (char.IsWhiteSpace(ch))
            {
                var start = pos;
                while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                    pos++;
                tokens.Add(new Token(TokenType.Text, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Regular text content
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
