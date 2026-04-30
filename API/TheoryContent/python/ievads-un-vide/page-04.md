# Komentāri un koda lasāmība

Komentārs ir teksts, kuru Python neizpilda. To izmanto, lai īsi paskaidrotu domu vai atstātu piezīmi nākotnei.

```python
# Aprēķina cenu ar PVN
price = 20
vat = 0.21
total = price + price * vat
print(total)
```

Komentāri nav jāliek pie katras rindas. Labs kods lielā daļā gadījumu paskaidro sevi ar skaidriem mainīgo nosaukumiem. Piemēram, `total_price` ir labāks par `x`, ja mainīgais glabā kopējo cenu.

Python kopienā bieži runā par PEP 8 - stila vadlīnijām. Šajā brīdī pietiek atcerēties dažus principus:

- mainīgo nosaukumos lieto mazos burtus un pasvītrojumus;
- atkāpes veido ar 4 atstarpēm;
- atstāj tukšas rindas, lai nodalītu lielākas idejas;
- neliec vienā rindā pārāk daudz darbību.

Lasāms kods ir vieglāk labojams, testējams un izskaidrojams citiem.
