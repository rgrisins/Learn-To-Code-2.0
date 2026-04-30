# Dokumentēšana un tipi

Funkcijas sākumā var pievienot docstring - īsu aprakstu trīskāršajās pēdiņās.

```python
def calculate_total(price, tax_rate):
    """Atgriež cenu kopā ar nodokli."""
    return price + price * tax_rate
```

Docstring ir noderīgs, ja funkciju izmantos arī citi vai ja loģika nav acīmredzama.

Python ļauj pierakstīt tipu norādes:

```python
def add(a: int, b: int) -> int:
    return a + b
```

Tipu norādes pašas par sevi programmu neaptur, ja nodod nepareizu tipu, bet tās palīdz redaktoram, testiem un cilvēkiem saprast nodomu.

Vēl viens piemērs:

```python
def format_name(first_name: str, last_name: str) -> str:
    return f"{first_name} {last_name}"
```

Tipi nav jāliek visur no pirmās dienas, bet funkcijām ar skaidru ieeju un izeju tie padara kodu uzticamāku.
