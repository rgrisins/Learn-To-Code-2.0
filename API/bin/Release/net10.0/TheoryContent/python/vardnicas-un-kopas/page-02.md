# Vārdnīcu maiņa

Vārdnīcai var pievienot jaunu atslēgu vai mainīt esošu vērtību.

```python
student = {"name": "Anna", "age": 17}

student["age"] = 18
student["email"] = "anna@example.com"
```

Elementu var izņemt ar `pop`:

```python
email = student.pop("email", None)
```

Otrais arguments ir noklusējuma vērtība, ja atslēga nav atrasta.

Lai pārbaudītu, vai atslēga pastāv, izmanto `in`:

```python
if "age" in student:
    print("Vecums ir zināms")
```

Vārdnīcas var ievietot sarakstos:

```python
students = [
    {"name": "Anna", "score": 90},
    {"name": "Juris", "score": 75},
]
```

Šāda struktūra ir ļoti praktiska tabulāriem datiem, kur katrs saraksta elements apraksta vienu ierakstu.
