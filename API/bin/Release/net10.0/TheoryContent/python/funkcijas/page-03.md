# return

`return` ļauj funkcijai atdot rezultātu.

```python
def add(a, b):
    return a + b

total = add(3, 5)
print(total)
```

Atšķirība starp `print` un `return` ir būtiska. `print` parāda vērtību ekrānā, bet `return` nodod vērtību tālāk programmai.

Funkcija var atgriezt arī `None`, ja nav norādīts `return` vai `return` ir bez vērtības.

```python
def say_hi():
    print("Hi")

result = say_hi()
print(result)
```

Šis izdrukās `None`, jo funkcija neko neatgrieza.

Laba prakse ir atdalīt aprēķinus no izvades. Funkcija aprēķina un atgriež rezultātu, bet cita programmas daļa izlemj, kā to parādīt.

```python
def calculate_vat(price):
    return price * 0.21
```
