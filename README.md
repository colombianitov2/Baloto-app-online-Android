# Baloto Online para Android

Aplicación móvil para consultar y analizar el historial de sorteos de Baloto.
Permite generar combinaciones de referencia usando datos históricos, revisar
frecuencias y comparar un tiquete con resultados guardados.

## Descargar e instalar

La aplicación se distribuye como un único archivo `.apk`. No necesita instalar
otro programa, complemento, runtime, navegador ni archivo adicional: todo lo
necesario para Android está incluido dentro del APK.

[Descargar Baloto Online Android 1.0.2](https://github.com/colombianitov2/Baloto-app-online-Android/releases/download/v1.0.2/Baloto-Android-v1.0.2.apk)

Abre el archivo descargado en el teléfono y autoriza la instalación desde esa
fuente cuando Android lo solicite. Requiere Android 8 o superior y está
preparada para teléfonos ARM64.

> Android utiliza archivos `.apk`; un `.exe` corresponde a Windows y no puede
> instalarse directamente en un teléfono Android. Para el teléfono, el único
> archivo necesario es el APK del enlace anterior.

## ¿Para qué sirve?

Baloto Online conserva y analiza resultados históricos para estudiar sus patrones
estadísticos. Puede:

- Generar una combinación automática.
- Proponer números por frecuencia o distribución gaussiana.
- Ingresar, importar y exportar sorteos en TXT.
- Consultar el historial y analizar frecuencias por posición, mes y año.
- Verificar un tiquete contra el historial guardado.
- Actualizar resultados desde la web oficial de Baloto.
- Descargar solo páginas nuevas después de la primera carga y conservar el
  progreso si una actualización se interrumpe.

Las sugerencias son estadísticas y no predicen ni garantizan el resultado. Cada
sorteo es aleatorio.

## Uso básico

1. Abre la aplicación.
2. Pulsa `Actualizar desde web` para cargar o completar el historial.
3. Consulta `Historial` y `Tabla de análisis`.
4. Usa uno de los generadores para obtener una combinación de referencia.
5. Usa `Verificador de tiquetes` para comparar tus números.

El historial se guarda en el almacenamiento privado de la aplicación. Al
desinstalarla, Android elimina esos datos; exporta el historial a TXT para
conservar una copia.

## Privacidad y conexión

No requiere una cuenta. Solo necesita Internet para actualizar resultados o
consultar nuevas versiones. El historial permanece en el teléfono, salvo que el
usuario lo exporte.

## Código y versiones

- [Repositorio Android](https://github.com/colombianitov2/Baloto-app-online-Android)
- [Release v1.0.2](https://github.com/colombianitov2/Baloto-app-online-Android/releases/tag/v1.0.2)
- Paquete: `com.colombianitov2.baloto`
- Versión: `1.0.2`

El proyecto Windows original se conserva en su repositorio independiente. Este
repositorio contiene la adaptación Android y el APK publicado.
