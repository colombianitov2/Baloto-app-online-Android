using Android.Graphics;
using Android.Views;
using Android.Widget;
using System.Text;

namespace BalotoAppOnline.Mobile;

public partial class MainActivity
{
    readonly string[] analysisNames = { "Frecuencia por posición", "Frecuencia mensual", "Frecuencia por año", "Combinaciones más repetidas" };
    List<string[]> analysisRows = new();
    string[] analysisHeaders = Array.Empty<string>();
    LinearLayout analysisBody, periodBody;
    int analysisMode;
    string period;

    Spinner Selector(string[] values, Action<int> changed, LinearLayout parent)
    {
        var spinner = new Spinner(this) { ContentDescription = "Seleccionar " + (parent == periodBody ? "periodo" : "tabla") };
        var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, values);
        adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        spinner.Adapter = adapter;
        parent.AddView(spinner, Full(Dp(52)));
        spinner.ItemSelected += (_, e) => changed(e.Position);
        return spinner;
    }

    void Statistics()
    {
        Begin("analysis", "Tabla de análisis");
        AddButton("Exportar a CSV", ExportAnalysis);
        periodBody = new LinearLayout(this) { Orientation = Orientation.Vertical };
        analysisBody = new LinearLayout(this) { Orientation = Orientation.Vertical };
        Selector(analysisNames, mode =>
        {
            analysisMode = mode;
            periodBody.RemoveAllViews();
            if (mode == 1)
            {
                var months = DatosBaloto.Sorteos.Select(s => s.Fecha.ToString("MMyyyy")).Distinct().OrderBy(s => s).ToArray();
                if (months.Length == 0) months = new[] { DateTime.Now.ToString("MMyyyy") };
                period = months[0];
                Selector(months, i => { period = months[i]; RenderAnalysis(); }, periodBody);
            }
            else if (mode == 2)
            {
                var years = DatosBaloto.Sorteos.Select(s => s.Fecha.Year).Distinct().OrderBy(s => s).Select(y => y.ToString()).ToArray();
                if (years.Length == 0) years = new[] { DateTime.Now.Year.ToString() };
                period = years[0];
                Selector(years, i => { period = years[i]; RenderAnalysis(); }, periodBody);
            }
            RenderAnalysis();
        }, content);
        content.AddView(periodBody, Full());
        content.AddView(analysisBody, Full());
    }

    void RenderAnalysis()
    {
        analysisBody.RemoveAllViews();
        analysisRows = new();
        if (analysisMode == 3)
        {
            analysisHeaders = new[] { "Combinación (5 balotas ordenadas)", "Veces repetida" };
            analysisRows = DatosBaloto.CombinacionesMasFrecuentesGeneral().Where(c => c.frecuencia > 1)
                .Select(c => new[] { c.combinacion, c.frecuencia.ToString() }).ToList();
            if (analysisRows.Count == 0) { analysisBody.AddView(Label("No hay combinaciones repetidas en el historial.")); return; }
        }
        else
        {
            var frequencies = analysisMode == 1 ? DatosBaloto.ObtenerFrecuenciasPorMes(period)
                : analysisMode == 2 ? DatosBaloto.ObtenerFrecuenciasPorAnio(int.Parse(period)) : DatosBaloto.ObtenerFrecuenciasPorPosicion();
            analysisHeaders = new[] { "Número", "Balota 1", "Balota 2", "Balota 3", "Balota 4", "Balota 5", "Súper Balota" };
            for (int n = 1; n <= 43; n++)
                analysisRows.Add(new[] { n.ToString() }.Concat(Enumerable.Range(0, 6).Select(p => frequencies[p].TryGetValue(n, out var count) ? count.ToString() : "0")).ToArray());
            analysisRows.Add(new[] { "Más repetido" }.Concat(Enumerable.Range(0, 6).Select(p => frequencies[p].Count == 0 ? "---"
                : string.Join(",", frequencies[p].Where(kv => kv.Value == frequencies[p].Values.Max()).Select(kv => kv.Key).OrderBy(n => n)))).ToArray());
        }
        DrawTable();
    }

    void DrawTable()
    {
        analysisBody.RemoveAllViews();
        var table = new LinearLayout(this) { Orientation = Orientation.Vertical };
        void Row(string[] cells, int index)
        {
            var display = index < 0 && analysisMode != 3
                ? new[] { "Número", "Balota\n1", "Balota\n2", "Balota\n3", "Balota\n4", "Balota\n5", "Súper\nBalota" }
                : cells;
            var weights = analysisMode == 3 ? new[] { 2f, 1f } : new[] { 1.4f, 1f, 1f, 1f, 1f, 1f, 1.2f };
            var row = CenteredRow(display, weights, index < 0);
            row.SetBackgroundColor(index < 0 ? Blue : index % 2 == 0 ? Color.White : Color.Rgb(232, 237, 244));
            for (int column = 0; column < cells.Length; column++)
            {
                var cell = row.GetChildAt(column);
                if (index < 0)
                {
                    int sortColumn = column; bool descending = false;
                    cell.Click += (_, _) =>
                    {
                        var summary = analysisRows.FirstOrDefault(r => r[0] == "Más repetido");
                        var rows = analysisRows.Where(r => r[0] != "Más repetido");
                        analysisRows = (descending ? rows.OrderByDescending(r => SortValue(r[sortColumn])) : rows.OrderBy(r => SortValue(r[sortColumn]))).ToList();
                        if (summary != null) analysisRows.Add(summary);
                        descending = !descending;
                        while (table.ChildCount > 1) table.RemoveViewAt(1);
                        for (int i = 0; i < analysisRows.Count; i++) Row(analysisRows[i], i);
                    };
                }
            }
            table.AddView(row);
        }
        Row(analysisHeaders, -1);
        for (int i = 0; i < analysisRows.Count; i++) Row(analysisRows[i], i);
        analysisBody.AddView(table, Full());
    }

    static string SortValue(string v) => int.TryParse(v, out var n) ? n.ToString("D10") : v;

    void ExportAnalysis()
    {
        if (analysisRows.Count == 0) { Message("Información", "No hay datos para exportar."); return; }
        static string Csv(string value) => value.Contains(',') || value.Contains('"') ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        var path = System.IO.Path.Combine(CacheDir.AbsolutePath, "analysis-export.csv");
        using (var writer = new StreamWriter(path, false, new UTF8Encoding(true)))
        {
            writer.WriteLine(string.Join(",", analysisHeaders.Select(Csv)));
            foreach (var row in analysisRows) writer.WriteLine(string.Join(",", row.Select(Csv)));
        }
        SaveDocument(path, $"Estadisticas_{DateTime.Now:yyyyMMdd_HHmmss}.csv", "text/csv");
    }
}
