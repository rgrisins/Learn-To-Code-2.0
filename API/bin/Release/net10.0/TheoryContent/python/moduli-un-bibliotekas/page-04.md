# Importu paradumi

Importus parasti liek faila sākumā. Tas ļauj ātri redzēt, no kā programma ir atkarīga.

```python
from pathlib import Path
from datetime import datetime

import requests
```

Parasti vispirms liek standarta bibliotēkas importus, tad ārējās pakotnes, tad sava projekta moduļus.

Izvairies no `from module import *`, jo tad nav skaidrs, no kurienes nāk nosaukumi.

Labāk:

```python
from math import sqrt
```

Vai:

```python
import math
```

Alias jeb īsāku nosaukumu izmanto, ja tas ir pieņemts paradums:

```python
import pandas as pd
```

Importu mērķis nav tikai palaist kodu. Tie arī padara projekta robežas saprotamas. Ja faila importu saraksts kļūst ļoti garš, iespējams, fails dara pārāk daudz.
