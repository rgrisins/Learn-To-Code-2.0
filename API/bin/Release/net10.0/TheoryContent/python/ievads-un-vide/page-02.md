# Interpretators un versija

Python kodu izpilda Python interpretators. Datorā var būt vairākas Python versijas, tāpēc sākumā ir vērts pārbaudīt, kuru versiju izmanto tava vide.

Terminālī parasti pietiek ar:

```bash
python --version
```

Dažās sistēmās komanda ir:

```bash
python3 --version
```

Versija ir svarīga, jo valoda attīstās. Modernam mācību darbam der Python 3.11 vai jaunāks. Ja redzi Python 2, tā ir ļoti veca versija un jaunam projektam to nevajadzētu izmantot.

Interpretators var darboties divos veidos. Pirmais ir palaist visu failu. Otrais ir interaktīvais režīms jeb REPL, kur komandas vari ievadīt pa vienai un uzreiz redzēt rezultātu. REPL ir ērts eksperimentiem:

```python
>>> 2 + 3
5
>>> "Py" + "thon"
'Python'
```

Kad ideja kļūst lielāka par pāris rindām, to labāk pārcelt uz `.py` failu.
