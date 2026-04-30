# Pakotņu izvēle

Ārējās pakotnes dod daudz iespēju, bet katra pakotne kļūst par projekta atkarību. Pirms instalē, paskaties, vai pakotne ir aktīvi uzturēta, vai tai ir dokumentācija un vai tā risina tieši tavu problēmu.

Labs sākums ir maza pārbaudes programma virtuālajā vidē. Instalē pakotni, izmēģini vienu galveno funkciju un tikai tad izmanto to lielākā projektā.

```bash
pip install requests
```

Pēc instalēšanas kodā vari importēt pakotni:

```python
import requests
```

Ja projekts jānodod citam cilvēkam, pieraksti atkarības `requirements.txt` failā. Tas ļauj citā datorā izveidot tādu pašu vidi.

Praktisks paradums: neinstalē pakotni tikai tāpēc, ka tā ir populāra. Izvēlies to tad, ja tā samazina sarežģītību un tu saproti, ko tā dara tavā projektā.
