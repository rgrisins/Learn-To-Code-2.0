# Skaitļi

Python ikdienā izmanto divus biežus skaitļu tipus: `int` un `float`. `int` ir veseli skaitļi, bet `float` ir skaitļi ar decimāldaļu.

```python
students = 24
temperature = 21.5
```

Ar skaitļiem vari veikt parastas darbības:

```python
print(10 + 3)
print(10 - 3)
print(10 * 3)
print(10 / 3)
print(10 // 3)
print(10 % 3)
print(2 ** 4)
```

`/` dod dalījumu ar decimāldaļu, `//` dod veselo dalījumu, `%` dod atlikumu, bet `**` kāpina pakāpē.

Skaitļus bieži vajag noapaļot:

```python
price = 19.995
print(round(price, 2))
```

Jāatceras, ka `float` nav ideāls naudas aprēķiniem, jo decimāldaļas datorā tiek glabātas binārā formā. Vienkāršiem mācību piemēriem tas ir pieņemami, bet nopietnai finanšu loģikai izmanto īpašus datu tipus, piemēram, `Decimal`.
