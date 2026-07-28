[![.NET](https://github.com/TahaZahit/MultiMap.NET/actions/workflows/dotnet.yml/badge.svg)](https://github.com/TahaZahit/MultiMap.NET/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/MultiMap.NET.svg)](https://www.nuget.org/packages/MultiMap.NET)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MultiMap.NET.svg)](https://www.nuget.org/packages/MultiMap.NET)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

# MultiMap.NET

A **thread-safe**, high-performance **multi-map** for .NET — a dictionary that holds more than one value per key.

```csharp
var map = new MultiMap<string, int>();
map.Add("primes", 2);
map.Add("primes", 3);
map.Add("primes", 5);

map["primes"];   // [2, 3, 5]
map.Count;       // 1  (keys)
map.ValueCount;  // 3  (pairs)
```

## Why this exists

.NET already has a multi-map: `ILookup<TKey, TValue>`, which you get from `Enumerable.ToLookup`. But it is **immutable** — once built, you cannot add or remove anything. So everyone ends up hand-rolling `Dictionary<TKey, List<TValue>>` and re-writing the same "does the list exist yet?" boilerplate.

`MultiMap<TKey, TValue>` is the mutable counterpart, and it **implements `ILookup<TKey, TValue>`** — so it drops straight into code that already expects a lookup.

```csharp
ILookup<string, Order> lookup = map;   // no conversion, no copy
```

## Install

```
dotnet add package MultiMap.NET
```

Targets `netstandard2.0` — works on .NET Framework 4.6.1+, .NET Core 2.0+, and every modern .NET.

## Features

- **Multiple values per key** with `Add`, `AddRange`, `Remove`, `RemoveAll`.
- **Thread-safe.** Backed by `ReaderWriterLockSlim`, so concurrent readers never block each other. Every returned collection is a snapshot, so you can enumerate while other threads mutate.
- **Implements `ILookup<TKey, TValue>`** for drop-in LINQ interop.
- **Insertion order preserved** under each key, in both duplicate modes.
- **Optional duplicate rejection** — list semantics by default, set semantics on request.
- **Custom comparers** for keys and values.
- **No dependencies.**

## Usage

### Creating

```csharp
using MultiMaps;

// Duplicate values allowed under a key (default)
var map = new MultiMap<string, int>();

// Reject a value already present under that key
var unique = new MultiMap<string, int>(allowDuplicateValues: false);

// Custom comparers
var caseInsensitive = new MultiMap<string, int>(
    allowDuplicateValues: true,
    keyComparer: StringComparer.OrdinalIgnoreCase,
    valueComparer: null);

// From existing pairs
var copied = new MultiMap<string, int>(existingPairs);
```

### Adding

```csharp
map.Add("a", 1);                        // true
map.AddRange("a", new[] { 2, 3 });      // 2  (how many were added)

unique.Add("a", 1);                     // true
unique.Add("a", 1);                     // false — already there
```

### Reading

```csharp
map["a"];                     // [1, 2, 3]
map["missing"];               // []  — empty, never throws

map.TryGetValues("a", out var values);   // true, values = [1, 2, 3]

map.ContainsKey("a");         // true
map.ContainsPair("a", 2);     // true  — this exact pair
map.ContainsValue(2);         // true  — under any key
```

`map[key]` returns an empty sequence for an unknown key rather than throwing — it follows `ILookup`, not `Dictionary`.

### Counting

Two different numbers, so they get two different names:

```csharp
map.Count;       // number of KEYS   (this is what ILookup.Count means)
map.ValueCount;  // number of PAIRS
```

### Removing

```csharp
map.Remove("a", 2);    // true — removes ONE occurrence of that value
map.RemoveAll("a");    // 2    — removes the key and returns how many values went with it
map.Clear();
```

A key disappears automatically once its last value is removed.

### LINQ

`MultiMap` is an `ILookup`, so it enumerates as one `IGrouping` per key:

```csharp
foreach (var group in map)
{
    Console.WriteLine($"{group.Key}: {string.Join(", ", group)}");
}

var all = map.SelectMany(g => g).ToList();
```

Build one straight from a sequence, the same way you would call `ToLookup`:

```csharp
var byFirstLetter = words.ToMultiMap(w => w[0]);
var lengthsByLetter = words.ToMultiMap(w => w[0], w => w.Length);

byFirstLetter.Add('z', "zebra");   // ...but unlike ToLookup, you can still change it
```

### Converting out

```csharp
map.Pairs();         // IEnumerable<KeyValuePair<TKey, TValue>>, one entry per pair
map.ToDictionary();  // Dictionary<TKey, List<TValue>>, an independent copy
map.Keys;            // snapshot of keys
map.Values;          // snapshot of every value
```

## `MultiValueDictionary`

If you know this data structure by the name Microsoft used in the unreleased `Microsoft.Experimental.Collections` package, that name works too:

```csharp
var dictionary = new MultiValueDictionary<string, int>();
```

`MultiValueDictionary<TKey, TValue>` derives from `MultiMap<TKey, TValue>` and behaves identically. Use whichever reads better.

## Thread safety

Reads take a shared lock, writes take an exclusive one. Each individual operation is atomic.

Every collection handed back — `map[key]`, `Keys`, `Values`, `Pairs()`, `ToDictionary()`, and enumeration — is a **snapshot taken under the lock**. That means iterating a `MultiMap` while another thread mutates it will never throw `InvalidOperationException`, unlike iterating a `Dictionary`.

Note that a *sequence* of operations is not atomic as a group. This is a race, and no collection can fix it for you:

```csharp
if (!map.ContainsPair("a", 1))   // another thread can add it right here
    map.Add("a", 1);
```

Use `allowDuplicateValues: false` and let `Add` return `false` instead.

## Performance notes

- Lookup, add and remove-by-key are O(1) on the key.
- `Remove(key, value)` scans that one key's values to find the occurrence: O(n) in the size of that key's bucket.
- With `allowDuplicateValues: false`, each `Add` scans that key's bucket for a match: O(n) in the size of that bucket. This is the price of keeping insertion order; for the handful of values a typical key holds it is faster than a hash set, but it is worth knowing if you plan to put thousands of values under a single key.
- `ContainsValue` scans everything: O(n) in total pairs.

## License

MIT — see [LICENSE](LICENSE).
