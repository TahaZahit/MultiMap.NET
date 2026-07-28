using MultiMaps;

namespace MultiMapNet.Tests;

public class CoreFunctionalityTests
{
    [Fact]
    public void Add_StoresMultipleValuesUnderOneKey()
    {
        var map = new MultiMap<string, int>();

        Assert.True(map.Add("a", 1));
        Assert.True(map.Add("a", 2));

        Assert.Equal(new[] { 1, 2 }, map["a"]);
    }

    [Fact]
    public void Add_AllowsDuplicateValues_ByDefault()
    {
        var map = new MultiMap<string, int>();

        map.Add("a", 1);
        map.Add("a", 1);

        Assert.Equal(new[] { 1, 1 }, map["a"]);
        Assert.Equal(2, map.ValueCount);
    }

    [Fact]
    public void Add_RejectsDuplicateValues_WhenDisallowed()
    {
        var map = new MultiMap<string, int>(allowDuplicateValues: false);

        Assert.True(map.Add("a", 1));
        Assert.False(map.Add("a", 1));

        Assert.Equal(new[] { 1 }, map["a"]);
        Assert.Equal(1, map.ValueCount);
    }

    [Fact]
    public void Add_SameValueUnderDifferentKeys_IsNeverRejected()
    {
        var map = new MultiMap<string, int>(allowDuplicateValues: false);

        Assert.True(map.Add("a", 1));
        Assert.True(map.Add("b", 1));

        Assert.Equal(2, map.Count);
        Assert.Equal(2, map.ValueCount);
    }

    [Fact]
    public void Add_NullKey_Throws()
    {
        var map = new MultiMap<string, int>();

        Assert.Throws<ArgumentNullException>(() => map.Add(null!, 1));
    }

    [Fact]
    public void AddRange_ReturnsNumberActuallyAdded()
    {
        var map = new MultiMap<string, int>(allowDuplicateValues: false);

        Assert.Equal(3, map.AddRange("a", new[] { 1, 2, 3 }));
        Assert.Equal(1, map.AddRange("a", new[] { 3, 4 }));

        Assert.Equal(new[] { 1, 2, 3, 4 }, map["a"]);
    }

    [Fact]
    public void Indexer_MissingKey_ReturnsEmptyInsteadOfThrowing()
    {
        var map = new MultiMap<string, int>();

        Assert.Empty(map["nope"]);
    }

    [Fact]
    public void Count_IsKeys_ValueCount_IsPairs()
    {
        var map = new MultiMap<string, int>();
        map.AddRange("a", new[] { 1, 2 });
        map.Add("b", 3);

        Assert.Equal(2, map.Count);
        Assert.Equal(3, map.ValueCount);
    }

    [Fact]
    public void Remove_DropsSingleOccurrenceOnly()
    {
        var map = new MultiMap<string, int>();
        map.AddRange("a", new[] { 1, 1, 2 });

        Assert.True(map.Remove("a", 1));

        Assert.Equal(new[] { 1, 2 }, map["a"]);
        Assert.Equal(2, map.ValueCount);
    }

    [Fact]
    public void Remove_DropsKey_WhenLastValueGoes()
    {
        var map = new MultiMap<string, int>();
        map.Add("a", 1);

        Assert.True(map.Remove("a", 1));

        Assert.False(map.ContainsKey("a"));
        Assert.Equal(0, map.Count);
    }

    [Fact]
    public void Remove_MissingPair_ReturnsFalse()
    {
        var map = new MultiMap<string, int>();
        map.Add("a", 1);

        Assert.False(map.Remove("a", 99));
        Assert.False(map.Remove("zzz", 1));
        Assert.Equal(1, map.ValueCount);
    }

    [Fact]
    public void RemoveAll_ReturnsRemovedCount()
    {
        var map = new MultiMap<string, int>();
        map.AddRange("a", new[] { 1, 2, 3 });

        Assert.Equal(3, map.RemoveAll("a"));
        Assert.Equal(0, map.RemoveAll("a"));
        Assert.Equal(0, map.ValueCount);
    }

    [Fact]
    public void Clear_EmptiesEverything()
    {
        var map = new MultiMap<string, int>();
        map.AddRange("a", new[] { 1, 2 });
        map.Add("b", 3);

        map.Clear();

        Assert.Equal(0, map.Count);
        Assert.Equal(0, map.ValueCount);
        Assert.Empty(map["a"]);
    }

    [Fact]
    public void ContainsPair_MatchesExactPair()
    {
        var map = new MultiMap<string, int>();
        map.Add("a", 1);

        Assert.True(map.ContainsPair("a", 1));
        Assert.False(map.ContainsPair("a", 2));
        Assert.False(map.ContainsPair("b", 1));
    }

