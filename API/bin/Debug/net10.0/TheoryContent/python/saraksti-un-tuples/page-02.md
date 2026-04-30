# Indeksi un slicing

Saraksta elementi sākas ar indeksu `0`.

```python
colors = ["sarkans", "zaļš", "zils"]

print(colors[0])
print(colors[1])
print(colors[-1])
```

Negatīvs indekss skaita no beigām. `-1` ir pēdējais elements.

Slicing ļauj paņemt saraksta daļu:

```python
numbers = [1, 2, 3, 4, 5]

print(numbers[1:4])
print(numbers[:3])
print(numbers[3:])
```

Robeža labajā pusē netiek iekļauta. `numbers[1:4]` paņem elementus ar indeksiem 1, 2 un 3.

Slicing neveic izmaiņas sākotnējā sarakstā, tas izveido jaunu sarakstu ar izvēlētajiem elementiem.

```python
first_three = numbers[:3]
```

Ja indekss ir ārpus robežām, Python izmet kļūdu. Tāpēc pirms tiešas indeksēšanas pārliecinies, ka sarakstā ir pietiekami daudz elementu.
