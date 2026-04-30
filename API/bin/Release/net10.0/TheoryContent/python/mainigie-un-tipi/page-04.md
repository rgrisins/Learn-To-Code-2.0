# Boolean un None

Patiesuma vērtības Python sauc par `bool`. Tām ir tikai divas iespējamās vērtības: `True` un `False`.

```python
is_active = True
has_errors = False
```

Booleans bieži rodas salīdzinājumos:

```python
age = 18
print(age >= 18)
print(age == 21)
print(age != 0)
```

`None` nozīmē, ka vērtības nav. Tas nav tas pats, kas `0`, tukšs teksts vai `False`. To izmanto, kad vērtība vēl nav zināma vai rezultāts nav atrasts.

```python
selected_user = None

if selected_user is None:
    print("Lietotājs nav izvēlēts.")
```

Salīdzinot ar `None`, lieto `is None` vai `is not None`. Tas ir skaidrāk un atbilst Python paradumiem.

Svarīgi atšķirt "nav vērtības" no "vērtība ir tukša". Piemēram, tukšs saraksts nozīmē, ka saraksts eksistē, bet tajā nav elementu. `None` nozīmē, ka saraksta vietā nav nekā.
