# Saraksti ciklos

Sarakstus visbiežāk apstrādā ar `for` ciklu.

```python
prices = [5.99, 12.50, 3.25]

for price in prices:
    print(f"Cena: {price}")
```

Ja vajag mainīt katru vērtību, bieži veido jaunu sarakstu:

```python
prices_with_vat = []

for price in prices:
    prices_with_vat.append(round(price * 1.21, 2))
```

Ja vajag mainīt esošo sarakstu pēc indeksa, izmanto `range(len(...))`, bet dari to tikai tad, kad tiešām vajag.

```python
for index in range(len(prices)):
    prices[index] = round(prices[index], 2)
```

Daudzos gadījumos labāk ir nelabot sarakstu cikla laikā, īpaši neizņemt elementus no tā paša saraksta, pa kuru pašlaik ej. Tas var radīt grūti pamanāmas kļūdas.

Drošāk ir izveidot jaunu sarakstu ar vajadzīgajiem elementiem.
