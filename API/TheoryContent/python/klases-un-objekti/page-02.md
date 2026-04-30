# Klases definēšana

Klasi definē ar `class`.

```python
class Student:
    def __init__(self, name, score):
        self.name = name
        self.score = score
```

`__init__` tiek izsaukts, kad veido jaunu objektu. `self` norāda uz konkrēto objektu.

```python
student = Student("Anna", 90)
print(student.name)
print(student.score)
```

`student.name` un `student.score` ir objekta atribūti.

Klases nosaukumus Python parasti raksta `PascalCase`: `Student`, `TheoryTopic`, `CodeRunner`. Mainīgos un funkcijas raksta ar mazajiem burtiem un pasvītrojumiem.

Sākumā `self` var šķist dīvains, bet tas vienkārši nozīmē: strādā ar šo konkrēto objektu, nevis ar visu klasi kopumā.
