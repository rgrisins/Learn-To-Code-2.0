# Pirmais fails

Python failiem parasti izmanto paplašinājumu `.py`. Piemēram, izveido failu `hello.py` un ieraksti tajā:

```python
name = "Anna"
print("Sveiki, " + name + "!")
```

Pēc tam palaid failu terminālī:

```bash
python hello.py
```

Programma izpildīs rindas no augšas uz leju. Pirmajā rindā mainīgajam `name` tiek piešķirta teksta vērtība. Otrajā rindā `print` izvada tekstu ekrānā.

Ja programma nestrādā, kļūdas ziņojums parasti norāda failu, rindas numuru un kļūdas tipu. Iesācējam tas var izskatīties biedējoši, bet patiesībā tā ir karte līdz problēmai.

Piemērs ar kļūdu:

```python
print("Sveiki"
```

Šeit trūkst aizverošās iekavas. Python ziņos par sintakses kļūdu. Lasi kļūdu no apakšas uz augšu: pēdējā rinda bieži pasaka būtiskāko.
