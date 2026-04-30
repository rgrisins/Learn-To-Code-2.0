# Dataclasses

Ja klase galvenokārt glabā datus, var izmantot `dataclass`.

```python
from dataclasses import dataclass

@dataclass
class Student:
    name: str
    score: int
```

Tagad Python pats izveido ērtu `__init__` un pārskatāmu attēlojumu.

```python
student = Student("Anna", 90)
print(student)
```

Dataclass var pievienot arī metodes:

```python
@dataclass
class Student:
    name: str
    score: int

    def has_passed(self) -> bool:
        return self.score >= 50
```

Šis pieraksts bieži ir tīrāks par manuālu klasi, ja nav vajadzīga sarežģīta inicializācija.

Dataclasses labi der konfigurācijām, vienkāršiem modeļiem, rezultātu objektiem un datiem, kurus nodod starp funkcijām.
