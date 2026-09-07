using BalotoAppOnline;
using Newtonsoft.Json;

static class WebSyncChecks
{
    public static async Task Run(string directory)
    {
        int passed = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL sync: " + name); passed++; }
        static Sorteo Draw(int id) => new() { Fecha = new DateTime(2026, 1, 1).AddDays(id), Numeros = new[] { 1, 2, 3, 4, 5, id % 16 + 1 } };
        static WebPage Page(bool next, params int[] ids) => new(ids.Select(Draw).ToList(), next);
        var requests = new List<int>();
        var pages = new Dictionary<int, WebPage> { [1] = Page(true, 6, 5), [2] = Page(true, 4, 3), [3] = Page(false, 2, 1) };
        int fail = -1;
        Task<WebPage> Fetch(int n) { requests.Add(n); if (n == fail) throw new IOException("Simulated network failure"); return Task.FromResult(pages[n]); }
        string path = Path.Combine(directory, "sync.json");
        var result = await new WebSync(path).RunAsync(Fetch);
        Check(result.Count == 6 && requests.SequenceEqual(new[] { 1, 2, 3 }), "first full scan");
        requests.Clear();
        result = await new WebSync(path).RunAsync(Fetch);
        Check(result.Count == 6 && requests.SequenceEqual(new[] { 1 }), "persistent cache fetches only page 1");
        pages = new() { [1] = Page(true, 7, 6), [2] = Page(true, 5, 4), [3] = Page(true, 3, 2), [4] = Page(false, 1) };
        requests.Clear();
        result = await new WebSync(path).RunAsync(Fetch);
        Check(result.Count == 7 && requests.SequenceEqual(new[] { 1, 2 }), "shifted pages stop on known results");
        requests.Clear();
        await new WebSync(path).RunAsync(Fetch);
        Check(requests.SequenceEqual(new[] { 1 }), "second update skips old pages");

        pages = new() { [1] = Page(true, 11, 10), [2] = Page(true, 9, 8), [3] = Page(true, 7, 6) };
        fail = 2;
        try { await new WebSync(path).RunAsync(Fetch); throw new Exception("Expected failure"); } catch (IOException) { }
        Check(JsonConvert.DeserializeObject<WebSyncState>(File.ReadAllText(path)).Results.Count == 7, "failed increment retains baseline");
        fail = -1; requests.Clear();
        result = await new WebSync(path).RunAsync(Fetch);
        Check(result.Count == 11 && requests.SequenceEqual(new[] { 1, 2, 3 }), "retry does not skip unseen new pages");

        path = Path.Combine(directory, "resume.json");
        pages = new() { [1] = Page(true, 6, 5), [2] = Page(true, 4, 3), [3] = Page(false, 2, 1) };
        fail = 3;
        try { await new WebSync(path).RunAsync(Fetch); throw new Exception("Expected failure"); } catch (IOException) { }
        var partial = JsonConvert.DeserializeObject<WebSyncState>(File.ReadAllText(path));
        Check(!partial.Complete && partial.LastPage == 2 && partial.Pending.Count == 4, "checkpoint saved without claiming completion");
        fail = -1; requests.Clear();
        result = await new WebSync(path).RunAsync(Fetch);
        Check(result.Count == 6 && requests.SequenceEqual(new[] { 1, 3 }), "unchanged head resumes without re-fetching page 2");

        path = Path.Combine(directory, "changed-head.json");
        fail = 3;
        try { await new WebSync(path).RunAsync(Fetch); } catch (IOException) { }
        pages = new() { [1] = Page(true, 7, 6), [2] = Page(true, 5, 4), [3] = Page(true, 3, 2), [4] = Page(false, 1) };
        fail = -1; requests.Clear();
        result = await new WebSync(path).RunAsync(Fetch);
        Check(result.Count == 7 && requests.SequenceEqual(new[] { 1, 2, 3, 4 }), "changed incomplete head safely rebuilds the checkpoint");
        path = Path.Combine(directory, "corrupt.json");
        File.WriteAllText(path, "not-json");
        result = await new WebSync(path).RunAsync(Fetch);
        Check(result.Count == 7, "corrupt cache recovery");

        string row(string kind, string numbers) => $"<tr><td><img src='{kind}-kind.png'></td><td>5 de Septiembre de 2026</td><td>{numbers}</td></tr>";
        var parsed = WebScraper.ParsePage("<table id='results-table'><tbody>" + row("baloto", "11 - 12 - 17 - 28 - 31 - <span>15</span>") + row("revancha", "1 - 2 - 3 - 4 - 5 - 6") + row("baloto", "1 - 1 - 3 - 4 - 5 - 6") + "</tbody></table><a href='?page=2'>Siguiente</a>");
        Check(parsed.Draws.Count == 1 && parsed.Draws[0].Numeros.SequenceEqual(new[] { 11, 12, 17, 28, 31, 15 }) && parsed.HasNext, "original HTML dates/numbers/revancha/validation preserved");
        try { WebScraper.ParsePage("<html>unavailable</html>"); throw new Exception("Expected parse failure"); } catch (IOException) { passed++; }
        path = Path.Combine(directory, "empty.json");
        try { await new WebSync(path).RunAsync(_ => Task.FromResult(new WebPage(new(), false))); throw new Exception("Expected empty rejection"); } catch (IOException) { Check(!File.Exists(path), "empty responses never mark scan complete"); }
        Console.WriteLine($"PASS: {passed} synchronization checks; cache, page shifts, retries, resumption, corruption and HTML parser.");
    }
}
