using Bunit;
using CodePunk.Highlight.RazorConsole.Components;
using CodePunk.Highlight.RazorConsole.Extensions;
using Shouldly;
using Xunit;

namespace CodePunk.Highlight.RazorConsole.Tests.ComponentTests;

public class CodeBlockTests : TestContext
{
    public CodeBlockTests()
    {
        // Register syntax highlighting services
        Services.AddSyntaxHighlighting();
    }

    [Fact]
    public void CodeBlock_WithCode_RendersMarkup()
    {
        // Arrange & Act
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, "int x = 42;")
            .Add(p => p.Language, "csharp"));

        // Assert
        component.Markup.ShouldNotBeEmpty();
    }

    [Fact]
    public void CodeBlock_WithKeyword_HighlightsInBlue()
    {
        // Arrange & Act
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, "class Program { }")
            .Add(p => p.Language, "csharp"));

        // Assert
        component.Markup.ShouldContain("[blue]class[/]");
    }

    [Fact]
    public void CodeBlock_WithString_HighlightsInGreen()
    {
        // Arrange & Act
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, "\"hello world\"")
            .Add(p => p.Language, "csharp"));

        // Assert
        component.Markup.ShouldContain("[green]");
    }

    [Fact]
    public void CodeBlock_WithNumber_HighlightsInMagenta()
    {
        // Arrange & Act
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, "42")
            .Add(p => p.Language, "csharp"));

        // Assert
        component.Markup.ShouldContain("[magenta]42[/]");
    }

    [Fact]
    public void CodeBlock_EmptyCode_RendersEmptyMarkup()
    {
        // Arrange & Act
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, string.Empty)
            .Add(p => p.Language, "csharp"));

        // Assert
        var markup = component.Markup.Trim();
        markup.ShouldBeEmpty();
    }

    [Fact]
    public void CodeBlock_WhenCodeChanges_ReRenders()
    {
        // Arrange
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, "int x = 1;")
            .Add(p => p.Language, "csharp"));

        var initialMarkup = component.Markup;

        // Act - Change the code
        component.SetParametersAndRender(parameters => parameters
            .Add(p => p.Code, "string y = \"hello\";")
            .Add(p => p.Language, "csharp"));

        var updatedMarkup = component.Markup;

        // Assert
        updatedMarkup.ShouldNotBe(initialMarkup);
        updatedMarkup.ShouldContain("string");
        updatedMarkup.ShouldContain("hello");
        updatedMarkup.ShouldNotContain("int x = 1");
    }

    [Fact]
    public void CodeBlock_WhenLanguageChanges_ReRenders()
    {
        // Arrange
        var code = "x = 42";
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, code)
            .Add(p => p.Language, "python"));

        var initialMarkup = component.Markup;

        // Act - Change the language
        component.SetParametersAndRender(parameters => parameters
            .Add(p => p.Code, code)
            .Add(p => p.Language, "javascript"));

        var updatedMarkup = component.Markup;

        // Assert
        // Both languages should highlight, but potentially differently
        updatedMarkup.ShouldNotBeEmpty();
        initialMarkup.ShouldNotBeEmpty();
    }

    [Fact]
    public void CodeBlock_WithMultipleLines_RendersAll()
    {
        // Arrange
        var code = @"class Program
{
    static void Main()
    {
        Console.WriteLine(""Hello"");
    }
}";

        // Act
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, code)
            .Add(p => p.Language, "csharp"));

        // Assert
        component.Markup.ShouldContain("class");
        component.Markup.ShouldContain("Main");
        component.Markup.ShouldContain("Console");
        component.Markup.ShouldContain("WriteLine");
    }

    [Fact]
    public void CodeBlock_WithSpecialCharacters_EscapesCorrectly()
    {
        // Arrange
        var code = "string text = \"[bold]not markup[/]\";";

        // Act
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, code)
            .Add(p => p.Language, "csharp"));

        // Assert
        // The string content should be escaped
        component.Markup.ShouldNotContain("[bold]not markup[/]");
        // But the color markup should be present
        component.Markup.ShouldContain("[blue]string[/]");
    }

    [Theory]
    [InlineData("csharp", "class")]
    [InlineData("python", "def")]
    [InlineData("javascript", "const")]
    [InlineData("java", "public")]
    public void CodeBlock_DifferentLanguages_HighlightsKeywords(string language, string keyword)
    {
        // Arrange & Act
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, keyword)
            .Add(p => p.Language, language));

        // Assert
        component.Markup.ShouldContain("[blue]");
    }

    [Fact]
    public void CodeBlock_UnknownLanguage_RendersWithoutHighlighting()
    {
        // Arrange
        var code = "some random text";

        // Act
        var component = RenderComponent<CodeBlock>(parameters => parameters
            .Add(p => p.Code, code)
            .Add(p => p.Language, "unknown-language-xyz"));

        // Assert
        component.Markup.ShouldContain(code);
        // Should not have color markup since language is unknown
        component.Markup.ShouldNotContain("[blue]");
        component.Markup.ShouldNotContain("[green]");
    }
}
