# MoTV Renamer

Creado por [teleonline](https://github.com/teleonline) · Código fuente: https://github.com/teleonline/MoTVRenamer

**Español** · [English](#english)

> **Renombrador gratuito de películas y series. Solo para uso personal, NO comercial.** No descarga ni distribuye contenido protegido: solo renombra archivos que ya tienes, con datos de bases de datos públicas. Sin garantía; los autores no se hacen responsables del uso del programa. Ver [LICENSE](LICENSE).

Renombra películas y series con datos de **TMDb, IMDb, OMDb y TheTVDB**. Aplicación de Windows de un solo archivo, sin instalar nada.

Desarrollado con la ayuda de Claude (Anthropic).

## Uso

1. **Añadir**: elige una carpeta o archivos sueltos (también puedes arrastrarlos).
2. **Buscar**: elige el idioma de tus títulos y pulsa «Buscar nombres para mis archivos».
3. **Renombrar**: revisa los nombres nuevos y pulsa «Renombrar». Se puede deshacer.

Interfaz en inglés (por defecto) y español, modo oscuro (por defecto) y claro, más de 45 idiomas de búsqueda (incluidos catalán y valenciano), episodios dobles, renombrado por tandas con progreso y cancelación, y contador de llamadas a las APIs con límite diario de OMDb.

## Compilar

Necesitas Windows 10/11 (trae el compilador de .NET Framework 4). No hace falta instalar nada más:

```
build.bat
```

Genera `MoTVRenamer.exe` (con el icono `MoTVRenamer.ico`) junto al código. El código fuente es C# 5 (`MoTVRenamer.cs`), un solo archivo.

## Claves de API (gratuitas)

Se pegan en **Ajustes**. Sin ninguna clave funciona con IMDb (solo búsqueda básica de películas).

| Servicio | Dónde conseguirla |
|---|---|
| TMDb (recomendada) | https://www.themoviedb.org/settings/api |
| OMDb | https://www.omdbapi.com/apikey.aspx |
| TheTVDB | https://thetvdb.com/api-information |

Las claves se guardan en `%APPDATA%\MoTVRenamer\config.json` en tu equipo. No se envían a ningún sitio salvo a cada servicio.

## Cómo nombrar tus archivos

- Películas: `Título (Año).mkv`, o con ID de IMDb: `Matrix (1999) {imdb-tt0133093}.mkv`
- Series: `Serie (Año de estreno)\Season 01\S01E02 - Título.mkv`
- Elige el idioma correcto en «Idioma de los títulos». La guía completa está dentro del programa, en «Instrucciones».

IMDb se consulta mediante un endpoint de sugerencias no oficial; puede cambiar o dejar de funcionar sin aviso.

## Licencia

Gratuito, solo para uso personal y no comercial (ver [LICENSE](LICENSE)). No es una licencia de código abierto aprobada por la OSI: el código está disponible, pero no se permite su uso comercial. El programa no descarga ni distribuye contenido protegido, se ofrece «tal cual» y los autores no se hacen responsables de su uso. Este producto usa las APIs de TMDb, OMDb y TheTVDB pero no está avalado ni certificado por ellas.

---

## English

Created by [teleonline](https://github.com/teleonline) · Source: https://github.com/teleonline/MoTVRenamer

> **Free renamer for movies and TV series. For personal, NON-commercial use only.** It does not download or distribute copyrighted content: it only renames files you already have, using data from public databases. No warranty; the authors are not responsible for how the program is used. See [LICENSE](LICENSE).

Rename movies and TV series using data from **TMDb, IMDb, OMDb and TheTVDB**. Single-file Windows app, nothing to install.

Built with the help of Claude (Anthropic).

### Usage

1. **Add**: choose a folder or loose files (you can also drag them in).
2. **Search**: pick the language of your titles and click “Find names for my files”.
3. **Rename**: check the new names and click “Rename”. It can be undone.

English (default) and Spanish interface, dark (default) and light mode, 45+ search languages (including Catalan and Valencian), double episodes, batch renaming with progress and cancel, and an API call counter with a daily OMDb limit.

### Build

Requires Windows 10/11 (it ships with the .NET Framework 4 compiler). Nothing else to install:

```
build.bat
```

This produces `MoTVRenamer.exe` (using `MoTVRenamer.ico` as its icon) next to the source. The source is C# 5 (`MoTVRenamer.cs`), a single file.

### API keys (free)

Paste them in **Settings**. With no key it works with IMDb only (basic movie search).

| Service | Where to get it |
|---|---|
| TMDb (recommended) | https://www.themoviedb.org/settings/api |
| OMDb | https://www.omdbapi.com/apikey.aspx |
| TheTVDB | https://thetvdb.com/api-information |

Keys are stored in `%APPDATA%\MoTVRenamer\config.json` on your machine and sent only to each service.

### Naming your files

- Movies: `Title (Year).mkv`, or with the IMDb ID: `Matrix (1999) {imdb-tt0133093}.mkv`
- Series: `Series (First-air year)\Season 01\S01E02 - Title.mkv`
- Choose the right language in “Title language”. The full guide is inside the program, under “Instructions”.

IMDb is queried through an unofficial suggestions endpoint that may change or stop working without notice.

### License

Free for personal, non-commercial use only (see [LICENSE](LICENSE)). It is not an OSI-approved open source license: the source is available, but commercial use is not allowed. The program does not download or distribute copyrighted content, is provided “as is”, and the authors are not responsible for how it is used. This product uses the TMDb, OMDb and TheTVDB APIs but is not endorsed or certified by them.
