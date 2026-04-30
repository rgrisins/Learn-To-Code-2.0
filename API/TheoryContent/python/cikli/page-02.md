# for cikls

`for` cikls iet cauri vērtību virknei. Tā var būt `range`, saraksts, teksts vai cits iterējams objekts.

```python
for number in range(1, 6):
    print(number)
```

Šis cikls izvada skaitļus no 1 līdz 5. `range` augšējā robeža netiek iekļauta.

Ar sarakstu:

```python
names = ["Anna", "Juris", "Marta"]

for name in names:
    print(f"Sveiki, {name}!")
```

Ar tekstu:

```python
word = "Python"

for letter in word:
    print(letter)
```

Ja vajag gan indeksu, gan vērtību, izmanto `enumerate`:

```python
for index, name in enumerate(names, start=1):
    print(f"{index}. {name}")
```

Tas ir lasāmāk nekā pašam uzturēt atsevišķu skaitītāju.
