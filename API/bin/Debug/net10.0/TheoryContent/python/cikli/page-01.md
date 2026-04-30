# Kāpēc vajag ciklus

Cikls ļauj atkārtot darbību vairākas reizes. Bez cikliem būtu jāraksta viena un tā pati rinda atkal un atkal.

```python
print("Python")
print("Python")
print("Python")
```

Ar ciklu:

```python
for _ in range(3):
    print("Python")
```

`range(3)` dod vērtības `0`, `1`, `2`. Ja pati vērtība nav vajadzīga, bieži izmanto `_`, lai parādītu, ka mainīgais nav svarīgs.

Cikli ir ļoti bieži: tie apstrādā sarakstus, lasa failus pa rindām, pārbauda datus, izvada tabulas un veido atkārtotas darbības spēlēs vai simulācijās.

Labs cikls dara vienu saprotamu darbu. Ja cikla ķermenis kļūst ļoti garš, to parasti var sadalīt funkcijās.
