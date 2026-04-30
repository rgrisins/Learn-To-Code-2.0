# List comprehension

List comprehension ir īss veids, kā izveidot jaunu sarakstu no esoša.

Parasts cikls:

```python
numbers = [1, 2, 3, 4]
squares = []

for number in numbers:
    squares.append(number ** 2)
```

Tas pats īsāk:

```python
squares = [number ** 2 for number in numbers]
```

Var pievienot nosacījumu:

```python
even_numbers = [number for number in numbers if number % 2 == 0]
```

List comprehension ir labs, ja pārveidošana ir īsa un skaidra. Ja loģika kļūst gara, parasts cikls būs lasāmāks.

Vēl viens piemērs:

```python
names = ["anna", "juris", "marta"]
display_names = [name.capitalize() for name in names]
```

Šo paņēmienu Python programmās redzēsi bieži, tāpēc ir vērts pierast to lasīt.
