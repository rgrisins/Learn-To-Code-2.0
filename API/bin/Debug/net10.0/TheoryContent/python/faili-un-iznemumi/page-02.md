# Failu rakstīšana

Tekstu failā var ierakstīt ar `write_text`.

```python
from pathlib import Path

path = Path("result.txt")
path.write_text("Sveiki!\n", encoding="utf-8")
```

Šī darbība pārraksta failu, ja tas jau eksistē. Ja vēlies pievienot tekstu beigās, izmanto `open` ar režīmu `"a"`.

```python
with open("log.txt", "a", encoding="utf-8") as file:
    file.write("Programma palaista\n")
```

`with` nodrošina, ka fails tiek korekti aizvērts arī tad, ja notiek kļūda.

Ja raksti vairākas rindas, vari sagatavot sarakstu un savienot:

```python
lines = ["Anna", "Juris", "Marta"]
Path("names.txt").write_text("\n".join(lines), encoding="utf-8")
```

Rakstot failus, uzmanies ar ceļiem. Pārbaudi, kurā mapē programma tiek palaista, jo relatīvie ceļi tiek skaitīti no darba mapes.
