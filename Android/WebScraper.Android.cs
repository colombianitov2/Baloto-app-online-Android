using System.Globalization;
using System.Net.Http;
using HtmlAgilityPack;

namespace BalotoAppOnline;

public static class WebScraper
{
    public static event Action<string> OnProgreso;
    public static event Action<int, int> OnPaginaProcesada;
    public static int UltimasPaginasConsultadas { get; private set; }
    static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task<List<Sorteo>> ObtenerResultadosHistoricosAsync()
    {
        if (!await Gate.WaitAsync(0)) throw new InvalidOperationException("Ya hay una actualización en curso.");
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("BalotoAppOnline/1.0 Android");
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BalotoAppOnline", "web-sync.json");
            var sync = new WebSync(path);
            var all = await sync.RunAsync(async page =>
            {
                OnProgreso?.Invoke($"Consultando página {page}...");
                string url = "https://www.baloto.com/resultados" + (page == 1 ? "" : $"?page={page}");
                if (page > 1) await Task.Delay(1000);
                return ParsePage(await client.GetStringAsync(url));
            }, (page, total) => OnPaginaProcesada?.Invoke(page, total));
            UltimasPaginasConsultadas = sync.RequestCount;
            var existing = DatosBaloto.Sorteos.Select(WebSync.Key).ToHashSet();
            return all.Where(d => !existing.Contains(WebSync.Key(d))).ToList();
        }
        finally { Gate.Release(); }
    }

    // Same table, Baloto-only filter, dates and validation as the Windows parser.
    public static WebPage ParsePage(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var table = doc.DocumentNode.SelectSingleNode("//table[@id='results-table']")
            ?? throw new IOException("No se encontró la tabla de resultados en la web.");
        var draws = new List<Sorteo>();
        var rows = table.SelectNodes(".//tbody/tr");
        if (rows != null)
            foreach (var row in rows)
            {
                var cells = row.SelectNodes(".//td");
                if (cells == null || cells.Count < 3) continue;
                var image = cells[0].SelectSingleNode(".//img");
                if (image == null || !image.GetAttributeValue("src", "").Contains("baloto-kind.png")) continue;
                string[] formats = { "dd 'de' MMMM 'de' yyyy", "d 'de' MMMM 'de' yyyy", "dd/MM/yyyy", "dd-MM-yyyy" };
                if (!DateTime.TryParseExact(cells[1].InnerText.Trim(), formats, CultureInfo.GetCultureInfo("es-CO"), DateTimeStyles.None, out var date)) continue;
                var numbers = new List<int>();
                foreach (var part in cells[2].InnerText.Trim().Split(new[] { '-', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    if (int.TryParse(part, out int number)) numbers.Add(number);
                if (numbers.Count < 6) continue;
                var draw = new Sorteo { Fecha = date, Numeros = numbers.Take(6).ToArray() };
                if (draw.EsValido() && !draws.Contains(draw)) draws.Add(draw);
            }
        var next = doc.DocumentNode.SelectSingleNode("//a[contains(text(), 'Siguiente')]");
        return new WebPage(draws, !string.IsNullOrEmpty(next?.GetAttributeValue("href", "")));
    }
}
