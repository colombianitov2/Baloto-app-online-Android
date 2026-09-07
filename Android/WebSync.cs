using Newtonsoft.Json;

namespace BalotoAppOnline;

public sealed record WebPage(List<Sorteo> Draws, bool HasNext);

public sealed class WebSyncState
{
    public int Schema { get; set; } = 1;
    public bool Complete { get; set; }
    public List<Sorteo> Results { get; set; } = new();
    public List<Sorteo> Pending { get; set; } = new();
    public string HeadSignature { get; set; } = "";
    public int LastPage { get; set; }
    public int LastRequestCount { get; set; }
}

// Page numbers move when new draws arrive. Stop on known results, not a saved URL.
public sealed class WebSync(string statePath)
{
    public int RequestCount { get; private set; }
    public static string Key(Sorteo draw) => draw.Fecha.ToString("yyyy-MM-dd") + ":" + string.Join(",", draw.Numeros);

    public async Task<List<Sorteo>> RunAsync(Func<int, Task<WebPage>> fetch, Action<int, int> progress = null)
    {
        RequestCount = 0;
        var state = ReadState();
        var known = state.Results.Select(Key).ToHashSet();
        var first = await GetPage(1);
        string head = string.Join(";", first.Draws.Select(Key));
        bool resume = !state.Complete && state.HeadSignature == head && state.LastPage >= 1;
        if (!state.Complete && !resume) { state.Pending.Clear(); state.LastPage = 0; }
        state.HeadSignature = head;
        var pending = state.Pending.DistinctBy(Key).ToDictionary(Key);
        var seenPages = new HashSet<string>();
        int number = 1;
        WebPage page = first;
        while (true)
        {
            var signature = string.Join(";", page.Draws.Select(Key));
            if (!seenPages.Add(signature)) throw new IOException("La web repitió una página; se conservó el avance para reintentar.");
            bool alreadyKnown = state.Complete && page.Draws.All(d => known.Contains(Key(d)));
            foreach (var draw in page.Draws) pending.TryAdd(Key(draw), draw);
            state.Pending = pending.Values.ToList();
            progress?.Invoke(number, state.Results.Concat(state.Pending).Select(Key).Distinct().Count());
            if (alreadyKnown || !page.HasNext)
            {
                state.Results = state.Results.Concat(state.Pending).DistinctBy(Key).OrderBy(d => d.Fecha).ToList();
                state.Pending.Clear();
                state.Complete = true;
                state.LastPage = 0;
                state.LastRequestCount = RequestCount;
                Save(state);
                return state.Results;
            }
            // Partial discoveries must not become the completed baseline: on retry
            // all new pages must still be fetched before declaring the scan complete.
            if (!state.Complete && !(number == 1 && resume)) state.LastPage = number;
            Save(state);
            number = number == 1 && resume ? state.LastPage + 1 : number + 1;
            if (number > 10000) throw new IOException("Se alcanzó el límite de páginas; se conservó el avance.");
            page = await GetPage(number);
        }

        async Task<WebPage> GetPage(int n)
        {
            RequestCount++;
            var result = await fetch(n);
            if (result.Draws.Count == 0 || result.Draws.Any(d => !d.EsValido()))
                throw new IOException("No se encontraron resultados válidos; no se marcará el historial como completo.");
            if (!result.Draws.Select(d => d.Fecha).SequenceEqual(result.Draws.OrderByDescending(d => d.Fecha).Select(d => d.Fecha)))
                throw new IOException("El orden de los resultados cambió; se conservó el historial anterior.");
            return result;
        }
    }

    WebSyncState ReadState()
    {
        try
        {
            if (!File.Exists(statePath)) return new();
            var value = JsonConvert.DeserializeObject<WebSyncState>(File.ReadAllText(statePath));
            if (value?.Schema != 1 || value.Results == null || value.Pending == null || value.LastPage < 0 ||
                value.Results.Concat(value.Pending).Any(d => d == null || d.Numeros == null || !d.EsValido())) return new();
            return value;
        }
        catch (JsonException) { return new(); }
    }

    void Save(WebSyncState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(statePath)));
        string temporary = statePath + ".tmp";
        File.WriteAllText(temporary, JsonConvert.SerializeObject(state));
        File.Move(temporary, statePath, true);
    }
}
