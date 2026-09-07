using BalotoAppOnline;

Environment.SetEnvironmentVariable("BALOTO_TEST_DATA", Path.GetFullPath(args[0]));
Directory.CreateDirectory(args[0]);
int passed = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL: " + name); passed++; }
Sorteo Draw(int[] nums, int day = 1, int month = 1, int year = 2000) => new() { Fecha = new DateTime(year, month, day), Numeros = nums };
var first = Draw(new[] { 1, 2, 3, 4, 5, 6 });
Check(first.EsValido(), "valid draw");
Check(!Draw(new[] { 1, 1, 3, 4, 5, 6 }).EsValido(), "duplicate main ball");
Check(!Draw(new[] { 0, 2, 3, 4, 5, 6 }).EsValido(), "lower main bound");
Check(!Draw(new[] { 1, 2, 3, 4, 44, 6 }).EsValido(), "upper main bound");
Check(!Draw(new[] { 1, 2, 3, 4, 5, 17 }).EsValido(), "upper super bound");
Check(!Draw(new[] { 1, 2, 3, 4, 5, 0 }).EsValido(), "lower super bound");
Check(DatosBaloto.Sorteos.Count == 0, "isolated empty storage");
Check(DatosBaloto.AgregarSorteo(first, out _), "save");
Check(!DatosBaloto.AgregarSorteo(first, out var duplicate) && duplicate.Contains("ya se encuentra"), "duplicate rejected");
var more = new List<Sorteo> { Draw(new[] { 1, 2, 3, 4, 5, 6 }, 2), Draw(new[] { 6, 7, 8, 9, 10, 11 }, 3), Draw(new[] { 11, 12, 13, 14, 15, 16 }, 1, 2), Draw(new[] { 21, 22, 23, 24, 25, 1 }, 1, 1, 2001) };
Check(DatosBaloto.AgregarSorteosMultiples(more, out var errors) == 4 && errors.Count == 0, "batch save");
DatosBaloto.Cargar();
Check(DatosBaloto.Sorteos.Count == 5, "persistence reload");
Check(DatosBaloto.ObtenerFrecuenciasPorPosicion()[0][1] == 2, "global frequency");
Check(DatosBaloto.ObtenerFrecuenciasPorMes("012000")[0].Values.Sum() == 3, "month filter");
Check(DatosBaloto.ObtenerFrecuenciasPorAnio(2000)[0].Values.Sum() == 4, "year filter");
Check(DatosBaloto.ObtenerFrecuenciasPorAnio(1999)[0].Count == 0, "empty period");
Check(DatosBaloto.SugerenciaFrecuentista().SequenceEqual(new[] { 1, 2, 3, 4, 5, 6 }), "frequency expected result");
Check(DatosBaloto.CombinacionesMasFrecuentesGeneral()[0] == ("1,2,3,4,5", 2), "repeated combination");
Check(DatosBaloto.CombinacionesMasFrecuentesPorMes("012000")[0].frecuencia == 2, "monthly repeated combination");
for (int seed = 0; seed < 100; seed++)
{
    Check(Draw(DatosBaloto.SugerenciaGaussiana(new Random(seed))).EsValido(), "weighted valid " + seed);
    var automatic = DatosBaloto.SugerenciaAutomatica(new Random(seed));
    Check(Draw(automatic).EsValido() && automatic.Take(5).SequenceEqual(automatic.Take(5).OrderBy(n => n)), "automatic valid/sorted " + seed);
}
var export = Path.Combine(args[0], "roundtrip.txt");
DatosBaloto.ExportarTxt(export);
Check(File.ReadAllLines(export).Length == 5 && File.ReadAllLines(export)[0] == "01/01/2000 01 02 03 04 05 06", "Windows TXT format");
DatosBaloto.Sorteos.Clear();
DatosBaloto.Guardar();
DatosBaloto.ImportarTxt(export);
Check(DatosBaloto.Sorteos.Count == 5, "TXT roundtrip");
DatosBaloto.ImportarTxt(export);
Check(DatosBaloto.Sorteos.Count == 5, "existing import duplicates skipped");
Check(DatosBaloto.Sorteos.Count(s => s.Numeros.SequenceEqual(first.Numeros)) == 2, "exact ticket matches");
Check(!DatosBaloto.Sorteos.Any(s => s.Numeros.SequenceEqual(new[] { 5, 4, 3, 2, 1, 6 })), "ticket order preserved");
DatosBaloto.Sorteos.RemoveAt(0);
DatosBaloto.Guardar(); DatosBaloto.Cargar();
Check(DatosBaloto.Sorteos.Count == 4, "delete persisted");
Console.WriteLine($"PASS: {passed} checks; original calculations, filters, validation, persistence and TXT compatibility.");
await WebSyncChecks.Run(args[0]);
