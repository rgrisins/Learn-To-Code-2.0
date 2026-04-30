# Kad klases nelietot

Klases nav mērķis pašas par sevi. Ja programma ir īsa un darbība ir vienkārša, funkcijas un datu struktūras var būt labāka izvēle.

Piemēram, šim nevajag klasi:

```python
def calculate_total(price, tax_rate):
    return price + price * tax_rate
```

Klase būtu pamatota, ja parādās stabils jēdziens ar vairākām īpašībām un darbībām:

```python
class Cart:
    ...
```

Labs jautājums: vai šim objektam ir stāvoklis, kas jāglabā, un metodes, kas loģiski darbojas ar šo stāvokli? Ja jā, klase var palīdzēt.

Vēl viens jautājums: vai nosaukta klase padarīs kodu skaidrāku citiem? Ja atbilde ir nē, iespējams, vienkāršāks risinājums būs labāks.

Pieredzējuši programmētāji bieži izvēlas vienkāršāko struktūru, kas skaidri atrisina konkrēto problēmu.
