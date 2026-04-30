# Tuple jeb kortežs

`tuple` ir līdzīgs sarakstam, bet tas nav maināms pēc izveides. To pieraksta ar apaļajām iekavām.

```python
point = (10, 20)
rgb = (255, 128, 0)
```

Kortežus izmanto, ja vērtību kopai jāpaliek stabilai. Piemēram, koordinātas, datuma daļas vai funkcijas rezultāts ar vairākām vērtībām.

```python
def get_name_parts():
    return ("Anna", "Ozola")

first_name, last_name = get_name_parts()
```

Šo sauc par atpakošanu. Tā strādā arī ar sarakstiem, bet kortežos tā bieži izskatās dabiskāk.

Vienas vērtības kortežam vajag komatu:

```python
single = ("Python",)
```

Bez komata Python to uztvertu tikai kā izteiksmi iekavās.

Izvēlies sarakstu, ja dati mainīsies. Izvēlies kortežu, ja vērtību grupa ir fiksēta un pēc nozīmes pieder kopā.
