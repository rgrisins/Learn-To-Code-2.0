# Moduļi

Modulis ir Python fails, kuru var importēt citā failā. Tas ļauj sadalīt programmu vairākos failos.

Piemēram, failā `math_tools.py`:

```python
def add(a, b):
    return a + b
```

Citā failā:

```python
import math_tools

print(math_tools.add(2, 3))
```

Var importēt konkrētu funkciju:

```python
from math_tools import add

print(add(2, 3))
```

Sadalīšana moduļos palīdz uzturēt kārtību. Vienā failā var būt lietotāja ievade, citā aprēķini, vēl citā datu saglabāšana.

Izvairies no situācijas, kur imports palaiž daudz negaidītu darbību. Moduļiem labāk definēt funkcijas un klases, bet programmas sākšanu atstāt galvenajam failam.
