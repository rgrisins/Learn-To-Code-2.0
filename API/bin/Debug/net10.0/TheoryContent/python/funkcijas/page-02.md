# Parametri

Parametri ļauj funkcijai saņemt datus.

```python
def greet(name):
    print(f"Sveiki, {name}!")

greet("Anna")
greet("Juris")
```

Funkcijai var būt vairāki parametri:

```python
def add(a, b):
    print(a + b)

add(3, 5)
```

Parametri ir lokāli funkcijai. Tas nozīmē, ka `name` funkcijas iekšpusē nav tas pats, kas cits `name` ārpus funkcijas, ja vien tu to nepārsūti.

Var izmantot noklusējuma vērtības:

```python
def greet(name, greeting="Sveiki"):
    print(f"{greeting}, {name}!")

greet("Anna")
greet("Marta", "Labrīt")
```

Noklusējuma vērtības palīdz, ja lielākajā daļā gadījumu parametrs ir vienāds, bet dažreiz vajag to mainīt.
