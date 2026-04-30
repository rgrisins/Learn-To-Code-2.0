# Teksts

Teksta tips Python valodā ir `str`. Tekstu var rakstīt vienpēdiņās vai dubultpēdiņās.

```python
first_name = "Jānis"
last_name = 'Ozols'
```

Tekstu var apvienot:

```python
full_name = first_name + " " + last_name
print(full_name)
```

Bieži ērtāka ir f-virkne:

```python
age = 17
print(f"{first_name} ir {age} gadus vecs.")
```

Tekstam ir metodes, kas palīdz to apstrādāt:

```python
message = "  Python ir foršs  "
print(message.strip())
print(message.upper())
print(message.lower())
print(message.replace("foršs", "praktisks"))
```

`strip` noņem liekās atstarpes sākumā un beigās. `upper` un `lower` maina burtu reģistru. `replace` aizvieto teksta daļu.

Teksts ir indeksējams. Pirmais simbols ir ar indeksu `0`:

```python
word = "Python"
print(word[0])
print(word[-1])
```
