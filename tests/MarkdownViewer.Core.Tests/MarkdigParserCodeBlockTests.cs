using System.Text;
using MarkdownViewer.Core.Elements;
using MarkdownViewer.Core.Implementations;
using Xunit;

namespace MarkdownViewer.Core.Tests;

public class MarkdigParserCodeBlockTests
{
    [Theory]
    [InlineData("    alpha\n    beta\n", "alpha\nbeta")]
    [InlineData("\talpha\n\tbeta\n", "alpha\nbeta")]
    [InlineData("    alpha\r\n    beta\r\n", "alpha\nbeta")]
    [InlineData("    alpha\n      beta  \n", "alpha\n  beta  ")]
    [InlineData("    café\n    测试", "café\n测试")]
    public async Task Indented_code_preserves_content_and_has_no_language(string markdown, string expectedCode)
    {
        foreach (var elements in await ParseWithBothApisAsync(markdown))
        {
            var code = Assert.IsType<CodeBlockElement>(Assert.Single(elements));
            Assert.Equal(MarkdownElementType.CodeBlock, code.ElementType);
            Assert.Equal(PlatformLines(expectedCode), code.Code);
            Assert.Equal(string.Empty, code.Language);
            Assert.Equal(PlatformLines(markdown.TrimEnd('\r', '\n') + "\n"), code.RawText);
        }
    }

    [Theory]
    [InlineData("```csharp\nvar x = 1;\n```\n", "var x = 1;", "csharp")]
    [InlineData("```\nalpha\nbeta\n```\n", "alpha\nbeta", "")]
    [InlineData("```text\r\nalpha\r\nbeta\r\n```\r\n", "alpha\nbeta", "text")]
    [InlineData("```text\nalpha\n\nbeta\n```\n", "alpha\n\nbeta", "text")]
    [InlineData("~~~text\nalpha\nbeta\n~~~\n", "alpha\nbeta", "text")]
    [InlineData("```csharp title=sample\nvar x = 1;\n```\n", "var x = 1;", "csharp")]
    public async Task Fenced_code_preserves_content_and_language(string markdown, string expectedCode, string expectedLanguage)
    {
        foreach (var elements in await ParseWithBothApisAsync(markdown))
        {
            var code = Assert.IsType<CodeBlockElement>(Assert.Single(elements));
            Assert.Equal(MarkdownElementType.CodeBlock, code.ElementType);
            Assert.Equal(PlatformLines(expectedCode), code.Code);
            Assert.Equal(expectedLanguage, code.Language);
            Assert.Equal(PlatformLines(markdown), code.RawText);
        }
    }

    [Fact]
    public async Task Math_blocks_remain_math_elements()
    {
        const string markdown = "$$\nx^2 + y^2\n$$\n";

        foreach (var elements in await ParseWithBothApisAsync(markdown))
        {
            var math = Assert.IsType<MathBlockElement>(Assert.Single(elements));
            Assert.Equal(MarkdownElementType.MathBlock, math.ElementType);
            Assert.Equal("x^2 + y^2", math.Content);
            Assert.Equal(PlatformLines(markdown), math.RawText);
        }
    }

    [Fact]
    public async Task Mixed_content_keeps_paragraphs_lists_and_code_in_order()
    {
        const string markdown = "Intro.\n\n    alpha\n    beta\n\nAfter.\n\n- first\n- second\n\n```csharp\nvar x = 1;\n```\n\n$$\nx^2\n$$\n";

        foreach (var elements in await ParseWithBothApisAsync(markdown))
        {
            Assert.Collection(elements,
                element => Assert.Equal("Intro.", PlainTextInlines(Assert.IsType<ParagraphElement>(element))),
                element =>
                {
                    var code = Assert.IsType<CodeBlockElement>(element);
                    Assert.Equal(PlatformLines("alpha\nbeta"), code.Code);
                    Assert.Equal(string.Empty, code.Language);
                },
                element => Assert.Equal("After.", PlainTextInlines(Assert.IsType<ParagraphElement>(element))),
                element =>
                {
                    var list = Assert.IsType<ListElement>(element);
                    Assert.False(list.IsOrdered);
                    Assert.Equal(new[] { " first", " second" }, list.Items.Select(item => item.Text));
                },
                element =>
                {
                    var code = Assert.IsType<CodeBlockElement>(element);
                    Assert.Equal("var x = 1;", code.Code);
                    Assert.Equal("csharp", code.Language);
                },
                element => Assert.Equal("x^2", Assert.IsType<MathBlockElement>(element).Content));
        }
    }

    [Fact]
    public async Task Issue_10_indented_sections_preserve_all_text()
    {
        // Reduced structural reproduction of https://github.com/LITTOMA/Avalonia.MarkdownViewer/issues/10.
        // Neutral text replaces the original lyrics. Do not assert how blank lines split code blocks:
        // that is a separate, pre-existing streaming-parser limitation.
        const string markdown = "\n    Première ligne.\n    Deuxième ligne.\n\n*(Refrain)*\n\n    Troisième ligne.\n    Quatrième ligne.\n\n    Cinquième ligne.\n    Sixième ligne.\n\n*(Fin)*\n\n    Dernière ligne.\n\n[audio](https://example.com/audio)";
        var expectedLines = new[]
        {
            "Première ligne.", "Deuxième ligne.", "Troisième ligne.", "Quatrième ligne.",
            "Cinquième ligne.", "Sixième ligne.", "Dernière ligne."
        };

        foreach (var elements in await ParseWithBothApisAsync(markdown))
        {
            var codes = elements.OfType<CodeBlockElement>().ToList();
            Assert.NotEmpty(codes);
            Assert.All(codes, code => Assert.Equal(string.Empty, code.Language));
            Assert.Equal(expectedLines, codes.SelectMany(code => code.Code.Split(Environment.NewLine)).Where(line => line.Length > 0));
            Assert.Equal(3, elements.OfType<ParagraphElement>().Count());
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("    \n\t\n")]
    public async Task Empty_or_whitespace_documents_have_no_elements(string markdown)
    {
        foreach (var elements in await ParseWithBothApisAsync(markdown))
            Assert.Empty(elements);
    }

    [Theory]
    [InlineData("   alpha\n", "   alpha")]
    [InlineData("paragraph\n    continuation\n", "paragraph\n    continuation")]
    public async Task Non_code_indentation_remains_a_paragraph(string markdown, string expectedText)
    {
        foreach (var elements in await ParseWithBothApisAsync(markdown))
        {
            var paragraph = Assert.IsType<ParagraphElement>(Assert.Single(elements));
            Assert.Equal(expectedText, PlainTextInlines(paragraph));
        }
    }

    private static string PlainTextInlines(ParagraphElement paragraph) =>
        string.Join("\n", paragraph.Inlines.OfType<TextElement>().Select(inline => inline.Text).Where(text => text.Length > 0));

    private static string PlatformLines(string value) =>
        value.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);

    private static async Task<List<MarkdownElement>[]> ParseWithBothApisAsync(string markdown)
    {
        var textElements = new List<MarkdownElement>();
        await foreach (var element in new MarkdigParser().ParseTextAsync(markdown))
            textElements.Add(element);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(markdown));
        var streamElements = new List<MarkdownElement>();
        await foreach (var element in new MarkdigParser().ParseStreamAsync(stream))
            streamElements.Add(element);

        return [textElements, streamElements];
    }
}
