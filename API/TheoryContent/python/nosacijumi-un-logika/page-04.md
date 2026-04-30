# Patiesuma vērtības praksē

Python daudzas vērtības var interpretēt kā patiesas vai aplamas. Tukšs teksts, `0`, tukšs saraksts un `None` tiek uzskatīti par aplamiem. Netukšas vērtības parasti ir patiesas.

```python
name = ""

if not name:
    print("Vārds nav ievadīts")
```

Tas ļauj rakstīt īsāku validāciju, bet jābūt uzmanīgam. Ja `0` ir derīga vērtība, nedrīkst to nejauši uztvert kā "nav ievadīts".

```python
count = 0

if count is None:
    print("Skaits nav zināms")
else:
    print(f"Skaits: {count}")
```

Labs nosacījums ir konkrēts. Ja pārbaudi `None`, raksti `is None`. Ja pārbaudi tukšu sarakstu, vari rakstīt `if not items`. Ja pārbaudi skaitli, salīdzini ar robežu.

Nosacījumu galvenais uzdevums ir padarīt programmas uzvedību paredzamu.
