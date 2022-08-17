using System.Collections.Generic;
using System.Threading.Tasks;
using Cdktf.Dotnet.Aws;
using FluentAssertions;
using Xunit;

namespace VpcModule.Tests;

public class UtilsTest
{
    #region Merge tests
    
    [Fact]
    public async Task Merge_Difference_Key_Dictionary_ShouldReturnAllElements()
    {
        // Arrange
        var dictionaryA = new Dictionary<string, string>
        {
            ["a"] = "b",
            ["c"] = "d"
        };

        var dictionaryB = new Dictionary<string, string>
        {
            ["e"] = "f",
            ["g"] = "h"
        };

        // Act
        var actual = Utils.Merge(dictionaryA, dictionaryB);

        // Assert
        actual["a"].Should().Be("b");
        actual["c"].Should().Be("d");
        actual["e"].Should().Be("f");
        actual["g"].Should().Be("h");
    }
    
    [Fact]
    public async Task Merge_Multiple_Dictionary_ShouldReturnAllElements()
    {
        // Arrange
        var dictionaryA = new Dictionary<string, string>
        {
            ["a"] = "b",
        };

        var dictionaryB = new Dictionary<string, string>
        {
            
            ["g"] = "h"
        };

        var dictionaryC = new Dictionary<string, string>
        {
            ["c"] = "d"
        };

        var dictionaryD = new Dictionary<string, string>
        {
            ["e"] = "f",
        };

        // Act
        var actual = Utils.Merge(dictionaryA, dictionaryB, dictionaryC, dictionaryD);

        // Assert
        actual["a"].Should().Be("b");
        actual["c"].Should().Be("d");
        actual["e"].Should().Be("f");
        actual["g"].Should().Be("h");
    }
    
    [Fact]
    public async Task Merge_SameKey_Dictionary_ShouldReplaceIt()
    {
        // Arrange
        var dictionaryA = new Dictionary<string, string>
        {
            ["a"] = "b",
            ["c"] = "d"
        };

        var dictionaryB = new Dictionary<string, string>
        {
            ["e"] = "f",
            ["c"] = "z"
        };

        // Act
        var actual = Utils.Merge(dictionaryA, dictionaryB);

        // Assert
        actual["a"].Should().Be("b");
        actual["c"].Should().Be("z");
        actual["e"].Should().Be("f");
    }
    
    [Fact]
    public async Task Merge_MultipleSameKey_Dictionary_ShouldReplaceIt()
    {
        // Arrange
        var dictionaryA = new Dictionary<string, string>
        {
            ["a"] = "b",
            ["c"] = "d"
        };

        var dictionaryB = new Dictionary<string, string>
        {
            ["e"] = "f",
            ["c"] = "z"
        };
        
        var dictionaryC = new Dictionary<string, string>
        {
            ["e"] = "f",
            ["c"] = "x"
        };

        // Act
        var actual = Utils.Merge(dictionaryA, dictionaryB, dictionaryC);

        // Assert
        actual["a"].Should().Be("b");
        actual["c"].Should().Be("x");
        actual["e"].Should().Be("f");
    }
    
    #endregion

    #region Element tests

    [Fact]
    public async Task Index_NotOverTheElementRange_ShouldReturnTheElement()
    {
        // Arrange
        var items = new[] { "a", "b", "c" };
        
        // Act
        var actual = Utils.Element(items, 0);
        
        // Assert
        actual.Should().Be("a");
    }
    
    [Theory]
    [InlineData(3, "a")]
    [InlineData(4, "b")]
    [InlineData(5, "c")]
    public async Task Index_OverTheElementRange_ShouldWrapAround(int index, string expected)
    {
        // Arrange
        var items = new[] { "a", "b", "c" };
        
        // Act
        var actual = Utils.Element(items, index);

        // Assert
        actual.Should().Be(expected);
    }

    #endregion
}