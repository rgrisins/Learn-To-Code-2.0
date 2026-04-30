# Kopas

Kopa jeb `set` glabā unikālas vērtības bez dublikātiem.

```python
tags = {"python", "web", "python"}
print(tags)
```

Rezultātā `python` būs tikai vienu reizi. Kopas ir noderīgas, ja jāatrod unikālas vērtības vai jāpārbauda piederība.

```python
allowed_roles = {"admin", "teacher"}

if "admin" in allowed_roles:
    print("Loma atļauta")
```

No saraksta var izveidot kopu:

```python
names = ["Anna", "Juris", "Anna"]
unique_names = set(names)
```

Kopām ir matemātiskas darbības:

```python
a = {"python", "sql", "html"}
b = {"python", "css", "html"}

print(a & b)
print(a | b)
print(a - b)
```

`&` dod kopīgās vērtības, `|` apvieno, bet `-` atrod vērtības, kas ir tikai pirmajā kopā.
