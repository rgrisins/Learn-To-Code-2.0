# Iterēšana pa vārdnīcu

Pa vārdnīcu var iet vairākos veidos. Noklusēti cikls dod atslēgas.

```python
student = {"name": "Anna", "age": 17}

for key in student:
    print(key)
```

Ja vajag vērtības:

```python
for value in student.values():
    print(value)
```

Ja vajag gan atslēgu, gan vērtību, izmanto `items`:

```python
for key, value in student.items():
    print(f"{key}: {value}")
```

Praktisks piemērs ar punktiem:

```python
scores = {"Anna": 90, "Juris": 75, "Marta": 82}

for name, score in scores.items():
    if score >= 80:
        print(f"{name} saņēma augstu vērtējumu")
```

Vārdnīcas saglabā ievietošanas secību modernās Python versijās, bet tās galvenā ideja joprojām ir ātra piekļuve pēc atslēgas.
