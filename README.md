# Baloto Online para Android

Adaptación del commit `51a6a00` del repositorio `colombianitov2/Baloto-App-Online`.
El proyecto Windows y sus archivos originales se conservan intactos. `Sorteo.cs` y
`DatosBaloto.cs` se compilan directamente como archivos enlazados: no se duplican
ni se cambian los algoritmos, validaciones del modelo o formato del historial.

## Compilar

Requisitos: .NET 10, workload `android`, Android SDK 36 y JDK 21.

Desde la raíz del repositorio, en PowerShell:

```powershell
./Android/prepare-port.ps1
dotnet build Android/Baloto.Android.csproj -c Debug
./Android.Tests/run.ps1
dotnet publish Android/Baloto.Android.csproj -c Release
```

El APK ARM64 se genera en
`Android/bin/Release/net10.0-android/android-arm64/publish/com.colombianitov2.baloto-Signed.apk`.
El proyecto detecta las rutas de SDK/JDK de este equipo. En otro equipo pueden
indicarse con `-p:AndroidSdkDirectory=... -p:JavaSdkDirectory=...`.
Referencia: [dependencias oficiales de .NET para Android](https://learn.microsoft.com/es-es/dotnet/android/getting-started/installation/dependencies).

Para instalar o actualizar conservando los datos:

```powershell
adb install -r Android/bin/Release/net10.0-android/android-arm64/publish/com.colombianitov2.baloto-Signed.apk
```

El artefacto de uso personal utiliza la firma de desarrollo local que genera el
SDK de Android. Para mantener las actualizaciones sobre esta instalación se debe
conservar la misma clave. Una distribución pública requiere una clave de publicación propia.

## Adaptaciones de plataforma

- Windows Forms se sustituye por vistas Android, con los mismos botones, orden,
  colores, opciones y contenido. Las tablas distribuyen sus columnas centradas
  en todo el ancho, sin desplazamiento horizontal. Las listas cortas quedan fijas;
  solo se desplaza verticalmente el contenido que supera la pantalla.
- Los diálogos TXT/CSV utilizan el selector de archivos de Android. Los sorteos se
  guardan en el directorio privado de la aplicación, con el mismo JSON y TXT.
- `WebScraper.Android.cs` utiliza HTTP y conserva la tabla original, el filtro
  exclusivo de Baloto, los límites y los formatos de fechas. `WebSync.cs` guarda
  el progreso en `web-sync.json`, separado del historial.
- La primera sincronización completa establece la caché. Después se consulta la
  primera página y solo se continúa si hay novedades, hasta encontrar una página
  de sorteos conocidos. Se comparan fecha y números, no números de página.
- Si la primera carga se interrumpe y la portada no cambió, se continúa desde el
  punto guardado. Si la web cambió durante una carga inicial incompleta, se vuelve
  a validar para no omitir sorteos. No se declara completo un resultado parcial.
- La caché conserva el historial anterior para recuperar registros faltantes sin
  descargar otra vez las páginas antiguas. Las correcciones remotas en páginas
  históricas que no se consultan no pueden detectarse con la actualización rápida.
- Se conserva el icono original y se extraen los textos de ayuda y descripción;
  únicamente se sustituyen las referencias exclusivas a Windows/Chromium.
- El verificador conserva la comparación exacta y el orden de las balotas.
- El actualizador consulta el repositorio independiente
  `colombianitov2/Baloto-app-online-Android` y descarga APK.
- La versión 1.0.2 desactiva `AndroidEnableMarshalMethods` para corregir
  el cierre al iniciar la compilación Release en Android.
- Comentarios conserva la apertura del cliente de correo, porque el endpoint
  del proyecto original está vacío. El usuario completa el envío desde su correo.

## Pruebas

`Android.Tests/run.ps1` ejecuta 224 comprobaciones del código original, cambiando
solo la carpeta de almacenamiento en una copia temporal para no usar datos reales
de Windows. Incluye límites, duplicados, frecuencias, filtros, combinaciones,
generadores, persistencia y compatibilidad TXT. Además ejecuta 13 comprobaciones
de caché, cambios de paginación, interrupciones, recuperación y análisis HTML.

La verificación en equipo físico se documenta en el informe entregado junto al APK.
El paquete usa `com.colombianitov2.baloto`, Android mínimo 8 y arquitectura ARM64;
el dispositivo probado es un TECNO CK7n con Android 14.
