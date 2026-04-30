# while cikls

`while` cikls darbojas tik ilgi, kamēr nosacījums ir patiess.

```python
count = 1

while count <= 5:
    print(count)
    count += 1
```

Šeit svarīgi neaizmirst mainīt vērtību, kas ietekmē nosacījumu. Citādi cikls var kļūt bezgalīgs.

`while` ir noderīgs, kad iepriekš nezini atkārtojumu skaitu. Piemēram, ievades prasīšana, kamēr lietotājs ievada derīgu vērtību:

```python
password = ""

while password != "python":
    password = input("Parole: ")

print("Pieeja atļauta")
```

Bezgalīgs cikls nav vienmēr kļūda. Dažās programmās tas ir apzināts, bet tad jābūt skaidram iziešanas nosacījumam.

```python
while True:
    command = input("> ")
    if command == "exit":
        break
```
