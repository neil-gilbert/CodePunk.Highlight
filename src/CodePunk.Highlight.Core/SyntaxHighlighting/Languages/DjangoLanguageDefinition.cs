using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.Core.SyntaxHighlighting.Languages;

/// <summary>
/// Django template language definition for syntax highlighting.
/// Provides a tokenizer for Django templates with support for tags, variables, filters, and comments.
/// </summary>
public class DjangoLanguageDefinition : ILanguageDefinition
{
    public string Name => "django";
    public string[] Aliases => new[] { "jinja", "jinja2", "htmldjango" };

    private static readonly HashSet<string> TemplateTags = new(StringComparer.Ordinal)
    {
        "if", "elif", "else", "endif", "for", "empty", "endfor", "block", "endblock",
        "extends", "include", "load", "url", "static", "csrf_token", "with", "endwith",
        "autoescape", "endautoescape", "comment", "endcomment", "filter", "endfilter",
        "firstof", "ifchanged", "endifchanged", "now", "regroup", "spaceless",
        "endspaceless", "templatetag", "widthratio", "verbatim", "endverbatim",
        "cycle", "debug", "lorem", "set", "endset", "macro", "endmacro", "call",
        "endcall", "import", "from"
    };

    private static readonly HashSet<string> CommonFilters = new(StringComparer.Ordinal)
    {
        "date", "time", "length", "default", "escape", "safe", "upper", "lower",
        "title", "capfirst", "truncatewords", "truncatechars", "linebreaks",
        "striptags", "join", "slice", "first", "last", "random", "add", "divisibleby",
        "pluralize", "floatformat", "filesizeformat", "urlize", "urlencode"
    };

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "in", "as", "not", "and", "or", "is", "with", "only", "true", "false",
        "True", "False", "None", "null"
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

