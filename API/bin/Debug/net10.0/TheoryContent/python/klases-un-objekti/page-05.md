# Neliels modelis

Iedomāsimies kursu ar vairākām tēmām. To var modelēt ar dataclasses.

```python
from dataclasses import dataclass

@dataclass
class Topic:
    title: str
    minutes: int

@dataclass
class Course:
    title: str
    topics: list[Topic]

    def total_minutes(self) -> int:
        return sum(topic.minutes for topic in self.topics)
```

Izmantošana:

```python
course = Course(
    title="Python pamati",
    topics=[
        Topic("Mainīgie", 35),
        Topic("Cikli", 40),
    ],
)

print(course.total_minutes())
```

Šāds modelis ir lasāmāks nekā vārdnīcu un sarakstu kombinācija, ja ar datiem jāstrādā vairākās vietās.

Klases palīdz nosaukt domēna jēdzienus. Kad kodā redzi `Course` un `Topic`, uzreiz ir skaidrāks, par ko programma runā.