    [Fact]
    public void ContainsValue_ScansEveryKey()
    {
        var map = new MultiMap<string, int>();
        map.Add("a", 1);
        map.Add("b", 2);

        Assert.True(map.ContainsValue(2));
        Assert.False(map.ContainsValue(3));
    }

    [Fact]
    public void TryGetValues_ReportsPresence()
    {
        var map = new MultiMap<string, int>();
        map.AddRange("a", new[] { 1, 2 });

        Assert.True(map.TryGetValues("a", out var found));
        Assert.Equal(new[] { 1, 2 }, found);

        Assert.False(map.TryGetValues("b", out var missing));
        Assert.Null(missing);
    }

    [Fact]
    public void Keys_And_Values_ReturnSnapshots()
    {
        var map = new MultiMap<string, int>();
        map.AddRange("a", new[] { 1, 2 });
        map.Add("b", 3);

        Assert.Equal(new[] { "a", "b" }, map.Keys.OrderBy(k => k));
        Assert.Equal(new[] { 1, 2, 3 }, map.Values.OrderBy(v => v));
    }

    [Fact]
    public void Pairs_YieldsOneEntryPerValue()
    {
        var map = new MultiMap<string, int>();
        map.AddRange("a", new[] { 1, 2 });

        var pairs = map.Pairs().OrderBy(p => p.Value).ToArray();

        Assert.Equal(2, pairs.Length);
        Assert.Equal(new KeyValuePair<string, int>("a", 1), pairs[0]);
        Assert.Equal(new KeyValuePair<string, int>("a", 2), pairs[1]);
    }

    [Fact]
    public void ToDictionary_ReturnsIndependentCopy()
    {
        var map = new MultiMap<string, int>();
        map.AddRange("a", new[] { 1, 2 });

        var copy = map.ToDictionary();
        copy["a"].Add(99);
        copy["b"] = new List<int> { 5 };

        Assert.Equal(new[] { 1, 2 }, map["a"]);
        Assert.False(map.ContainsKey("b"));
    }

    [Fact]
    public void Indexer_ResultIsSnapshot_UnaffectedByLaterMutation()
    {
        var map = new MultiMap<string, int>();
        map.Add("a", 1);

        var snapshot = map["a"].ToArray();
        map.Add("a", 2);

        Assert.Equal(new[] { 1 }, snapshot);
        Assert.Equal(new[] { 1, 2 }, map["a"]);
    }

    [Fact]
    public void InsertionOrder_IsPreserved_InBothDuplicateModes()
    {
        var withDuplicates = new MultiMap<string, int>();
        var withoutDuplicates = new MultiMap<string, int>(allowDuplicateValues: false);

        foreach (var value in new[] { 5, 3, 9, 1 })
        {
            withDuplicates.Add("a", value);
            withoutDuplicates.Add("a", value);
        }

        Assert.Equal(new[] { 5, 3, 9, 1 }, withDuplicates["a"]);
        Assert.Equal(new[] { 5, 3, 9, 1 }, withoutDuplicates["a"]);
    }

    [Fact]
    public void CustomKeyComparer_IsHonoured()
    {
        var map = new MultiMap<string, int>(true, StringComparer.OrdinalIgnoreCase, null);

        map.Add("Key", 1);
        map.Add("KEY", 2);

        Assert.Equal(1, map.Count);
        Assert.Equal(new[] { 1, 2 }, map["key"]);
    }

    [Fact]
    public void CustomValueComparer_IsHonoured()
    {
        var map = new MultiMap<string, string>(false, null, StringComparer.OrdinalIgnoreCase);

        Assert.True(map.Add("a", "Value"));
        Assert.False(map.Add("a", "VALUE"));

        Assert.Single(map["a"]);
    }

    [Fact]
    public void PairsConstructor_CopiesEverything()
    {
        var source = new[]
        {
            new KeyValuePair<string, int>("a", 1),
            new KeyValuePair<string, int>("a", 2),
            new KeyValuePair<string, int>("b", 3),
        };

        var map = new MultiMap<string, int>(source);

        Assert.Equal(2, map.Count);
        Assert.Equal(3, map.ValueCount);
    }

    [Fact]
    public void PairsConstructor_CollapsesRepeats_WhenDuplicatesDisallowed()
    {
        var source = new[]
        {
            new KeyValuePair<string, int>("a", 1),
            new KeyValuePair<string, int>("a", 1),
        };

        var map = new MultiMap<string, int>(source, allowDuplicateValues: false);

        Assert.Equal(1, map.ValueCount);
    }
}
