# break un continue

`break` pārtrauc ciklu pilnībā. `continue` pārlec uz nākamo cikla soli.

```python
for number in range(1, 10):
    if number == 5:
        break
    print(number)
```

Šis izvada skaitļus līdz 4. Kad `number` ir 5, cikls beidzas.

`continue` piemērs:

```python
for number in range(1, 6):
    if number == 3:
        continue
    print(number)
```

Šis izlaidīs 3, bet turpinās ar 4 un 5.

Šie rīki ir noderīgi, bet tos nevajag pārmērīgi lietot. Ja ciklā ir daudz `break` un `continue`, iespējams, nosacījumus var sakārtot vienkāršāk.

Praktisks piemērs:

```python
numbers = [4, -2, 7, 0, 9]

for number in numbers:
    if number < 0:
        continue
    print(number)
```

Te negatīvie skaitļi tiek izlaisti.
