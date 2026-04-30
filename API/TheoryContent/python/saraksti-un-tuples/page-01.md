# Saraksti

Saraksts jeb `list` glabā vairākas vērtības noteiktā secībā. Sarakstu pieraksta ar kvadrātiekavām.

```python
numbers = [10, 20, 30]
names = ["Anna", "Juris", "Marta"]
mixed = ["Python", 3, True]
```

Parasti labāk vienā sarakstā glabāt līdzīga tipa vērtības, jo tad ar datiem ir vieglāk strādāt.

Sarakstā var uzzināt elementu skaitu:

```python
print(len(names))
```

Saraksts saglabā secību. Tas nozīmē, ka pirmais elements paliek pirmais, kamēr tu pats to nepārvieto.

```python
for name in names:
    print(name)
```

Saraksti ir maināmi. Tu vari pievienot, izņemt un pārkārtot elementus. Tas ir ērti, bet arī nozīmē, ka jāuzmanās, ja to pašu sarakstu izmanto vairākās vietās.
