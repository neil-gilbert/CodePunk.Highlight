using CodePunk.Highlight.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.SyntaxHighlighting.Languages;

/// <summary>
/// Bash/Shell script language definition for syntax highlighting.
/// Provides a tokenizer for Bash with support for commands, variables, strings, and operators.
/// </summary>
public class BashLanguageDefinition : ILanguageDefinition
{
    public string Name => "bash";
    public string[] Aliases => new[] { "sh", "shell", "zsh" };

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "if", "then", "else", "elif", "fi", "case", "esac", "for", "in", "do", "done",
        "while", "until", "function", "select", "time", "coproc", "return", "break",
        "continue", "exit", "trap", "source", "alias", "unalias", "export", "readonly",
        "local", "declare", "typeset", "set", "unset", "shift"
    };

    private static readonly HashSet<string> Builtins = new(StringComparer.Ordinal)
    {
        "cd", "pwd", "echo", "printf", "read", "test", "eval", "exec", "wait",
        "true", "false", "getopts", "let", "command", "type", "which", "whereis",
        "bg", "fg", "jobs", "kill", "disown", "suspend"
    };

    private static readonly HashSet<string> CommonCommands = new(StringComparer.Ordinal)
    {
        "ls", "cat", "grep", "sed", "awk", "find", "sort", "uniq", "cut", "tr",
        "head", "tail", "wc", "diff", "patch", "tar", "gzip", "gunzip", "zip", "unzip",
        "chmod", "chown", "chgrp", "mkdir", "rmdir", "rm", "cp", "mv", "ln", "touch",
        "make", "git", "docker", "npm", "yarn", "pip", "curl", "wget", "ssh", "scp"
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

            // Whitespace
            if (char.IsWhiteSpace(ch))
            {
                var start = pos;
                while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                    pos++;
                tokens.Add(new Token(TokenType.Text, source.Slice(start, pos - start).ToString()));
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

            // Double-quoted strings (with variable expansion)
            if (ch == '"')
            {
                var start = pos;
                pos++;
                while (pos < source.Length)
                {
                    if (source[pos] == '\\' && pos + 1 < source.Length)
                    {
                        pos += 2;
                        continue;
                    }
                    if (source[pos] == '"')
                    {
                        pos++;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Single-quoted strings (no variable expansion)
            if (ch == '\'')
            {
                var start = pos;
                pos++;
                while (pos < source.Length)
                {
                    if (source[pos] == '\'')
                    {
                        pos++;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Variables ($VAR, ${VAR}, $1, $@, etc.)
            if (ch == '$')
            {
                var start = pos;
                pos++;

                // Special variables: $?, $!, $$, $*, $@, $#, $0-9
                if (pos < source.Length && (source[pos] == '?' || source[pos] == '!' ||
                    source[pos] == '$' || source[pos] == '*' || source[pos] == '@' ||
                    source[pos] == '#' || char.IsDigit(source[pos])))
                {
                    pos++;
                }
                // Brace expansion: ${VAR}
                else if (pos < source.Length && source[pos] == '{')
                {
                    pos++;
                    while (pos < source.Length && source[pos] != '}')
                        pos++;
                    if (pos < source.Length) pos++; // Include closing brace
                }
                // Command substitution: $(command)
                else if (pos < source.Length && source[pos] == '(')
                {
                    pos++;
                    var depth = 1;
                    while (pos < source.Length && depth > 0)
                    {
                        if (source[pos] == '(') depth++;
                        else if (source[pos] == ')') depth--;
                        pos++;
                    }
                }
                // Regular variable: $VAR
                else if (pos < source.Length && (char.IsLetter(source[pos]) || source[pos] == '_'))
                {
                    while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                        pos++;
                }

                tokens.Add(new Token(TokenType.Type, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Backticks (command substitution)
            if (ch == '`')
            {
                var start = pos;
                pos++;
                while (pos < source.Length && source[pos] != '`')
                    pos++;
                if (pos < source.Length) pos++; // Include closing backtick
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Numbers
            if (char.IsDigit(ch))
            {
                var start = pos;
                while (pos < source.Length && (char.IsDigit(source[pos]) || source[pos] == '.'))
                    pos++;
                tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Redirects and operators (>, >>, <, <<, |, ||, &&, etc.)
            if (ch == '>' || ch == '<' || ch == '|' || ch == '&')
            {
                var start = pos;
                pos++;
                if (pos < source.Length)
                {
                    // >>, <<, ||, &&, &>, &>>
                    if ((ch == '>' || ch == '<' || ch == '|' || ch == '&') && source[pos] == ch)
                    {
                        pos++;
                        // Handle <<< (here-string)
                        if (ch == '<' && pos < source.Length && source[pos] == '<')
                            pos++;
                    }
                    // Check for &> or &>>
                    else if (ch == '&' && (source[pos] == '>' || source[pos] == '<'))
                    {
                        pos++;
                        if (pos < source.Length && source[pos] == '>')
                            pos++;
                    }
                }
                tokens.Add(new Token(TokenType.Operator, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Other operators
            if (ch == '!' || ch == '=' || ch == '+' || ch == '-' || ch == '*' || ch == '/')
            {
                var start = pos;
                pos++;
                // Handle ==, !=, +=, -=, etc.
                if (pos < source.Length && source[pos] == '=')
                    pos++;
                tokens.Add(new Token(TokenType.Operator, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Punctuation
            if (ch == ';' || ch == '(' || ch == ')' || ch == '{' || ch == '}' ||
                ch == '[' || ch == ']' || ch == ',' || ch == ':')
            {
                tokens.Add(new Token(TokenType.Punctuation, ch.ToString()));
                pos++;
                continue;
            }

            // Identifiers (commands, keywords, variables)
            if (char.IsLetter(ch) || ch == '_' || ch == '.' || ch == '/' || ch == '-')
            {
                var start = pos;

                // Path-like identifiers
                if (ch == '.' || ch == '/')
                {
                    while (pos < source.Length && !char.IsWhiteSpace(source[pos]) &&
                           source[pos] != ';' && source[pos] != '|' && source[pos] != '&' &&
                           source[pos] != '>' && source[pos] != '<' && source[pos] != '(' && source[pos] != ')')
                        pos++;
                    tokens.Add(new Token(TokenType.Identifier, source.Slice(start, pos - start).ToString()));
                    continue;
                }

                // Regular identifiers
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) ||
                       source[pos] == '_' || source[pos] == '-' || source[pos] == '.'))
                    pos++;

                var text = source.Slice(start, pos - start).ToString();

                TokenType type = TokenType.Identifier;
                if (Keywords.Contains(text))
                    type = TokenType.Keyword;
                else if (Builtins.Contains(text) || CommonCommands.Contains(text))
                    type = TokenType.Type;

                tokens.Add(new Token(type, text));
                continue;
            }

            // Everything else
            tokens.Add(new Token(TokenType.Text, ch.ToString()));
            pos++;
        }

        return tokens;
    }
}
