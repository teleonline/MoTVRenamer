# MoTV Renamer 1.0.3

## English
- IMDb ID in the file name is now detected in any form: `[tt1234567]`, `(tt1234567)`, `"tt1234567"`, `{imdb-tt1234567}` or `imdbid-tt1234567`, and it is removed cleanly from the title.
- Episodes named only "Episodio 1", "Capítulo 2", "Episode 3", "Ep 4" or "Temporada 2 Episodio 5" (no S01E02) are now recognised; the series name is taken from the folder name (and the season from a "Season 02" folder).
- Special titles are respected: names like "F1: La película" or "Wall-E Pelotón de limpieza" keep their key word (F1, Wall-E) when searching and matching; accents and symbols are kept, and ":" in the new file name becomes " - " (e.g. "F1 - La película").
- Last step: after renaming, the "Back" button becomes "Start over" and begins again with an empty list.

## Español
- El ID de IMDb en el nombre del archivo se detecta en cualquier forma: `[tt1234567]`, `(tt1234567)`, `"tt1234567"`, `{imdb-tt1234567}` o `imdbid-tt1234567`, y se elimina limpiamente del título.
- Los episodios llamados solo "Episodio 1", "Capítulo 2", "Episode 3", "Ep 4" o "Temporada 2 Episodio 5" (sin S01E02) ahora se reconocen; el nombre de la serie se toma de la carpeta (y la temporada de una carpeta "Temporada 02").
- Se respetan los títulos especiales: nombres como "F1: La película" o "Wall-E Pelotón de limpieza" conservan su palabra clave (F1, Wall-E) al buscar y comparar; se mantienen acentos y símbolos, y ":" en el nuevo nombre pasa a " - " (p. ej. "F1 - La película").
- Último paso: tras renombrar, el botón "Anterior" pasa a "Empezar de nuevo" y comienza con la lista vacía.
