# Datu strukturēšana

Programmēšanā liela daļa darba ir pareizi izvēlēties datu struktūru. Vienu un to pašu informāciju var pierakstīt dažādi, bet ne visi varianti būs ērti.

Ja vajag secību un dublikāti ir atļauti, izmanto sarakstu:

```python
answers = ["jā", "nē", "jā"]
```

Ja vajag unikālas vērtības, izmanto kopu:

```python
unique_answers = {"jā", "nē"}
```

Ja vajag īpašības ar nosaukumiem, izmanto vārdnīcu:

```python
profile = {
    "username": "anna",
    "role": "student"
}
```

Bieži struktūras kombinē:

```python
course = {
    "title": "Python pamati",
    "topics": ["Mainīgie", "Cikli", "Funkcijas"]
}
```

Pirms raksti daudz koda, pajautā sev: kā es vēlāk atradīšu vajadzīgo vērtību? Atbilde bieži pasaka, kura struktūra derēs vislabāk.
