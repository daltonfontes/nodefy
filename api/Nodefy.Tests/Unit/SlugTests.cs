using FluentAssertions;
using Nodefy.Api.Lib;
using Xunit;

namespace Nodefy.Tests.Unit;

public class SlugTests
{
    [Theory]
    [InlineData("Vendas — Acme Corp", "vendas-acme-corp")]
    [InlineData("Açaí & Café", "acai-cafe")]
    [InlineData("  Espaços  ", "espacos")]
    public void GenerateSlug_HandlesAccents_LowercasesAndStripsDiacritics(string input, string expected)
        => Slug.Generate(input).Should().Be(expected);

    [Fact]
    public void GenerateSlug_RejectsLongInput_TruncatesTo50()
        => Slug.Generate(new string('a', 200)).Length.Should().BeLessThanOrEqualTo(50);
}
