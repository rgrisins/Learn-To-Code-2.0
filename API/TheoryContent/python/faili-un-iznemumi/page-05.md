# Validācija praksē

Validācija nozīmē pārbaudīt, vai dati ir derīgi. To bieži apvieno ar izņēmumu apstrādi.

```python
def read_positive_int(prompt):
    while True:
        raw_value = input(prompt)

        try:
            value = int(raw_value)
        except ValueError:
            print("Ievadi veselu skaitli.")
            continue

        if value <= 0:
            print("Skaitlim jābūt pozitīvam.")
            continue

        return value
```

Šī funkcija atkārto ievadi, kamēr lietotājs ievada pozitīvu veselu skaitli.

Svarīgi validāciju turēt tuvu vietai, kur dati ienāk programmā. Ja dati jau sākumā ir pārbaudīti, pārējais kods var būt vienkāršāks.

Ar failiem validācija var nozīmēt pārbaudīt, vai fails eksistē, vai tas nav tukšs un vai rindas ir gaidītajā formātā.

Labs kļūdas paziņojums pasaka, kas nav kārtībā un ko lietotājam darīt tālāk.
