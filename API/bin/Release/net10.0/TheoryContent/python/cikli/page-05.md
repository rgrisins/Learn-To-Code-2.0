# Uzkrāšana ciklā

Bieži ciklā vajag uzkrāt rezultātu: summu, skaitu, lielāko vērtību vai jaunu sarakstu.

```python
numbers = [3, 5, 2, 8]
total = 0

for number in numbers:
    total += number

print(total)
```

Skaitīšanas piemērs:

```python
scores = [40, 75, 90, 55]
passed = 0

for score in scores:
    if score >= 50:
        passed += 1

print(passed)
```

Ja veido jaunu sarakstu, sākumā izveido tukšu sarakstu un ciklā pievieno vērtības:

```python
names = ["anna", "juris", "marta"]
capitalized = []

for name in names:
    capitalized.append(name.capitalize())
```

Šī domāšana ir pamats daudzām programmām: sāc ar sākuma vērtību, ej cauri datiem un soli pa solim veido rezultātu.
