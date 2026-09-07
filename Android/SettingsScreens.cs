using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using Uri = Android.Net.Uri;

namespace BalotoAppOnline.Mobile;

public partial class MainActivity
{
    const string Repository = "https://github.com/colombianitov2/Baloto-app-online-Android";

    void Settings()
    {
        Begin("settings", "Configuración de la aplicación");
        var status = Label("Listo");
        Button update = null;
        update = AddButton("Actualizar", async () =>
        {
            update.Enabled = false;
            status.Text = "Buscando la versión más reciente...";
            try { await CheckUpdate(status); }
            catch (Exception ex) { status.Text = "No se pudo consultar GitHub."; Message("Actualización", "No se pudo completar la actualización: " + ex.Message); }
            finally { update.Enabled = true; }
        }, Green);
        AddButton("Ayuda / comentarios", Comments);
        AddButton("Abrir GitHub", () => OpenUrl("https://github.com/colombianitov2"));
        content.AddView(status, Full());
        AddButton("Cerrar", Home);
    }

    void OpenUrl(string url) => StartActivity(new Intent(Intent.ActionView, Uri.Parse(url)));

    async Task CheckUpdate(TextView status)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("BalotoAppOnline/1.0 Android");
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        using var response = await client.GetAsync("https://api.github.com/repos/colombianitov2/Baloto-app-online-Android/releases/latest");
        if (response.StatusCode == HttpStatusCode.NotFound) { status.Text = "El repositorio aún no tiene un release publicado."; Message("Actualización", status.Text); return; }
        response.EnsureSuccessStatusCode();
        var release = JObject.Parse(await response.Content.ReadAsStringAsync());
        var tag = ((string)release["tag_name"] ?? "").Trim().TrimStart('v', 'V');
        var asset = release["assets"]?.Children().FirstOrDefault(a => ((string)a["name"] ?? "").EndsWith(".apk", StringComparison.OrdinalIgnoreCase));
        if (asset == null)
        {
            status.Text = "El release más reciente no tiene un instalador para Android (.apk).";
            Message("Actualización", status.Text);
            return;
        }
        var current = PackageManager.GetPackageInfo(PackageName, 0).VersionName;
        if (!Version.TryParse(tag, out var available) || available <= Version.Parse(current))
        { status.Text = "El paquete más reciente ya está instalado."; Message("Actualización", status.Text); return; }
        var url = (string)asset["browser_download_url"];
        if (!System.Uri.TryCreate(url, UriKind.Absolute, out var download) || download.Scheme != "https" || download.Host != "github.com")
            throw new InvalidOperationException("El enlace del instalador no es válido.");
        status.Text = "Descargando versión " + tag + "...";
        var manager = (DownloadManager)GetSystemService(DownloadService);
        var request = new DownloadManager.Request(Uri.Parse(url));
        request.SetTitle("Baloto Online " + tag);
        request.SetMimeType("application/vnd.android.package-archive");
        request.SetNotificationVisibility(DownloadVisibility.VisibleNotifyCompleted);
        request.SetDestinationInExternalFilesDir(this, Android.OS.Environment.DirectoryDownloads, System.IO.Path.GetFileName((string)asset["name"]));
        long id = manager.Enqueue(request);
        for (int attempt = 0; attempt < 600; attempt++)
        {
            await Task.Delay(1000);
            using var query = new DownloadManager.Query().SetFilterById(id);
            using var cursor = manager.InvokeQuery(query);
            if (!cursor.MoveToFirst()) throw new IOException("No se encuentra la descarga.");
            int state = cursor.GetInt(cursor.GetColumnIndex(DownloadManager.ColumnStatus));
            if (state == (int)DownloadStatus.Failed) throw new IOException("No se pudo descargar el instalador.");
            if (state != (int)DownloadStatus.Successful) continue;
            status.Text = "Actualización descargada correctamente.";
            Confirm("Actualización descargada", $"Se descargó la versión {tag}.\n\n¿Quieres abrir el instalador ahora?", () =>
            {
                var intent = new Intent(Intent.ActionView);
                intent.SetDataAndType(manager.GetUriForDownloadedFile(id), "application/vnd.android.package-archive");
                intent.AddFlags(ActivityFlags.GrantReadUriPermission);
                StartActivity(intent);
            });
            return;
        }
        status.Text = "La descarga continúa. Puedes verla en las notificaciones de Android.";
    }

    void Comments()
    {
        Begin("comments", "Ayuda / comentarios");
        content.AddView(Label("Escribe tu comentario o solicitud de ayuda", 16, true));
        var comment = new EditText(this) { InputType = InputTypes.ClassText | InputTypes.TextFlagMultiLine, Gravity = GravityFlags.Top, ContentDescription = "Comentario" };
        comment.SetMinLines(6);
        content.AddView(comment, Full());
        content.AddView(Label("Tu contacto (opcional)"));
        var contact = new EditText(this) { InputType = InputTypes.ClassText | InputTypes.TextVariationEmailAddress, ContentDescription = "Tu contacto (opcional)" };
        contact.SetSingleLine(true);
        content.AddView(contact, Full());
        var state = Label("");
        content.AddView(state, Full());
        AddButton("Enviar", () =>
        {
            var text = comment.Text.Trim();
            if (text.Length < 5) { Message("Comentario", "Escribe un comentario un poco más completo."); return; }
            // The upstream endpoint is empty: preserve its mail-client fallback.
            var address = string.Concat("epernett", "1020", "@hotmail.com");
            var body = "Comentario:\r\n" + text + "\r\n\r\nContacto:\r\n" + contact.Text.Trim();
            var uri = "mailto:" + address + "?subject=" + System.Uri.EscapeDataString("Comentario BalotoAppOnline") + "&body=" + System.Uri.EscapeDataString(body);
            try { StartActivity(new Intent(Intent.ActionSendto, Uri.Parse(uri))); state.Text = "Se abrió tu correo para terminar el envío."; }
            catch (ActivityNotFoundException) { Message("Comentarios", "No hay una aplicación de correo disponible para terminar el envío."); }
        });
        AddButton("Cerrar", Settings);
    }

    string AssetText(string name) { using var reader = new StreamReader(Assets.Open(name)); return reader.ReadToEnd(); }

    void About()
    {
        Begin("about", "Acerca de / créditos");
        var body = new LinearLayout(this) { Orientation = Orientation.Vertical };
        Selector(new[] { "Créditos", "Ayuda" }, selected =>
        {
            body.RemoveAllViews();
            if (selected == 1) { body.AddView(Label(AssetText("help.txt")), Full()); return; }
            body.AddView(Label("Generador de Baloto Online", 21, true));
            body.AddView(Label($"Versión {PackageManager.GetPackageInfo(PackageName, 0).VersionName} · © 2026", 13));
            body.AddView(Label(AssetText("description.txt")), Full());
            body.AddView(Label("COLABORADORES", 13, true));
            foreach (var person in new[] {
                ("Ernesto Pernett Cuesta", "Idea, dirección y diseño · Ingeniero Mecánico"),
                ("Claude · Anthropic", "Desarrollo de software e interfaz"),
                ("DeepSeek (IA)", "Asistencia y revisión de algoritmos"),
                ("Gemini · Google", "Asesor de imagen y consultas variadas"),
                ("ChatGPT · OpenAI", "Adaptación a Android, interfaz y pruebas funcionales") })
            { body.AddView(Label(person.Item1, 16, true)); body.AddView(Label(person.Item2, 14)); }
        }, content);
        content.AddView(body, Full());
        AddButton("Cerrar", Home);
    }
}
