# Failu lasīšana

Python var lasīt tekstu no failiem. Moderns un ērts veids ir izmantot `pathlib`.

```python
from pathlib import Path

path = Path("notes.txt")
content = path.read_text(encoding="utf-8")
print(content)
```

`encoding="utf-8"` ir svarīgi, ja tekstā ir latviešu burti. Tas palīdz izvairīties no nepareizi attēlotiem simboliem.

Ja fails var neeksistēt, pirms lasīšanas vari pārbaudīt:

```python
if path.exists():
    print(path.read_text(encoding="utf-8"))
else:
    print("Fails nav atrasts")
```

Failus var lasīt arī pa rindām:

```python
for line in path.read_text(encoding="utf-8").splitlines():
    print(line)
```

Lieliem failiem izmanto straumēšanu ar `open`, lai neielādētu visu saturu atmiņā uzreiz.
