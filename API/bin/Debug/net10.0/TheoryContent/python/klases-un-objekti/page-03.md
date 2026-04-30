# Metodes

Metode ir funkcija, kas pieder klasei un strādā ar objektu.

```python
class Student:
    def __init__(self, name, score):
        self.name = name
        self.score = score

    def has_passed(self):
        return self.score >= 50
```

Izmantošana:

```python
student = Student("Juris", 75)

if student.has_passed():
    print("Ieskaitīts")
```

Metodei pirmais parametrs ir `self`, bet izsaukumā to neraksta. Python pats padod konkrēto objektu.

Metodes ir labas, ja darbība loģiski pieder objektam. Piemēram, `student.has_passed()` ir saprotamāk nekā ārēja funkcija, ja pārbaude vienmēr balstās uz studenta punktiem.

Tomēr ne visu vajag pārvērst metodēs. Ja darbība nepieder vienam konkrētam objektam, vienkārša funkcija var būt skaidrāka.
