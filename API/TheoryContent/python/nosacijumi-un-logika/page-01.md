# if, elif, else

Nosacījumi ļauj programmai izvēlēties, kuru kodu izpildīt. Python izmanto `if`, `elif` un `else`.

```python
score = 82

if score >= 90:
    print("Izcili")
elif score >= 70:
    print("Labi")
else:
    print("Jāpatrenējas")
```

Python pārbauda nosacījumus no augšas uz leju. Tiklīdz viens nosacījums ir patiess, pārējās alternatīvas netiek izpildītas.

Atkāpes ir obligātas. Viss, kas pieder `if` blokam, jāieraksta ar vienādu atkāpi.

```python
if score >= 50:
    print("Ieskaitīts")
    print("Rezultāts saglabāts")
```

Ja nav jāveic nekāda darbība, var izmantot `pass`, bet parasti labāk rakstīt kodu tā, lai tukši bloki nav vajadzīgi.
