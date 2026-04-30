# Sarakstu metodes

Sarakstiem ir metodes, kas palīdz pievienot, izņemt un sakārtot elementus.

```python
tasks = []
tasks.append("Izlasīt teoriju")
tasks.append("Atrisināt uzdevumu")
print(tasks)
```

`append` pievieno elementu beigās. `insert` ievieto noteiktā vietā:

```python
tasks.insert(0, "Atvērt projektu")
```

Elementu var izņemt pēc vērtības:

```python
tasks.remove("Izlasīt teoriju")
```

Vai pēc indeksa:

```python
last_task = tasks.pop()
```

Kārtošana:

```python
scores = [80, 45, 100, 70]
scores.sort()
print(scores)
```

`sort` maina esošo sarakstu. Ja gribi iegūt sakārtotu kopiju, izmanto `sorted`:

```python
sorted_scores = sorted(scores, reverse=True)
```

Izvēlies metodi pēc tā, vai drīksti mainīt sākotnējo sarakstu.
