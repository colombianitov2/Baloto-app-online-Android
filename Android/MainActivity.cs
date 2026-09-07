using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using System.Globalization;
using Color = Android.Graphics.Color;
using Uri = Android.Net.Uri;

namespace BalotoAppOnline.Mobile;

[Activity(Label = "Baloto Online", MainLauncher = true, Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden,
    WindowSoftInputMode = SoftInput.AdjustResize)]
public partial class MainActivity : Activity
{
    static readonly Color Blue = Color.Rgb(0, 69, 166), Yellow = Color.Rgb(253, 200, 47),
        Green = Color.Rgb(0, 150, 100), Background = Color.Rgb(240, 242, 245), Ink = Color.Rgb(35, 35, 35);
    LinearLayout root, content;
    string screen = "home";
    TextView progress, clock;
    Button updateWeb;
    bool updating;
    string updateStatus = "Listo";
    readonly Handler clockHandler = new(Looper.MainLooper);
    Action clockTick;
    EditText[] numberInputs;
    DateTime drawDate = DateTime.Today;
    string pendingExport;
    const int ImportRequest = 100, ExportRequest = 101;

    protected override void OnCreate(Bundle state)
    {
        base.OnCreate(state);
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("es-CO");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("es-CO");
        pendingExport = state?.GetString("pendingExport");
        WebScraper.OnProgreso += WebProgress;
        WebScraper.OnPaginaProcesada += WebPage;
        Home();
        clockTick = () => { if (clock != null) clock.Text = DateTime.Now.ToString("dddd, dd/MM/yyyy HH:mm"); clockHandler.PostDelayed(clockTick, 60000); };
        clockTick();
    }

    protected override void OnSaveInstanceState(Bundle state)
    {
        state.PutString("pendingExport", pendingExport);
        base.OnSaveInstanceState(state);
    }

    protected override void OnDestroy()
    {
        WebScraper.OnProgreso -= WebProgress;
        WebScraper.OnPaginaProcesada -= WebPage;
        if (clockTick != null) clockHandler.RemoveCallbacks(clockTick);
        base.OnDestroy();
    }

    int Dp(float n) => (int)(n * Resources.DisplayMetrics.Density + .5f);
    LinearLayout.LayoutParams Full(int height = -2) => new(-1, height);

    void Begin(string id, string title, bool scroll = true)
    {
        screen = id;
        numberInputs = null;
        root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        root.SetBackgroundColor(Background);
        root.SetOnApplyWindowInsetsListener(new InsetsListener());
        SetContentView(root);
        root.RequestApplyInsets();
        var header = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        header.SetGravity(GravityFlags.CenterVertical);
        if (id != "home")
        {
            var back = new Button(this) { Text = "‹", ContentDescription = "Volver", TextSize = 30 };
            back.SetTextColor(Blue);
            back.SetBackgroundColor(Color.Transparent);
            back.Click += (_, _) => Back();
            header.AddView(back, new LinearLayout.LayoutParams(Dp(52), Dp(52)));
        }
        var heading = Label(title, 20, true);
        heading.Gravity = GravityFlags.Center;
        heading.SetTextColor(Blue);
        heading.SetPadding(Dp(12), Dp(12), Dp(12), Dp(12));
        header.AddView(heading, new LinearLayout.LayoutParams(0, -2, 1));
        if (id != "home") header.AddView(new View(this), new LinearLayout.LayoutParams(Dp(52), 1));
        root.AddView(header, Full());
        content = new LinearLayout(this) { Orientation = Orientation.Vertical };
        content.SetPadding(Dp(18), Dp(4), Dp(18), Dp(16));
        content.FocusableInTouchMode = true;
        if (scroll)
        {
            var scroller = new ScrollView(this) { FillViewport = true, OverScrollMode = OverScrollMode.IfContentScrolls };
            scroller.AddView(content, new ScrollView.LayoutParams(-1, -2));
            root.AddView(scroller, new LinearLayout.LayoutParams(-1, 0, 1));
        }
        else root.AddView(content, new LinearLayout.LayoutParams(-1, 0, 1));
    }

