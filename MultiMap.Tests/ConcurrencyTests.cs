using System.Collections.Concurrent;
using MultiMaps;

namespace MultiMapNet.Tests;

public class ConcurrencyTests
{
    private const int Threads = 8;
    private const int PerThread = 2_000;

    [Fact]
    public void ParallelAdds_KeepValueCountExact()
    {
        var map = new MultiMap<int, int>();

        Parallel.For(0, Threads, thread =>
        {
            for (var i = 0; i < PerThread; i++)
            {
                map.Add(i % 50, thread * PerThread + i);
            }
        });

        Assert.Equal(Threads * PerThread, map.ValueCount);
        Assert.Equal(50, map.Count);
        Assert.Equal(map.ValueCount, map.Pairs().Count());
    }

    [Fact]
    public void ParallelAddsAndRemoves_LeaveTheMapConsistent()
    {
        var map = new MultiMap<int, int>();
        for (var i = 0; i < 1_000; i++) map.Add(i % 20, i);

        Parallel.For(0, Threads, thread =>
        {
            for (var i = 0; i < PerThread; i++)
            {
                var key = i % 20;
                if ((thread + i) % 2 == 0) map.Add(key, thread * PerThread + i);
                else map.Remove(key, i);
            }
        });

        // The exact total depends on interleaving, but the two views of it must never disagree.
        Assert.Equal(map.ValueCount, map.Pairs().Count());
        Assert.Equal(map.ValueCount, map.Values.Count);
        Assert.Equal(map.Count, map.Keys.Count);
    }

    [Fact]
    public async Task ConcurrentReadsDuringWrites_NeverThrow()
    {
        var map = new MultiMap<int, int>();
        for (var i = 0; i < 200; i++) map.Add(i % 10, i);

        var failures = new ConcurrentBag<Exception>();
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var writer = Task.Run(() =>
        {
            var n = 0;
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    map.Add(n % 10, n);
                    if (n % 3 == 0) map.Remove(n % 10, n - 1);
                    n++;
                }
            }
            catch (Exception ex) { failures.Add(ex); }
        });

        var readers = Enumerable.Range(0, Threads).Select(readerIndex => Task.Run(() =>
        {
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    _ = map.Count;
                    _ = map.ValueCount;
                    foreach (var group in map) _ = group.Count();
                    _ = map[5].Count();
                    _ = map.ContainsValue(7);
                }
            }
            catch (Exception ex) { failures.Add(ex); }
        })).ToArray();

        await Task.WhenAll(readers.Append(writer));

        Assert.Empty(failures);
    }

    [Fact]
    public void ParallelRemoveAll_RemovesEachKeyExactlyOnce()
    {
        var map = new MultiMap<int, int>();
        for (var key = 0; key < 100; key++)
        {
            for (var i = 0; i < 10; i++) map.Add(key, i);
        }

        var removed = 0;
        Parallel.For(0, Threads, _ =>
        {
            for (var key = 0; key < 100; key++)
            {
                Interlocked.Add(ref removed, map.RemoveAll(key));
            }
        });

        Assert.Equal(1_000, removed);
        Assert.Equal(0, map.Count);
        Assert.Equal(0, map.ValueCount);
    }

    [Fact]
    public void ParallelAdds_WithDuplicatesDisallowed_StoreEachValueOnce()
    {
        var map = new MultiMap<int, int>(allowDuplicateValues: false);

        Parallel.For(0, Threads, _ =>
        {
            for (var i = 0; i < 500; i++) map.Add(0, i);
        });

        Assert.Equal(500, map.ValueCount);
        Assert.Equal(500, map[0].Distinct().Count());
    }
}
