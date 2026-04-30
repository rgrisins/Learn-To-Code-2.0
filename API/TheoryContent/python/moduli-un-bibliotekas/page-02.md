# Standarta bibliotēka

Python nāk ar plašu standarta bibliotēku. Tas nozīmē, ka daudzām vajadzībām nav jāinstalē nekas papildus.

Daži bieži moduļi:

```python
import math
import random
from datetime import datetime
from pathlib import Path
```

Piemēri:

```python
print(math.sqrt(16))
print(random.randint(1, 6))
print(datetime.now())
```

`pathlib` palīdz strādāt ar failu ceļiem:

```python
path = Path("data") / "names.txt"
print(path.exists())
```

Pirms meklē ārēju pakotni, pajautā: vai Python standarta bibliotēkā jau ir rīks šim darbam? Bieži atbilde ir jā.

Standarta bibliotēka ir stabila un labi dokumentēta. Tas ir labs pamats, uz kura būvēt pirmos projektus.
