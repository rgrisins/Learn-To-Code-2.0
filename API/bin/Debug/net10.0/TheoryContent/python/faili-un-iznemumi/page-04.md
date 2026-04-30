# Izņēmumi

Izņēmums ir kļūda programmas izpildes laikā. Python ļauj to apstrādāt ar `try` un `except`.

```python
try:
    age = int(input("Vecums: "))
    print(age + 1)
except ValueError:
    print("Lūdzu ievadi veselu skaitli.")
```

Šeit `ValueError` rodas, ja tekstu nevar pārveidot par `int`.

Nevajag ķert visas kļūdas bez vajadzības:

```python
try:
    ...
except Exception:
    ...
```

Tas var noslēpt īsto problēmu. Labāk ķert konkrētu kļūdas tipu, kuru proti apstrādāt.

Var izmantot arī `else` un `finally`:

```python
try:
    number = int("42")
except ValueError:
    print("Kļūda")
else:
    print("Viss izdevās")
finally:
    print("Šis izpildās vienmēr")
```

Izņēmumu apstrāde palīdz programmām reaģēt saprotami, nevis vienkārši apstāties.
