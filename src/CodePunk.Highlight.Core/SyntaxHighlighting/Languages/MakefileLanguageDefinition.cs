using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.Core.SyntaxHighlighting.Languages;

/// <summary>
/// Makefile language definition for syntax highlighting.
/// Provides a tokenizer for Makefiles with support for targets, variables, and commands.
/// </summary>
public class MakefileLanguageDefinition : ILanguageDefinition
{
    public string Name => "makefile";
    public string[] Aliases => new[] { "make" };

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "include", "ifeq", "ifneq", "ifdef", "ifndef", "else", "endif", "export",
        "unexport", "define", "endef", "override", "private"
    };

    private static readonly HashSet<string> Functions = new(StringComparer.Ordinal)
    {
        "subst", "patsubst", "strip", "findstring", "filter", "filter-out", "sort",
        "word", "wordlist", "words", "firstword", "lastword", "dir", "notdir",
        "suffix", "basename", "addsuffix", "addprefix", "join", "wildcard",
        "realpath", "abspath", "error", "warning", "info", "shell", "foreach", "if",
        "or", "and", "call", "eval", "origin", "flavor", "value"
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
        var lineStart = true;

        while (pos < source.Length)
        {
            var ch = source[pos];

            // Handle newlines
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

            // Comments
            if (ch == '#')
            {
                var start = pos;
                while (pos < source.Length && source[pos] != '\n')
                    pos++;
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Whitespace (excluding newlines)
            if (char.IsWhiteSpace(ch))
            {
                var start = pos;
                while (pos < source.Length && char.IsWhiteSpace(source[pos]) && source[pos] != '\n' && source[pos] != '\r')
                    pos++;
                tokens.Add(new Token(TokenType.Text, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Target at start of line (identifier followed by :)
            if (lineStart && (char.IsLetter(ch) || ch == '.' || ch == '_'))
            {
                var lineEnd = pos;
                while (lineEnd < source.Length && source[lineEnd] != '\n' && source[lineEnd] != '\r')
                    lineEnd++;

                var colonIdx = -1;
                for (var i = pos; i < lineEnd; i++)
                {
                    if (source[i] == ':')
                    {
                        colonIdx = i;
                        break;
                    }
                }

                if (colonIdx > pos)
                {
                    // Check if it's not a variable assignment (no =)
                    var hasEquals = false;
                    for (var i = pos; i < colonIdx; i++)
                    {
                        if (source[i] == '=')
                        {
                            hasEquals = true;
                            break;
                        }
                    }

                    if (!hasEquals)
                    {
                        // It's a target
                        var targetText = source.Slice(pos, colonIdx - pos).ToString().Trim();
                        tokens.Add(new Token(TokenType.Keyword, targetText));
                        tokens.Add(new Token(TokenType.Punctuation, ":"));
                        pos = colonIdx + 1;
                        lineStart = false;

                        // Rest of line is dependencies
                        while (pos < lineEnd)
                        {
                            if (char.IsWhiteSpace(source[pos]))
                            {
                                tokens.Add(new Token(TokenType.Text, source[pos].ToString()));
                                pos++;
                            }
                            else if (source[pos] == '#')
                            {
                                var commentStart = pos;
                                while (pos < lineEnd)
                                    pos++;
                                tokens.Add(new Token(TokenType.Comment, source.Slice(commentStart, pos - commentStart).ToString()));
                                break;
                            }
                            else
                            {
                                var depStart = pos;
                                while (pos < lineEnd && !char.IsWhiteSpace(source[pos]) && source[pos] != '#')
                                    pos++;
                                tokens.Add(new Token(TokenType.Identifier, source.Slice(depStart, pos - depStart).ToString()));
                            }
                        }
                        continue;
                    }
                }
            }

            // Variable assignment or reference
            if (lineStart && char.IsLetter(ch) || ch == '_')
            {
                var lineEnd = pos;
                while (lineEnd < source.Length && source[lineEnd] != '\n' && source[lineEnd] != '\r')
                    lineEnd++;

                var equalsIdx = -1;
                for (var i = pos; i < lineEnd; i++)
                {
                    if (source[i] == '=' || (source[i] == ':' && i + 1 < lineEnd && source[i + 1] == '=') ||
                        (source[i] == '+' && i + 1 < lineEnd && source[i + 1] == '=') ||
                        (source[i] == '?' && i + 1 < lineEnd && source[i + 1] == '='))
                    {
                        equalsIdx = i;
                        break;
                    }
                }

                if (equalsIdx > pos)
                {
                    // Variable assignment
                    var varName = source.Slice(pos, equalsIdx - pos).ToString().Trim();
                    tokens.Add(new Token(TokenType.Type, varName));
                    pos = equalsIdx;

                    // Operator
                    var opStart = pos;
                    if (source[pos] == ':' || source[pos] == '+' || source[pos] == '?')
                        pos++;
                    if (pos < lineEnd && source[pos] == '=')
                        pos++;
                    tokens.Add(new Token(TokenType.Operator, source.Slice(opStart, pos - opStart).ToString()));

                    // Value (rest of line)
                    while (pos < lineEnd)
                    {
                        if (source[pos] == '$')
                        {
                            var varStart = pos;
                            pos++;
                            if (pos < lineEnd && source[pos] == '(')
                            {
                                pos++;
                                while (pos < lineEnd && source[pos] != ')')
                                    pos++;
                                if (pos < lineEnd) pos++;
                            }
                            else if (pos < lineEnd && source[pos] == '{')
                            {
                                pos++;
                                while (pos < lineEnd && source[pos] != '}')
                                    pos++;
                                if (pos < lineEnd) pos++;
                            }
                            else if (pos < lineEnd)
                            {
                                pos++;
                            }
                            tokens.Add(new Token(TokenType.Type, source.Slice(varStart, pos - varStart).ToString()));
                        }
                        else
                        {
                            var textStart = pos;
                            while (pos < lineEnd && source[pos] != '$')
                                pos++;
                            if (pos > textStart)
                                tokens.Add(new Token(TokenType.String, source.Slice(textStart, pos - textStart).ToString()));
                        }
                    }
                    lineStart = false;
                    continue;
                }
            }

            // Keywords at line start
            if (lineStart && char.IsLetter(ch))
            {
                var wordStart = pos;
                while (pos < source.Length && char.IsLetter(source[pos]))
                    pos++;
                var word = source.Slice(wordStart, pos - wordStart).ToString();

                if (Keywords.Contains(word))
                {
                    tokens.Add(new Token(TokenType.Keyword, word));
                    lineStart = false;
                    continue;
                }
                else
                {
                    pos = wordStart;
                }
            }

            // Variable references $(VAR) or ${VAR}
            if (ch == '$')
            {
                var start = pos;
                pos++;
                if (pos < source.Length && (source[pos] == '(' || source[pos] == '{'))
                {
                    var closing = source[pos] == '(' ? ')' : '}';
                    pos++;
                    while (pos < source.Length && source[pos] != closing)
                        pos++;
                    if (pos < source.Length) pos++;
                }
                else if (pos < source.Length)
                {
                    pos++;
                }
                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                lineStart = false;
                continue;
            }

            // Tab at beginning (recipe command)
            if (lineStart && ch == '\t')
            {
                tokens.Add(new Token(TokenType.Text, "\t"));
                pos++;

                // Rest of line is a shell command
                var cmdStart = pos;
                while (pos < source.Length && source[pos] != '\n' && source[pos] != '\r')
                    pos++;
                if (pos > cmdStart)
                    tokens.Add(new Token(TokenType.String, source.Slice(cmdStart, pos - cmdStart).ToString()));
                continue;
            }

            // Default: consume as text
            var textPos = pos;
            while (pos < source.Length && source[pos] != '\n' && source[pos] != '\r' && source[pos] != '$' && source[pos] != '#')
                pos++;
            if (pos > textPos)
                tokens.Add(new Token(TokenType.Text, source.Slice(textPos, pos - textPos).ToString()));
            lineStart = false;
        }

        return tokens;
    }
}
