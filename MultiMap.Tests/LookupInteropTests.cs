using MultiMaps;

namespace MultiMapNet.Tests;

public class LookupInteropTests
{
    [Fact]
    public void MultiMap_IsUsableAsILookup()
    {
        ILookup<string, int> lookup = new MultiMap<string, int>();
        var map = (MultiMap<string, int>)lookup;
        map.AddRange("a", new[] { 1, 2 });
        map.Add("b", 3);

        Assert.True(lookup.Contains("a"));
        Assert.False(lookup.Contains("zzz"));
        Assert.Equal(2, lookup.Count);
        Assert.Equal(new[] { 1, 2 }, lookup["a"]);
    }

    [Fact]
    public void Enumeration_YieldsOneGroupingPerKey()
    {
        var map = new MultiMap<string, int>();
        map.AddRange("a", new[] { 1, 2 });
        map.Add("b", 3);

        var groups = map.OrderBy(g => g.Key).ToArray();

        Assert.Equal(2, groups.Length);
        Assert.Equal("a", groups[0].Key);
        Assert.Equal(new[] { 1, 2 }, groups[0]);
        Assert.Equal("b", groups[1].Key);
        Assert.Equal(new[] { 3 }, groups[1]);
    }

    [Fact]
    public void Enumeration_IsSnapshot_SoMutationDuringIterationIsSafe()
    {
        var map = new MultiMap<string, int>();
        map.Add("a", 1);
        map.Add("b", 2);

        var seen = 0;
        foreach (var group in map)
        {
            seen++;
            map.Add("added-during-iteration-" + group.Key, 99);
        }

        Assert.Equal(2, seen);
        Assert.Equal(4, map.Count);
    }

    [Fact]
    public void SelectMany_FlattensLikeALookup()
    {
        var map = new MultiMap<string, int>();
        map.AddRange("a", new[] { 1, 2 });
        map.Add("b", 3);

        var flattened = map.SelectMany(g => g).OrderBy(v => v).ToArray();

        Assert.Equal(new[] { 1, 2, 3 }, flattened);
    }

    [Fact]
    public void ToMultiMap_GroupsByKeySelector()
    {
        var words = new[] { "apple", "avocado", "banana", "blueberry", "cherry" };

        var map = words.ToMultiMap(w => w[0]);

        Assert.Equal(3, map.Count);
        Assert.Equal(new[] { "apple", "avocado" }, map['a']);
        Assert.Equal(new[] { "cherry" }, map['c']);
    }

    [Fact]
    public void ToMultiMap_ProjectsValues()
    {
        var words = new[] { "apple", "avocado", "banana" };

        var map = words.ToMultiMap(w => w[0], w => w.Length);

        Assert.Equal(new[] { 5, 7 }, map['a']);
        Assert.Equal(new[] { 6 }, map['b']);
    }

    [Fact]
    public void ToMultiMap_CanCollapseDuplicates()
    {
        var numbers = new[] { 1, 1, 2, 2, 2, 3 };

        var map = numbers.ToMultiMap(n => n % 2 == 0 ? "even" : "odd", allowDuplicateValues: false);

        Assert.Equal(new[] { 1, 3 }, map["odd"]);
        Assert.Equal(new[] { 2 }, map["even"]);
    }

    [Fact]
    public void ToMultiMap_ResultIsMutable_UnlikeToLookup()
    {
        var map = new[] { 1, 2, 3 }.ToMultiMap(n => n % 2 == 0 ? "even" : "odd");

        map.Add("even", 4);

        Assert.Equal(new[] { 2, 4 }, map["even"]);
    }

    [Fact]
    public void MultiValueDictionary_BehavesIdenticallyToMultiMap()
    {
        var dictionary = new MultiValueDictionary<string, int>();
        dictionary.AddRange("a", new[] { 1, 2 });

        Assert.IsAssignableFrom<MultiMap<string, int>>(dictionary);
        Assert.IsAssignableFrom<ILookup<string, int>>(dictionary);
        Assert.Equal(new[] { 1, 2 }, dictionary["a"]);
        Assert.Equal(2, dictionary.ValueCount);
    }

    [Fact]
    public void MultiValueDictionary_HonoursDuplicateSetting()
    {
        var dictionary = new MultiValueDictionary<string, int>(allowDuplicateValues: false);

        Assert.True(dictionary.Add("a", 1));
        Assert.False(dictionary.Add("a", 1));
    }
}
