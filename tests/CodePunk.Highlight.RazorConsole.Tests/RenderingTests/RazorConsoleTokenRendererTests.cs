using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;
using CodePunk.Highlight.RazorConsole.Rendering;
using Shouldly;
using Xunit;

namespace CodePunk.Highlight.RazorConsole.Tests.RenderingTests;

public class RazorConsoleTokenRendererTests
{
    [Fact]
    public void RenderToken_Keyword_ProducesColoredMarkup()
    {
        // Arrange
        var renderer = new RazorConsoleTokenRenderer();
        var token = new Token(TokenType.Keyword, "class");

        // Act
        renderer.RenderToken(token);
        var markup = renderer.GetMarkup();

        // Assert
        markup.ShouldContain("[blue]class[/]");
    }

    [Fact]
    public void RenderToken_String_ProducesGreenMarkup()
    {
        // Arrange
        var renderer = new RazorConsoleTokenRenderer();
        var token = new Token(TokenType.String, "\"hello\"");

        // Act
        renderer.RenderToken(token);
        var markup = renderer.GetMarkup();

        // Assert
        markup.ShouldContain("[green]");
        markup.ShouldContain("hello");
    }

    [Fact]
    public void RenderToken_Comment_ProducesGreyMarkup()
    {
        // Arrange
        var renderer = new RazorConsoleTokenRenderer();
        var token = new Token(TokenType.Comment, "// comment");

        // Act
        renderer.RenderToken(token);
        var markup = renderer.GetMarkup();

        // Assert
        markup.ShouldContain("[grey]");
        markup.ShouldContain("// comment");
    }

    [Fact]
    public void RenderToken_Number_ProducesMagentaMarkup()
    {
        // Arrange
        var renderer = new RazorConsoleTokenRenderer();
        var token = new Token(TokenType.Number, "42");

        // Act
        renderer.RenderToken(token);
        var markup = renderer.GetMarkup();

        // Assert
        markup.ShouldContain("[magenta]42[/]");
    }

    [Fact]
    public void RenderToken_Text_ProducesUnstyledMarkup()
    {
        // Arrange
        var renderer = new RazorConsoleTokenRenderer();
        var token = new Token(TokenType.Text, "plain text");

        // Act
        renderer.RenderToken(token);
        var markup = renderer.GetMarkup();

        // Assert
        markup.ShouldBe("plain text");
        markup.ShouldNotContain("[");
    }

    [Fact]
    public void RenderToken_WithSpecialCharacters_EscapesMarkup()
    {
        // Arrange
        var renderer = new RazorConsoleTokenRenderer();
        var token = new Token(TokenType.String, "\"[bold]test[/]\"");

        // Act
        renderer.RenderToken(token);
        var markup = renderer.GetMarkup();

        // Assert
        // Spectre.Console Markup.Escape should escape the brackets
        markup.ShouldNotContain("[bold]");
        markup.ShouldContain("&"); // Escaped content
    }

    [Fact]
    public void RenderMultipleTokens_BuildsCompleteMarkup()
    {
        // Arrange
        var renderer = new RazorConsoleTokenRenderer();

        // Act
        renderer.RenderToken(new Token(TokenType.Keyword, "public"));
        renderer.RenderToken(new Token(TokenType.Text, " "));
        renderer.RenderToken(new Token(TokenType.Keyword, "class"));
        renderer.RenderToken(new Token(TokenType.Text, " "));
        renderer.RenderToken(new Token(TokenType.Type, "Program"));

        var markup = renderer.GetMarkup();

        // Assert
        markup.ShouldContain("[blue]public[/]");
        markup.ShouldContain("[blue]class[/]");
        markup.ShouldContain("[cyan]Program[/]");
    }

    [Fact]
    public void Clear_EmptiesMarkupBuffer()
    {
        // Arrange
        var renderer = new RazorConsoleTokenRenderer();
        renderer.RenderToken(new Token(TokenType.Keyword, "class"));

        // Act
        renderer.Clear();
        var markup = renderer.GetMarkup();

        // Assert
        markup.ShouldBeEmpty();
    }

    [Fact]
    public void GetMarkup_EmptyRenderer_ReturnsEmptyString()
    {
        // Arrange
        var renderer = new RazorConsoleTokenRenderer();

        // Act
        var markup = renderer.GetMarkup();

        // Assert
        markup.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(TokenType.Keyword, "blue")]
    [InlineData(TokenType.Type, "cyan")]
    [InlineData(TokenType.String, "green")]
    [InlineData(TokenType.Comment, "grey")]
    [InlineData(TokenType.Number, "magenta")]
    [InlineData(TokenType.Operator, "yellow")]
    [InlineData(TokenType.Punctuation, "silver")]
    [InlineData(TokenType.Preprocessor, "purple")]
    [InlineData(TokenType.Identifier, "white")]
    public void RenderToken_VariousTokenTypes_ProducesCorrectColors(TokenType tokenType, string expectedColor)
    {
        // Arrange
        var renderer = new RazorConsoleTokenRenderer();
        var token = new Token(tokenType, "test");

        // Act
        renderer.RenderToken(token);
        var markup = renderer.GetMarkup();

        // Assert
        markup.ShouldContain($"[{expectedColor}]test[/]");
    }
}
