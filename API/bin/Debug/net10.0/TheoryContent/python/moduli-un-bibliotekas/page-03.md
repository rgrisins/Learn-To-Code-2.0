# Virtuālā vide

Virtuālā vide izolē projekta atkarības. Tas nozīmē, ka vienam projektam var būt vienas pakotņu versijas, bet citam citas.

Virtuālo vidi parasti izveido ar:

```bash
python -m venv .venv
```

Windows aktivizēšana:

```bash
.venv\Scripts\activate
```

macOS vai Linux:

```bash
source .venv/bin/activate
```

Pēc aktivizēšanas instalētās pakotnes nonāk šajā vidē, nevis globāli visā datorā.

```bash
pip install requests
```

Atkarības bieži pieraksta failā `requirements.txt`:

```bash
pip freeze > requirements.txt
```

Vēlāk cits cilvēks var instalēt tās pašas atkarības:

```bash
pip install -r requirements.txt
```

Virtuālā vide palīdz izvairīties no situācijas, kur viens projekts sabojā cita projekta iestatījumus.
