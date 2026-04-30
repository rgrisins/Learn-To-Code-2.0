# Mazi testi

Funkcijas ir viegli pārbaudīt, jo tām var iedot ievadi un salīdzināt rezultātu.

```python
def is_even(number):
    return number % 2 == 0

print(is_even(4))
print(is_even(5))
```

Vienkāršai pārbaudei var izmantot `assert`:

```python
assert is_even(4) == True
assert is_even(5) == False
```

Ja apgalvojums nav patiess, Python izmet kļūdu. Tas palīdz ātri pamanīt, ka funkcija strādā citādi nekā gaidīts.

Praktiski domā par trim gadījumiem:

- parasts gadījums;
- robežgadījums;
- kļūdaina vai negaidīta ievade.

Piemēram, ja funkcija aprēķina atlaidi, pārbaudi cenu `100`, cenu `0` un negatīvu cenu. Jo mazāka funkcija, jo vieglāk tai uzrakstīt precīzu pārbaudi.