    sealed class InsetsListener : Java.Lang.Object, View.IOnApplyWindowInsetsListener
    {
        public WindowInsets OnApplyWindowInsets(View view, WindowInsets insets)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var bars = insets.GetInsets(WindowInsets.Type.SystemBars() | WindowInsets.Type.DisplayCutout());
                view.SetPadding(bars.Left, bars.Top, bars.Right, bars.Bottom);
            }
            else view.SetPadding(insets.SystemWindowInsetLeft, insets.SystemWindowInsetTop, insets.SystemWindowInsetRight, insets.SystemWindowInsetBottom);
            return insets;
        }
    }

    TextView Label(string text, float size = 15, bool bold = false)
    {
        var v = new TextView(this) { Text = text, TextSize = size };
        v.SetTextColor(Ink);
        v.SetPadding(0, Dp(6), 0, Dp(6));
        if (bold) v.SetTypeface(null, TypefaceStyle.Bold);
        return v;
    }

    Button AddButton(string title, Action action, Color? color = null, LinearLayout parent = null)
    {
        var fill = color ?? Blue;
        var b = new Button(this) { Text = title, TextSize = 16 };
        b.SetAllCaps(false);
        b.SetTextColor(fill == Yellow ? Ink : Color.White);
        b.SetMinHeight(Dp(48));
        b.SetMinimumHeight(Dp(48));
        b.SetPadding(Dp(12), Dp(8), Dp(12), Dp(8));
        var shape = new GradientDrawable();
        shape.SetColor(fill);
        shape.SetCornerRadius(Dp(14));
        b.Background = new RippleDrawable(Android.Content.Res.ColorStateList.ValueOf(Color.Argb(55, 255, 255, 255)), shape, null);
        var lp = Full(); lp.TopMargin = Dp(4); lp.BottomMargin = Dp(4);
        (parent ?? content).AddView(b, lp);
        b.Click += (_, _) => { try { action(); } catch (Exception ex) { Message("Error", ex.Message); } };
        return b;
    }

    void Message(string title, string text) => new AlertDialog.Builder(this).SetTitle(title).SetMessage(text).SetPositiveButton("Aceptar", (_, _) => { }).Show();
    void Confirm(string title, string text, Action yes) => new AlertDialog.Builder(this).SetTitle(title).SetMessage(text).SetNegativeButton("No", (_, _) => { }).SetPositiveButton("Sí", (_, _) => { try { yes(); } catch (Exception ex) { Message("Error", ex.Message); } }).Show();
    void HideKeyboard() => ((InputMethodManager)GetSystemService(InputMethodService)).HideSoftInputFromWindow(root.WindowToken, HideSoftInputFlags.None);
    void Back() { HideKeyboard(); if (screen == "comments") Settings(); else if (screen == "home") MoveTaskToBack(true); else Home(); }
    public override void OnBackPressed() => Back();

    void Home()
    {
        Begin("home", "Generador de Baloto Online");
        clock = Label(DateTime.Now.ToString("dddd, dd/MM/yyyy HH:mm"), 14, true);
        clock.Gravity = GravityFlags.Center;
        content.AddView(clock, Full());
        progress = Label(updateStatus, 13);
        progress.Gravity = GravityFlags.Center;
        content.AddView(progress, Full());
        AddButton("Automático", () => Suggest(0), Yellow);
        AddButton("Resultado por frecuencia", () => Suggest(1), Yellow);
        AddButton("Sugerencia Gaussiana", () => Suggest(2), Yellow);
        AddButton("Ingresar datos", () => Numbers(false));
        AddButton("Exportar datos (.txt)", ExportHistory);
        AddButton("Importar datos (.txt)", ImportHistory);
        AddButton("Historial", History);
        AddButton("Verificador de tiquetes", () => Numbers(true));
        AddButton("Tabla de análisis", Statistics);
        updateWeb = AddButton(updating ? "Actualizando..." : "Actualizar desde web", async () => await UpdateFromWeb(), Green);
        updateWeb.Enabled = !updating;
        AddButton("Configuración", Settings);
        AddButton("Acerca de / créditos", About);
    }

    void Suggest(int mode)
    {
        if (DatosBaloto.Sorteos.Count < 5) { Message("Datos insuficientes", "Mínimo 5 sorteos para generar sugerencia."); return; }
        var n = mode == 0 ? DatosBaloto.SugerenciaAutomatica(new Random()) : mode == 1 ? DatosBaloto.SugerenciaFrecuentista() : DatosBaloto.SugerenciaGaussiana(new Random());
        Message(mode == 0 ? "Sugerencia Automática (Inteligente)" : mode == 1 ? "Resultado por frecuencia" : "Sugerencia Gaussiana",
            $"Balotas: {n[0]:00} {n[1]:00} {n[2]:00} {n[3]:00} {n[4]:00}\nSúper Balota: {n[5]:00}");
    }

    void Numbers(bool verify)
    {
        Begin(verify ? "verify" : "entry", verify ? "Verificador de tiquetes" : "Ingresar sorteo ganador");
        numberInputs = new EditText[6];
        for (int i = 0; i < 6; i++)
        {
            var row = new LinearLayout(this) { Orientation = Orientation.Horizontal };
            row.SetGravity(GravityFlags.CenterVertical);
            string name = i < 5 ? $"Balota {i + 1} (1-43):" : "Súper Balota (1-16):";
            row.AddView(Label(name), new LinearLayout.LayoutParams(0, -2, 1));
            var input = new EditText(this) { InputType = InputTypes.ClassNumber, Gravity = GravityFlags.Center, TextSize = 20, ContentDescription = name };
            input.SetSingleLine(true);
            input.SetFilters(new IInputFilter[] { new InputFilterLengthFilter(2) });
            input.SetMinHeight(Dp(52));
            numberInputs[i] = input;
            row.AddView(input, new LinearLayout.LayoutParams(Dp(92), -2));
            content.AddView(row, Full());
        }
        if (!verify)
        {
            drawDate = DateTime.Today;
            Button date = null;
            date = AddButton($"Fecha: {drawDate:dd/MM/yyyy}", () => new DatePickerDialog(this, (_, e) => { drawDate = e.Date.Date; date.Text = $"Fecha: {drawDate:dd/MM/yyyy}"; }, drawDate.Year, drawDate.Month - 1, drawDate.Day).Show());
            AddButton("Guardar sorteo", () =>
            {
                var nums = ReadNumbers(false);
                if (nums == null) return;
                if (DatosBaloto.AgregarSorteo(new Sorteo { Fecha = drawDate.Date, Numeros = nums }, out var error))
                { HideKeyboard(); Home(); Message("Éxito", "Sorteo guardado correctamente."); }
                else Message("Sorteo duplicado", error);
            });
        }
        else
        {
            var result = Label("");
            result.SetTextIsSelectable(true);
            AddButton("Verificar combinación", () =>
            {
                result.Text = "";
                var nums = ReadNumbers(true);
                if (nums == null) return;
                HideKeyboard();
                var matches = DatosBaloto.Sorteos.Where(s => s.Numeros.SequenceEqual(nums)).OrderBy(s => s.Fecha).ToList();
                result.SetTextColor(matches.Count == 0 ? Color.Rgb(190, 30, 30) : Green);
                result.Text = matches.Count == 0
                    ? $"❌ NO SE HA ENCONTRADO\nLa combinación {string.Join(", ", nums.Take(5))} - {nums[5]} no ha aparecido en ningún sorteo.\nSi acabas de agregar sorteos recientes, asegúrate de haber actualizado desde la web."
                    : $"✅ ¡ENCONTRADO! La combinación ha aparecido {matches.Count} vez/veces.\n\n" + string.Join("\n\n", matches.Select(s => $"📅 {s.Fecha:dddd, dd/MM/yyyy}\nNúmeros: {string.Join(" - ", s.Numeros.Select(n => n.ToString("00")))}"));
            });
            content.AddView(result, Full());
        }
    }

    int[] ReadNumbers(bool verify)
    {
        var nums = new int[6];
        for (int i = 0; i < 5; i++)
            if (!int.TryParse(numberInputs[i].Text, out nums[i]) || nums[i] < 1 || nums[i] > 43)
            { Message(verify ? "Error de entrada" : "Error de validación", $"Balota {i + 1} debe ser {(verify ? "un número " : "")}entre 1 y 43."); return null; }
        if (nums.Take(5).Distinct().Count() != 5)
        { Message("Error de validación", "Las cinco balotas deben ser distintas."); return null; }
        if (!int.TryParse(numberInputs[5].Text, out nums[5]) || nums[5] < 1 || nums[5] > 16)
        { Message("Error de validación", "Súper balota debe ser entre 1 y 16."); return null; }
        return nums;
    }

    void History()
    {
        Begin("history", "Historial de sorteos", false);
        var draws = DatosBaloto.Sorteos.OrderByDescending(s => s.Fecha).ToList();
        var weights = new[] { 2.2f, 1f, 1f, 1f, 1f, 1f, 1f };
        content.AddView(CenteredRow(new[] { "Fecha", "Balota\n1", "Balota\n2", "Balota\n3", "Balota\n4", "Balota\n5", "Súper" }, weights, true), Full());
        var list = new ListView(this) { ChoiceMode = ChoiceMode.Single, OverScrollMode = OverScrollMode.IfContentScrolls };
        list.Adapter = new HistoryAdapter(this, draws);
        content.AddView(list, new LinearLayout.LayoutParams(-1, 0, 1));
        AddButton("Eliminar sorteo seleccionado", () =>
        {
            int index = list.CheckedItemPosition;
            if (index < 0 || index >= draws.Count) { Message("Historial", "Seleccione un sorteo."); return; }
            var draw = draws[index];
            Confirm("Confirmar", $"¿Eliminar sorteo del {draw.Fecha:dd/MM/yyyy} con números {string.Join(",", draw.Numeros)}?", () =>
            { DatosBaloto.Sorteos.Remove(draw); DatosBaloto.Guardar(); History(); Message("Historial", "Eliminado."); });
        }, Color.Rgb(231, 76, 60));
    }

    LinearLayout CenteredRow(string[] cells, float[] weights, bool header = false)
    {
        var row = new LinearLayout(this) { Orientation = Orientation.Horizontal, BaselineAligned = false };
        row.SetMinimumHeight(Dp(44));
        if (header) row.SetBackgroundColor(Blue);
        for (int i = 0; i < cells.Length; i++)
        {
            var text = cells[i].Replace(",", ",\u200b");
            var cell = Label(text, header ? 11 : 13, header || cells[0] == "Más repetido");
            cell.Gravity = GravityFlags.Center;
            cell.SetPadding(Dp(2), Dp(10), Dp(2), Dp(10));
            if (header) cell.SetTextColor(Color.White);
            row.AddView(cell, new LinearLayout.LayoutParams(0, -1, weights[i]));
        }
        return row;
    }

    sealed class HistoryAdapter(MainActivity activity, List<Sorteo> draws) : BaseAdapter<Sorteo>
    {
        public override int Count => draws.Count;
        public override Sorteo this[int position] => draws[position];
        public override long GetItemId(int position) => position;
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var draw = draws[position];
            var cells = new[] { draw.Fecha.ToString("dd/MM/yyyy") }.Concat(draw.Numeros.Select(n => n.ToString("00"))).ToArray();
            var row = activity.CenteredRow(cells, new[] { 2.2f, 1f, 1f, 1f, 1f, 1f, 1f });
            var colors = new StateListDrawable();
            colors.AddState(new[] { Android.Resource.Attribute.StateActivated }, new ColorDrawable(Color.Rgb(202, 224, 255)));
            colors.AddState(Array.Empty<int>(), new ColorDrawable(position % 2 == 0 ? Color.White : Background));
            row.Background = colors;
            return row;
        }
    }

    void ImportHistory()
    {
        var intent = new Intent(Intent.ActionOpenDocument);
        intent.AddCategory(Intent.CategoryOpenable);
        intent.SetType("text/*");
        StartActivityForResult(intent, ImportRequest);
    }

    void ExportHistory()
    {
        var temp = System.IO.Path.Combine(CacheDir.AbsolutePath, "historial-export.txt");
        DatosBaloto.ExportarTxt(temp);
        SaveDocument(temp, "historial.txt", "text/plain");
    }

    void SaveDocument(string path, string name, string mime)
    {
        pendingExport = path;
        var intent = new Intent(Intent.ActionCreateDocument);
        intent.AddCategory(Intent.CategoryOpenable);
        intent.SetType(mime);
        intent.PutExtra(Intent.ExtraTitle, name);
        StartActivityForResult(intent, ExportRequest);
    }

    protected override async void OnActivityResult(int requestCode, Result resultCode, Intent data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (resultCode != Result.Ok || data?.Data == null) return;
        try
        {
            if (requestCode == ImportRequest)
            {
                var path = System.IO.Path.Combine(CacheDir.AbsolutePath, "historial-import.txt");
                using (var input = ContentResolver.OpenInputStream(data.Data))
                using (var output = File.Create(path)) await input.CopyToAsync(output);
                DatosBaloto.ImportarTxt(path);
                Message("Éxito", $"Importación completada. Total sorteos: {DatosBaloto.Sorteos.Count}");
            }
            if (requestCode == ExportRequest && pendingExport != null)
            {
                using (var input = File.OpenRead(pendingExport))
                using (var output = ContentResolver.OpenOutputStream(data.Data, "wt")) await input.CopyToAsync(output);
                pendingExport = null;
                Message("Éxito", "Exportado correctamente.");
            }
        }
        catch (Exception ex) { Message("Error", ex.Message); }
    }

    void WebProgress(string text) => RunOnUiThread(() => { updateStatus = text; if (screen == "home" && progress != null) progress.Text = text; });
    void WebPage(int page, int total) => WebProgress($"Página {page} - Sorteos acumulados: {total}");

    async Task UpdateFromWeb()
    {
        if (updating) return;
        updating = true;
        updateWeb.Enabled = false; updateWeb.Text = "Actualizando...";
        WebProgress("Iniciando...");
        try
        {
            var results = await WebScraper.ObtenerResultadosHistoricosAsync();
            string checkedPages = $"Páginas consultadas: {WebScraper.UltimasPaginasConsultadas}.";
            if (results.Count == 0) { WebProgress("Historial al día. " + checkedPages); Message("Actualización", "El historial ya está actualizado.\n" + checkedPages); return; }
            WebProgress("Guardando en la base de datos...");
            int added = DatosBaloto.AgregarSorteosMultiples(results, out var errors);
            var message = $"Se encontraron {results.Count} sorteos.\nSe agregaron {added} nuevos.\n{checkedPages}";
            if (errors.Count > 0) message += $"\n\nErrores ({errors.Count}):\n" + string.Join("\n", errors.Take(5));
            WebProgress($"Finalizado. Agregados: {added}. {checkedPages}");
            Message("Actualización automática", message);
        }
        catch (Exception ex) { WebProgress("Error en la actualización."); Message("Error", $"Error: {ex.Message}"); }
        finally { updating = false; if (screen == "home") { updateWeb.Enabled = true; updateWeb.Text = "Actualizar desde web"; } }
    }
}
