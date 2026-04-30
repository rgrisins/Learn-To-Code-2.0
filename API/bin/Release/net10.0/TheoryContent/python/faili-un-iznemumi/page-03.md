# Ceļi un mapes

`pathlib.Path` ļauj strādāt ar ceļiem neatkarīgi no operētājsistēmas.

```python
from pathlib import Path

data_dir = Path("data")
file_path = data_dir / "students.txt"
```

Mapi var izveidot, ja tā vēl neeksistē:

```python
data_dir.mkdir(exist_ok=True)
```

Ja vajag izveidot arī vecākmapes:

```python
Path("output/reports").mkdir(parents=True, exist_ok=True)
```

Failu saraksts mapē:

```python
for path in Path("data").glob("*.txt"):
    print(path.name)
```

`path.name` dod faila nosaukumu, `path.suffix` dod paplašinājumu, bet `path.parent` dod vecākmapi.

Ceļus nevajag būvēt ar teksta savienošanu, piemēram, `"data/" + filename`. Izmanto `/` operatoru ar `Path`, jo tas korekti pielāgojas Windows, macOS un Linux ceļu pierakstam.
