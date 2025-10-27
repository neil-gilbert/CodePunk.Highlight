using CodePunk.Highlight.Core.SyntaxHighlighting;
using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Languages;
using Microsoft.Extensions.DependencyInjection;

namespace CodePunk.Highlight.RazorConsole.Extensions;

/// <summary>
/// Extension methods for configuring syntax highlighting services in a RazorConsole application.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds syntax highlighting services to the service collection.
    /// Registers all supported language definitions and the syntax highlighter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSyntaxHighlighting(this IServiceCollection services)
    {
        // Register all language definitions
        services.AddSingleton<ILanguageDefinition, BashLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, CLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, ClojureLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, CSharpLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, CssLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, DjangoLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, DockerfileLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, ElixirLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, ErlangLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, FSharpLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, GoLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, GraphQLLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, HandlebarsLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, HaskellLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, HtmlLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, HttpLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, JavaLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, JavaScriptLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, JsonLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, KotlinLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, MakefileLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, MarkdownLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, ObjectiveCLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, PerlLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, PhpLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, PowerShellLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, PythonLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, RLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, RubyLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, RustLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, ScssLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, SqlLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, SwiftLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, TypeScriptLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, XmlLanguageDefinition>();
        services.AddSingleton<ILanguageDefinition, YamlLanguageDefinition>();

        // Register the syntax highlighter
        services.AddSingleton<ISyntaxHighlighter, SyntaxHighlighter>();

        return services;
    }
}
