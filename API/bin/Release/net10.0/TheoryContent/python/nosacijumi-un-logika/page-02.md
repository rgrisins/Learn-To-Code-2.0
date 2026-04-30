# Salīdzinājumi

Salīdzinājumi atgriež `True` vai `False`. Tos izmanto nosacījumos, ciklos un validācijā.

```python
age = 18

print(age == 18)
print(age != 18)
print(age > 18)
print(age >= 18)
print(age < 18)
print(age <= 18)
```

Svarīgi nejaukt `=` un `==`. Viena vienādības zīme piešķir vērtību, bet divas salīdzina.

Tekstu arī var salīdzināt:

```python
role = "admin"

if role == "admin":
    print("Pieeja atļauta")
```

Salīdzinot lietotāja ievadi, bieži vispirms noņem liekās atstarpes un pārveido burtus mazajos:

```python
answer = input("jā/nē: ").strip().lower()

if answer == "jā":
    print("Turpinām")
```

Šādi programma kļūst iecietīgāka pret dažādiem ievades variantiem.
