// MoTV Renamer - Renombrador GRATUITO de peliculas y series para Windows (C# / WinForms). Solo uso personal, no comercial.
// Autor: teleonline - https://github.com/teleonline/MoTVRenamer - Licencia: uso personal no comercial (ver LICENSE)
// Fuentes: TMDb, IMDb (busqueda sin clave) y OMDb.
// Compatible con el compilador que trae Windows (C# 5, .NET Framework 4.x). No necesita instalar nada.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace MoTVRenamer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try { ServicePointManager.SecurityProtocol = (SecurityProtocolType)(3072 | 768 | 192); } catch (Exception) { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += delegate(object so, ThreadExceptionEventArgs ea)
            {
                MessageBox.Show(ea.Exception.ToString(), "MoTV Renamer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            try { Application.Run(new MainForm()); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "MoTV Renamer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ------------------------------------------------------------------ estilo
    static class Ui
    {
        public static bool IsDark;
        public static Color Bg, Card, Border, Text, Muted, Accent, AccentHover, AccentOff, AccentText;
        public static Color Dark, DarkHover, Select, RowAlt, HeadBg, Input, Hover, Press, OffText;
        public static readonly Color Green = Color.FromArgb(22, 163, 74);
        public static readonly Color Amber = Color.FromArgb(217, 119, 6);
        public static readonly Color Red = Color.FromArgb(220, 38, 38);

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern int SetWindowTheme(IntPtr hwnd, string subApp, string subIdList);

        static Ui() { SetDark(false); }

        // "dark", "light" o "auto" (sigue el tema de aplicaciones de Windows)
        public static bool Resolve(string theme)
        {
            if (theme == "dark") return true;
            if (theme == "light") return false;
            try
            {
                object v = Microsoft.Win32.Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
                return v is int && (int)v == 0;
            }
            catch (Exception) { return false; }
        }

        public static void SetDark(bool d)
        {
            IsDark = d;
            if (d)
            {
                Bg = Color.FromArgb(17, 24, 39); Card = Color.FromArgb(31, 41, 55); Border = Color.FromArgb(55, 65, 81);
                Text = Color.FromArgb(229, 231, 235); Muted = Color.FromArgb(156, 163, 175);
                Accent = Color.FromArgb(59, 130, 246); AccentHover = Color.FromArgb(37, 99, 235);
                AccentOff = Color.FromArgb(48, 74, 130); AccentText = Color.FromArgb(96, 165, 250);
                Dark = Color.FromArgb(2, 6, 23); DarkHover = Color.FromArgb(30, 41, 59);
                Select = Color.FromArgb(30, 58, 138); RowAlt = Color.FromArgb(36, 48, 66); HeadBg = Color.FromArgb(24, 32, 46);
                Input = Color.FromArgb(17, 24, 39); Hover = Color.FromArgb(55, 65, 81); Press = Color.FromArgb(75, 85, 99);
                OffText = Color.FromArgb(107, 114, 128);
            }
            else
            {
                Bg = Color.FromArgb(243, 244, 246); Card = Color.White; Border = Color.FromArgb(229, 231, 235);
                Text = Color.FromArgb(31, 41, 55); Muted = Color.FromArgb(107, 114, 128);
                Accent = Color.FromArgb(37, 99, 235); AccentHover = Color.FromArgb(29, 78, 216);
                AccentOff = Color.FromArgb(147, 179, 245); AccentText = Color.FromArgb(37, 99, 235);
                Dark = Color.FromArgb(30, 41, 59); DarkHover = Color.FromArgb(51, 65, 85);
                Select = Color.FromArgb(219, 234, 254); RowAlt = Color.FromArgb(249, 250, 251); HeadBg = Color.FromArgb(249, 250, 251);
                Input = Color.White; Hover = Color.FromArgb(243, 244, 246); Press = Color.FromArgb(229, 231, 235);
                OffText = Color.FromArgb(156, 163, 175);
            }
        }

        public static Font Base()
        {
            return new Font("Segoe UI", 9.5f);
        }

        // kind: 0 secundario, 1 principal, 2 discreto (sobre la barra oscura)
        public static void Style(Button b, int kind)
        {
            b.Tag = kind;
            b.FlatStyle = FlatStyle.Flat;
            b.UseVisualStyleBackColor = false;
            b.Cursor = Cursors.Hand;
            Restyle(b);
            b.EnabledChanged += delegate { Restyle(b); };
        }

        public static void Restyle(Button b)
        {
            int kind = b.Tag is int ? (int)b.Tag : 0;
            b.FlatAppearance.BorderSize = kind == 0 ? 1 : 0;
            b.FlatAppearance.BorderColor = Border;
            b.FlatAppearance.MouseOverBackColor = kind == 1 ? AccentHover : (kind == 2 ? DarkHover : Hover);
            b.FlatAppearance.MouseDownBackColor = kind == 1 ? AccentHover : (kind == 2 ? DarkHover : Press);
            if (kind == 1)
            {
                b.BackColor = b.Enabled ? Accent : AccentOff;
                b.ForeColor = Color.White;
            }
            else if (kind == 2)
            {
                b.BackColor = Dark;
                b.ForeColor = Color.White;
            }
            else
            {
                b.BackColor = Card;
                b.ForeColor = b.Enabled ? Text : OffText;
            }
        }

        // Aplica los colores del tema a un control y a todos sus hijos
        public static void Theme(Control c)
        {
            string tag = c.Tag as string;
            if (tag == "card") { c.BackColor = Card; c.Invalidate(); }
            else if (tag == "header") c.BackColor = Dark;
            else if (tag == "border") c.BackColor = Border;
            else if (tag == "muted") c.ForeColor = Muted;
            else if (tag == "empty") { c.BackColor = Card; c.ForeColor = Muted; }
            else if (tag == "strip") { c.BackColor = Card; c.ForeColor = Text; }
            else if (tag == "banner") c.BackColor = Select;

            if (c is Button) Restyle((Button)c);
            else if (c is RichTextBox) { }
            else if (c is TextBox) { c.BackColor = Input; c.ForeColor = Text; }
            else if (c is ComboBox)
            {
                ((ComboBox)c).FlatStyle = FlatStyle.Flat;
                c.BackColor = Input;
                c.ForeColor = Text;
            }
            else if (c is ListBox) { c.BackColor = Input; c.ForeColor = Text; }
            else if (c is GroupBox) c.ForeColor = Text;
            else if (c is ListView) c.BackColor = Card;

            if (c.IsHandleCreated && (c is ListView || c is TextBox || c is RichTextBox || c is ListBox || c is ComboBox))
            {
                try { SetWindowTheme(c.Handle, IsDark ? (c is ComboBox ? "DarkMode_CFD" : "DarkMode_Explorer") : "Explorer", null); }
                catch (Exception) { }
            }
            foreach (Control k in c.Controls) Theme(k);
        }

        // Etiquetas de los dialogos: color de texto explicito (no depender del heredado)
        public static void FixLabels(Control c)
        {
            if (c is Label && !(c is LinkLabel) && !"muted".Equals(c.Tag)) c.ForeColor = Text;
            foreach (Control k in c.Controls) FixLabels(k);
        }

        // Barra de titulo oscura (Windows 10 1903 o superior / Windows 11)
        public static void TitleBar(Form f)
        {
            if (!f.IsHandleCreated) return;
            try
            {
                int v = IsDark ? 1 : 0;
                DwmSetWindowAttribute(f.Handle, 20, ref v, 4);
                DwmSetWindowAttribute(f.Handle, 19, ref v, 4);
            }
            catch (Exception) { }
        }
    }

    // ------------------------------------------------------------------ idioma de la interfaz (ES / EN)
    static class L
    {
        public static bool En;

        static readonly Dictionary<string, string[]> S = new Dictionary<string, string[]>
        {
            { "title", new string[] { "MoTV Renamer – Renombrador gratuito de películas y series (uso personal)", "MoTV Renamer – Free renamer for movies and TV series (personal use)" } },
            { "tagline", new string[] { "Renombrador gratuito · solo uso personal, no comercial", "Free renamer · personal, non-commercial use only" } },
            { "legal", new string[] { "MoTV Renamer es un programa GRATUITO para renombrar películas y series, de uso personal y NO comercial: no se puede vender, alquilar ni usar en ninguna actividad comercial. No descarga, distribuye ni da acceso a contenido protegido y no elude ninguna protección: solo cambia el nombre de archivos que ya tienes, usando datos de bases de datos online públicas. Eres el único responsable de los archivos que renombras y de cumplir las leyes de tu país y las condiciones de TMDb, OMDb, TheTVDB e IMDb. El programa se ofrece «tal cual», sin garantía de ningún tipo; los autores no se hacen responsables de daños, pérdida de datos ni de un mal uso. Haz copia de seguridad antes de renombrar.", "MoTV Renamer is a FREE program for renaming movies and TV series, for personal, NON-commercial use only: it must not be sold, rented or used in any commercial activity. It does not download, distribute or give access to copyrighted content and does not bypass any protection: it only renames files you already have, using data from public online databases. You alone are responsible for the files you rename and for complying with the laws of your country and the terms of TMDb, OMDb, TheTVDB and IMDb. The software is provided “as is”, without warranty of any kind; the authors accept no liability for damage, data loss or misuse. Back up your files before renaming." } },
            { "step1", new string[] { "Añadir", "Add" } },
            { "step2", new string[] { "Buscar", "Search" } },
            { "btn_about", new string[] { "Acerca de", "About" } },
            { "about_title", new string[] { "Acerca de MoTV Renamer", "About MoTV Renamer" } },
            { "about_by", new string[] { "Creado por teleonline", "Created by teleonline" } },
            { "about_what", new string[] { "Renombra tus películas y series con nombres limpios y ordenados, usando los datos de TMDb, IMDb, OMDb y TheTVDB. Funciona en tres pasos: añadir archivos, buscar nombres y renombrar, y todo se puede deshacer.", "Renames your movies and TV series with clean, consistent names using data from TMDb, IMDb, OMDb and TheTVDB. It works in three steps: add files, find names and rename, and everything can be undone." } },
            { "about_ai", new string[] { "Desarrollado con la ayuda de Claude, la IA de Anthropic.", "Built with the help of Claude, Anthropic's AI." } },
            { "about_open", new string[] { "Gratuito, solo para uso personal y no comercial (ver LICENSE). Código fuente, avisos de errores y contribuciones:", "Free, for personal non-commercial use only (see LICENSE). Source code, bug reports and contributions:" } },
            { "about_credits", new string[] { "Este producto usa las APIs de TMDb, OMDb y TheTVDB, y consulta IMDb, pero no está avalado ni certificado por ninguno de ellos. Los datos pertenecen a sus respectivos servicios.", "This product uses the TMDb, OMDb and TheTVDB APIs and queries IMDb, but is not endorsed or certified by any of them. The data belongs to their respective services." } },
            { "btn_searching", new string[] { "Buscando…", "Searching…" } },
            { "searching_banner", new string[] { "Buscando nombres para tus archivos… {i} de {n}. Esto puede tardar un poco; puedes cancelar cuando quieras.", "Looking up names for your files… {i} of {n}. This can take a moment; you can cancel at any time." } },
            { "st_searching", new string[] { "Buscando…", "Searching…" } },
            { "st_renaming", new string[] { "Renombrando…", "Renaming…" } },
            { "st_queued", new string[] { "En cola", "Queued" } },
            { "show_folder", new string[] { "Ver en carpeta", "Show in folder" } },
            { "btn_back", new string[] { "Anterior", "Back" } },
            { "btn_next", new string[] { "Siguiente", "Next" } },
            { "step1_desc", new string[] { "Elige la carpeta con tus películas y series, o añade archivos sueltos. También puedes arrastrarlos a la lista.", "Choose the folder with your movies and TV series, or add loose files. You can also drag them onto the list." } },
            { "step2_desc", new string[] { "Elige el idioma en que están escritos los nombres de tus archivos y pulsa «Buscar nombres para mis archivos».", "Choose the language your file names are written in and click “Find names for my files”." } },
            { "step3_desc", new string[] { "Revisa los nombres nuevos. Las filas en ámbar necesitan tu atención: selecciónalas y usa «Cambiar coincidencia…» u «Omitir».", "Check the new names. Amber rows need your attention: select them and use “Change match…” or “Skip”." } },
            { "step3_sum", new string[] { "{n} archivo(s) listos para renombrar · {r} para revisar", "{n} file(s) ready to rename · {r} to review" } },
            { "btn_dark", new string[] { "Modo oscuro", "Dark mode" } },
            { "btn_light", new string[] { "Modo claro", "Light mode" } },
            { "set_theme", new string[] { "Tema:", "Theme:" } },
            { "theme_auto", new string[] { "Automático (Windows)", "Automatic (Windows)" } },
            { "theme_light", new string[] { "Claro", "Light" } },
            { "theme_dark", new string[] { "Oscuro", "Dark" } },
            { "step3", new string[] { "Renombrar", "Rename" } },
            { "btn_add_folder", new string[] { "Añadir carpeta…", "Add folder…" } },
            { "btn_add_files", new string[] { "Añadir archivos…", "Add files…" } },
            { "btn_remove", new string[] { "Quitar selección", "Remove selected" } },
            { "folder_lbl", new string[] { "Carpeta: {p}", "Folder: {p}" } },
            { "folder_none", new string[] { "(ninguna)", "(none)" } },
            { "lang_lbl", new string[] { "Idioma de los títulos:", "Title language:" } },
            { "lang_note", new string[] { "el idioma en que están escritos tus nombres de archivo", "the language your file names are written in" } },
            { "search_in", new string[] { "Buscar en:", "Search in:" } },
            { "all", new string[] { "Todas", "All" } },
            { "btn_settings", new string[] { "Ajustes…", "Settings…" } },
            { "btn_tips", new string[] { "Instrucciones", "Instructions" } },
            { "btn_search", new string[] { "Buscar nombres para mis archivos", "Find names for my files" } },
            { "btn_apply", new string[] { "Renombrar", "Rename" } },
            { "btn_cancel", new string[] { "Cancelar ya", "Cancel now" } },
            { "btn_undo", new string[] { "Deshacer último renombrado", "Undo last rename" } },
            { "btn_change", new string[] { "Cambiar coincidencia…", "Change match…" } },
            { "btn_skip", new string[] { "Omitir / incluir selección", "Skip / include selection" } },
            { "btn_imdb", new string[] { "Abrir en IMDb", "Open in IMDb" } },
            { "col_orig", new string[] { "Archivo original", "Original file" } },
            { "col_new", new string[] { "Nuevo nombre", "New name" } },
            { "col_state", new string[] { "Estado", "Status" } },
            { "empty", new string[] { "Aquí aparecerán tus películas y series.\n\n1. Pulsa «Añadir carpeta…» o «Añadir archivos…»\n2. Pulsa «Buscar nombres para mis archivos»\n3. Revisa los nombres y pulsa «Renombrar»", "Your movies and TV series will appear here.\n\n1. Click “Add folder…” or “Add files…”\n2. Click “Find names for my files”\n3. Check the names and click “Rename”" } },
            { "empty_dd", new string[] { "Aquí aparecerán tus películas y series.\n\nArrastra carpetas o archivos aquí\no pulsa «Añadir carpeta…»", "Your movies and TV series will appear here.\n\nDrag folders or files here\nor click “Add folder…”" } },
            { "status_initial", new string[] { "Empieza por el paso 1: añade una carpeta o archivos.", "Start with step 1: add a folder or files." } },
            { "st_done", new string[] { "Renombrado ✓", "Renamed ✓" } },
            { "st_skipped", new string[] { "Omitido", "Skipped" } },
            { "st_ok", new string[] { "OK ({s})", "OK ({s})" } },
            { "st_review", new string[] { "Revisar: {why} ({s})", "Review: {why} ({s})" } },
            { "st_noname", new string[] { "sin nombre válido", "no valid name" } },
            { "st_noresult", new string[] { "Sin resultados", "No results" } },
            { "st_pending", new string[] { "Pendiente", "Pending" } },
            { "why_ep", new string[] { "episodio no encontrado", "episode not found" } },
            { "why_kind", new string[] { "¿película o serie?", "movie or series?" } },
            { "why_title", new string[] { "título distinto", "different title" } },
            { "why_noyear", new string[] { "sin año", "no year" } },
            { "why_year", new string[] { "año {y}", "year {y}" } },
            { "pick_any", new string[] { "Elige una carpeta o añade archivos sueltos.", "Choose a folder or add loose files." } },
            { "no_folder", new string[] { "La carpeta no existe.", "The folder does not exist." } },
            { "n_videos", new string[] { "{n} archivos de vídeo en la lista.", "{n} video files in the list." } },
            { "add_title", new string[] { "Añadir archivos de vídeo", "Add video files" } },
            { "ft_video", new string[] { "Vídeos", "Videos" } },
            { "ft_all", new string[] { "Todos los archivos", "All files" } },
            { "added", new string[] { "{a} archivo(s) añadidos; {n} en la lista.", "{a} file(s) added; {n} in the list." } },
            { "ignored", new string[] { " {i} ignorados (no son vídeo).", " {i} ignored (not video)." } },
            { "sel_many", new string[] { "Selecciona uno o varios archivos de la lista.", "Select one or more files in the list." } },
            { "sel_one", new string[] { "Selecciona un archivo de la lista.", "Select a file in the list." } },
            { "removed", new string[] { "{r} archivo(s) quitados de la lista; quedan {n}.", "{r} file(s) removed from the list; {n} left." } },
            { "need_source", new string[] { "Activa IMDb o configura una clave API (TMDb, OMDb o TVDB).", "Enable IMDb or set up an API key (TMDb, OMDb or TVDB)." } },
            { "searching", new string[] { "Buscando {i}/{n}…", "Searching {i}/{n}…" } },
            { "search_done", new string[] { "Listo. {ok} con coincidencia, {miss} sin resultados. Ahora revisa los nombres y pulsa «Renombrar».", "Done. {ok} matched, {miss} with no results. Now check the names and click “Rename”." } },
            { "search_cancel", new string[] { "Búsqueda cancelada: {p} de {n} procesados, {pend} pendientes ({ok} con coincidencia, {miss} sin resultados).", "Search cancelled: {p} of {n} processed, {pend} pending ({ok} matched, {miss} with no results)." } },
            { "warn_lang", new string[] { "  Aviso: sin clave de TMDb ni de TVDB no se puede forzar el idioma en IMDb/OMDb.", "  Note: without a TMDb or TVDB key the language cannot be forced on IMDb/OMDb." } },
            { "errors_pfx", new string[] { "  Errores: ", "  Errors: " } },
            { "cancelling", new string[] { "Cancelando…", "Cancelling…" } },
            { "pick_title", new string[] { "Elegir coincidencia", "Choose match" } },
            { "pick_search", new string[] { "Buscar", "Search" } },
            { "pick_use", new string[] { "Usar esta coincidencia", "Use this match" } },
            { "series_pfx", new string[] { "Serie", "Series" } },
            { "ep_missing", new string[] { "(episodio no encontrado)", "(episode not found)" } },
            { "nothing", new string[] { "No hay nada que renombrar.", "Nothing to rename." } },
            { "q_rename", new string[] { "¿Renombrar {n} archivo(s)?", "Rename {n} file(s)?" } },
            { "q_batches", new string[] { "\n\nSe harán automáticamente en {nb} tandas de {m}: no tienes que pulsar nada más. Verás el progreso total y un aviso cuando termine. Puedes cancelar en cualquier momento.", "\n\nIt will run automatically in {nb} batches of {m}: you don't need to click anything else. You will see the total progress and a notice when it finishes. You can cancel at any time." } },
            { "q_review", new string[] { "\n\n{r} están marcados «Revisar» (sin año, año distinto, título distinto, ¿película o serie? o episodio sin identificar). Comprueba su nuevo nombre antes de continuar.", "\n\n{r} are marked “Review” (no year, different year, different title, movie or series? or unidentified episode). Check their new names before continuing." } },
            { "preparing", new string[] { "Tanda {b}/{nb}: preparando {k}/{m}…", "Batch {b}/{nb}: preparing {k}/{m}…" } },
            { "renaming", new string[] { "Tanda {b}/{nb} · renombrando {i}/{n}…", "Batch {b}/{nb} · renaming {i}/{n}…" } },
            { "renaming_banner", new string[] { "Renombrando tus archivos… {i} de {n}. No cierres la aplicación.", "Renaming your files… {i} of {n}. Please don't close the app." } },
            { "undo_save_err", new string[] { "No se pudo guardar el historial para deshacer: {e}", "Could not save the undo history: {e}" } },
            { "fin_cancel", new string[] { "Cancelado: {d} de {t} archivo(s) renombrados. Quedan {l} sin renombrar.", "Cancelled: {d} of {t} file(s) renamed. {l} left unrenamed." } },
            { "fin_some", new string[] { "Terminado: {d} de {t} archivo(s) renombrados; {f} con problemas.", "Finished: {d} of {t} file(s) renamed; {f} with problems." } },
            { "fin_ok", new string[] { "Terminado: los {d} archivo(s) se han renombrado correctamente.", "Finished: all {d} file(s) were renamed successfully." } },
            { "fin_undo_hint", new string[] { "\n\nPuedes revertirlo con «Deshacer último renombrado».", "\n\nYou can revert it with “Undo last rename”." } },
            { "q_undo", new string[] { "¿Deshacer el último renombrado?", "Undo the last rename?" } },
            { "undone", new string[] { "{n} archivo(s) restaurados.", "{n} file(s) restored." } },
            { "nothing_undo", new string[] { "No hay nada que deshacer.", "Nothing to undo." } },
            { "exists", new string[] { "Ya existe: {p}", "Already exists: {p}" } },
            { "usage", new string[] { "Llamadas (sesión): TMDb {a} · TVDB {b} · IMDb {c} · OMDb {d} (hoy {t}/{l})", "API calls (session): TMDb {a} · TVDB {b} · IMDb {c} · OMDb {d} (today {t}/{l})" } },
            { "omdb_warn", new string[] { "Llevas {n} de {l} llamadas diarias a OMDb (80 %). Al llegar al límite dejará de usarse hasta mañana.", "You have used {n} of {l} daily OMDb calls (80 %). At the limit OMDb will stop being used until tomorrow." } },
            { "omdb_quota", new string[] { "OMDb: límite diario alcanzado ({l} llamadas); se reinicia mañana", "OMDb: daily limit reached ({l} calls); it resets tomorrow" } },
            { "src_na", new string[] { "{n}: no disponible (falta la clave o está desactivado)", "{n}: not available (missing key or disabled)" } },
            { "imdb_series_need", new string[] { "IMDb: las series necesitan clave de TMDb o de TVDB para leer los episodios", "IMDb: series need a TMDb or TVDB key to read the episodes" } },
            { "omdb_no_series", new string[] { "OMDb no admite series: usa TMDb, TVDB, IMDb o «Todas».", "OMDb does not support series: use TMDb, TVDB, IMDb or “All”." } },
            { "no_keys", new string[] { "Sin claves API: se usará solo IMDb. Añade claves en «Ajustes…» para más datos.", "No API keys: only IMDb will be used. Add keys in “Settings…” for more data." } },
            { "set_title", new string[] { "Ajustes", "Settings" } },
            { "set_keys", new string[] { "Claves API", "API keys" } },
            { "set_tmdb", new string[] { "TMDb (clave v3 o token v4):", "TMDb (v3 key or v4 token):" } },
            { "set_omdb", new string[] { "OMDb (apikey):", "OMDb (apikey):" } },
            { "set_tvdb", new string[] { "TheTVDB (clave de proyecto):", "TheTVDB (project key):" } },
            { "set_pin", new string[] { "TheTVDB PIN (solo suscriptores):", "TheTVDB PIN (subscribers only):" } },
            { "set_links", new string[] { "Claves gratuitas:", "Get free keys:" } },
            { "set_names", new string[] { "Formato de los nombres", "Name format" } },
            { "set_tpl_movie", new string[] { "Películas:", "Movies:" } },
            { "set_tpl_tv", new string[] { "Series:", "Series:" } },
            { "set_opts", new string[] { "Opciones", "Options" } },
            { "set_recursive", new string[] { "Incluir subcarpetas", "Include subfolders" } },
            { "set_folder_movie", new string[] { "Crear una carpeta por película", "Create one folder per movie" } },
            { "set_imdb", new string[] { "Usar IMDb (búsqueda sin clave, endpoint no oficial)", "Use IMDb (keyless search, unofficial endpoint)" } },
            { "set_omdb_lim", new string[] { "OMDb: límite diario de llamadas:", "OMDb: daily call limit:" } },
            { "set_ui", new string[] { "Idioma de la interfaz:", "Interface language:" } },
            { "set_ui_auto", new string[] { "Automático", "Automatic" } },
            { "set_ui_restart", new string[] { "Si cambias el idioma, el programa se reinicia solo para aplicarlo.", "If you change the language, the program restarts by itself to apply it." } },
            { "set_save", new string[] { "Guardar", "Save" } },
            { "tips_title", new string[] { "Instrucciones", "Instructions" } },
            { "close", new string[] { "Cerrar", "Close" } },
            { "wel_title", new string[] { "Bienvenido a MoTV Renamer", "Welcome to MoTV Renamer" } },
            { "wel_body", new string[] { "MoTV Renamer pone nombre limpio a tus películas y series usando bases de datos online.\n\n1. Añade una carpeta o archivos.\n2. Pulsa «Buscar nombres para mis archivos».\n3. Revisa los nombres y pulsa «Renombrar».\n\nSin claves ya funciona con IMDb (películas; las series no traen episodios). Para series completas y más precisión consigue una clave gratuita de TMDb y pégala en «Ajustes…».\n\nDos cosas importantes:\n • Elige el idioma en que están escritos tus títulos (por ejemplo Català / Valencià para «Bola de Drac»).\n • Nombra las carpetas de series como «Serie (año)», por ejemplo «Bola de Drac (1986)». Mira «Instrucciones» para más detalles.", "MoTV Renamer gives your movies and TV series clean names using online databases.\n\n1. Add a folder or files.\n2. Click “Find names for my files”.\n3. Check the names and click “Rename”.\n\nIt already works without keys using IMDb (movies; series come without episodes). For full series support and better accuracy get a free TMDb key and paste it in “Settings…”.\n\nTwo important things:\n • Choose the language your titles are written in (for example Català / Valencià for “Bola de Drac”).\n • Name series folders like “Series (year)”, for example “Dragon Ball (1986)”. See “Instructions” for details." } },
            { "wel_get", new string[] { "Conseguir clave de TMDb", "Get a TMDb key" } },
            { "wel_settings", new string[] { "Abrir Ajustes", "Open Settings" } },
            { "wel_start", new string[] { "Empezar", "Start" } },
            { "wait_search", new string[] { "Espera a que termine la búsqueda.", "Wait for the search to finish." } },
            { "wait_op", new string[] { "Espera a que termine la operación en curso.", "Wait for the current operation to finish." } }
        };

        public static string Detect()
        {
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant() == "es" ? "es" : "en";
        }

        // T("clave", "nombre", valor, ...) sustituye {nombre} por el valor
        public static string T(string key, params object[] kv)
        {
            string[] v;
            if (!S.TryGetValue(key, out v)) return key;
            string t = En ? v[1] : v[0];
            for (int i = 0; i + 1 < kv.Length; i += 2)
                t = t.Replace("{" + kv[i] + "}", Convert.ToString(kv[i + 1], CultureInfo.InvariantCulture));
            return t;
        }
    }

    // ------------------------------------------------------------------ modelos
    class Candidate
    {
        public string Title = "";
        public string OriginalTitle = "";
        public string OmdbTitle = "";
        public string TmdbTitle = "";
        public string Year = "";
        public string TmdbId = "";
        public string TvdbId = "";
        public string ImdbId = "";
        public string Source = "";
        public double Rating;
        public bool Enriched;
        public bool Localized;
        public string Kind = "movie";   // movie | tv
        public string Season = "";
        public string Ep = "";
        public string Ep2 = "";         // ultimo episodio si es doble (S01E12-E13)
        public string EpTitle = "";
        public bool IsTv { get { return Kind == "tv"; } }
    }

    // Pista de serie sacada del nombre del archivo (S01E02, "Serie - Episodio"...)
    class Hint
    {
        public string Series = "";
        public string Year;
        public int Season = -1;
        public int Ep;
        public int Ep2;
        public string EpName = "";
        public bool HasSE { get { return Season >= 0 && Ep > 0; } }
    }

    class Item
    {
        public string FullPath;
        public string Title = "";
        public string Year;
        public string ImdbId = "";
        public List<Candidate> Cands = new List<Candidate>();
        public int Sel;
        public bool Skip;
        public bool Searched;
        public bool Confirmed;
        public bool Done;   // ya renombrado en esta sesion
        public bool Manual; // archivo suelto anadido a mano (no viene de la carpeta)
        public Hint Hint;
    }

    class Plan
    {
        public string Base;
        public string Dst;
    }

    // ------------------------------------------------------------------ JSON minimo
    static class Json
    {
        public static object Parse(string text)
        {
            int i = 0;
            return Value(text, ref i);
        }

        static void Ws(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        static bool Lit(string s, ref int i, string lit)
        {
            if (string.CompareOrdinal(s, i, lit, 0, lit.Length) == 0) { i += lit.Length; return true; }
            return false;
        }

        static object Value(string s, ref int i)
        {
            Ws(s, ref i);
            if (i >= s.Length) throw new FormatException("JSON vacio");
            char c = s[i];
            if (c == '{') return Obj(s, ref i);
            if (c == '[') return Arr(s, ref i);
            if (c == '"') return Str(s, ref i);
            if (Lit(s, ref i, "true")) return true;
            if (Lit(s, ref i, "false")) return false;
            if (Lit(s, ref i, "null")) return null;
            return Num(s, ref i);
        }

        static Dictionary<string, object> Obj(string s, ref int i)
        {
            Dictionary<string, object> d = new Dictionary<string, object>();
            i++;
            Ws(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return d; }
            while (true)
            {
                Ws(s, ref i);
                if (i >= s.Length || s[i] != '"') throw new FormatException("JSON invalido");
                string k = Str(s, ref i);
                Ws(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new FormatException("JSON invalido");
                i++;
                d[k] = Value(s, ref i);
                Ws(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == '}') { i++; return d; }
                throw new FormatException("JSON invalido");
            }
        }

        static List<object> Arr(string s, ref int i)
        {
            List<object> l = new List<object>();
            i++;
            Ws(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return l; }
            while (true)
            {
                l.Add(Value(s, ref i));
                Ws(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == ']') { i++; return l; }
                throw new FormatException("JSON invalido");
            }
        }

        static string Str(string s, ref int i)
        {
            StringBuilder sb = new StringBuilder();
            i++; // comilla de apertura
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c == '\\' && i < s.Length)
                {
                    char e = s[i++];
                    switch (e)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u':
                            if (i + 4 <= s.Length)
                            {
                                sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                                i += 4;
                            }
                            break;
                        default: sb.Append(e); break;
                    }
                }
                else sb.Append(c);
            }
            throw new FormatException("JSON invalido");
        }

        static object Num(string s, ref int i)
        {
            int st = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (st == i) throw new FormatException("JSON invalido");
            return double.Parse(s.Substring(st, i - st), CultureInfo.InvariantCulture);
        }

        // ---- ayudantes de lectura
        public static string S(Dictionary<string, object> d, string k) { return S(d, k, ""); }

        public static string S(Dictionary<string, object> d, string k, string def)
        {
            object o;
            if (d == null || !d.TryGetValue(k, out o) || o == null) return def;
            if (o is double)
            {
                double v = (double)o;
                if (v == Math.Floor(v)) return ((long)v).ToString(CultureInfo.InvariantCulture);
                return v.ToString(CultureInfo.InvariantCulture);
            }
            if (o is bool) return ((bool)o) ? "true" : "false";
            return o.ToString();
        }

        public static bool B(Dictionary<string, object> d, string k, bool def)
        {
            object o;
            if (d == null || !d.TryGetValue(k, out o) || !(o is bool)) return def;
            return (bool)o;
        }

        public static double N(Dictionary<string, object> d, string k)
        {
            object o;
            if (d == null || !d.TryGetValue(k, out o) || o == null) return 0;
            if (o is double) return (double)o;
            double r;
            if (double.TryParse(o.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out r)) return r;
            return 0;
        }

        public static List<object> L(Dictionary<string, object> d, string k)
        {
            object o;
            if (d != null && d.TryGetValue(k, out o))
            {
                List<object> l = o as List<object>;
                if (l != null) return l;
            }
            return new List<object>();
        }

        public static Dictionary<string, object> D(object o)
        {
            return o as Dictionary<string, object>;
        }

        public static string Q(string s)
        {
            StringBuilder sb = new StringBuilder("\"");
            foreach (char c in (s ?? ""))
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32) sb.Append("\\u" + ((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }
    }

    // ------------------------------------------------------------------ configuracion
    class Config
    {
        public string TmdbKey = "";
        public string OmdbKey = "";
        public string TvdbKey = "";
        public string TvdbPin = "";
        public string Language = "es-ES";      // codigo que entienden las APIs
        public string LanguageUi = "";         // lo elegido en el desplegable (p. ej. valenciano)
        public string Primary = "TMDb";
        public string Template = "{title} ({year})";
        public string TvTemplate = "{series} - {sxe} - {ep_title}";
        public int OmdbDailyLimit = 1000;
        public string UiLang = "en";
        public string Theme = "dark";
        public bool Welcomed;
        public string LastDir = "";
        public bool UseImdb = true;
        public bool FolderPerMovie;
        public bool Recursive;

        // Idiomas del desplegable (codigos de TMDb). El valenciano no tiene codigo propio en las bases de datos:
        // se piden en catalan (ca-ES), que es donde esta su contenido.
        public static readonly string[][] Languages = new string[][]
        {
            new string[] { "es-ES", "Español (España)" }, new string[] { "es-MX", "Español (México)" },
            new string[] { "es-AR", "Español (Argentina)" }, new string[] { "ca-ES", "Català / Valencià" },
            new string[] { "gl-ES", "Galego" },
            new string[] { "eu-ES", "Euskara" }, new string[] { "en-US", "English (US)" },
            new string[] { "en-GB", "English (UK)" }, new string[] { "fr-FR", "Français" },
            new string[] { "fr-CA", "Français (Canada)" }, new string[] { "de-DE", "Deutsch" },
            new string[] { "it-IT", "Italiano" }, new string[] { "pt-PT", "Português (Portugal)" },
            new string[] { "pt-BR", "Português (Brasil)" }, new string[] { "nl-NL", "Nederlands" },
            new string[] { "sv-SE", "Svenska" }, new string[] { "da-DK", "Dansk" }, new string[] { "fi-FI", "Suomi" },
            new string[] { "nb-NO", "Norsk" }, new string[] { "is-IS", "Íslenska" }, new string[] { "pl-PL", "Polski" },
            new string[] { "cs-CZ", "Čeština" }, new string[] { "sk-SK", "Slovenčina" }, new string[] { "hu-HU", "Magyar" },
            new string[] { "ro-RO", "Română" }, new string[] { "bg-BG", "Български" }, new string[] { "el-GR", "Ελληνικά" },
            new string[] { "hr-HR", "Hrvatski" }, new string[] { "sr-RS", "Srpski" }, new string[] { "sl-SI", "Slovenščina" },
            new string[] { "lt-LT", "Lietuvių" }, new string[] { "lv-LV", "Latviešu" }, new string[] { "et-EE", "Eesti" },
            new string[] { "tr-TR", "Türkçe" }, new string[] { "ru-RU", "Русский" }, new string[] { "uk-UA", "Українська" },
            new string[] { "ar-SA", "العربية" }, new string[] { "he-IL", "עברית" }, new string[] { "hi-IN", "हिन्दी" },
            new string[] { "ja-JP", "日本語" }, new string[] { "ko-KR", "한국어" }, new string[] { "zh-CN", "中文 (简体)" },
            new string[] { "zh-TW", "中文 (繁體)" }, new string[] { "th-TH", "ไทย" }, new string[] { "vi-VN", "Tiếng Việt" },
            new string[] { "id-ID", "Bahasa Indonesia" }
        };

        public static string LangDisplay(string code)
        {
            foreach (string[] l in Languages)
                if (string.Equals(l[0], code, StringComparison.OrdinalIgnoreCase)) return l[0] + "  " + l[1];
            if (!string.IsNullOrEmpty(code) && code.StartsWith("ca-", StringComparison.OrdinalIgnoreCase))
                return LangDisplay("ca-ES");   // configuraciones antiguas con "ca-ES-valencia"
            return code ?? "";
        }

        // 'ca-ES  Català' -> 'ca-ES' (tambien acepta un codigo escrito a mano)
        public static string LangCode(string text)
        {
            string t = (text ?? "").Trim();
            int i = t.IndexOf(' ');
            return i > 0 ? t.Substring(0, i) : t;
        }

        // Codigo que entienden las APIs: 'ca-ES-valencia' -> 'ca-ES'
        public static string ApiLanguage(string code)
        {
            string[] p = (string.IsNullOrEmpty(code) ? "es-ES" : code).Split('-');
            return p.Length >= 2 ? p[0] + "-" + p[1] : p[0];
        }

        public static string Dir()
        {
            string b = Environment.GetEnvironmentVariable("APPDATA");
            if (string.IsNullOrEmpty(b)) b = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string p = Path.Combine(b, "MoTVRenamer");
            if (!Directory.Exists(p))
            {
                Directory.CreateDirectory(p);
                try   // migra la configuracion de versiones anteriores (se llamaba PeliRenamer)
                {
                    foreach (string oldName in new string[] { "MediaRenamer", "PeliRenamer" })
                    {
                        string old = Path.Combine(b, oldName);
                        if (Directory.Exists(old))
                        {
                            foreach (string f in Directory.GetFiles(old))
                                File.Copy(f, Path.Combine(p, Path.GetFileName(f)), false);
                            break;
                        }
                    }
                }
                catch (Exception) { }
            }
            return p;
        }

        static string FilePath() { return Path.Combine(Dir(), "config.json"); }

        public static Config Load()
        {
            Config c = new Config();
            try
            {
                string f = FilePath();
                if (File.Exists(f))
                {
                    Dictionary<string, object> d = Json.D(Json.Parse(File.ReadAllText(f, Encoding.UTF8)));
                    if (d != null)
                    {
                        c.TmdbKey = Json.S(d, "tmdb_key", c.TmdbKey).Trim();
                        c.OmdbKey = Json.S(d, "omdb_key", c.OmdbKey).Trim();
                        c.TvdbKey = Json.S(d, "tvdb_key", c.TvdbKey).Trim();
                        c.TvdbPin = Json.S(d, "tvdb_pin", c.TvdbPin).Trim();
                        c.Language = Json.S(d, "language", c.Language);
                        c.LanguageUi = Json.S(d, "language_ui", c.LanguageUi);
                        c.Primary = Json.S(d, "primary", c.Primary);
                        c.Template = Json.S(d, "template", c.Template);
                        c.TvTemplate = Json.S(d, "tv_template", c.TvTemplate);
                        int lim = (int)Json.N(d, "omdb_daily_limit");
                        if (lim > 0) c.OmdbDailyLimit = lim;
                        c.LastDir = Json.S(d, "last_dir", c.LastDir);
                        c.UiLang = Json.S(d, "ui_lang", c.UiLang);
                        c.Theme = Json.S(d, "theme", c.Theme);
                        c.Welcomed = Json.B(d, "welcomed", c.Welcomed);
                        c.UseImdb = Json.B(d, "use_imdb", c.UseImdb);
                        c.FolderPerMovie = Json.B(d, "folder_per_movie", c.FolderPerMovie);
                        c.Recursive = Json.B(d, "recursive", c.Recursive);
                    }
                }
            }
            catch (Exception) { }
            if (Array.IndexOf(Api.Choices, c.Primary) < 0) c.Primary = "TMDb";
            return c;
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"tmdb_key\": " + Json.Q(TmdbKey) + ",");
                sb.AppendLine("  \"omdb_key\": " + Json.Q(OmdbKey) + ",");
                sb.AppendLine("  \"tvdb_key\": " + Json.Q(TvdbKey) + ",");
                sb.AppendLine("  \"tvdb_pin\": " + Json.Q(TvdbPin) + ",");
                sb.AppendLine("  \"language\": " + Json.Q(Language) + ",");
                sb.AppendLine("  \"language_ui\": " + Json.Q(LanguageUi) + ",");
                sb.AppendLine("  \"primary\": " + Json.Q(Primary) + ",");
                sb.AppendLine("  \"use_imdb\": " + (UseImdb ? "true" : "false") + ",");
                sb.AppendLine("  \"template\": " + Json.Q(Template) + ",");
                sb.AppendLine("  \"tv_template\": " + Json.Q(TvTemplate) + ",");
                sb.AppendLine("  \"omdb_daily_limit\": " + OmdbDailyLimit + ",");
                sb.AppendLine("  \"ui_lang\": " + Json.Q(UiLang) + ",");
                sb.AppendLine("  \"theme\": " + Json.Q(Theme) + ",");
                sb.AppendLine("  \"welcomed\": " + (Welcomed ? "true" : "false") + ",");
                sb.AppendLine("  \"folder_per_movie\": " + (FolderPerMovie ? "true" : "false") + ",");
                sb.AppendLine("  \"recursive\": " + (Recursive ? "true" : "false") + ",");
                sb.AppendLine("  \"last_dir\": " + Json.Q(LastDir));
                sb.AppendLine("}");
                File.WriteAllText(FilePath(), sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception) { }
        }
    }

    // ------------------------------------------------------------------ nombres
    static class Names
    {
        // Marcas "fuertes" de release: a partir de la primera se descarta el resto del nombre.
        const string Strong =
            @"2160p|1080p|1080i|720p|576p|480p|4k|uhd|" +
            @"blu[\s-]?ray|bd[\s-]?rip|br[\s-]?rip|bd[\s-]?remux|web[\s-]?rip|web[\s-]?dl|hd[\s-]?rip|dvd[\s-]?rip|" +
            @"dvd[\s-]?scr|dvd[\s-]?r|dvd[59]|hdtv|pdtv|hd[\s-]?cam|cam[\s-]?rip|telesync|" +
            @"x[\s-]?26[45]|h[\s.-]?26[45]|hevc|xvid|divx|avc|" +
            @"aac|e?ac[\s-]?3|dts(?:[\s-]?hd)?|truehd|atmos|mp3|flac|dd[p+]?[\s.]?5[\s.]?1|" +
            @"hdr10?|remux|proper|repack|extended|unrated|remastered|director'?s?[\s-]?cut|" +
            @"yify|yts|rarbg|10[\s-]?bit|8[\s-]?bit";

        static readonly Regex StrongRe = new Regex(@"(?<![a-z0-9])(?:" + Strong + @")(?![a-z0-9])", RegexOptions.IgnoreCase);

        // Palabras "debiles" (idiomas, etiquetas de audio/subtitulos): solo se quitan dentro de parentesis/corchetes
        // o al final del titulo, porque tambien pueden ser parte de un titulo real ("The English Patient").
        static readonly HashSet<string> Weak = new HashSet<string>(
            ("catala catalan castellano espanol spanish spa esp ingles english eng japones japanese jap jpn " +
             "frances french fra fre aleman german ger deu italiano italian ita portugues portuguese por ruso russian rus " +
             "coreano korean kor chino chinese chi latino latam lat vose vos vosi vo vf vff vostfr subs sub subtitulado " +
             "subtitulada subtitulos dual multi multilang audio dub dubbed subbed hardsub")
            .Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

        static readonly Regex ImdbRe = new Regex(@"(?<![a-z0-9])tt\d{7,9}(?!\d)", RegexOptions.IgnoreCase);
        static readonly Regex InvalidRe = new Regex(@"[<>:""/\\|?*\x00-\x1f]");

        public static string FindImdbId(string text)
        {
            Match m = ImdbRe.Match(text ?? "");
            return m.Success ? m.Value.ToLowerInvariant() : "";
        }

        // Minusculas y sin acentos (para comparar palabras)
        public static string Fold(string text)
        {
            string d = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            foreach (char ch in d)
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            return sb.ToString();
        }

        static readonly HashSet<string> Stop = new HashSet<string>(
            "el la los las un una de del y e en the a an of and le les des et il lo gli der die das".Split(' '));

        // Palabras significativas (sin acentos, sin articulos/preposiciones, singularizadas a lo bruto)
        public static HashSet<string> SigTokens(string text)
        {
            HashSet<string> set = new HashSet<string>();
            foreach (string t in Regex.Split(Fold(text ?? ""), "[^a-z0-9]+"))
            {
                if (t.Length == 0 || Stop.Contains(t)) continue;
                set.Add(t.Length > 3 && t.EndsWith("s") ? t.Substring(0, t.Length - 1) : t);
            }
            return set;
        }

        // Fraccion de palabras del nombre del archivo que aparecen en alguno de los titulos del candidato
        public static double TitleCoverage(string fileTitle, Candidate c)
        {
            HashSet<string> ft = SigTokens(fileTitle);
            if (ft.Count == 0) return 1.0;
            HashSet<string> ct = SigTokens(c.Title);
            ct.UnionWith(SigTokens(c.OriginalTitle));
            ct.UnionWith(SigTokens(c.TmdbTitle));
            ct.UnionWith(SigTokens(c.OmdbTitle));
            int hit = 0;
            foreach (string t in ft) if (ct.Contains(t)) hit++;
            return (double)hit / ft.Count;
        }

        // Un grupo (...) o [...] es ruido si solo lleva idiomas, resolucion, codec, grupo de release...
        static bool IsNoiseGroup(string content)
        {
            string[] toks = Regex.Split(Fold(content), "[^a-z0-9]+").Where(t => t.Length > 0).ToArray();
            if (toks.Length == 0) return true;
            if (StrongRe.IsMatch(content)) return true;
            return toks.All(t => Weak.Contains(t));
        }

        static string StripNoiseGroups(string s)
        {
            return Regex.Replace(s, @"([\(\[\{])([^\)\]\}]*)[\)\]\}]",
                delegate(Match m) { return IsNoiseGroup(m.Groups[2].Value) ? " " : m.Value; });
        }

        public static void ParseName(string stem, out string title, out string year)
        {
            string s = Regex.Replace(stem, @"[._]+", " ");
            s = Regex.Replace(s, @"\{[^}]*\}", " ");        // {imdb-tt0133093} estilo Plex
            s = ImdbRe.Replace(s, " ");
            s = StripNoiseGroups(s);                          // (Catala Spanish Japanese), [1080p], [YTS.MX]...
            year = null;
            int cut = s.Length;
            MatchCollection ms = Regex.Matches(s, @"(?<!\d)(19\d{2}|20\d{2})(?!\d)");
            for (int i = ms.Count - 1; i >= 0; i--)
            {
                if (ms[i].Index > 0) { year = ms[i].Groups[1].Value; cut = ms[i].Index; break; }
            }
            Match junk = StrongRe.Match(s);
            if (junk.Success && junk.Index < cut) cut = junk.Index;
            string t = s.Substring(0, cut);
            // [Grupo] al principio (solo si queda un titulo con letras detras; "[REC] 2007" se conserva)
            Match lead = Regex.Match(t, @"^\s*\[[^\]]*\]\s*(.*)$", RegexOptions.Singleline);
            if (lead.Success && Regex.IsMatch(lead.Groups[1].Value, @"[^\W\d_]{3,}")) t = lead.Groups[1].Value;
            t = Regex.Replace(t, @"[\(\)\[\]{}]", " ");
            t = Regex.Replace(t, @"\s+[-–—]+\s+", " ");   // " - " como separador -> espacio
            t = Regex.Replace(t, @"\s+", " ").Trim(' ', '-', '–', '—', ',', ';', ':');
            List<string> words = new List<string>(t.Length > 0 ? t.Split(' ') : new string[0]);
            while (words.Count > 1 && Weak.Contains(Fold(words[words.Count - 1]).Trim('.', ',', ';', ':', '-')))
                words.RemoveAt(words.Count - 1);                // idioma suelto al final
            string res = string.Join(" ", words.ToArray());
            if (res.Length == 0) res = Regex.Replace(s, @"\s+", " ").Trim();
            title = res;
        }

        // ---- series
        static readonly Regex EpRe = new Regex(@"(?<![a-z0-9])s(\d{1,2})[\s._-]?e(\d{1,3})(?:[\s._-]?(?:e|-e?)(\d{1,3}))?(?![a-z0-9])", RegexOptions.IgnoreCase);
        static readonly Regex EpXRe = new Regex(@"(?<![a-z0-9])(\d{1,2})x(\d{2,3})(?:-(\d{2,3}))?(?![a-z0-9])", RegexOptions.IgnoreCase);
        // "(Parte I)", "Part 2", "Pt. 1"... al final del titulo de un episodio
        static readonly Regex PartRe = new Regex(@"\s*[\(\[]?\s*(?:parte|part|pt\.?|partie|teil)\s*(?:[ivx]+|\d+)\s*[\)\]]?\s*$", RegexOptions.IgnoreCase);

        static bool HasWords(string text) { return Regex.IsMatch(text ?? "", @"[^\W\d_]{2,}"); }

        public static string BaseEpName(string name) { return PartRe.Replace(name ?? "", "").Trim(); }

        // Quita ruido de release (llaves, IDs de IMDb, grupos de idioma/calidad) y corta en la primera marca tecnica
        static string CleanRelease(string s)
        {
            s = Regex.Replace(s, @"[._]+", " ");
            s = Regex.Replace(s, @"\{[^}]*\}", " ");
            s = ImdbRe.Replace(s, " ");
            s = StripNoiseGroups(s);
            Match junk = StrongRe.Match(s);
            return junk.Success ? s.Substring(0, junk.Index) : s;
        }

        static readonly Regex SeasonDirRe = new Regex(
            @"^(?:season|temporada|temp|saison|staffel|s|t)[\s._-]*\d{1,3}$|^(?:specials?|especiales|extras?)$",
            RegexOptions.IgnoreCase);
        static readonly HashSet<string> GenericDirs = new HashSet<string>(
            ("series serie tv shows series_tv video videos downloads descargas peliculas movies").Split(' '));

        // Serie deducida de las carpetas: ...\Bola de Drac\Season 01 -> "Bola de Drac"
        public static string SeriesFromFolder(string folder, out string year)
        {
            year = null;
            string d = (folder ?? "").TrimEnd('\\', '/');
            for (int lvl = 0; lvl < 3; lvl++)
            {
                string name = Path.GetFileName(d).Trim();
                if (name.Length == 0) return "";
                int dummy;
                if (SeasonDirRe.IsMatch(name) || int.TryParse(name, out dummy))
                {
                    string up = Path.GetDirectoryName(d);
                    d = (up ?? "").TrimEnd('\\', '/');
                    continue;
                }
                if (GenericDirs.Contains(Fold(name).Replace(' ', '_')) || GenericDirs.Contains(Fold(name))) return "";
                string t;
                ParseName(name, out t, out year);
                return t;
            }
            return "";
        }

        // Detecta S01E02, S01E02-E03, 1x02...
        public static Hint ParseEpisode(string stem, string folder)
        {
            string s = Regex.Replace(stem, @"[._]+", " ");
            Match m = EpRe.Match(s);
            if (!m.Success) m = EpXRe.Match(s);
            if (!m.Success) return null;
            string before = s.Substring(0, m.Index);
            string rest = s.Substring(m.Index + m.Length);
            string series, year, epName = "";
            if (HasWords(before))   // "Serie - S01E02 - Episodio"
            {
                ParseName(before, out series, out year);
                if (HasWords(rest))
                {
                    string ignoredYear;
                    ParseName(rest, out epName, out ignoredYear);
                }
                if (StrongRe.IsMatch(epName)) epName = "";   // solo etiquetas tecnicas, sin titulo de episodio
            }
            else   // "S01E02 - Serie - Episodio"
            {
                string[] parts = new Regex(@"\s*[-–—]+\s+").Split(
                    CleanRelease(rest).Trim(' ', '-', '–', '—', '.', ',', ';', ':'), 2);
                bool firstOk = HasWords(parts[0]);
                string fYear = null;
                string fSeries = !string.IsNullOrEmpty(folder) ? SeriesFromFolder(folder, out fYear) : "";
                if (parts.Length == 1 && fSeries.Length > 0)   // "S01E02 - Titulo": la serie sale de la carpeta
                {
                    series = fSeries;
                    year = fYear;
                    if (firstOk)
                    {
                        string ignoredYear3;
                        ParseName(parts[0], out epName, out ignoredYear3);
                    }
                }
                else if (firstOk)
                {
                    ParseName(parts[0], out series, out year);
                    if (parts.Length == 2 && HasWords(parts[1]))
                    {
                        string ignoredYear2;
                        ParseName(parts[1], out epName, out ignoredYear2);
                    }
                }
                else if (fSeries.Length > 0)
                {
                    series = fSeries;
                    year = fYear;
                }
                else return null;
            }
            Hint h = new Hint();
            h.Series = series;
            h.Year = year;
            h.Season = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            h.Ep = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            if (m.Groups[3].Success) h.Ep2 = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
            h.EpName = epName;
            return h;
        }

        // Pista de serie: 'S01E02' explicito o formato 'Serie - Episodio' (separador ' - ')
        public static Hint MakeHint(string stem, string folder)
        {
            Hint ep = ParseEpisode(stem, folder);
            if (ep != null) return ep;
            string s = CleanRelease(stem);
            Match sep = Regex.Match(s, @"\s+[-–—]+\s+");
            if (!sep.Success) return null;
            string a = s.Substring(0, sep.Index);
            string b = s.Substring(sep.Index + sep.Length);
            if (!HasWords(a) || !HasWords(b)) return null;
            Hint h = new Hint();
            string ignoredYear;
            ParseName(a, out h.Series, out h.Year);
            ParseName(b, out h.EpName, out ignoredYear);
            return h;
        }

        static string Pad2(string n)
        {
            int v;
            return int.TryParse(n, out v) ? v.ToString("00", CultureInfo.InvariantCulture) : n;
        }

        // S01E12 o S01E12-E13 (episodio doble)
        public static string EpisodeCode(Candidate c)
        {
            if (c.Ep.Length == 0) return "";
            string code = "S" + Pad2(c.Season) + "E" + Pad2(c.Ep);
            if (c.Ep2.Length > 0) code += "-E" + Pad2(c.Ep2);
            return code;
        }

        public static string BuildTv(string template, Candidate c)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d["series"] = c.Title;
            d["title"] = c.Title;
            d["year"] = c.Year;
            d["season"] = c.Season.Length > 0 ? Pad2(c.Season) : "";
            d["episode"] = c.Ep.Length > 0 ? Pad2(c.Ep) : "";
            d["sxe"] = EpisodeCode(c);
            d["ep_title"] = c.EpTitle;
            d["imdb_id"] = c.ImdbId;
            d["tmdb_id"] = c.TmdbId;
            d["tvdb_id"] = c.TvdbId;
            string o = Format(template, d);
            o = Regex.Replace(o, @"\(\s*\)|\[\s*\]", "");
            o = Regex.Replace(o.Trim(), @"(\s+-)+\s*$", "");   // sin titulo de episodio: no deja " -" colgando
            return Sanitize(o);
        }

        // Parecido entre el nombre del archivo y el candidato (para series se compara solo la parte de la serie)
        public static double CandCoverage(Item it, Candidate c)
        {
            if (c.IsTv)
            {
                string ser = (it.Hint != null && it.Hint.Series.Length > 0) ? it.Hint.Series : it.Title;
                return TitleCoverage(ser, c);
            }
            return TitleCoverage(it.Title, c);
        }

        // Si el nombre parece de serie: ¿hay tambien una opcion del otro tipo (pelicula/serie) igual de buena?
        public static bool OtherKindMatch(Item it, Candidate c)
        {
            if (it.Hint == null) return false;
            foreach (Candidate x in it.Cands)
                if (x.Kind != c.Kind && CandCoverage(it, x) >= 0.9 && (!x.IsTv || x.Ep.Length > 0)) return true;
            return false;
        }

        // Clave de orden: mejor parecido primero; entre iguales, las series con episodio encontrado
        public static double RankKey(Item it, Candidate c)
        {
            return -Math.Round(CandCoverage(it, c), 2) + ((c.IsTv && c.Ep.Length == 0) ? 0.001 : 0.0);
        }

        public static string Sanitize(string name)
        {
            name = InvalidRe.Replace(name, "");
            name = Regex.Replace(name, @"\s+", " ").Trim();
            return name.TrimEnd('.', ' ');
        }

        static string Format(string t, Dictionary<string, string> d)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < t.Length; i++)
            {
                char c = t[i];
                if (c == '{')
                {
                    if (i + 1 < t.Length && t[i + 1] == '{') { sb.Append('{'); i++; continue; }
                    int j = t.IndexOf('}', i + 1);
                    if (j < 0) { sb.Append(t.Substring(i)); break; }
                    string key = t.Substring(i + 1, j - i - 1);
                    int colon = key.IndexOf(':');
                    if (colon >= 0) key = key.Substring(0, colon);
                    string v;
                    if (d.TryGetValue(key.Trim(), out v)) sb.Append(v);
                    i = j;
                }
                else if (c == '}')
                {
                    if (i + 1 < t.Length && t[i + 1] == '}') i++;
                    sb.Append('}');
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        public static string Build(string template, Candidate c)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d["title"] = c.Title;
            d["original_title"] = c.OriginalTitle.Length > 0 ? c.OriginalTitle : c.Title;
            d["omdb_title"] = c.OmdbTitle.Length > 0 ? c.OmdbTitle : c.Title;
            d["tmdb_title"] = c.TmdbTitle.Length > 0 ? c.TmdbTitle : c.Title;
            d["year"] = c.Year;
            d["imdb_id"] = c.ImdbId;
            d["tmdb_id"] = c.TmdbId;
            d["tvdb_id"] = c.TvdbId;
            d["rating"] = c.Rating > 0 ? c.Rating.ToString("0.0", CultureInfo.InvariantCulture) : "";
            string o = Format(template, d);
            o = Regex.Replace(o, @"\(\s*\)|\[\s*\]", "");   // quita parentesis vacios si falta el anio
            return Sanitize(o);
        }
    }

    // ------------------------------------------------------------------ contador de llamadas
    class QuotaException : Exception
    {
        public QuotaException(string message) : base(message) { }
    }

    static class Usage
    {
        static readonly object Gate = new object();
        static readonly Dictionary<string, int> Session = new Dictionary<string, int>();
        static string omdbDate = "";
        static int omdbCount;
        static bool loaded;
        public static int OmdbLimit = 1000;

        public static string SourceOf(string url)
        {
            if (url.IndexOf("themoviedb.org", StringComparison.OrdinalIgnoreCase) >= 0) return "TMDb";
            if (url.IndexOf("omdbapi.com", StringComparison.OrdinalIgnoreCase) >= 0) return "OMDb";
            if (url.IndexOf("thetvdb.com", StringComparison.OrdinalIgnoreCase) >= 0) return "TVDB";
            if (url.IndexOf("media-imdb.com", StringComparison.OrdinalIgnoreCase) >= 0) return "IMDb";
            return null;
        }

        static string Today() { return DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
        static string FilePath() { return Path.Combine(Config.Dir(), "usage.json"); }

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                if (File.Exists(FilePath()))
                {
                    Dictionary<string, object> d = Json.D(Json.Parse(File.ReadAllText(FilePath(), Encoding.UTF8)));
                    omdbDate = Json.S(d, "omdb_date");
                    omdbCount = (int)Json.N(d, "omdb_count");
                }
            }
            catch (Exception) { }
        }

        static void Save()
        {
            try
            {
                File.WriteAllText(FilePath(), "{\"omdb_date\": " + Json.Q(omdbDate) + ", \"omdb_count\": " + omdbCount + "}",
                                  new UTF8Encoding(false));
            }
            catch (Exception) { }
        }

        // Cuenta una llamada. Para OMDb aplica el limite diario (se guarda en disco y se reinicia cada dia).
        public static void Count(string source)
        {
            lock (Gate)
            {
                Load();
                if (source == "OMDb")
                {
                    string today = Today();
                    if (omdbDate != today) { omdbDate = today; omdbCount = 0; }
                    if (omdbCount >= OmdbLimit)
                        throw new QuotaException(L.T("omdb_quota", "l", OmdbLimit));
                    omdbCount++;
                    Save();
                }
                int n;
                Session.TryGetValue(source, out n);
                Session[source] = n + 1;
            }
        }

        static int SessionCount(string src)
        {
            int n;
            return Session.TryGetValue(src, out n) ? n : 0;
        }

        public static int OmdbToday()
        {
            lock (Gate) { Load(); return omdbDate == Today() ? omdbCount : 0; }
        }

        public static double OmdbFraction() { return (double)OmdbToday() / Math.Max(1, OmdbLimit); }

        public static string Summary()
        {
            int today = OmdbToday();
            lock (Gate)
            {
return L.T("usage", "a", SessionCount("TMDb"), "b", SessionCount("TVDB"), "c", SessionCount("IMDb"),
                           "d", SessionCount("OMDb"), "t", today, "l", OmdbLimit);
            }
        }
    }

    static class Help
    {
        const string Es = @"INSTRUCCIONES

PELÍCULAS

Para que el programa encuentre correctamente el nombre de las películas:

1. El nombre de archivo debería tener el año entre paréntesis, así se distinguen remakes y películas con el mismo título. Ejemplo: Matrix (1999).mkv
2. El nombre de archivo debería tener también el ID de IMDb, así la coincidencia será exacta. Ejemplo: Matrix (1999) {imdb-tt0133093}.mkv
3. Las etiquetas de release (1080p, x264, WEB-DL...) se ignoran, pero cuanto más limpio el nombre del archivo, mejor.

SERIES

Para que el programa encuentre correctamente el nombre de las series:

1. La carpeta de la serie debe llevar el título de la serie y el año de estreno entre paréntesis. Ejemplo: ...\Dragon Ball Z (1986)\Season 01\S01E02 - Título.mkv
2. Las carpetas de temporada se reconocen y se saltan: «Season 01», «Temporada 1», «S01», «Specials», «Especiales».
3. Si el archivo no lleva el nombre de la serie (S01E02 - Título), la serie se toma de la carpeta.
4. Un episodio doble en un solo archivo se escribe S01E12-E13.
5. La numeración que se aplica es la de la base de datos elegida. Si el número del archivo no cuadra con el título del episodio, se busca por el título.
6. Los episodios solo se leen de TMDb y TVDB (OMDb e IMDb no los ofrecen): hace falta clave de una de las dos.

IDIOMA

Elige en «Idioma de los títulos» el idioma en que están escritos los títulos de tus carpetas y archivos. Por ejemplo, si buscas «Bola de Drac» (título catalán) con idioma Español, las bases de datos pueden no encontrarlo: elige Català / Valencià (las bases de datos no distinguen valenciano de catalán). Catalán, gallego y euskera tienen menos títulos traducidos: si falla cualquier idioma, prueba con inglés.

BUSCAR EN

Una sola fuente: busca únicamente ahí (sin saltar a otras). «Todas» junta los resultados de las cuatro.

LÍMITES DE LAS APIS

 - OMDb (clave gratuita): 1.000 llamadas al día. El programa las cuenta y avisa al llegar al 80 %; al llegar al límite deja de usar OMDb hasta el día siguiente. Si tienes una clave de pago, cambia el límite en «Ajustes…».
 - TMDb: sin tope diario; límite blando de unas 40 peticiones por segundo. Si responde «demasiadas peticiones» (429), el programa espera unos segundos y reintenta una vez.
 - TVDB e IMDb: no publican límites. Solo se cuentan.

AVISO LEGAL

MoTV Renamer es un programa GRATUITO para renombrar películas y series, de uso personal y NO comercial: no se puede vender, alquilar ni usar en ninguna actividad comercial. No descarga, distribuye ni da acceso a contenido protegido y no elude ninguna protección: solo cambia el nombre de archivos que ya tienes, usando datos de bases de datos online públicas. Eres el único responsable de los archivos que renombras y de cumplir las leyes de tu país y las condiciones de TMDb, OMDb, TheTVDB e IMDb. El programa se ofrece «tal cual», sin garantía de ningún tipo; los autores no se hacen responsables de daños, pérdida de datos ni de un mal uso. Haz copia de seguridad antes de renombrar.";

        const string En = @"INSTRUCTIONS

MOVIES

For the program to find your movies correctly:

1. The file name should have the year in parentheses; this tells remakes and same-title movies apart. Example: Matrix (1999).mkv
2. The file name should also have the IMDb ID, so the match will be exact. Example: Matrix (1999) {imdb-tt0133093}.mkv
3. Release tags (1080p, x264, WEB-DL...) are ignored, but the cleaner the file name, the better.

TV SERIES

For the program to find your series correctly:

1. The series folder must carry the series title and the first-air year in parentheses. Example: ...\Dragon Ball Z (1986)\Season 01\S01E02 - Title.mkv
2. Season folders are recognised and skipped: “Season 01”, “Temporada 1”, “S01”, “Specials”, “Especiales”.
3. If the file does not carry the series name (S01E02 - Title), the series is taken from the folder.
4. A double episode in one file is written S01E12-E13.
5. The numbering used is the one of the chosen database. If the number in the file does not match the episode title, the title is used to find it.
6. Episodes are only read from TMDb and TVDB (OMDb and IMDb do not provide them): you need a key for one of them.

LANGUAGE

In “Title language” choose the language your folder and file names are written in. For example, if you search “Bola de Drac” (a Catalan title) with English selected, the databases may not find it: choose Català / Valencià (the databases do not tell Valencian from Catalan). Catalan, Galician and Basque have fewer translated titles: if any language fails, try English.

SEARCH IN

A single source: searches only there (no fallback to others). “All” merges the results of all four.

API LIMITS

 - OMDb (free key): 1,000 calls per day. The program counts them and warns at 80 %; at the limit it stops using OMDb until the next day. With a paid key, change the limit in “Settings…”.
 - TMDb: no daily cap; soft limit of about 40 requests per second. If it answers “too many requests” (429), the program waits a few seconds and retries once.
 - TVDB and IMDb: no published limits. They are only counted.

LEGAL NOTICE

MoTV Renamer is a FREE program for renaming movies and TV series, for personal, NON-commercial use only: it must not be sold, rented or used in any commercial activity. It does not download, distribute or give access to copyrighted content and does not bypass any protection: it only renames files you already have, using data from public online databases. You alone are responsible for the files you rename and for complying with the laws of your country and the terms of TMDb, OMDb, TheTVDB and IMDb. The software is provided “as is”, without warranty of any kind; the authors accept no liability for damage, data loss or misuse. Back up your files before renaming.";

        public static string Get() { return L.En ? En : Es; }
    }

    // ------------------------------------------------------------------ APIs
    static class Api
    {
        public static readonly string[] Order = new string[] { "TMDb", "IMDb", "OMDb", "TVDB" };
        public static readonly string[] Choices = new string[] { "TMDb", "IMDb", "OMDb", "TVDB", "Todas" };
        const string ImdbUa = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) MoTVRenamer/1.1 (+https://github.com/teleonline/MoTVRenamer)";

        // Corte inmediato: «Cancelar» aborta las llamadas en curso y rechaza las siguientes
        static volatile bool aborted;
        static readonly List<HttpWebRequest> Active = new List<HttpWebRequest>();

        public static void BeginOperation() { aborted = false; }

        public static void Abort()
        {
            aborted = true;
            lock (Active)
            {
                foreach (HttpWebRequest r in Active.ToArray())
                {
                    try { r.Abort(); } catch (Exception) { }
                }
            }
        }

        static void PauseCancellable(int seconds)
        {
            for (int i = 0; i < seconds * 10 && !aborted; i++) Thread.Sleep(100);
            if (aborted) throw new OperationCanceledException("Cancelado");
        }

        static string Get(string url, string bearer, string ua, string lang)
        {
            string src = Usage.SourceOf(url);
            for (int attempt = 0; ; attempt++)
            {
                if (aborted) throw new OperationCanceledException("Cancelado");
                if (src != null) Usage.Count(src);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.UserAgent = ua ?? "MoTVRenamer/1.0 (+https://github.com/teleonline/MoTVRenamer)";
                req.Accept = "application/json";
                req.Timeout = 15000;
                req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                if (!string.IsNullOrEmpty(lang))   // idioma forzado tambien en la cabecera HTTP
                    req.Headers["Accept-Language"] = lang + "," + lang.Split('-')[0] + ";q=0.9,en;q=0.3";
                if (bearer != null) req.Headers["Authorization"] = "Bearer " + bearer;
                lock (Active) { Active.Add(req); }
                try
                {
                    if (aborted) throw new OperationCanceledException("Cancelado");
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        return sr.ReadToEnd();
                    }
                }
                catch (WebException wex)
                {
                    HttpWebResponse hr = wex.Response as HttpWebResponse;
                    if (attempt == 0 && hr != null && (int)hr.StatusCode == 429)   // demasiadas peticiones: espera y reintenta una vez
                    {
                        int wait;
                        if (!int.TryParse(hr.Headers["Retry-After"], out wait)) wait = 3;
                        PauseCancellable(Math.Min(10, Math.Max(1, wait)));
                        continue;
                    }
                    throw;
                }
                finally { lock (Active) { Active.Remove(req); } }
            }
        }

        static string Enc(string s) { return Uri.EscapeDataString(s ?? ""); }

        static string Qs(params string[] kv)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i + 1 < kv.Length; i += 2)
            {
                if (kv[i + 1] == null) continue;
                if (sb.Length > 0) sb.Append('&');
                sb.Append(Enc(kv[i])).Append('=').Append(Enc(kv[i + 1]));
            }
            return sb.ToString();
        }

        static Dictionary<string, object> Tmdb(string path, Config cfg, params string[] kv)
        {
            string q = Qs(kv);
            string bearer = null;
            if (cfg.TmdbKey.StartsWith("eyJ")) bearer = cfg.TmdbKey;   // token v4
            else q += (q.Length > 0 ? "&" : "") + "api_key=" + Enc(cfg.TmdbKey); // clave v3
            string txt = Get("https://api.themoviedb.org/3" + path + "?" + q, bearer, null, cfg.Language);
            return Json.D(Json.Parse(txt));
        }

        static Dictionary<string, object> Omdb(Config cfg, params string[] kv)
        {
            string q = Qs(kv) + "&apikey=" + Enc(cfg.OmdbKey);
            return Json.D(Json.Parse(Get("https://www.omdbapi.com/?" + q, null, null, cfg.Language)));
        }

        public static bool Available(string name, Config c)
        {
            if (name == "TMDb") return c.TmdbKey.Length > 0;
            if (name == "OMDb") return c.OmdbKey.Length > 0;
            if (name == "TVDB") return c.TvdbKey.Length > 0;
            return c.UseImdb;   // IMDb no necesita clave
        }

        // ---- TheTVDB (API v4): clave de proyecto (+ PIN opcional si eres suscriptor)
        static readonly Dictionary<string, string> TvdbLangs = new Dictionary<string, string>
        {
            { "es", "spa" }, { "en", "eng" }, { "fr", "fra" }, { "de", "deu" }, { "it", "ita" }, { "pt", "por" },
            { "ja", "jpn" }, { "ca", "cat" }, { "ru", "rus" }, { "zh", "zho" }, { "ko", "kor" }, { "nl", "nld" },
            { "pl", "pol" }, { "sv", "swe" }, { "gl", "glg" }, { "eu", "eus" }, { "da", "dan" }, { "fi", "fin" },
            { "nb", "nor" }, { "no", "nor" }, { "is", "isl" }, { "cs", "ces" }, { "sk", "slk" }, { "hu", "hun" },
            { "ro", "ron" }, { "bg", "bul" }, { "el", "ell" }, { "hr", "hrv" }, { "sr", "srp" }, { "sl", "slv" },
            { "lt", "lit" }, { "lv", "lav" }, { "et", "est" }, { "tr", "tur" }, { "uk", "ukr" }, { "ar", "ara" },
            { "he", "heb" }, { "hi", "hin" }, { "th", "tha" }, { "vi", "vie" }, { "id", "ind" }
        };
        static readonly Dictionary<string, string> TvdbTokens = new Dictionary<string, string>();
        static readonly object TvdbLock = new object();

        public static string TvdbLang(Config cfg)
        {
            string l = (cfg.Language ?? "es-ES").Split('-')[0].ToLowerInvariant();
            string r;
            return TvdbLangs.TryGetValue(l, out r) ? r : "eng";
        }

        static string TvdbLogin(Config cfg, bool force)
        {
            if (aborted) throw new OperationCanceledException("Cancelado");
            Usage.Count("TVDB");
            lock (TvdbLock)
            {
                string cached;
                if (!force && TvdbTokens.TryGetValue(cfg.TvdbKey, out cached)) return cached;
                string body = "{\"apikey\":" + Json.Q(cfg.TvdbKey) +
                              (cfg.TvdbPin.Length > 0 ? ",\"pin\":" + Json.Q(cfg.TvdbPin) : "") + "}";
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://api4.thetvdb.com/v4/login");
                req.Method = "POST";
                req.ContentType = "application/json";
                req.Accept = "application/json";
                req.UserAgent = "MoTVRenamer/1.0 (+https://github.com/teleonline/MoTVRenamer)";
                req.Timeout = 15000;
                byte[] data = Encoding.UTF8.GetBytes(body);
                req.ContentLength = data.Length;
                using (Stream st = req.GetRequestStream()) st.Write(data, 0, data.Length);
                string txt;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    txt = sr.ReadToEnd();
                Dictionary<string, object> d = Json.D(Json.Parse(txt));
                string token = Json.S(Json.D(d != null && d.ContainsKey("data") ? d["data"] : null), "token");
                if (token.Length == 0) throw new Exception("login sin token");
                TvdbTokens[cfg.TvdbKey] = token;
                return token;
            }
        }

        static Dictionary<string, object> TvdbGet(string path, Config cfg, params string[] kv)
        {
            string q = Qs(kv);
            string url = "https://api4.thetvdb.com/v4" + path + (q.Length > 0 ? "?" + q : "");
            for (int attempt = 0; attempt < 2; attempt++)
            {
                string token = TvdbLogin(cfg, attempt > 0);
                try
                {
                    return Json.D(Json.Parse(Get(url, token, null, cfg.Language)));
                }
                catch (WebException ex)
                {
                    HttpWebResponse hr = ex.Response as HttpWebResponse;
                    if (attempt == 0 && hr != null && hr.StatusCode == HttpStatusCode.Unauthorized) continue;   // token caducado
                    throw;
                }
            }
            return null;
        }

        static string Remote(List<object> rids, string name)
        {
            foreach (object o in rids)
            {
                Dictionary<string, object> r = Json.D(o);
                if (r != null && string.Equals(Json.S(r, "sourceName"), name, StringComparison.OrdinalIgnoreCase))
                    return Json.S(r, "id");
            }
            return "";
        }

        static Dictionary<string, object> DataOf(Dictionary<string, object> d, string key)
        {
            return Json.D(d != null && d.ContainsKey(key) ? d[key] : null);
        }

        static List<Candidate> SearchTvdb(string title, string year, Config cfg, List<string> errors)
        {
            List<Candidate> res = new List<Candidate>();
            string lang = TvdbLang(cfg);
            try
            {
                string[] ys = string.IsNullOrEmpty(year) ? new string[] { null } : new string[] { year, null };
                foreach (string y in ys)
                {
                    Dictionary<string, object> d = TvdbGet("/search", cfg, "query", title, "type", "movie", "year", y);
                    foreach (object o in Json.L(d, "data"))
                    {
                        Dictionary<string, object> r = Json.D(o);
                        if (r == null) continue;
                        if (res.Count >= 8) break;
                        string translated = Json.S(DataOf(r, "translations"), lang);
                        string name = Json.S(r, "name");
                        string imdb = Remote(Json.L(r, "remote_ids"), "IMDB");
                        string tvid = Json.S(r, "tvdb_id");
                        if (tvid.Length == 0) tvid = Json.S(r, "id").Replace("movie-", "");
                        res.Add(new Candidate
                        {
                            Title = translated.Length > 0 ? translated : name,
                            OriginalTitle = name,
                            Year = Json.S(r, "year"),
                            TvdbId = tvid,
                            ImdbId = imdb.StartsWith("tt") ? imdb : "",
                            TmdbId = Remote(Json.L(r, "remote_ids"), "TheMovieDB.com"),
                            Source = "TVDB",
                            Localized = translated.Length > 0
                        });
                    }
                    if (res.Count > 0) break;
                }
            }
            catch (Exception e) { errors.Add("TVDB: " + e.Message); }
            return res;
        }

        static string Year4(string s)
        {
            return (s != null && s.Length >= 4) ? s.Substring(0, 4) : "";
        }

        static List<Candidate> SearchTmdb(string title, string year, Config cfg, List<string> errors)
        {
            List<Candidate> res = new List<Candidate>();
            try
            {
                string[] ys = string.IsNullOrEmpty(year) ? new string[] { null } : new string[] { year, null };
                foreach (string y in ys)
                {
                    Dictionary<string, object> d = Tmdb("/search/movie", cfg, "query", title, "language", cfg.Language,
                                                        "include_adult", "false", "year", y);
                    foreach (object o in Json.L(d, "results"))
                    {
                        Dictionary<string, object> r = Json.D(o);
                        if (r == null) continue;
                        if (res.Count >= 8) break;
                        string t = Json.S(r, "title");
                        string ot = Json.S(r, "original_title");
                        res.Add(new Candidate
                        {
                            Title = t.Length > 0 ? t : ot,
                            OriginalTitle = ot,
                            Year = Year4(Json.S(r, "release_date")),
                            TmdbId = Json.S(r, "id"),
                            Rating = Json.N(r, "vote_average"),
                            Source = "TMDb"
                        });
                    }
                    if (res.Count > 0) break;
                }
            }
            catch (Exception e) { errors.Add("TMDb: " + e.Message); }
            return res;
        }

        static List<Candidate> SearchImdb(string title, string year, Config cfg, List<string> errors)
        {
            List<Candidate> res = new List<Candidate>();
            string q = (title ?? "").Trim().ToLowerInvariant();
            if (q.Length == 0) return res;
            try
            {
                char f = q[0];
                string first = (f < 128 && char.IsLetterOrDigit(f)) ? f.ToString() : "x";
                string url = "https://v3.sg.media-imdb.com/suggestion/" + first + "/" + Enc(q) + ".json";
                Dictionary<string, object> data = Json.D(Json.Parse(Get(url, null, ImdbUa, cfg.Language)));
                List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
                foreach (object o in Json.L(data, "d"))
                {
                    Dictionary<string, object> r = Json.D(o);
                    if (r == null) continue;
                    string id = Json.S(r, "id");
                    string qid = Json.S(r, "qid");
                    if (id.StartsWith("tt") && (qid == "movie" || qid == "tvMovie" || qid == "video")) rows.Add(r);
                }
                if (!string.IsNullOrEmpty(year))
                    rows = rows.OrderBy(r => Json.S(r, "y") == year ? 0 : 1).ToList();   // primero las del anio
                foreach (Dictionary<string, object> r in rows.Take(8))
                {
                    res.Add(new Candidate
                    {
                        Title = Json.S(r, "l"),
                        Year = Json.S(r, "y"),
                        ImdbId = Json.S(r, "id"),
                        Source = "IMDb"
                    });
                }
            }
            catch (Exception e) { errors.Add("IMDb: " + e.Message); }
            return res;
        }

        static List<Candidate> SearchOmdb(string title, string year, Config cfg, List<string> errors)
        {
            List<Candidate> res = new List<Candidate>();
            try
            {
                Dictionary<string, object> d = Omdb(cfg, "s", title, "type", "movie", "y", string.IsNullOrEmpty(year) ? null : year);
                if (Json.S(d, "Response") == "True")
                {
                    foreach (object o in Json.L(d, "Search"))
                    {
                        Dictionary<string, object> r = Json.D(o);
                        if (r == null) continue;
                        if (res.Count >= 8) break;
                        res.Add(new Candidate
                        {
                            Title = Json.S(r, "Title"),
                            Year = Year4(Json.S(r, "Year")),
                            ImdbId = Json.S(r, "imdbID"),
                            Source = "OMDb"
                        });
                    }
                }
            }
            catch (Exception e) { errors.Add("OMDb: " + e.Message); }
            return res;
        }

        // Busca en la fuente principal y, si no hay resultados, en las demas.
        // Con allSources (o "Todas") junta los resultados de todas las fuentes disponibles.
        public static List<Candidate> Search(string title, string year, Config cfg, bool allSources, List<string> errors)
        {
            // Solo la fuente elegida en "Buscar en:"; con "Todas" (o allSources) se juntan todas
            List<string> order = new List<string>();
            if (cfg.Primary == "Todas")
            {
                allSources = true;
                order.AddRange(Order);
            }
            else if (allSources)
            {
                order.Add(cfg.Primary);
                foreach (string s in Order) if (s != cfg.Primary) order.Add(s);
            }
            else order.Add(cfg.Primary);
            HashSet<string> seen = new HashSet<string>();
            List<Candidate> cands = new List<Candidate>();
            foreach (string name in order)
            {
                if (!Available(name, cfg))
                {
                    if (!allSources) errors.Add(L.T("src_na", "n", name));
                    continue;
                }
                List<Candidate> c;
                if (name == "TMDb") c = SearchTmdb(title, year, cfg, errors);
                else if (name == "IMDb") c = SearchImdb(title, year, cfg, errors);
                else if (name == "TVDB") c = SearchTvdb(title, year, cfg, errors);
                else c = SearchOmdb(title, year, cfg, errors);
                foreach (Candidate x in c)
                {
                    if (x.ImdbId.Length > 0)
                    {
                        if (seen.Contains(x.ImdbId)) continue;   // evita duplicados IMDb/OMDb
                        seen.Add(x.ImdbId);
                    }
                    cands.Add(x);
                }
                if (cands.Count > 0 && !allSources) break;
            }

            // Idioma forzado: los resultados de IMDb/OMDb (que no traducen) se pasan por TMDb
            int nLoc = allSources ? cands.Count : Math.Min(1, cands.Count);
            for (int i = 0; i < nLoc; i++) LocalizeOne(cands[i], cfg);

            // Tras localizar, quita duplicados que son la misma pelicula de TMDb
            HashSet<string> seenTmdb = new HashSet<string>();
            List<Candidate> uniq = new List<Candidate>();
            foreach (Candidate x in cands)
            {
                if (x.TmdbId.Length > 0)
                {
                    if (seenTmdb.Contains(x.TmdbId)) continue;
                    seenTmdb.Add(x.TmdbId);
                }
                uniq.Add(x);
            }
            return uniq;
        }

        static bool LocalizeViaTmdb(Candidate c, Config cfg)
        {
            try
            {
                Dictionary<string, object> found = Tmdb("/find/" + c.ImdbId, cfg,
                    "external_source", "imdb_id", "language", cfg.Language);
                foreach (object o in Json.L(found, "movie_results"))
                {
                    Dictionary<string, object> r = Json.D(o);
                    if (r == null) continue;
                    string t = Json.S(r, "title");
                    string ot = Json.S(r, "original_title");
                    c.TmdbId = Json.S(r, "id");
                    c.TmdbTitle = t;
                    if (ot.Length > 0) c.OriginalTitle = ot;
                    if (t.Length > 0) c.Title = t;
                    if (c.Year.Length == 0) c.Year = Year4(Json.S(r, "release_date"));
                    if (c.Rating <= 0) c.Rating = Json.N(r, "vote_average");
                    return true;
                }
            }
            catch (Exception) { }
            return false;
        }

        static void TvdbTranslate(Candidate c, string tvdbId, Config cfg)
        {
            Dictionary<string, object> tr = TvdbGet((c.IsTv ? "/series/" : "/movies/") + tvdbId + "/translations/" + TvdbLang(cfg), cfg);
            string name = Json.S(DataOf(tr, "data"), "name");
            if (name.Length > 0)
            {
                if (c.OriginalTitle.Length == 0) c.OriginalTitle = c.Title;
                c.Title = name;
            }
        }

        // IMDb/OMDb -> ficha de TheTVDB por ID de IMDb -> traduccion al idioma elegido
        static bool LocalizeViaTvdb(Candidate c, Config cfg)
        {
            try
            {
                Dictionary<string, object> d = TvdbGet("/search/remoteid/" + c.ImdbId, cfg);
                foreach (object o in Json.L(d, "data"))
                {
                    Dictionary<string, object> mv = DataOf(Json.D(o), "movie");
                    if (mv != null && Json.S(mv, "id").Length > 0)
                    {
                        c.TvdbId = Json.S(mv, "id");
                        TvdbTranslate(c, c.TvdbId, cfg);
                        return true;
                    }
                }
            }
            catch (Exception) { }
            return false;
        }

        // Fuerza el idioma elegido en resultados que no vienen traducidos (IMDb, OMDb, TVDB sin traduccion)
        public static void LocalizeOne(Candidate c, Config cfg)
        {
            if (c.Localized) return;
            if (c.Source == "TVDB")
            {
                c.Localized = true;
                if (cfg.TvdbKey.Length > 0 && c.TvdbId.Length > 0)
                {
                    try { TvdbTranslate(c, c.TvdbId, cfg); } catch (Exception) { }
                }
                return;
            }
            if (c.ImdbId.Length == 0 || c.TmdbId.Length > 0 || c.TvdbId.Length > 0) return;
            c.Localized = true;
            if (cfg.TmdbKey.Length > 0 && LocalizeViaTmdb(c, cfg)) return;
            if (cfg.TvdbKey.Length > 0) LocalizeViaTvdb(c, cfg);
        }

        // ---- Series (TMDb / TVDB / IMDb): busqueda, episodios y episodios dobles
        static Candidate TvCand(Dictionary<string, object> r, string imdbId)
        {
            string n = Json.S(r, "name");
            string on = Json.S(r, "original_name");
            Candidate c = new Candidate();
            c.Kind = "tv";
            c.Title = n.Length > 0 ? n : on;
            c.OriginalTitle = on;
            c.Year = Year4(Json.S(r, "first_air_date"));
            c.TmdbId = Json.S(r, "id");
            c.ImdbId = imdbId;
            c.Rating = Json.N(r, "vote_average");
            c.Source = "TMDb";
            c.Localized = true;
            return c;
        }

        static Candidate TvdbTvCand(Dictionary<string, object> r, Config cfg)
        {
            string translated = Json.S(DataOf(r, "translations"), TvdbLang(cfg));
            string name = Json.S(r, "name");
            string imdb = Remote(Json.L(r, "remote_ids"), "IMDB");
            string tvid = Json.S(r, "tvdb_id");
            if (tvid.Length == 0) tvid = Json.S(r, "id").Replace("series-", "");
            Candidate c = new Candidate();
            c.Kind = "tv";
            c.Title = translated.Length > 0 ? translated : name;
            c.OriginalTitle = name;
            c.Year = Year4(Json.S(r, "year"));
            c.TvdbId = tvid;
            c.ImdbId = imdb.StartsWith("tt") ? imdb : "";
            c.TmdbId = Remote(Json.L(r, "remote_ids"), "TheMovieDB.com");
            c.Source = "TVDB";
            c.Localized = translated.Length > 0;
            return c;
        }

        static List<string> ImdbSeriesIds(string title, Config cfg)
        {
            List<string> ids = new List<string>();
            string q = (title ?? "").Trim().ToLowerInvariant();
            if (q.Length == 0) return ids;
            char f = q[0];
            string first = (f < 128 && char.IsLetterOrDigit(f)) ? f.ToString() : "x";
            string url = "https://v3.sg.media-imdb.com/suggestion/" + first + "/" + Enc(q) + ".json";
            Dictionary<string, object> data = Json.D(Json.Parse(Get(url, null, ImdbUa, cfg.Language)));
            foreach (object o in Json.L(data, "d"))
            {
                Dictionary<string, object> r = Json.D(o);
                if (r == null) continue;
                string id = Json.S(r, "id");
                string qid = Json.S(r, "qid");
                if (id.StartsWith("tt") && (qid == "tvSeries" || qid == "tvMiniSeries")) ids.Add(id);
            }
            return ids;
        }

        // ID de serie de IMDb -> ficha con episodios (TMDb si hay clave; si no, TVDB). IMDb no ofrece episodios.
        static Candidate MapImdbSeries(string imdb, Config cfg)
        {
            if (cfg.TmdbKey.Length > 0)
            {
                Dictionary<string, object> found = Tmdb("/find/" + imdb, cfg,
                    "external_source", "imdb_id", "language", cfg.Language);
                foreach (object fo in Json.L(found, "tv_results"))
                {
                    Dictionary<string, object> fr = Json.D(fo);
                    if (fr == null) continue;
                    Candidate ic = TvCand(fr, imdb);
                    ic.Source = "IMDb";
                    return ic;
                }
            }
            if (cfg.TvdbKey.Length > 0)
            {
                Dictionary<string, object> dv = TvdbGet("/search/remoteid/" + imdb, cfg);
                foreach (object vo in Json.L(dv, "data"))
                {
                    Dictionary<string, object> sv = DataOf(Json.D(vo), "series");
                    if (sv == null || Json.S(sv, "id").Length == 0) continue;
                    Candidate tc = new Candidate();
                    tc.Kind = "tv";
                    tc.Title = Json.S(sv, "name");
                    tc.Year = Year4(Json.S(sv, "year"));
                    tc.TvdbId = Json.S(sv, "id");
                    tc.ImdbId = imdb;
                    tc.Source = "IMDb";
                    try { TvdbTranslate(tc, tc.TvdbId, cfg); } catch (Exception) { }
                    tc.Localized = true;
                    return tc;
                }
            }
            return null;
        }

        // De donde salen los episodios de esta serie: "tvdb", "tmdb" o "" (sin acceso)
        static string Backend(Candidate c, Config cfg)
        {
            if ((c.Source == "TVDB" || c.TmdbId.Length == 0) && c.TvdbId.Length > 0 && cfg.TvdbKey.Length > 0) return "tvdb";
            if (c.TmdbId.Length > 0 && cfg.TmdbKey.Length > 0) return "tmdb";
            return "";
        }

        static readonly Dictionary<string, List<Dictionary<string, object>>> SeasonCache =
            new Dictionary<string, List<Dictionary<string, object>>>();

        static List<Dictionary<string, object>> TvdbAllEps(string tvdbId, Config cfg)
        {
            string key = "tvdb/" + tvdbId + "/" + TvdbLang(cfg);
            lock (SeasonCache)
            {
                List<Dictionary<string, object>> hit;
                if (SeasonCache.TryGetValue(key, out hit)) return hit;
            }
            List<Dictionary<string, object>> eps = new List<Dictionary<string, object>>();
            int page = 0;
            while (page < 30)
            {
                Dictionary<string, object> d;
                try { d = TvdbGet("/series/" + tvdbId + "/episodes/default/" + TvdbLang(cfg), cfg, "page", page.ToString()); }
                catch (WebException)
                {
                    if (page > 0) break;
                    d = TvdbGet("/series/" + tvdbId + "/episodes/default", cfg, "page", "0");   // sin traduccion
                }
                foreach (object o in Json.L(DataOf(d, "data"), "episodes"))
                {
                    Dictionary<string, object> r = Json.D(o);
                    if (r == null) continue;
                    Dictionary<string, object> e = new Dictionary<string, object>();
                    e["season"] = Json.N(r, "seasonNumber");
                    e["episode_number"] = Json.N(r, "number");
                    e["name"] = Json.S(r, "name");
                    eps.Add(e);
                }
                Dictionary<string, object> links = DataOf(d, "links");
                if (links == null || !links.ContainsKey("next") || links["next"] == null) break;
                page++;
            }
            lock (SeasonCache) { SeasonCache[key] = eps; }
            return eps;
        }

        static List<int> SeasonNumbers(Candidate c, Config cfg)
        {
            HashSet<int> set = new HashSet<int>();
            if (Backend(c, cfg) == "tvdb")
            {
                foreach (Dictionary<string, object> te in TvdbAllEps(c.TvdbId, cfg))
                {
                    int tsn = (int)Json.N(te, "season");
                    if (tsn > 0) set.Add(tsn);
                }
            }
            else
            {
                Dictionary<string, object> info = Tmdb("/tv/" + c.TmdbId, cfg, "language", cfg.Language);
                foreach (object so in Json.L(info, "seasons"))
                {
                    int sn0 = (int)Json.N(Json.D(so), "season_number");
                    if (sn0 > 0) set.Add(sn0);
                }
            }
            List<int> l = set.ToList();
            l.Sort();
            return l.Take(25).ToList();
        }

        static List<Dictionary<string, object>> SeasonEps(Candidate c, int season, Config cfg)
        {
            if (Backend(c, cfg) == "tvdb")
            {
                List<Dictionary<string, object>> sel = new List<Dictionary<string, object>>();
                foreach (Dictionary<string, object> te in TvdbAllEps(c.TvdbId, cfg))
                    if ((int)Json.N(te, "season") == season) sel.Add(te);
                return sel;
            }
            string key = "tmdb/" + c.TmdbId + "/" + season + "/" + cfg.Language;
            lock (SeasonCache)
            {
                List<Dictionary<string, object>> hit;
                if (SeasonCache.TryGetValue(key, out hit)) return hit;
            }
            Dictionary<string, object> d = Tmdb("/tv/" + c.TmdbId + "/season/" + season, cfg, "language", cfg.Language);
            List<Dictionary<string, object>> eps = new List<Dictionary<string, object>>();
            foreach (object o in Json.L(d, "episodes"))
            {
                Dictionary<string, object> ed = Json.D(o);
                if (ed != null) eps.Add(ed);
            }
            lock (SeasonCache) { SeasonCache[key] = eps; }
            return eps;
        }

        static int EpNum(Dictionary<string, object> e) { return (int)Json.N(e, "episode_number"); }

        class EpRes
        {
            public int Season;
            public int Ep;
            public int Ep2;
            public string Title = "";
        }

        // Fraccion de palabras del titulo buscado que aparecen en el titulo del episodio (sin la marca de parte)
        static double EpCov(HashSet<string> want, string epName)
        {
            HashSet<string> have = Names.SigTokens(Names.BaseEpName(epName));
            int hits = 0;
            foreach (string tk in want) if (have.Contains(tk)) hits++;
            return (double)hits / want.Count;
        }

        static EpRes ByNumber(Candidate c, Hint h, Config cfg)
        {
            List<Dictionary<string, object>> eps = SeasonEps(c, h.Season, cfg);
            Dictionary<string, object> hit = null;
            foreach (Dictionary<string, object> e1 in eps) if (EpNum(e1) == h.Ep) { hit = e1; break; }
            if (hit == null) return null;
            EpRes r = new EpRes();
            r.Season = h.Season;
            r.Ep = h.Ep;
            r.Ep2 = h.Ep2;
            r.Title = Json.S(hit, "name");
            if (r.Ep2 > 0)   // el archivo ya declara el rango (S01E12-E13)
            {
                foreach (Dictionary<string, object> e2 in eps)
                {
                    if (EpNum(e2) == r.Ep2 && Names.BaseEpName(Json.S(e2, "name")) == Names.BaseEpName(r.Title))
                    {
                        r.Title = Names.BaseEpName(r.Title);
                        break;
                    }
                }
            }
            return r;
        }

        static EpRes ByName(Candidate c, Hint h, Config cfg)
        {
            HashSet<string> want = Names.SigTokens(h.EpName);
            if (want.Count == 0) return null;
            List<int> seasons = SeasonNumbers(c, cfg);
            seasons.Add(0);   // especiales al final
            Dictionary<string, object> best = null;
            double bestCov = 0;
            int bestSeason = -1;
            foreach (int sn in seasons)
            {
                List<Dictionary<string, object>> eps3;
                try { eps3 = SeasonEps(c, sn, cfg); }
                catch (Exception) { continue; }
                foreach (Dictionary<string, object> e3 in eps3)
                {
                    double cov = EpCov(want, Json.S(e3, "name"));
                    if (cov > bestCov + 1e-9) { best = e3; bestCov = cov; bestSeason = sn; }
                }
            }
            if (best == null || bestCov < 0.6) return null;
            EpRes r = new EpRes();
            r.Season = bestSeason;
            r.Ep = EpNum(best);
            r.Title = Json.S(best, "name");
            string bas = Names.BaseEpName(r.Title);
            if (bas != r.Title.Trim())   // lleva marca de parte: buscar las demas partes consecutivas
            {
                HashSet<int> nums = new HashSet<int>();
                foreach (Dictionary<string, object> e4 in SeasonEps(c, bestSeason, cfg))
                {
                    string nm = Json.S(e4, "name");
                    if (nm.Length > 0 && nm.Trim() != bas &&
                        string.Equals(Names.BaseEpName(nm), bas, StringComparison.OrdinalIgnoreCase))
                        nums.Add(EpNum(e4));
                }
                int first = r.Ep;
                int last = r.Ep;
                while (nums.Contains(first - 1)) first--;
                while (nums.Contains(last + 1)) last++;
                if (last > first) { r.Ep = first; r.Ep2 = last; r.Title = bas; }
            }
            return r;
        }

        // Rellena temporada/episodio/titulo de una serie. Une los episodios 'Parte I/II' en uno doble.
        // Usa el SxxExx del nombre; si el titulo del episodio no cuadra (numeraciones distintas entre fuentes),
        // busca por el titulo que trae el nombre.
        public static bool ResolveEpisode(Candidate c, Hint h, Config cfg)
        {
            if (h == null || Backend(c, cfg).Length == 0) return false;
            EpRes res = null;
            if (h.HasSE)
            {
                try { res = ByNumber(c, h, cfg); }
                catch (Exception) { res = null; }
            }
            HashSet<string> want = Names.SigTokens(h.EpName);
            if (want.Count > 0 && (res == null || EpCov(want, res.Title) < 0.6))
            {
                EpRes alt = ByName(c, h, cfg);
                if (alt != null) res = alt;
            }
            if (res == null) return false;
            c.Season = res.Season.ToString(CultureInfo.InvariantCulture);
            c.Ep = res.Ep.ToString(CultureInfo.InvariantCulture);
            c.Ep2 = (res.Ep2 > 0 && res.Ep2 != res.Ep) ? res.Ep2.ToString(CultureInfo.InvariantCulture) : "";
            c.EpTitle = res.Title;
            return true;
        }

        // Misma serie (por cualquiera de sus IDs): completa los IDs que falten; si no, la anade
        static void AddSeries(List<Candidate> res, Candidate c)
        {
            foreach (Candidate x in res)
            {
                bool same = (c.TmdbId.Length > 0 && c.TmdbId == x.TmdbId) ||
                            (c.ImdbId.Length > 0 && c.ImdbId == x.ImdbId) ||
                            (c.TvdbId.Length > 0 && c.TvdbId == x.TvdbId);
                if (!same) continue;
                if (x.TmdbId.Length == 0) x.TmdbId = c.TmdbId;
                if (x.ImdbId.Length == 0) x.ImdbId = c.ImdbId;
                if (x.TvdbId.Length == 0) x.TvdbId = c.TvdbId;
                return;
            }
            res.Add(c);
        }

        // Series en las fuentes elegidas en "Buscar en:". Con pista, localiza el episodio en cada una.
        public static List<Candidate> SearchTv(string series, string year, Config cfg, Hint hint, bool allSources, List<string> errors)
        {
            List<Candidate> res = new List<Candidate>();
            series = (series ?? "").Trim();
            if (series.Length == 0) return res;
            bool wide = allSources || cfg.Primary == "Todas";
            List<string> order = new List<string>();
            if (wide) order.AddRange(Order); else order.Add(cfg.Primary);
            string[] ys = string.IsNullOrEmpty(year) ? new string[] { null } : new string[] { year, null };
            foreach (string name in order)
            {
                if (!Available(name, cfg))
                {
                    if (!wide) errors.Add(L.T("src_na", "n", name));
                    continue;
                }
                try
                {
                    if (name == "TMDb")
                    {
                        foreach (string y in ys)
                        {
                            Dictionary<string, object> d = Tmdb("/search/tv", cfg, "query", series, "language", cfg.Language,
                                                                "first_air_date_year", y);
                            int got = 0;
                            foreach (object o in Json.L(d, "results"))
                            {
                                Dictionary<string, object> r = Json.D(o);
                                if (r == null || got >= 5) continue;
                                AddSeries(res, TvCand(r, ""));
                                got++;
                            }
                            if (got > 0) break;
                        }
                    }
                    else if (name == "IMDb")
                    {
                        if (cfg.TmdbKey.Length == 0 && cfg.TvdbKey.Length == 0)
                            errors.Add(L.T("imdb_series_need"));
                        else
                        {
                            List<string> ids = ImdbSeriesIds(series, cfg);
                            for (int i = 0; i < ids.Count && i < 4; i++)
                            {
                                Candidate ic = MapImdbSeries(ids[i], cfg);
                                if (ic != null) AddSeries(res, ic);
                            }
                        }
                    }
                    else if (name == "TVDB")
                    {
                        foreach (string y2 in ys)
                        {
                            Dictionary<string, object> d2 = TvdbGet("/search", cfg, "query", series, "type", "series", "year", y2);
                            int got2 = 0;
                            foreach (object o2 in Json.L(d2, "data"))
                            {
                                Dictionary<string, object> r2 = Json.D(o2);
                                if (r2 == null || got2 >= 5) continue;
                                AddSeries(res, TvdbTvCand(r2, cfg));
                                got2++;
                            }
                            if (got2 > 0) break;
                        }
                    }
                    else if (!wide) errors.Add(L.T("omdb_no_series"));
                }
                catch (Exception ex) { errors.Add(name + ": " + ex.Message); }
            }
            if (hint != null)
            {
                for (int k = 0; k < res.Count && k < 6; k++)
                {
                    try { ResolveEpisode(res[k], hint, cfg); }
                    catch (Exception ex3) { errors.Add(res[k].Source + ": " + ex3.Message); }
                }
            }
            return res;
        }

        // Peliculas + (si el nombre parece de serie) series
        public static List<Candidate> SearchAll(string title, string year, Hint hint, Config cfg, bool allSources, List<string> errors)
        {
            List<Candidate> cands = new List<Candidate>();
            bool tvFirst = hint != null && hint.HasSE;
            if (!tvFirst) cands = Search(title, year, cfg, allSources, errors);
            if (hint != null)
            {
                string ys = !string.IsNullOrEmpty(year) ? year : hint.Year;
                List<Candidate> tv = SearchTv(hint.Series, ys, cfg, hint, allSources, errors);
                tv.AddRange(cands);   // empate de parecido: gana la serie si se encontro el episodio
                cands = tv;
            }
            return cands;
        }

        // Todo el flujo de busqueda de un archivo: ID de IMDb en el nombre, peliculas y series
        public static List<Candidate> FindForItem(Item it, Config cfg, List<string> errs)
        {
            List<Candidate> cands = new List<Candidate>();
            if (it.ImdbId.Length > 0)
            {
                cands = LookupByImdb(it.ImdbId, cfg, errs);
                if (it.Hint != null)
                {
                    foreach (Candidate c in cands)
                    {
                        if (!c.IsTv || c.Ep.Length > 0) continue;
                        try { ResolveEpisode(c, it.Hint, cfg); }
                        catch (Exception ex) { errs.Add(c.Source + ": " + ex.Message); }
                    }
                }
            }
            if (cands.Count == 0) cands = SearchAll(it.Title, it.Year, it.Hint, cfg, false, errs);
            return cands;
        }

        static bool Allowed(string name, Config cfg) { return cfg.Primary == "Todas" || cfg.Primary == name; }

        // Coincidencia exacta a partir de un ID de IMDb (tt1234567)
        public static List<Candidate> LookupByImdb(string imdbId, Config cfg, List<string> errors)
        {
            List<Candidate> res = new List<Candidate>();
            if (cfg.TmdbKey.Length > 0 && Allowed("TMDb", cfg))
            {
                try
                {
                    Dictionary<string, object> found = Tmdb("/find/" + imdbId, cfg,
                        "external_source", "imdb_id", "language", cfg.Language);
                    foreach (object o in Json.L(found, "movie_results"))
                    {
                        Dictionary<string, object> r = Json.D(o);
                        if (r == null) continue;
                        string t = Json.S(r, "title");
                        string ot = Json.S(r, "original_title");
                        res.Add(new Candidate
                        {
                            Title = t.Length > 0 ? t : ot,
                            OriginalTitle = ot,
                            Year = Year4(Json.S(r, "release_date")),
                            TmdbId = Json.S(r, "id"),
                            ImdbId = imdbId,
                            Rating = Json.N(r, "vote_average"),
                            Source = "TMDb"
                        });
                        break;
                    }
                    if (res.Count == 0)   // el ID puede ser de una serie
                    {
                        foreach (object to in Json.L(found, "tv_results"))
                        {
                            Dictionary<string, object> tr = Json.D(to);
                            if (tr == null) continue;
                            res.Add(TvCand(tr, imdbId));
                            break;
                        }
                    }
                    if (res.Count == 0)   // ...o de un episodio concreto
                    {
                        foreach (object eo in Json.L(found, "tv_episode_results"))
                        {
                            Dictionary<string, object> er = Json.D(eo);
                            if (er == null) continue;
                            Dictionary<string, object> show = Tmdb("/tv/" + Json.S(er, "show_id"), cfg, "language", cfg.Language);
                            Candidate ec = TvCand(show, "");   // el ID de IMDb era del episodio: la serie lo obtiene en Enrich()
                            ec.Season = Json.S(er, "season_number");
                            ec.Ep = Json.S(er, "episode_number");
                            ec.EpTitle = Json.S(er, "name");
                            res.Add(ec);
                            break;
                        }
                    }
                }
                catch (Exception e) { errors.Add("TMDb: " + e.Message); }
            }
            if (res.Count == 0 && cfg.OmdbKey.Length > 0 && Allowed("OMDb", cfg))
            {
                try
                {
                    Dictionary<string, object> d = Omdb(cfg, "i", imdbId);
                    if (Json.S(d, "Response") == "True")
                        res.Add(new Candidate { Title = Json.S(d, "Title"), Year = Year4(Json.S(d, "Year")), ImdbId = imdbId, Source = "OMDb" });
                }
                catch (Exception e) { errors.Add("OMDb: " + e.Message); }
            }
            if (res.Count == 0 && cfg.TvdbKey.Length > 0 && Allowed("TVDB", cfg))
            {
                try
                {
                    Dictionary<string, object> dv = TvdbGet("/search/remoteid/" + imdbId, cfg);
                    foreach (object o in Json.L(dv, "data"))
                    {
                        Dictionary<string, object> mv = DataOf(Json.D(o), "movie");
                        if (mv != null && Json.S(mv, "id").Length > 0)
                        {
                            res.Add(new Candidate { Title = Json.S(mv, "name"), Year = Json.S(mv, "year"), ImdbId = imdbId,
                                                    TvdbId = Json.S(mv, "id"), Source = "TVDB" });
                            break;
                        }
                        Dictionary<string, object> sv = DataOf(Json.D(o), "series");
                        if (sv != null && Json.S(sv, "id").Length > 0)
                        {
                            Candidate sc = new Candidate();
                            sc.Kind = "tv";
                            sc.Title = Json.S(sv, "name");
                            sc.Year = Year4(Json.S(sv, "year"));
                            sc.ImdbId = imdbId;
                            sc.TvdbId = Json.S(sv, "id");
                            sc.Source = "TVDB";
                            res.Add(sc);
                            break;
                        }
                    }
                }
                catch (Exception e) { errors.Add("TVDB: " + e.Message); }
            }
            if (res.Count == 0 && cfg.UseImdb && Allowed("IMDb", cfg))
            {
                try
                {
                    Dictionary<string, object> data = Json.D(Json.Parse(
                        Get("https://v3.sg.media-imdb.com/suggestion/t/" + Enc(imdbId) + ".json", null, ImdbUa, cfg.Language)));
                    foreach (object o in Json.L(data, "d"))
                    {
                        Dictionary<string, object> r = Json.D(o);
                        if (r != null && Json.S(r, "id") == imdbId)
                        {
                            res.Add(new Candidate { Title = Json.S(r, "l"), Year = Json.S(r, "y"), ImdbId = imdbId, Source = "IMDb" });
                            break;
                        }
                    }
                }
                catch (Exception e) { errors.Add("IMDb: " + e.Message); }
            }
            if (res.Count > 0) LocalizeOne(res[0], cfg);
            return res;
        }

        // Cruza IDs entre fuentes y completa rating / titulos alternativos
        public static void Enrich(Candidate c, Config cfg)
        {
            if (c.Enriched) return;
            if (c.IsTv)   // serie: IDs de IMDb/TVDB desde la ficha de su fuente
            {
                if (Backend(c, cfg) == "tvdb" && c.ImdbId.Length == 0)
                {
                    try
                    {
                        Dictionary<string, object> xd = DataOf(TvdbGet("/series/" + c.TvdbId + "/extended", cfg, "short", "true"), "data");
                        string xi = Remote(Json.L(xd, "remoteIds"), "IMDB");
                        if (xi.StartsWith("tt")) c.ImdbId = xi;
                    }
                    catch (Exception) { }
                }
                else if (cfg.TmdbKey.Length > 0 && c.TmdbId.Length > 0 && c.ImdbId.Length == 0)
                {
                    try
                    {
                        Dictionary<string, object> ext = Tmdb("/tv/" + c.TmdbId + "/external_ids", cfg);
                        c.ImdbId = Json.S(ext, "imdb_id");
                        string tvdb = Json.S(ext, "tvdb_id");
                        if (tvdb.Length > 0) c.TvdbId = tvdb;
                    }
                    catch (Exception) { }
                }
                c.Enriched = true;
                return;
            }
            if (c.Source == "TVDB" && c.TvdbId.Length > 0 && cfg.TvdbKey.Length > 0 && c.ImdbId.Length == 0)
            {
                try
                {
                    Dictionary<string, object> dd = DataOf(TvdbGet("/movies/" + c.TvdbId + "/extended", cfg), "data");
                    List<object> rids = Json.L(dd, "remoteIds");
                    string imdb = Remote(rids, "IMDB");
                    if (imdb.StartsWith("tt")) c.ImdbId = imdb;
                    if (c.TmdbId.Length == 0) c.TmdbId = Remote(rids, "TheMovieDB.com");
                    if (c.Year.Length == 0) c.Year = Json.S(dd, "year");
                }
                catch (Exception) { }
            }
            if (cfg.TmdbKey.Length > 0)
            {
                try
                {
                    if (c.TmdbId.Length > 0 && c.ImdbId.Length == 0)
                    {
                        Dictionary<string, object> ext = Tmdb("/movie/" + c.TmdbId + "/external_ids", cfg, "language", cfg.Language);
                        c.ImdbId = Json.S(ext, "imdb_id");
                    }
                    else if (c.ImdbId.Length > 0 && c.TmdbId.Length == 0)
                    {
                        LocalizeOne(c, cfg);
                    }
                }
                catch (Exception) { }
            }
            if (cfg.OmdbKey.Length > 0 && c.ImdbId.Length > 0)
            {
                try
                {
                    Dictionary<string, object> d = Omdb(cfg, "i", c.ImdbId);
                    if (Json.S(d, "Response") == "True")
                    {
                        c.OmdbTitle = Json.S(d, "Title");
                        double r;
                        if (double.TryParse(Json.S(d, "imdbRating"), NumberStyles.Float, CultureInfo.InvariantCulture, out r)) c.Rating = r;
                    }
                }
                catch (Exception) { }
            }
            c.Enriched = true;
        }
    }

    // ------------------------------------------------------------------ archivos
    static class Files
    {
        static readonly HashSet<string> VideoExt = new HashSet<string>(
            new string[] { ".mkv", ".mp4", ".avi", ".mov", ".wmv", ".m4v", ".mpg", ".mpeg", ".ts", ".flv", ".webm" },
            StringComparer.OrdinalIgnoreCase);
        static readonly HashSet<string> SidecarExt = new HashSet<string>(
            new string[] { ".srt", ".sub", ".idx", ".ass", ".ssa", ".vtt", ".nfo" },
            StringComparer.OrdinalIgnoreCase);

        static void Walk(string dir, bool recursive, List<string> outp)
        {
            try
            {
                foreach (string f in Directory.GetFiles(dir))
                    if (VideoExt.Contains(Path.GetExtension(f))) outp.Add(f);
                if (recursive)
                    foreach (string d in Directory.GetDirectories(dir)) Walk(d, recursive, outp);
            }
            catch (Exception) { }
        }

        public static bool IsVideo(string path) { return VideoExt.Contains(Path.GetExtension(path)); }

        public static List<string> ScanVideos(string folder, bool recursive)
        {
            List<string> l = new List<string>();
            Walk(folder, recursive, l);
            l.Sort(StringComparer.OrdinalIgnoreCase);
            return l;
        }

        // Subtitulos/nfo con el mismo nombre base (p.ej. peli.es.srt) -> {archivo, sufijo}
        static List<string[]> Sidecars(string videoPath)
        {
            List<string[]> res = new List<string[]>();
            try
            {
                string dir = Path.GetDirectoryName(videoPath);
                string fname = Path.GetFileName(videoPath);
                string stem = Path.GetFileNameWithoutExtension(videoPath);
                foreach (string f in Directory.GetFiles(dir))
                {
                    string name = Path.GetFileName(f);
                    if (string.Equals(name, fname, StringComparison.OrdinalIgnoreCase)) continue;
                    string ext = Path.GetExtension(name);
                    if (!SidecarExt.Contains(ext)) continue;
                    string fs = Path.GetFileNameWithoutExtension(name);
                    if (string.Equals(fs, stem, StringComparison.OrdinalIgnoreCase) ||
                        fs.StartsWith(stem + ".", StringComparison.OrdinalIgnoreCase))
                        res.Add(new string[] { name, fs.Substring(stem.Length) + ext });
                }
            }
            catch (Exception) { }
            return res;
        }

        // Motivo por el que conviene revisar la coincidencia a mano ("" si es fiable)
        public static string ReviewReason(Item it)
        {
            if (it.Cands.Count == 0) return "";
            Candidate c = it.Cands[it.Sel < it.Cands.Count ? it.Sel : 0];
            if (c.IsTv)
            {
                if (c.Ep.Length == 0) return L.T("why_ep");
                if (it.Confirmed || (it.ImdbId.Length > 0 && it.ImdbId == c.ImdbId)) return "";
                if (Names.OtherKindMatch(it, c)) return L.T("why_kind");
                return Names.CandCoverage(it, c) < 0.6 ? L.T("why_title") : "";
            }
            if (it.Confirmed) return "";
            if (it.ImdbId.Length > 0 && c.ImdbId == it.ImdbId) return "";   // el nombre ya trae el ID de IMDb
            if (Names.OtherKindMatch(it, c)) return L.T("why_kind");
            if (Names.TitleCoverage(it.Title, c) < 0.6) return L.T("why_title");
            if (string.IsNullOrEmpty(it.Year)) return L.T("why_noyear");
            int cy, fy;
            if (int.TryParse(c.Year, out cy) && int.TryParse(it.Year, out fy) && Math.Abs(cy - fy) > 1) return L.T("why_year", "y", c.Year);
            return "";
        }

        public const int MaxRenames = 20;   // tamano de cada tanda (el renombrado encadena las tandas solo)

        public static Plan PlanTarget(Item it, string template, bool folderPerMovie, string tvTemplate)
        {
            if (it.Cands.Count == 0) return null;
            int sel = it.Sel < it.Cands.Count ? it.Sel : 0;
            Candidate cd = it.Cands[sel];
            if (cd.IsTv && cd.Ep.Length == 0) return null;   // serie sin episodio identificado: no se renombra
            string b = cd.IsTv ? Names.BuildTv(tvTemplate, cd) : Names.Build(template, cd);
            if (b.Length == 0) return null;
            string ext = Path.GetExtension(it.FullPath);
            string srcDir = Path.GetDirectoryName(it.FullPath);
            string dstDir = (folderPerMovie && !cd.IsTv) ? Path.Combine(srcDir, b) : srcDir;
            return new Plan { Base = b, Dst = Path.Combine(dstDir, b + ext) };
        }

        public static string Relative(string baseDir, string path)
        {
            if (path.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                return path.Substring(baseDir.Length).TrimStart('\\', '/');
            return path;
        }

        // Archivos que realmente cambiarian de nombre: los primeros 'limit'; 'left' = cuantos quedan pendientes
        public static List<Item> PlanBatch(List<Item> items, string template, bool folderPerMovie, string tvTemplate,
                                           int limit, out int left)
        {
            List<Item> pending = new List<Item>();
            foreach (Item it in items)
            {
                if (it.Skip || it.Cands.Count == 0) continue;
                Plan p = PlanTarget(it, template, folderPerMovie, tvTemplate);
                if (p != null && !string.Equals(it.FullPath, p.Dst, StringComparison.OrdinalIgnoreCase)) pending.Add(it);
            }
            left = Math.Max(0, pending.Count - limit);
            return pending.Take(limit).ToList();
        }

        // Renombra como maximo 'limit' archivos. Devuelve el registro para deshacer.
        public static List<string[]> ApplyRenames(List<Item> items, string template, bool folderPerMovie, string tvTemplate,
                                                  int limit, Action<int, int> progress, Func<bool> shouldStop,
                                                  out List<string> errors, out int done, out int left)
        {
            List<string[]> log = new List<string[]>();
            errors = new List<string>();
            done = 0;
            List<Item> batch = PlanBatch(items, template, folderPerMovie, tvTemplate, limit, out left);
            for (int n = 0; n < batch.Count; n++)
            {
                Item it = batch[n];
                if (shouldStop != null && shouldStop()) break;
                if (progress != null) progress(n, batch.Count);
                Plan plan = PlanTarget(it, template, folderPerMovie, tvTemplate);
                if (plan == null) continue;
                string src = it.FullPath;
                string dst = plan.Dst;
                if (File.Exists(dst)) { errors.Add(L.T("exists", "p", dst)); continue; }
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dst));
                    List<string[]> moves = new List<string[]>();
                    moves.Add(new string[] { src, dst });
                    foreach (string[] sc in Sidecars(src))
                        moves.Add(new string[] {
                            Path.Combine(Path.GetDirectoryName(src), sc[0]),
                            Path.Combine(Path.GetDirectoryName(dst), plan.Base + sc[1]) });
                    foreach (string[] m in moves)
                    {
                        if (File.Exists(m[1])) { errors.Add(L.T("exists", "p", m[1])); continue; }
                        File.Move(m[0], m[1]);
                        log.Add(new string[] { m[1], m[0] });
                    }
                    it.FullPath = dst;
                    it.Done = true;
                    done++;
                }
                catch (Exception e) { errors.Add(src + ": " + e.Message); }
            }
            if (progress != null) progress(batch.Count, batch.Count);
            return log;
        }

        static string LogPath() { return Path.Combine(Config.Dir(), "last_rename.json"); }

        public static void SaveUndo(List<string[]> log)
        {
            StringBuilder sb = new StringBuilder("[");
            for (int i = 0; i < log.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('[').Append(Json.Q(log[i][0])).Append(',').Append(Json.Q(log[i][1])).Append(']');
            }
            sb.Append(']');
            File.WriteAllText(LogPath(), sb.ToString(), new UTF8Encoding(false));
        }

        public static int UndoLast(out List<string> errors, out List<string[]> restored)
        {
            errors = new List<string>();
            restored = new List<string[]>();
            int n = 0;
            string f = LogPath();
            List<object> arr = null;
            try { if (File.Exists(f)) arr = Json.Parse(File.ReadAllText(f, Encoding.UTF8)) as List<object>; }
            catch (Exception) { }
            if (arr == null) { errors.Add(L.T("nothing_undo")); return 0; }
            for (int i = arr.Count - 1; i >= 0; i--)
            {
                List<object> pair = arr[i] as List<object>;
                if (pair == null || pair.Count < 2) continue;
                string nw = Convert.ToString(pair[0]);
                string old = Convert.ToString(pair[1]);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(old));
                    File.Move(nw, old);
                    n++;
                    restored.Add(new string[] { nw, old });
                    string d = Path.GetDirectoryName(nw);
                    if (!string.Equals(d, Path.GetDirectoryName(old), StringComparison.OrdinalIgnoreCase))
                    {
                        try { if (Directory.GetFileSystemEntries(d).Length == 0) Directory.Delete(d); }
                        catch (Exception) { }
                    }
                }
                catch (Exception e) { errors.Add(nw + ": " + e.Message); }
            }
            try { File.Delete(f); } catch (Exception) { }
            return n;
        }
    }

    // ------------------------------------------------------------------ interfaz
    class Spinner : Control
    {
        System.Windows.Forms.Timer tm = new System.Windows.Forms.Timer();
        int angle;
        bool running;
        public Spinner()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            tm.Interval = 40;
            tm.Tick += delegate { angle = (angle + 30) % 360; Invalidate(); };
        }
        public bool Running
        {
            get { return running; }
            set { running = value; if (value) tm.Start(); else tm.Stop(); Invalidate(); }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent != null ? Parent.BackColor : Ui.Bg);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int d = Math.Min(Width, Height) - 16;
            if (d < 8) return;
            Rectangle r = new Rectangle((Width - d) / 2, (Height - d) / 2, d, d);
            float pw = Math.Max(3f, d / 12f);
            using (Pen bg = new Pen(Ui.Border, pw))
                e.Graphics.DrawEllipse(bg, r);
            using (Pen fg = new Pen(Ui.Accent, pw))
            {
                fg.StartCap = LineCap.Round;
                fg.EndCap = LineCap.Round;
                e.Graphics.DrawArc(fg, r, angle, 100);
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) tm.Dispose();
            base.Dispose(disposing);
        }
    }

    class MainForm : Form
    {
        const string App = "MoTV Renamer";

        Config cfg = Config.Load();
        List<Item> items = new List<Item>();
        bool busy;

        TextBox txtFolder;
        TextBox txtTemplate;
        CheckBox chkRecursive;
        CheckBox chkFolder;
        ComboBox cmbLang;
        ComboBox cmbPrimary;
        ListView lv;
        ToolStripStatusLabel status;
        Button btnSearch;
        Button btnApply;
        Button btnCancel;
        List<string> manualFiles = new List<string>();   // archivos sueltos anadidos a mano
        HashSet<string> excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);   // quitados de la lista
        volatile bool cancelRequested;
        ToolStripStatusLabel usageLbl;
        bool omdbWarned;
        Label lblFolder;
        Button btnTheme;
        Button btnShow;
        Panel searchBanner;
        Label bannerLbl;
        ProgressBar searchBar;
        Spinner spinner;
        Item searchingItem;
        bool searchRunning;
        HashSet<Item> renameSet;
        Item renamingItem;
        int rowAngle;
        System.Windows.Forms.Timer rowTimer;
        Button btnBack;
        Button btnNext;
        Panel stepBar;
        FlowLayoutPanel bottom2;
        Label lblSummary;
        FlowLayoutPanel[] pages = new FlowLayoutPanel[3];
        int step;
        Label emptyLbl;
        TextBox txtTvTemplate;
        ProgressBar bar;

        public MainForm()
        {
            L.En = cfg.UiLang != "es";   // por defecto ingles
            Ui.SetDark(Ui.Resolve(cfg.Theme));
            Text = L.T("title");
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception) { }   // icono del exe
            Font = Ui.Base();
            BackColor = Ui.Bg;
            ForeColor = Ui.Text;
            ClientSize = new Size(1100, 680);
            MinimumSize = new Size(980, 580);
            StartPosition = FormStartPosition.CenterScreen;

            // Estos valores los edita el dialogo de Ajustes: viven en controles que no se muestran en la ventana principal.
            txtFolder = new TextBox();
            txtFolder.Text = "";   // la carpeta recordada (cfg.LastDir) solo se usa como punto de partida del selector
            txtTemplate = new TextBox();
            txtTemplate.Text = cfg.Template;
            txtTvTemplate = new TextBox();
            txtTvTemplate.Text = cfg.TvTemplate;
            chkRecursive = new CheckBox();
            chkRecursive.Checked = cfg.Recursive;
            chkFolder = new CheckBox();
            chkFolder.Checked = cfg.FolderPerMovie;
            chkFolder.CheckedChanged += delegate { RefreshRows(); };

            // --- barra superior oscura con el nombre y los accesos a Ajustes y Consejos
            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 52;
            header.BackColor = Ui.Dark;
            header.Tag = "header";
            Label appName = new Label();
            appName.Text = "MoTV Renamer";
            appName.ForeColor = Color.White;
            appName.Font = new Font("Segoe UI", 15f, FontStyle.Bold);
            appName.AutoSize = true;
            appName.Location = new Point(16, 11);
            Label appSub = new Label();
            appSub.Text = L.T("tagline");
            appSub.ForeColor = Color.FromArgb(148, 163, 184);
            appSub.AutoSize = true;
            appSub.Location = new Point(170, 19);
            FlowLayoutPanel hb = new FlowLayoutPanel();
            hb.Dock = DockStyle.Right;
            hb.AutoSize = true;
            hb.WrapContents = false;
            hb.Padding = new Padding(0, 10, 10, 0);
            btnTheme = Btn(L.T(Ui.IsDark ? "btn_light" : "btn_dark"), delegate
            {
                bool d = !Ui.IsDark;
                cfg.Theme = d ? "dark" : "light";
                Ui.SetDark(d);
                ApplyTheme();
                SyncCfg();
            }, 2);
            hb.Controls.Add(btnTheme);
            hb.Controls.Add(Btn(L.T("btn_about"), delegate { AboutDialog(); }, 2));
            hb.Controls.Add(Btn(L.T("btn_tips"), delegate { TipsDialog(); }, 2));
            hb.Controls.Add(Btn(L.T("btn_settings"), delegate { SettingsDialog(); }, 2));
            header.Controls.Add(appName);
            header.Controls.Add(appSub);
            header.Controls.Add(hb);

            lblFolder = new Label();
            lblFolder.AutoSize = true;
            lblFolder.MaximumSize = new Size(900, 0);
            lblFolder.Margin = new Padding(0, 4, 0, 0);
            lblFolder.ForeColor = Ui.Muted;
            lblFolder.Tag = "muted";
            txtFolder.TextChanged += delegate { UpdateFolderLabel(); };
            UpdateFolderLabel();

            cmbLang = new ComboBox();
            cmbLang.Width = 250;
            foreach (string[] lg in Config.Languages) cmbLang.Items.Add(lg[0] + "  " + lg[1]);
            cmbLang.DropDownStyle = ComboBoxStyle.DropDownList;   // lista cerrada: sin texto seleccionado en azul
            string curLang = Config.LangDisplay(cfg.LanguageUi.Length > 0 ? cfg.LanguageUi : cfg.Language);
            int langIdx = cmbLang.Items.IndexOf(curLang);
            if (langIdx < 0)
                for (int li = 0; li < cmbLang.Items.Count; li++)
                    if (curLang.Length > 0 && ((string)cmbLang.Items[li]).StartsWith(curLang)) { langIdx = li; break; }
            cmbLang.SelectedIndex = langIdx >= 0 ? langIdx : 0;

            cmbPrimary = new ComboBox();
            cmbPrimary.Width = 90;
            cmbPrimary.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPrimary.Items.AddRange(new object[] { "TMDb", "IMDb", "OMDb", "TVDB", L.T("all") });
            cmbPrimary.SelectedItem = cfg.Primary == "Todas" ? L.T("all") : cfg.Primary;

            btnSearch = Btn(L.T("btn_search"), delegate { StartSearch(); }, 1);
            btnSearch.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnSearch.MinimumSize = new Size(0, 44);
            btnSearch.Padding = new Padding(22, 0, 22, 0);
            btnCancel = Btn(L.T("btn_cancel"), delegate
            {
                cancelRequested = true;
                Api.Abort();   // corta tambien las llamadas a las APIs que estan en curso
                SetStatus(L.T("cancelling"));
            });
            btnCancel.Enabled = false;
            btnCancel.Visible = false;
            btnCancel.EnabledChanged += delegate { btnCancel.Visible = btnCancel.Enabled; };
            btnApply = Btn(L.T("btn_apply"), delegate { DoApply(); }, 1);

            // --- asistente: cada paso muestra solo sus controles encima de la lista
            Label lblLang = Lbl(L.T("lang_lbl"));
            lblLang.Margin = new Padding(0, 6, 6, 0);
            Label lblNote = Lbl(L.T("lang_note"));
            lblNote.ForeColor = Ui.Muted;
            lblNote.Tag = "muted";
            lblNote.Margin = new Padding(0, 0, 0, 6);
            Label lblIn = Lbl(L.T("search_in"));
            lblIn.Margin = new Padding(28, 6, 6, 0);
            cmbLang.Margin = new Padding(0, 2, 0, 0);
            cmbPrimary.Margin = new Padding(0, 2, 0, 0);
            lblSummary = Lbl("");
            lblSummary.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            lblSummary.Margin = new Padding(0, 0, 0, 8);

            pages[0] = Page(L.T("step1_desc"));
            pages[0].Controls.Add(Row(Btn(L.T("btn_add_folder"), delegate { Browse(); }, 1),
                                      Btn(L.T("btn_add_files"), delegate { AddFilesDialog(); }),
                                      Btn(L.T("btn_remove"), delegate { RemoveSelected(); })));
            pages[0].Controls.Add(lblFolder);

            pages[1] = Page(L.T("step2_desc"));
            pages[1].Controls.Add(Row(lblLang, cmbLang, lblIn, cmbPrimary));
            pages[1].Controls.Add(lblNote);
            pages[1].Controls.Add(Row(btnSearch));

            pages[2] = Page(L.T("step3_desc"));
            pages[2].Controls.Add(lblSummary);
            btnShow = Btn(L.T("show_folder"), delegate { ShowInFolder(items.Count == 1 ? items[0].FullPath : txtFolder.Text.Trim(), items.Count == 1); });
            btnShow.Visible = false;
            pages[2].Controls.Add(Row(btnShow, Btn(L.T("btn_undo"), delegate { DoUndo(); })));

            Panel steps = new Panel();
            steps.Dock = DockStyle.Top;
            steps.AutoSize = true;
            steps.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            steps.Padding = new Padding(16, 14, 16, 8);
            for (int pi = 2; pi >= 0; pi--) steps.Controls.Add(pages[pi]);

            // barra de pasos:  1. Anadir  >  2. Buscar  >  3. Renombrar
            stepBar = new Panel();
            stepBar.Dock = DockStyle.Top;
            stepBar.Height = 46;
            stepBar.BackColor = Ui.Card;
            stepBar.Tag = "card";
            int[] stepX = new int[3];
            int[] stepW = new int[3];
            stepBar.Paint += delegate(object so, PaintEventArgs ea)
            {
                Graphics g = ea.Graphics;
                string[] names = new string[] { L.T("step1"), L.T("step2"), L.T("step3") };
                using (Font norm = new Font("Segoe UI", 10.5f))
                using (Font bold = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                {
                    int x = 16;
                    for (int i = 0; i < 3; i++)
                    {
                        string txt = (i + 1) + ".  " + names[i];
                        Font f = i == step ? bold : norm;
                        Size sz = TextRenderer.MeasureText(g, txt, f, new Size(1000, 30), TextFormatFlags.NoPadding);
                        TextRenderer.DrawText(g, txt, f, new Point(x, 12), i == step ? Ui.AccentText : (i < step ? Ui.Text : Ui.Muted),
                                              TextFormatFlags.NoPadding);
                        if (i == step)
                            using (Pen ap = new Pen(Ui.Accent, 3)) g.DrawLine(ap, x, 41, x + sz.Width, 41);
                        stepX[i] = x;
                        stepW[i] = sz.Width;
                        x += sz.Width;
                        if (i < 2)
                        {
                            TextRenderer.DrawText(g, ">", norm, new Point(x + 14, 12), Ui.Muted, TextFormatFlags.NoPadding);
                            x += 44;
                        }
                    }
                }
                using (Pen bp = new Pen(Ui.Border)) g.DrawLine(bp, 0, stepBar.Height - 1, stepBar.Width, stepBar.Height - 1);
            };
            stepBar.MouseClick += delegate(object so, MouseEventArgs ea)   // se puede volver a un paso anterior
            {
                for (int i = 0; i < step; i++)
                    if (ea.X >= stepX[i] && ea.X <= stepX[i] + stepW[i]) GoStep(i);
            };

            // --- tabla
            lv = new ListView();
            lv.Dock = DockStyle.Fill;
            lv.View = View.Details;
            lv.FullRowSelect = true;
            lv.GridLines = false;
            lv.MultiSelect = true;
            lv.HideSelection = false;
            lv.BorderStyle = BorderStyle.None;
            lv.BackColor = Ui.Card;
            lv.OwnerDraw = true;
            ImageList rowSize = new ImageList();   // truco para subir la altura de las filas
            rowSize.ImageSize = new Size(1, 30);
            lv.SmallImageList = rowSize;
            lv.Columns.Add(L.T("col_orig"), 400);
            lv.Columns.Add(L.T("col_new"), 400);
            lv.Columns.Add(L.T("col_state"), 250);
            lv.Columns.Add("", 0);   // enlace «Ver en carpeta» por archivo (solo con archivos sueltos)
            lv.DoubleClick += delegate { PickDialog(); };
            Font headFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            lv.DrawColumnHeader += delegate(object so, DrawListViewColumnHeaderEventArgs ea)
            {
                using (Brush hbr = new SolidBrush(Ui.HeadBg)) ea.Graphics.FillRectangle(hbr, ea.Bounds);
                using (Pen hp = new Pen(Ui.Border)) ea.Graphics.DrawLine(hp, ea.Bounds.Left, ea.Bounds.Bottom - 1, ea.Bounds.Right, ea.Bounds.Bottom - 1);
                TextRenderer.DrawText(ea.Graphics, ea.Header.Text, headFont,
                    new Rectangle(ea.Bounds.X + 10, ea.Bounds.Y, ea.Bounds.Width - 10, ea.Bounds.Height), Ui.Muted,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            };
            lv.DrawItem += delegate(object so, DrawListViewItemEventArgs ea) { };   // cada celda se pinta en DrawSubItem
            lv.DrawSubItem += delegate(object so, DrawListViewSubItemEventArgs ea)
            {
                bool selected = ea.Item.Selected;
                Color back = selected ? Ui.Select : (ea.ItemIndex % 2 == 1 ? Ui.RowAlt : Ui.Card);
                using (Brush bb = new SolidBrush(back)) ea.Graphics.FillRectangle(bb, ea.Bounds);
                int x = ea.Bounds.X + 10;
                string txt = ea.SubItem.Text;
                Color fore = Ui.Text;
                if (ea.ColumnIndex == 2)
                {
                    Color dot = ea.Item.ForeColor;
                    if (dot.ToArgb() == SystemColors.WindowText.ToArgb())
                        dot = txt.StartsWith("OK") ? Ui.Green : Color.FromArgb(156, 163, 175);
                    fore = dot.ToArgb() == Color.FromArgb(156, 163, 175).ToArgb() ? Ui.Muted : dot;
                    ea.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    if (txt == L.T("st_searching") || txt == L.T("st_renaming"))
                    {
                        Rectangle sr = new Rectangle(x, ea.Bounds.Y + (ea.Bounds.Height - 12) / 2, 12, 12);
                        using (Pen p0 = new Pen(Ui.Border, 2f)) ea.Graphics.DrawEllipse(p0, sr);
                        using (Pen p1 = new Pen(Ui.Accent, 2f))
                        {
                            p1.StartCap = LineCap.Round;
                            p1.EndCap = LineCap.Round;
                            ea.Graphics.DrawArc(p1, sr, rowAngle, 100);
                        }
                    }
                    else
                        using (Brush db = new SolidBrush(dot))
                            ea.Graphics.FillEllipse(db, x, ea.Bounds.Y + (ea.Bounds.Height - 9) / 2, 9, 9);
                    x += 20;
                }
                else if (ea.ColumnIndex == 1 && txt.Length == 0) fore = Ui.Muted;
                Font cellFont = lv.Font;
                if (ea.ColumnIndex == 3)
                {
                    cellFont = new Font(lv.Font, FontStyle.Underline);
                    fore = Ui.AccentText;
                }
                TextRenderer.DrawText(ea.Graphics, txt, cellFont,
                    new Rectangle(x, ea.Bounds.Y, Math.Max(0, ea.Bounds.Right - x - 6), ea.Bounds.Height), fore,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                if (ea.ColumnIndex == 3) cellFont.Dispose();
            };

            DragEventHandler onEnter = delegate(object so, DragEventArgs ea)
            {
                if (ea.Data.GetDataPresent(DataFormats.FileDrop)) ea.Effect = DragDropEffects.Copy;
            };
            DragEventHandler onDrop = delegate(object so, DragEventArgs ea)
            {
                string[] dropped = ea.Data.GetData(DataFormats.FileDrop) as string[];
                if (dropped != null) AddPaths(dropped);
            };
            rowTimer = new System.Windows.Forms.Timer();
            rowTimer.Interval = 50;
            rowTimer.Tick += delegate { rowAngle = (rowAngle + 30) % 360; lv.Invalidate(); };
            lv.AllowDrop = true;   // arrastrar archivos o carpetas desde el Explorador
            lv.DragEnter += onEnter;
            lv.DragDrop += onDrop;

            emptyLbl = new Label();   // guia para quien abre el programa por primera vez
            emptyLbl.Text = L.T("empty_dd");
            emptyLbl.AutoSize = true;
            emptyLbl.ForeColor = Ui.Muted;
            emptyLbl.BackColor = Ui.Card;
            emptyLbl.Tag = "empty";
            emptyLbl.Font = new Font("Segoe UI", 12f);
            emptyLbl.TextAlign = ContentAlignment.MiddleCenter;
            emptyLbl.AllowDrop = true;
            emptyLbl.DragEnter += onEnter;
            emptyLbl.DragDrop += onDrop;
            lv.Controls.Add(emptyLbl);

            lv.Resize += delegate { LayoutColumns(); CenterEmpty(); };
            lv.MouseClick += delegate(object so, MouseEventArgs ea)
            {
                int idx;
                if (OverLink(ea.Location, out idx)) ShowInFolder(items[idx].FullPath, true);
            };
            lv.MouseMove += delegate(object so, MouseEventArgs ea)
            {
                int idx;
                lv.Cursor = OverLink(ea.Location, out idx) ? Cursors.Hand : Cursors.Default;
            };

            Panel listWrap = new Panel();   // marco fino alrededor de la tabla
            listWrap.Dock = DockStyle.Fill;
            listWrap.Padding = new Padding(1);
            listWrap.BackColor = Ui.Border;
            listWrap.Tag = "border";
            listWrap.Controls.Add(lv);
            Panel listOuter = new Panel();
            listOuter.Dock = DockStyle.Fill;
            listOuter.Padding = new Padding(12, 4, 12, 4);
            listOuter.Controls.Add(listWrap);

            // aviso que aparece sobre la lista mientras se buscan los nombres
            searchBanner = new Panel();
            searchBanner.Dock = DockStyle.Top;
            searchBanner.Height = 44;
            searchBanner.BackColor = Ui.Select;
            searchBanner.Tag = "banner";
            searchBanner.Visible = false;
            bannerLbl = new Label();
            bannerLbl.AutoSize = false;
            bannerLbl.Dock = DockStyle.Fill;
            bannerLbl.Padding = new Padding(14, 0, 0, 0);
            bannerLbl.TextAlign = ContentAlignment.MiddleLeft;
            bannerLbl.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            searchBar = new ProgressBar();
            searchBar.Dock = DockStyle.Bottom;
            searchBar.Height = 6;
            searchBar.Style = ProgressBarStyle.Continuous;
            spinner = new Spinner();
            spinner.Dock = DockStyle.Left;
            spinner.Width = 44;
            searchBanner.Controls.Add(bannerLbl);
            searchBanner.Controls.Add(spinner);
            bannerLbl.BringToFront();
            searchBanner.Controls.Add(searchBar);
            listOuter.Controls.Add(searchBanner);

            bottom2 = new FlowLayoutPanel();   // acciones sobre las filas seleccionadas
            bottom2.Dock = DockStyle.Bottom;
            bottom2.Height = 44;
            bottom2.Padding = new Padding(12, 4, 0, 0);
            bottom2.Controls.Add(Btn(L.T("btn_change"), delegate { PickDialog(); }));
            bottom2.Controls.Add(Btn(L.T("btn_skip"), delegate { ToggleSkip(); }));
            bottom2.Controls.Add(Btn(L.T("btn_imdb"), delegate { OpenImdb(); }));

            btnBack = Btn(L.T("btn_back"), delegate { GoStep(step - 1); });
            btnNext = Btn(L.T("btn_next"), delegate { GoStep(step + 1); }, 1);
            btnNext.Margin = new Padding(0);
            btnApply.Margin = new Padding(0);
            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 54;
            FlowLayoutPanel fl = new FlowLayoutPanel();
            fl.Dock = DockStyle.Left;
            fl.AutoSize = true;
            fl.WrapContents = false;
            fl.Padding = new Padding(16, 11, 0, 0);
            fl.Controls.Add(btnBack);
            fl.Controls.Add(btnCancel);
            FlowLayoutPanel fr = new FlowLayoutPanel();
            fr.Dock = DockStyle.Right;
            fr.AutoSize = true;
            fr.WrapContents = false;
            fr.Padding = new Padding(0, 11, 16, 0);
            fr.Controls.Add(btnNext);
            fr.Controls.Add(btnApply);
            footer.Controls.Add(fl);
            footer.Controls.Add(fr);

            bar = new ProgressBar();
            bar.Dock = DockStyle.Bottom;
            bar.Height = 8;
            bar.Style = ProgressBarStyle.Continuous;
            bar.Minimum = 0;
            bar.Maximum = 1;
            bar.Visible = false;

            StatusStrip strip = new StatusStrip();
            strip.BackColor = Ui.Card;
            strip.Tag = "strip";
            strip.SizingGrip = false;
            status = new ToolStripStatusLabel();
            status.Spring = true;
            status.TextAlign = ContentAlignment.MiddleLeft;
            usageLbl = new ToolStripStatusLabel();
            usageLbl.Alignment = ToolStripItemAlignment.Right;
            usageLbl.Text = Usage.Summary();
            status.Text = L.T("status_initial");
            strip.Items.Add(status);
            strip.Items.Add(usageLbl);

            Usage.OmdbLimit = Math.Max(1, cfg.OmdbDailyLimit);
            System.Windows.Forms.Timer usageTimer = new System.Windows.Forms.Timer();
            usageTimer.Interval = 1000;
            usageTimer.Tick += delegate
            {
                usageLbl.Text = Usage.Summary();
                bool high = Usage.OmdbFraction() >= 0.8;
                usageLbl.ForeColor = high ? Color.Firebrick : Ui.Muted;
                if (high && !omdbWarned)
                {
                    omdbWarned = true;
                    MessageBox.Show(L.T("omdb_warn", "n", Usage.OmdbToday(), "l", Usage.OmdbLimit),
                        App, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            usageTimer.Start();

            // El control Fill va primero; los demas se acoplan despues (el ultimo anadido se acopla antes).
            Controls.Add(listOuter);
            Controls.Add(bottom2);
            Controls.Add(footer);
            Controls.Add(bar);
            Controls.Add(strip);
            Controls.Add(steps);
            Controls.Add(stepBar);
            Controls.Add(header);
            GoStep(0);

            txtTemplate.TextChanged += delegate { RefreshRows(); };
            txtTvTemplate.TextChanged += delegate { RefreshRows(); };
            FormClosing += delegate { SyncCfg(); };
            Ui.Theme(this);
            Shown += delegate
            {
                ApplyTheme();
                RefreshRows();
                if (cfg.TmdbKey.Length == 0 && cfg.OmdbKey.Length == 0 && cfg.TvdbKey.Length == 0)
                {
                    SetStatus(L.T("no_keys"));
                    if (!cfg.Welcomed) WelcomeDialog();
                }
            };
        }

        static FlowLayoutPanel Page(string description)
        {
            FlowLayoutPanel f = new FlowLayoutPanel();
            f.Dock = DockStyle.Top;
            f.AutoSize = true;
            f.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            f.FlowDirection = FlowDirection.TopDown;
            f.WrapContents = false;
            Label d = new Label();
            d.Text = description;
            d.AutoSize = true;
            d.MaximumSize = new Size(900, 0);
            d.Margin = new Padding(0, 0, 0, 10);
            f.Controls.Add(d);
            return f;
        }

        void GoStep(int n)
        {
            if (busy || n < 0 || n > 2) return;
            step = n;
            for (int i = 0; i < 3; i++) pages[i].Visible = i == n;
            bottom2.Visible = n > 0;
            btnBack.Visible = n > 0;
            btnNext.Visible = n < 2;
            btnApply.Visible = n == 2;
            UpdateNav();
            UpdateSummary();
            stepBar.Invalidate();
        }

        void UpdateNav()
        {
            if (btnNext == null) return;
            bool any = false;
            foreach (Item x in items) if (x.Searched) any = true;
            btnBack.Enabled = !busy;
            btnNext.Enabled = !busy && (step == 0 ? items.Count > 0 : any);
        }

        // Con archivos sueltos (mas de uno) cada fila lleva su propio «Ver en carpeta»; si no, un unico boton.
        bool PerFileMode() { return manualFiles.Count > 0 && items.Count > 1; }

        void LayoutColumns()
        {
            if (lv == null || lv.Columns.Count < 4) return;
            int link = PerFileMode() ? 130 : 0;
            int w = lv.ClientSize.Width;
            lv.Columns[3].Width = link;
            lv.Columns[2].Width = 250;
            lv.Columns[0].Width = Math.Max(120, (w - 250 - link) / 2);
            lv.Columns[1].Width = Math.Max(120, w - 250 - link - lv.Columns[0].Width);
        }

        bool OverLink(Point pt, out int index)
        {
            index = -1;
            ListViewHitTestInfo h = lv.HitTest(pt);
            if (h.Item == null || h.SubItem == null) return false;
            if (h.Item.SubItems.IndexOf(h.SubItem) != 3 || h.SubItem.Text.Length == 0) return false;
            index = h.Item.Index;
            return index >= 0 && index < items.Count;
        }

        // Abre el Explorador: con un archivo lo deja seleccionado; con una carpeta la abre
        void ShowInFolder(string path, bool selectFile)
        {
            try
            {
                if (selectFile && File.Exists(path))
                    Process.Start("explorer.exe", "/select,\"" + path + "\"");
                else if (Directory.Exists(path))
                    Process.Start("explorer.exe", "\"" + path + "\"");
                else if (File.Exists(path))
                    Process.Start("explorer.exe", "\"" + Path.GetDirectoryName(path) + "\"");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, App, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        void UpdateSummary()
        {
            LayoutColumns();
            if (btnShow != null)
            {
                bool anyDone = false;
                foreach (Item x in items) if (x.Done) anyDone = true;
                btnShow.Visible = anyDone && !PerFileMode();
            }
            if (step != 2 || lblSummary == null) return;
            int ignored;
            List<Item> ready = Files.PlanBatch(items, txtTemplate.Text, chkFolder.Checked, txtTvTemplate.Text,
                                               int.MaxValue, out ignored);
            lblSummary.Text = L.T("step3_sum", "n", ready.Count, "r", ready.Count(x => Files.ReviewReason(x).Length > 0));
        }

        void ApplyTheme()
        {
            BackColor = Ui.Bg;
            ForeColor = Ui.Text;
            Ui.Theme(this);
            status.ForeColor = Ui.Text;
            usageLbl.ForeColor = Ui.Muted;
            btnTheme.Text = L.T(Ui.IsDark ? "btn_light" : "btn_dark");
            lv.Invalidate();
            stepBar.Invalidate();
            Ui.TitleBar(this);
        }

        const string Version = "1.0.0";
        const string RepoUrl = "https://github.com/teleonline/MoTVRenamer";

        void AboutDialog()
        {
            Form dlg = new Form();
            StyleDialog(dlg);
            dlg.BackColor = Ui.Card;
            dlg.Text = L.T("about_title");
            dlg.ClientSize = new Size(520, 400);
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.ShowInTaskbar = false;

            Label name = new Label();
            name.Text = "MoTV Renamer  " + Version;
            name.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            name.SetBounds(24, 20, 472, 34);
            Label by = new Label();
            by.Text = L.T("about_by");
            by.SetBounds(24, 58, 472, 22);
            Label what = new Label();
            what.Text = L.T("about_what");
            what.SetBounds(24, 92, 472, 62);
            Label ai = new Label();
            ai.Text = L.T("about_ai");
            ai.SetBounds(24, 158, 472, 22);
            Label open = new Label();
            open.Text = L.T("about_open");
            open.SetBounds(24, 196, 472, 40);
            LinkLabel link = new LinkLabel();
            link.Text = RepoUrl;
            link.LinkColor = Ui.AccentText;
            link.ActiveLinkColor = Ui.Accent;
            link.SetBounds(24, 238, 472, 22);
            link.LinkClicked += delegate
            {
                try { Process.Start(RepoUrl); } catch (Exception) { }
            };
            Label credits = new Label();
            credits.Text = L.T("about_credits");
            credits.ForeColor = Ui.Muted;
            credits.Tag = "muted";
            credits.SetBounds(24, 276, 472, 74);
            Button close = Btn(L.T("close"), delegate { dlg.Close(); }, 1);
            close.Location = new Point(396, 354);
            dlg.AcceptButton = close;
            dlg.Controls.AddRange(new Control[] { name, by, what, ai, open, link, credits, close });
            ShowThemed(dlg);
        }

        void ShowThemed(Form dlg)
        {
            Ui.Theme(dlg);
            Ui.FixLabels(dlg);
            dlg.Shown += delegate { Ui.TitleBar(dlg); Ui.Theme(dlg); Ui.FixLabels(dlg); };
            dlg.ShowDialog(this);
            dlg.Dispose();
        }

        void UpdateFolderLabel()
        {
            string p = txtFolder.Text.Trim();
            lblFolder.Text = L.T("folder_lbl", "p", p.Length > 0 ? p : L.T("folder_none"));
        }

        void CenterEmpty()
        {
            if (emptyLbl == null) return;
            emptyLbl.Left = Math.Max(0, (lv.ClientSize.Width - emptyLbl.Width) / 2);
            emptyLbl.Top = Math.Max(30, (lv.ClientSize.Height - emptyLbl.Height) / 2);
        }

        // ---- utilidades de interfaz
        static Label Lbl(string text)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.Anchor = AnchorStyles.Left;
            return l;
        }

        static Button Btn(string text, EventHandler h)
        {
            return Btn(text, h, 0);
        }

        static Button Btn(string text, EventHandler h, int kind)
        {
            Button b = new Button();
            b.Text = text;
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.MinimumSize = new Size(0, 32);
            b.Padding = new Padding(10, 0, 10, 0);
            b.Margin = new Padding(0, 0, 8, 0);
            b.Click += h;
            Ui.Style(b, kind);
            return b;
        }

        static FlowLayoutPanel Row(params Control[] cs)
        {
            FlowLayoutPanel f = new FlowLayoutPanel();
            f.AutoSize = true;
            f.WrapContents = true;
            f.Margin = new Padding(0, 0, 0, 6);
            f.Controls.AddRange(cs);
            return f;
        }

        void StyleDialog(Form d)
        {
            d.Font = Font;
            d.BackColor = Ui.Bg;
            d.ForeColor = Ui.Text;
        }

        void SetStatus(string s) { status.Text = s; }

        void Info(string msg)
        {
            MessageBox.Show(msg, App, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void TipsDialog()
        {
            Form dlg = new Form();
            StyleDialog(dlg);
            dlg.BackColor = Ui.Card;
            dlg.Text = L.T("tips_title");
            dlg.ClientSize = new Size(820, 640);
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.ShowInTaskbar = false;
            dlg.MinimizeBox = false;

            RichTextBox rt = new RichTextBox();
            rt.ReadOnly = true;
            rt.BorderStyle = BorderStyle.None;
            rt.BackColor = Ui.Card;
            rt.ForeColor = Ui.Text;
            rt.Dock = DockStyle.Fill;
            rt.TabStop = false;
            rt.DetectUrls = false;
            rt.ScrollBars = RichTextBoxScrollBars.Vertical;
            Font normal = new Font("Segoe UI", 10.5f);
            Font head = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            Font code = new Font("Consolas", 10.5f);
            Font title = new Font("Segoe UI", 15f, FontStyle.Bold);
            bool first = true;
            foreach (string raw in Help.Get().Replace("\r\n", "\n").Split('\n'))
            {
                bool isHead = raw.Trim().Length > 0 && !raw.StartsWith(" ") && raw == raw.ToUpperInvariant() &&
                              raw != raw.ToLowerInvariant();
                bool isCode = raw.StartsWith("   ") && (raw.Contains(".mkv") || raw.Contains(".mp4"));
                int start = rt.TextLength;
                rt.AppendText(raw + "\n");
                rt.Select(start, raw.Length);
                rt.SelectionFont = isHead ? (first ? title : head) : (isCode ? code : normal);
                rt.SelectionColor = isHead ? Ui.AccentText : Ui.Text;
                first = false;
            }
            rt.SelectionStart = 0;
            rt.SelectionLength = 0;

            Panel pad = new Panel();
            pad.Dock = DockStyle.Fill;
            pad.Padding = new Padding(24, 18, 16, 8);
            pad.BackColor = Ui.Card;
            pad.Controls.Add(rt);

            Panel foot = new Panel();
            foot.Dock = DockStyle.Bottom;
            foot.Height = 56;
            foot.BackColor = Ui.Bg;
            Button close = Btn(L.T("close"), delegate { dlg.Close(); }, 1);
            close.Location = new Point(dlg.ClientSize.Width - 120, 12);
            close.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            foot.Controls.Add(close);

            dlg.Controls.Add(pad);
            dlg.Controls.Add(foot);
            dlg.AcceptButton = close;
            dlg.Shown += delegate { dlg.ActiveControl = close; };   // el texto no queda seleccionado en azul
            ShowThemed(dlg);
        }

        void SyncCfg()
        {
            cfg.Template = txtTemplate.Text;
            cfg.TvTemplate = txtTvTemplate.Text;
            cfg.Recursive = chkRecursive.Checked;
            cfg.FolderPerMovie = chkFolder.Checked;
            string ui = Config.LangCode(cmbLang.Text);   // lo elegido (p. ej. valenciano) y el codigo real para las APIs
            if (ui.Length == 0) ui = "es-ES";
            cfg.LanguageUi = ui;
            cfg.Language = Config.ApiLanguage(ui);
            string pv = cmbPrimary.SelectedItem != null ? (string)cmbPrimary.SelectedItem : "TMDb";
            cfg.Primary = pv == L.T("all") ? "Todas" : pv;
            if (txtFolder.Text.Trim().Length > 0) cfg.LastDir = txtFolder.Text.Trim();
            cfg.Save();
        }

        List<int> SelectedIdx()
        {
            List<int> l = new List<int>();
            foreach (int i in lv.SelectedIndices) l.Add(i);
            return l;
        }

        string[] RowData(Item it, out Color col)
        {
            string nw = "";
            string st;
            col = SystemColors.WindowText;
            Plan plan = it.Cands.Count > 0 ? Files.PlanTarget(it, txtTemplate.Text, chkFolder.Checked, txtTvTemplate.Text) : null;
            if (it.Done) { st = L.T("st_done"); col = Color.FromArgb(27, 127, 59); }
            else if (it.Skip) { st = L.T("st_skipped"); col = Color.Gray; }
            else if (plan != null)
            {
                nw = Files.Relative(Path.GetDirectoryName(it.FullPath), plan.Dst);
                int sel = it.Sel < it.Cands.Count ? it.Sel : 0;
                string why = Files.ReviewReason(it);
                if (why.Length > 0)
                {
                    st = L.T("st_review", "why", why, "s", it.Cands[sel].Source);
                    col = Color.FromArgb(179, 107, 0);
                }
                else st = L.T("st_ok", "s", it.Cands[sel].Source);
            }
            else if (it.Cands.Count > 0)
            {
                string why2 = Files.ReviewReason(it);
                st = L.T("st_review", "why", why2.Length > 0 ? why2 : L.T("st_noname"), "s", it.Cands[it.Sel < it.Cands.Count ? it.Sel : 0].Source);
                col = Color.FromArgb(179, 107, 0);
            }
            else if (it.Searched) { st = L.T("st_noresult"); col = Color.Firebrick; }
            else if (searchRunning)
            {
                if (it == searchingItem) { st = L.T("st_searching"); col = Ui.Accent; }
                else { st = L.T("st_queued"); col = Ui.Muted; }
            }
            else st = L.T("st_pending");
            if (renameSet != null && renameSet.Contains(it) && !it.Done)
            {
                if (it == renamingItem) { st = L.T("st_renaming"); col = Ui.Accent; }
                else { st = L.T("st_queued"); col = Ui.Muted; }
            }
            return new string[] { Path.GetFileName(it.FullPath), nw, st, it.Done && PerFileMode() ? L.T("show_folder") : "" };
        }

        void RefreshRows()
        {
            HashSet<int> sel = new HashSet<int>(SelectedIdx());
            lv.BeginUpdate();
            lv.Items.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                Color col;
                string[] data = RowData(items[i], out col);
                ListViewItem lvi = new ListViewItem(data);
                lvi.ForeColor = col;
                lv.Items.Add(lvi);
                if (sel.Contains(i)) lvi.Selected = true;
            }
            lv.EndUpdate();
            emptyLbl.Visible = items.Count == 0;
            CenterEmpty();
            UpdateNav();
            UpdateSummary();
        }

        void UpdateRow(int i)
        {
            if (i < 0 || i >= items.Count || i >= lv.Items.Count) return;
            Color col;
            string[] data = RowData(items[i], out col);
            ListViewItem lvi = lv.Items[i];
            for (int k = 0; k < 4; k++) lvi.SubItems[k].Text = data[k];
            lvi.ForeColor = col;
        }

        void Browse()
        {
            if (busy) return;
            using (FolderBrowserDialog fb = new FolderBrowserDialog())
            {
                if (Directory.Exists(txtFolder.Text)) fb.SelectedPath = txtFolder.Text;
                else if (cfg.LastDir.Length > 0 && Directory.Exists(cfg.LastDir)) fb.SelectedPath = cfg.LastDir;
                if (fb.ShowDialog(this) == DialogResult.OK)
                {
                    txtFolder.Text = fb.SelectedPath;
                    excluded.Clear();
                    LoadFolder();
                }
            }
        }

        Item MakeItem(string p, bool manual)
        {
            string stem = Path.GetFileNameWithoutExtension(p);
            string t, y;
            Names.ParseName(stem, out t, out y);
            Item it = new Item();
            it.FullPath = p;
            it.Title = t;
            it.Year = y;
            it.ImdbId = Names.FindImdbId(stem);
            it.Hint = Names.MakeHint(stem, Path.GetDirectoryName(p));
            it.Manual = manual;
            if (string.IsNullOrEmpty(it.Year) && it.Hint != null) it.Year = it.Hint.Year;
            return it;
        }

        // Lista = archivos de la carpeta (menos los quitados) + archivos sueltos anadidos a mano
        bool LoadFolder()
        {
            string folder = txtFolder.Text.Trim();
            bool hasFolder = folder.Length > 0 && Directory.Exists(folder);
            if (manualFiles.Count == 0)
            {
                if (folder.Length == 0)
                {
                    MessageBox.Show(L.T("pick_any"), App, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                if (!hasFolder)
                {
                    MessageBox.Show(L.T("no_folder"), App, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            items.Clear();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (hasFolder)
            {
                foreach (string p in Files.ScanVideos(folder, chkRecursive.Checked))
                {
                    if (excluded.Contains(p)) continue;
                    items.Add(MakeItem(p, false));
                    seen.Add(p);
                }
            }
            foreach (string m in manualFiles)
            {
                if (File.Exists(m) && seen.Add(m)) items.Add(MakeItem(m, true));
            }
            RefreshRows();
            SetStatus(L.T("n_videos", "n", items.Count));
            return true;
        }

        void AddFilesDialog()
        {
            if (busy) return;
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = L.T("add_title");
                ofd.Multiselect = true;
                ofd.Filter = L.T("ft_video") + "|*.mkv;*.mp4;*.avi;*.mov;*.wmv;*.m4v;*.mpg;*.mpeg;*.ts;*.flv;*.webm|" + L.T("ft_all") + "|*.*";
                if (Directory.Exists(txtFolder.Text.Trim())) ofd.InitialDirectory = txtFolder.Text.Trim();
                if (ofd.ShowDialog(this) == DialogResult.OK) AddPaths(ofd.FileNames);
            }
        }

        // Anade archivos (o carpetas, con sus videos) a la lista sin tocar los resultados ya buscados
        void AddPaths(string[] paths)
        {
            if (busy) return;
            HashSet<string> known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Item x in items) known.Add(x.FullPath);
            int added = 0;
            int ignored = 0;
            foreach (string p in paths)
            {
                List<string> files = new List<string>();
                if (Directory.Exists(p)) files.AddRange(Files.ScanVideos(p, chkRecursive.Checked));
                else if (File.Exists(p)) files.Add(p);
                foreach (string f in files)
                {
                    if (!Files.IsVideo(f)) { ignored++; continue; }
                    excluded.Remove(f);
                    if (!known.Add(f)) continue;
                    manualFiles.Add(f);
                    items.Add(MakeItem(f, true));
                    added++;
                }
            }
            RefreshRows();
            string msg = L.T("added", "a", added, "n", items.Count);
            if (ignored > 0) msg += L.T("ignored", "i", ignored);
            SetStatus(msg);
        }

        void RemoveSelected()
        {
            if (busy) return;
            List<int> sel = SelectedIdx();
            if (sel.Count == 0) { Info(L.T("sel_many")); return; }
            sel.Sort();
            for (int k = sel.Count - 1; k >= 0; k--)
            {
                Item it = items[sel[k]];
                string pth = it.FullPath;
                manualFiles.RemoveAll(delegate(string m) { return string.Equals(m, pth, StringComparison.OrdinalIgnoreCase); });
                if (!it.Manual) excluded.Add(pth);
                items.RemoveAt(sel[k]);
            }
            RefreshRows();
            SetStatus(L.T("removed", "r", sel.Count, "n", items.Count));
        }

        // ---- busqueda
        void StartSearch()
        {
            if (busy) return;
            SyncCfg();
            bool any = false;
            foreach (string s in Api.Order) if (Api.Available(s, cfg)) any = true;
            if (!any)
            {
                Info(L.T("need_source"));
                SettingsDialog();
                return;
            }
            if (!LoadFolder() || items.Count == 0) return;

            busy = true;
            cancelRequested = false;
            Api.BeginOperation();
            btnSearch.Enabled = false;
            UpdateNav();
            btnCancel.Enabled = true;
            List<Item> snapshot = new List<Item>(items);
            Config c = cfg;
            searchRunning = true;
            rowTimer.Start();
            searchingItem = null;
            btnSearch.Text = L.T("btn_searching");
            bannerLbl.Text = L.T("searching_banner", "i", 1, "n", snapshot.Count);
            searchBar.Maximum = Math.Max(1, snapshot.Count);
            searchBar.Value = 0;
            RefreshRows();

            Thread t = new Thread(delegate()
            {
                HashSet<string> allErrors = new HashSet<string>();
                try
                {
                    for (int i = 0; i < snapshot.Count; i++)
                    {
                        if (cancelRequested) break;
                        Item it = snapshot[i];
                        int rowIdx = i;
                        BeginInvoke(new Action(delegate
                        {
                            searchingItem = snapshot[rowIdx];
                            bannerLbl.Text = L.T("searching_banner", "i", rowIdx + 1, "n", snapshot.Count);
                            UpdateRow(rowIdx);
                        }));
                        List<string> errs = new List<string>();
                        List<Candidate> cands = new List<Candidate>();
                        try
                        {
                            cands = Api.FindForItem(it, c, errs);   // ID en el nombre (exacto), peliculas y series
                            if (cands.Count > 0)
                            {
                                for (int k = 0; k < cands.Count && k < 4; k++) Api.LocalizeOne(cands[k], c);   // idioma forzado antes de comparar
                                Item cur = it;
                                cands = cands.OrderBy(x => Names.RankKey(cur, x)).ToList();   // estable: mejor parecido primero
                                Api.Enrich(cands[0], c);
                            }
                        }
                        catch (Exception ex) { errs.Add(ex.Message); }
                        if (cancelRequested) break;   // cancelado a medias: este archivo se queda pendiente
                        foreach (string e in errs) allErrors.Add(e);
                        it.Cands = cands;
                        it.Sel = 0;
                        it.Searched = true;
                        int n = i;
                        BeginInvoke(new Action(delegate
                        {
                            SetStatus(L.T("searching", "i", n + 1, "n", snapshot.Count));
                            if (n + 1 <= searchBar.Maximum) searchBar.Value = n + 1;
                            UpdateRow(n);
                        }));
                    }
                    bool wasCancelled = cancelRequested;
                    BeginInvoke(new Action(delegate { FinishSearch(allErrors, wasCancelled); }));
                }
                catch (Exception) { }
            });
            t.IsBackground = true;
            t.Start();
        }

        void FinishSearch(HashSet<string> errors, bool cancelled)
        {
            busy = false;
            searchRunning = false;
            if (renameSet == null) rowTimer.Stop();
            searchingItem = null;
            searchBanner.Visible = false;
            btnSearch.Text = L.T("btn_search");
            btnSearch.Enabled = true;
            btnCancel.Enabled = false;
            spinner.Running = false;
            RefreshRows();
            int miss = 0;
            int pending = 0;
            foreach (Item it in items)
            {
                if (!it.Searched) pending++;
                else if (it.Cands.Count == 0) miss++;
            }
            string msg = L.T("search_done", "ok", items.Count - miss - pending, "miss", miss);
            if (cancelled)
                msg = L.T("search_cancel", "p", items.Count - pending, "n", items.Count, "pend", pending,
                          "ok", items.Count - pending - miss, "miss", miss);
            if (cfg.TmdbKey.Length == 0 && cfg.TvdbKey.Length == 0 &&
                !cfg.Language.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                bool foreign = false;
                foreach (Item row in items)
                    foreach (Candidate cd in row.Cands)
                        if (cd.Source == "IMDb" || cd.Source == "OMDb") foreign = true;
                if (foreign) msg += L.T("warn_lang");
            }
            if (errors.Count > 0)
            {
                string e = string.Join("; ", errors.OrderBy(x => x).ToArray());
                if (e.Length > 200) e = e.Substring(0, 200);
                msg += L.T("errors_pfx") + e;
            }
            SetStatus(msg);
        }

        void ToggleSkip()
        {
            foreach (int i in SelectedIdx()) items[i].Skip = !items[i].Skip;
            RefreshRows();
        }

        static string Describe(Candidate c)
        {
            string r = c.Rating > 0 ? " ★" + c.Rating.ToString("0.0", CultureInfo.InvariantCulture) : "";
            if (c.IsTv)
            {
                string epd = c.Ep.Length > 0 ? "  " + Names.EpisodeCode(c) + " " + c.EpTitle : "  " + L.T("ep_missing");
                return L.T("series_pfx") + ": " + c.Title + " (" + c.Year + ")" + epd + r + "  [" + c.Source + "]";
            }
            return c.Title + " (" + c.Year + ")" + r + "  [" + c.Source + "]";
        }

        // ---- elegir coincidencia
        void PickDialog()
        {
            List<int> sel = SelectedIdx();
            if (sel.Count == 0) { Info(L.T("sel_one")); return; }
            if (busy) { Info(L.T("wait_search")); return; }
            SyncCfg();
            Item it = items[sel[0]];

            Form dlg = new Form();
            StyleDialog(dlg);
            dlg.Text = L.T("pick_title");
            dlg.ClientSize = new Size(640, 384);
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.ShowInTaskbar = false;

            Label lbl = new Label();
            lbl.Text = Path.GetFileName(it.FullPath);
            lbl.AutoEllipsis = true;
            lbl.SetBounds(8, 8, 624, 20);
            TextBox txt = new TextBox();
            txt.SetBounds(8, 34, 540, 24);
            txt.Text = it.Title + (string.IsNullOrEmpty(it.Year) ? "" : " " + it.Year);
            Button bSearch = new Button();
            bSearch.Text = L.T("pick_search");
            bSearch.SetBounds(556, 32, 76, 26);
            ListBox lb = new ListBox();
            lb.SetBounds(8, 66, 624, 270);
            Button bOk = new Button();
            bOk.Text = L.T("pick_use");
            bOk.SetBounds(232, 346, 176, 30);
            Ui.Style(bSearch, 0);
            Ui.Style(bOk, 1);

            List<Candidate> list = new List<Candidate>(it.Cands);
            int pref = it.Sel;

            Action fill = delegate
            {
                lb.Items.Clear();
                foreach (Candidate c in list) lb.Items.Add(Describe(c));
                if (list.Count > 0) lb.SelectedIndex = pref < list.Count ? pref : 0;
            };

            bSearch.Click += delegate
            {
                string q = txt.Text.Trim();
                List<string> errs = new List<string>();
                List<Candidate> cands;
                Cursor old = dlg.Cursor;
                dlg.Cursor = Cursors.WaitCursor;
                try
                {
                    string imdb = Names.FindImdbId(q);   // acepta "tt0133093" o una URL de imdb.com
                    if (imdb.Length > 0) cands = Api.LookupByImdb(imdb, cfg, errs);
                    else
                    {
                        Hint dh = Names.MakeHint(q, null);
                        if (dh == null && it.Hint != null)   // busqueda manual dentro de una serie: conserva temporada/episodio
                        {
                            dh = new Hint();
                            dh.Series = q;
                            dh.Year = it.Hint.Year;
                            dh.Season = it.Hint.Season;
                            dh.Ep = it.Hint.Ep;
                            dh.Ep2 = it.Hint.Ep2;
                            dh.EpName = it.Hint.EpName;
                        }
                        string y = null;
                        Match m = Regex.Match(q, @"(?<!\d)(19\d{2}|20\d{2})\s*$");
                        if (m.Success) { y = m.Groups[1].Value; q = q.Substring(0, m.Index).Trim(); }
                        cands = Api.SearchAll(q, y, dh, cfg, true, errs);
                    }
                }
                finally { dlg.Cursor = old; }
                list = cands;
                pref = 0;
                fill();
                if (errs.Count > 0)
                    MessageBox.Show(dlg, string.Join(Environment.NewLine, errs.ToArray()), App, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };

            Action accept = delegate
            {
                int idx = lb.SelectedIndex;
                if (idx < 0) return;
                Cursor old = dlg.Cursor;
                dlg.Cursor = Cursors.WaitCursor;
                try { Api.Enrich(list[idx], cfg); }
                finally { dlg.Cursor = old; }
                it.Cands = list;
                it.Sel = idx;
                it.Searched = true;
                it.Skip = false;
                it.Confirmed = true;   // elegida a mano: ya no se marca para revisar
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };
            bOk.Click += delegate { accept(); };
            lb.DoubleClick += delegate { accept(); };

            dlg.AcceptButton = bSearch;
            dlg.Controls.AddRange(new Control[] { lbl, txt, bSearch, lb, bOk });
            Cursor oldCur = Cursor;
            Cursor = Cursors.WaitCursor;
            try { foreach (Candidate lc in list) Api.LocalizeOne(lc, cfg); }   // idioma forzado en toda la lista
            finally { Cursor = oldCur; }
            fill();
            ShowThemed(dlg);
            RefreshRows();
        }

        void OpenImdb()
        {
            List<int> sel = SelectedIdx();
            if (sel.Count == 0) { Info(L.T("sel_one")); return; }
            Item it = items[sel[0]];
            string url = "";
            if (it.Cands.Count > 0)
            {
                Candidate c = it.Cands[it.Sel < it.Cands.Count ? it.Sel : 0];
                Cursor = Cursors.WaitCursor;
                try { Api.Enrich(c, cfg); } finally { Cursor = Cursors.Default; }
                if (c.ImdbId.Length > 0) url = "https://www.imdb.com/title/" + c.ImdbId + "/";
            }
            if (url.Length == 0) url = "https://www.imdb.com/find/?s=tt&q=" + Uri.EscapeDataString(it.Title);
            try { Process.Start(url); }
            catch (Exception e) { MessageBox.Show(e.Message, App, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        // ---- renombrar / deshacer
        void SetProgress(int v, string msg)
        {
            if (v > searchBar.Maximum) v = searchBar.Maximum;
            if (v < 0) v = 0;
            searchBar.Value = v;
            bannerLbl.Text = msg;
        }

        void DoApply()
        {
            if (busy) { Info(L.T("wait_op")); return; }
            int ignored;
            List<Item> all = Files.PlanBatch(items, txtTemplate.Text, chkFolder.Checked, txtTvTemplate.Text,
                                             int.MaxValue, out ignored);
            if (all.Count == 0) { Info(L.T("nothing")); return; }
            int n = all.Count;
            int nb = (n + Files.MaxRenames - 1) / Files.MaxRenames;
            int nRev = all.Count(x => Files.ReviewReason(x).Length > 0);
            string nl = Environment.NewLine + Environment.NewLine;
            string question = L.T("q_rename", "n", n);
            if (nb > 1) question += L.T("q_batches", "nb", nb, "m", Files.MaxRenames);
            if (nRev > 0) question += L.T("q_review", "r", nRev);
            if (MessageBox.Show(question, App, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            SyncCfg();

            busy = true;
            cancelRequested = false;
            Api.BeginOperation();
            btnApply.Enabled = false;
            btnSearch.Enabled = false;
            UpdateNav();
            btnCancel.Enabled = true;
            bannerLbl.Text = L.T("renaming_banner", "i", 1, "n", n);
            searchBar.Maximum = Math.Max(1, n);
            searchBar.Value = 0;
            renameSet = new HashSet<Item>(all);
            renamingItem = null;
            rowTimer.Start();
            RefreshRows();
            string tpl = txtTemplate.Text;
            string tvt = txtTvTemplate.Text;
            bool fpm = chkFolder.Checked;
            Config c = cfg;

            Thread t = new Thread(delegate()
            {
                List<string[]> logAll = new List<string[]>();
                List<string> errorsAll = new List<string>();
                int doneTotal = 0;
                try
                {
                    for (int b = 0; b < nb && !cancelRequested; b++)
                    {
                        int bi = b + 1;
                        int baseCount = b * Files.MaxRenames;
                        List<Item> batch = all.Skip(baseCount).Take(Files.MaxRenames).ToList();
                        for (int i = 0; i < batch.Count && !cancelRequested; i++)
                        {
                            Item it = batch[i];
                            try { Api.Enrich(it.Cands[it.Sel < it.Cands.Count ? it.Sel : 0], c); }   // IMDb ID, rating...
                            catch (Exception) { }
                            int k = i + 1;
                            int m = batch.Count;
                            BeginInvoke(new Action(delegate { bannerLbl.Text = L.T("preparing", "b", bi, "nb", nb, "k", k, "m", m); }));
                        }
                        List<string> errs;
                        int done;
                        int pend;
                        List<string[]> log = Files.ApplyRenames(batch, tpl, fpm, tvt, Files.MaxRenames,
                            delegate(int a, int tot)
                            {
                                BeginInvoke(new Action(delegate
                                {
                                    int gi = baseCount + a;
                                    Item prev = renamingItem;
                                    renamingItem = gi < all.Count ? all[gi] : null;
                                    if (prev != null) UpdateRow(items.IndexOf(prev));
                                    if (renamingItem != null) UpdateRow(items.IndexOf(renamingItem));
                                    SetProgress(baseCount + a, L.T("renaming_banner", "i", baseCount + a + 1, "n", n));
                                }));
                            },
                            delegate { return cancelRequested; },
                            out errs, out done, out pend);
                        logAll.AddRange(log);
                        errorsAll.AddRange(errs);
                        doneTotal += done;
                        if (logAll.Count > 0)   // se guarda tras cada tanda: si se cierra a medias, sigue siendo deshacible
                        {
                            try { Files.SaveUndo(logAll); }
                            catch (Exception ex) { errorsAll.Add(L.T("undo_save_err", "e", ex.Message)); }
                        }
                        BeginInvoke(new Action(delegate { RefreshRows(); }));
                    }
                }
                catch (Exception ex2) { errorsAll.Add(ex2.Message); }
                bool wasCancelled = cancelRequested;
                BeginInvoke(new Action(delegate { FinishApply(errorsAll, doneTotal, n, wasCancelled); }));
            });
            t.IsBackground = true;
            t.Start();
        }

        void FinishApply(List<string> errors, int done, int total, bool cancelled)
        {
            busy = false;
            renameSet = null;
            renamingItem = null;
            if (!searchRunning) rowTimer.Stop();
            btnApply.Enabled = true;
            btnSearch.Enabled = true;
            btnCancel.Enabled = false;
            manualFiles = items.Where(x => x.Manual).Select(x => x.FullPath).ToList();   // rutas nuevas tras renombrar
            RefreshRows();
            string msg;
            if (cancelled)
                msg = L.T("fin_cancel", "d", done, "t", total, "l", Math.Max(0, total - done - errors.Count));
            else if (errors.Count > 0)
                msg = L.T("fin_some", "d", done, "t", total, "f", errors.Count);
            else
            {
                msg = L.T("fin_ok", "d", done);
            }
            SetStatus(msg);
            if (done > 0 || cancelled)
                MessageBox.Show(msg + L.T("fin_undo_hint"), App,
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (errors.Count > 0)
                MessageBox.Show(string.Join(Environment.NewLine, errors.Take(15).ToArray()), App,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            spinner.Running = false;
            searchBanner.Visible = false;
        }

        void DoUndo()
        {
            if (busy) { Info(L.T("wait_search")); return; }
            if (MessageBox.Show(L.T("q_undo"), App,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            List<string> errors;
            List<string[]> restored;
            int n = Files.UndoLast(out errors, out restored);
            SetStatus(L.T("undone", "n", n));
            if (errors.Count > 0)
                MessageBox.Show(string.Join(Environment.NewLine, errors.Take(15).ToArray()), App,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            foreach (string[] pr in restored)   // los archivos sueltos vuelven a su ruta anterior
            {
                string nw = pr[0];
                string old = pr[1];
                for (int k = 0; k < manualFiles.Count; k++)
                    if (string.Equals(manualFiles[k], nw, StringComparison.OrdinalIgnoreCase)) manualFiles[k] = old;
            }
            if (Directory.Exists(txtFolder.Text.Trim()) || manualFiles.Count > 0) LoadFolder();
        }

        // ---- ajustes
        static Label DLbl(string text, int x, int y, int w)
        {
            Label l = new Label();
            l.Text = text;
            l.SetBounds(x, y + 4, w, 20);
            return l;
        }

        static TextBox DTxt(string text, int x, int y, int w, bool secret)
        {
            TextBox t = new TextBox();
            t.SetBounds(x, y, w, 24);
            t.UseSystemPasswordChar = secret;
            t.Text = text;
            return t;
        }

        static Label DNote(string text, int x, int y, int w)
        {
            Label l = new Label();
            l.Text = text;
            l.ForeColor = Ui.Muted;
            l.Tag = "muted";
            l.SetBounds(x, y, w, 18);
            return l;
        }

        static LinkLabel KeyLink(string text, string url, int x, int y)
        {
            LinkLabel l = new LinkLabel();
            l.Text = text;
            l.AutoSize = true;
            l.Location = new Point(x, y);
            l.LinkColor = Ui.AccentText;
            l.ActiveLinkColor = Ui.Accent;
            l.LinkClicked += delegate
            {
                try { Process.Start(url); } catch (Exception) { }
            };
            return l;
        }

        void SettingsDialog()
        {
            bool restart = false;
            Form dlg = new Form();
            StyleDialog(dlg);
            dlg.Text = L.T("set_title");
            dlg.ClientSize = new Size(600, 604);
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.ShowInTaskbar = false;

            GroupBox g1 = new GroupBox();
            g1.Text = L.T("set_keys");
            g1.SetBounds(12, 8, 576, 214);
            TextBox t1 = DTxt(cfg.TmdbKey, 216, 24, 346, true);
            TextBox t2 = DTxt(cfg.OmdbKey, 216, 56, 346, true);
            TextBox t3 = DTxt(cfg.TvdbKey, 216, 88, 346, true);
            TextBox t4 = DTxt(cfg.TvdbPin, 216, 120, 346, true);
            TextBox t5 = DTxt(cfg.OmdbDailyLimit.ToString(CultureInfo.InvariantCulture), 216, 152, 80, false);
            g1.Controls.AddRange(new Control[] {
                DLbl(L.T("set_tmdb"), 12, 24, 200), t1, DLbl(L.T("set_omdb"), 12, 56, 200), t2,
                DLbl(L.T("set_tvdb"), 12, 88, 200), t3, DLbl(L.T("set_pin"), 12, 120, 200), t4,
                DLbl(L.T("set_omdb_lim"), 12, 152, 200), t5, DNote(L.T("set_links"), 12, 187, 110),
                KeyLink("TMDb", "https://www.themoviedb.org/settings/api", 124, 186),
                KeyLink("OMDb", "https://www.omdbapi.com/apikey.aspx", 190, 186),
                KeyLink("TheTVDB", "https://thetvdb.com/api-information", 258, 186) });

            GroupBox g2 = new GroupBox();
            g2.Text = L.T("set_names");
            g2.SetBounds(12, 228, 576, 132);
            TextBox tp = DTxt(txtTemplate.Text, 216, 24, 346, false);
            TextBox tq = DTxt(txtTvTemplate.Text, 216, 76, 346, false);
            g2.Controls.AddRange(new Control[] {
                DLbl(L.T("set_tpl_movie"), 12, 24, 200), tp,
                DNote("{title} {original_title} {year} {imdb_id} {tmdb_id} {tvdb_id} {rating}", 216, 50, 350),
                DLbl(L.T("set_tpl_tv"), 12, 76, 200), tq,
                DNote("{series} {year} {season} {episode} {sxe} {ep_title} {imdb_id} {tmdb_id}", 216, 102, 350) });

            GroupBox g3 = new GroupBox();
            g3.Text = L.T("set_opts");
            g3.SetBounds(12, 366, 576, 186);
            CheckBox c1 = new CheckBox();
            c1.Text = L.T("set_recursive");
            c1.SetBounds(12, 22, 550, 22);
            c1.Checked = chkRecursive.Checked;
            CheckBox c2 = new CheckBox();
            c2.Text = L.T("set_folder_movie");
            c2.SetBounds(12, 46, 550, 22);
            c2.Checked = chkFolder.Checked;
            CheckBox c3 = new CheckBox();
            c3.Text = L.T("set_imdb");
            c3.SetBounds(12, 70, 550, 22);
            c3.Checked = cfg.UseImdb;
            ComboBox cu = new ComboBox();
            cu.DropDownStyle = ComboBoxStyle.DropDownList;
            cu.Items.AddRange(new object[] { "Español", "English" });
            cu.SelectedIndex = cfg.UiLang == "es" ? 0 : 1;
            cu.SetBounds(216, 100, 150, 24);
            ComboBox ct = new ComboBox();
            ct.DropDownStyle = ComboBoxStyle.DropDownList;
            ct.Items.AddRange(new object[] { L.T("theme_auto"), L.T("theme_light"), L.T("theme_dark") });
            ct.SelectedIndex = cfg.Theme == "light" ? 1 : (cfg.Theme == "dark" ? 2 : 0);
            ct.SetBounds(216, 130, 150, 24);
            g3.Controls.AddRange(new Control[] { c1, c2, c3, DLbl(L.T("set_ui"), 12, 100, 200), cu,
                DLbl(L.T("set_theme"), 12, 130, 200), ct,
                DNote(L.T("set_ui_restart"), 12, 160, 556) });

            Button ok = new Button();
            ok.Text = L.T("set_save");
            ok.SetBounds(250, 562, 100, 32);
            Ui.Style(ok, 1);
            ok.Click += delegate
            {
                cfg.TmdbKey = t1.Text.Trim();
                cfg.OmdbKey = t2.Text.Trim();
                cfg.TvdbKey = t3.Text.Trim();
                cfg.TvdbPin = t4.Text.Trim();
                cfg.UseImdb = c3.Checked;
                int newLim;
                cfg.OmdbDailyLimit = (int.TryParse(t5.Text.Trim(), out newLim) && newLim > 0) ? newLim : 1000;
                Usage.OmdbLimit = cfg.OmdbDailyLimit;
                cfg.UiLang = cu.SelectedIndex == 0 ? "es" : "en";
                restart = (cfg.UiLang == "en") != L.En;   // el idioma de la interfaz solo se aplica al arrancar
                txtTemplate.Text = tp.Text;
                txtTvTemplate.Text = tq.Text;
                chkRecursive.Checked = c1.Checked;
                chkFolder.Checked = c2.Checked;
                cfg.Theme = ct.SelectedIndex == 1 ? "light" : (ct.SelectedIndex == 2 ? "dark" : "auto");
                Ui.SetDark(Ui.Resolve(cfg.Theme));
                ApplyTheme();
                SyncCfg();
                dlg.Close();
            };
            dlg.AcceptButton = ok;
            dlg.Controls.AddRange(new Control[] { g1, g2, g3, ok });
            ShowThemed(dlg);
            if (restart && !busy) Application.Restart();   // guarda y vuelve a abrir con el idioma nuevo
        }

        // ---- bienvenida (solo la primera vez, sin claves)
        void WelcomeDialog()
        {
            Form dlg = new Form();
            StyleDialog(dlg);
            dlg.BackColor = Ui.Card;
            dlg.Text = L.T("wel_title");
            dlg.ClientSize = new Size(560, 520);
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.ShowInTaskbar = false;

            Label body = new Label();
            body.Text = L.T("wel_body");
            body.SetBounds(24, 20, 512, 340);
            body.Font = new Font("Segoe UI", 10.5f);
            Label wlegal = new Label();
            wlegal.Text = L.T("legal");
            wlegal.ForeColor = Ui.Muted;
            wlegal.Tag = "muted";
            wlegal.Font = new Font("Segoe UI", 8f);
            wlegal.SetBounds(24, 366, 512, 96);

            bool openSettings = false;
            Button bGet = new Button();
            bGet.Text = L.T("wel_get");
            bGet.SetBounds(16, 474, 190, 32);
            bGet.Click += delegate
            {
                try { Process.Start("https://www.themoviedb.org/settings/api"); } catch (Exception) { }
            };
            Button bSet = new Button();
            bSet.Text = L.T("wel_settings");
            bSet.SetBounds(214, 474, 150, 32);
            bSet.Click += delegate { openSettings = true; dlg.Close(); };
            Button bGo = new Button();
            bGo.Text = L.T("wel_start");
            bGo.SetBounds(372, 474, 172, 32);
            Ui.Style(bGo, 1);
            Ui.Style(bGet, 0);
            Ui.Style(bSet, 0);
            bGo.Click += delegate { dlg.Close(); };
            dlg.AcceptButton = bGo;
            dlg.Controls.AddRange(new Control[] { body, wlegal, bGet, bSet, bGo });
            ShowThemed(dlg);
            cfg.Welcomed = true;
            cfg.Save();
            if (openSettings) SettingsDialog();
        }
    }
}
