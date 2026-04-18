using FluentAssertions;
using Nodefy.Api.Lib;
using Xunit;

namespace Nodefy.Tests.Unit;

public class FractionalIndexTests
{
    [Theory]
    [InlineData(1_000_000.0, 2_000_000.0, 1_500_000.0)]
    [InlineData(0.0, 1_000_000.0, 500_000.0)]
    [InlineData(999_000.0, 1_000_000.0, 999_500.0)]
    public void Between_ReturnsCorrectMidpoint(double prev, double next, double expected)
        => FractionalIndex.Between(prev, next).Should().BeApproximately(expected, 0.001);

    [Fact]
    public void After_AddsOneMillionToLast()
        => FractionalIndex.After(2_000_000.0).Should().Be(3_000_000.0);

    [Fact]
    public void Before_HalvesFirst()
        => FractionalIndex.Before(1_000_000.0).Should().Be(500_000.0);

    [Fact]
    public void NeedsRebalance_ReturnsTrueWhenPositionBelowThreshold()
        => FractionalIndex.NeedsRebalance(new[] { 1_000_000.0, 1e-10 }).Should().BeTrue();

    [Fact]
    public void NeedsRebalance_ReturnsTrueWhenPositionAboveThreshold()
        => FractionalIndex.NeedsRebalance(new[] { 1e16 }).Should().BeTrue();

    [Fact]
    public void NeedsRebalance_ReturnsFalseForNormalPositions()
        => FractionalIndex.NeedsRebalance(new[] { 1_000_000.0, 2_000_000.0, 3_000_000.0 }).Should().BeFalse();

    [Fact]
    public void Rebalance_Returns_EvenlySpacedValues()
    {
        var result = FractionalIndex.Rebalance(3);
        result.Should().HaveCount(3);
        result[0].Should().Be(1_000_000.0);
        result[1].Should().Be(2_000_000.0);
        result[2].Should().Be(3_000_000.0);
    }
}