            // Template comments {# ... #}
            if (ch == '{' && pos + 1 < source.Length && source[pos + 1] == '#')
            {
                var start = pos;
                pos += 2;
                while (pos < source.Length - 1)
                {
                    if (source[pos] == '#' && source[pos + 1] == '}')
                    {
                        pos += 2;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Template tags {% ... %}
            if (ch == '{' && pos + 1 < source.Length && source[pos + 1] == '%')
            {
                var start = pos;
                tokens.Add(new Token(TokenType.Punctuation, "{%"));
                pos += 2;

                // Skip whitespace
                while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                {
                    tokens.Add(new Token(TokenType.Text, source[pos].ToString()));
                    pos++;
                }

                // Parse tag name
                if (pos < source.Length && (char.IsLetter(source[pos]) || source[pos] == '_'))
                {
                    var tagStart = pos;
                    while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                        pos++;

                    var tagName = source.Slice(tagStart, pos - tagStart).ToString();
                    if (TemplateTags.Contains(tagName))
                        tokens.Add(new Token(TokenType.Keyword, tagName));
                    else
                        tokens.Add(new Token(TokenType.Identifier, tagName));

                    // Parse tag contents
                    while (pos < source.Length - 1 && !(source[pos] == '%' && source[pos + 1] == '}'))
                    {
                        var current = source[pos];

                        // Whitespace
                        if (char.IsWhiteSpace(current))
                        {
                            tokens.Add(new Token(TokenType.Text, current.ToString()));
                            pos++;
                            continue;
                        }

                        // String literals
                        if (current == '"' || current == '\'')
                        {
                            var stringStart = pos;
                            var quote = current;
                            pos++;
                            while (pos < source.Length)
                            {
                                if (source[pos] == '\\' && pos + 1 < source.Length)
                                {
                                    pos += 2;
                                    continue;
                                }
                                if (source[pos] == quote)
                                {
                                    pos++;
                                    break;
                                }
                                pos++;
                            }
                            tokens.Add(new Token(TokenType.String, source.Slice(stringStart, pos - stringStart).ToString()));
                            continue;
                        }

                        // Numbers
                        if (char.IsDigit(current))
                        {
                            var numStart = pos;
                            while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.'))
                                pos++;
                            tokens.Add(new Token(TokenType.Number, source.Slice(numStart, pos - numStart).ToString()));
                            continue;
                        }

                        // Keywords and identifiers
                        if (char.IsLetter(current) || current == '_')
                        {
                            var identStart = pos;
                            while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                                pos++;

                            var ident = source.Slice(identStart, pos - identStart).ToString();
                            if (Keywords.Contains(ident))
                                tokens.Add(new Token(TokenType.Keyword, ident));
                            else
                                tokens.Add(new Token(TokenType.Identifier, ident));
                            continue;
                        }

                        // Operators and punctuation
                        if (current == '|' || current == ':' || current == '=' || current == '!' ||
                            current == '<' || current == '>' || current == '.' || current == ',')
                        {
                            tokens.Add(new Token(TokenType.Operator, current.ToString()));
                            pos++;
                            continue;
                        }

                        // Other characters
                        tokens.Add(new Token(TokenType.Text, current.ToString()));
                        pos++;
                    }
                }

                // Closing tag
                if (pos < source.Length - 1 && source[pos] == '%' && source[pos + 1] == '}')
                {
                    tokens.Add(new Token(TokenType.Punctuation, "%}"));
                    pos += 2;
                }
                continue;
            }

            // Template variables {{ ... }}
            if (ch == '{' && pos + 1 < source.Length && source[pos + 1] == '{')
            {
                tokens.Add(new Token(TokenType.Punctuation, "{{"));
                pos += 2;

                // Parse variable expression
                while (pos < source.Length - 1 && !(source[pos] == '}' && source[pos + 1] == '}'))
                {
                    var current = source[pos];

                    // Whitespace
                    if (char.IsWhiteSpace(current))
                    {
                        tokens.Add(new Token(TokenType.Text, current.ToString()));
                        pos++;
                        continue;
                    }

                    // String literals
                    if (current == '"' || current == '\'')
                    {
                        var stringStart = pos;
                        var quote = current;
                        pos++;
                        while (pos < source.Length)
                        {
                            if (source[pos] == '\\' && pos + 1 < source.Length)
                            {
                                pos += 2;
                                continue;
                            }
                            if (source[pos] == quote)
                            {
                                pos++;
                                break;
                            }
                            pos++;
                        }
                        tokens.Add(new Token(TokenType.String, source.Slice(stringStart, pos - stringStart).ToString()));
                        continue;
                    }

                    // Numbers
                    if (char.IsDigit(current))
                    {
                        var numStart = pos;
                        while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.'))
                            pos++;
                        tokens.Add(new Token(TokenType.Number, source.Slice(numStart, pos - numStart).ToString()));
                        continue;
                    }

                    // Variables and filters
                    if (char.IsLetter(current) || current == '_')
                    {
                        var identStart = pos;
                        while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                            pos++;

                        var ident = source.Slice(identStart, pos - identStart).ToString();
                        if (CommonFilters.Contains(ident))
                            tokens.Add(new Token(TokenType.Type, ident));
                        else if (Keywords.Contains(ident))
                            tokens.Add(new Token(TokenType.Keyword, ident));
                        else
                            tokens.Add(new Token(TokenType.Identifier, ident));
                        continue;
                    }

                    // Operators (filter pipe |, dot access ., etc.)
                    if (current == '|' || current == '.' || current == ':' || current == ',')
                    {
                        tokens.Add(new Token(TokenType.Operator, current.ToString()));
                        pos++;
                        continue;
                    }

                    // Other characters
                    tokens.Add(new Token(TokenType.Text, current.ToString()));
                    pos++;
                }

                // Closing variable
                if (pos < source.Length - 1 && source[pos] == '}' && source[pos + 1] == '}')
                {
                    tokens.Add(new Token(TokenType.Punctuation, "}}"));
                    pos += 2;
                }
                continue;
            }

            // HTML content (treated as text)
            var textStart = pos;
            while (pos < source.Length && source[pos] != '{')
                pos++;

            if (pos > textStart)
            {
                tokens.Add(new Token(TokenType.Text, source.Slice(textStart, pos - textStart).ToString()));
            }
        }

        return tokens;
    }
}
