# Tipu pārveidošana

Programmas bieži saņem datus kā tekstu. `input` vienmēr atgriež `str`, tāpēc skaitļošanai jāveic pārveidošana.

```python
raw_age = input("Ievadi vecumu: ")
age = int(raw_age)
print(age + 1)
```

Ja lietotājs ievada tekstu, kuru nevar pārveidot par skaitli, programma saņems kļūdu. Vēlāk to apstrādāsim ar izņēmumiem, bet sākumā vari pieņemt, ka ievade ir pareiza.

Biežākās pārveidošanas funkcijas:

```python
int("42")
float("3.14")
str(100)
bool(1)
```

`bool` ir jālieto uzmanīgi. Tukšas vērtības parasti kļūst par `False`, bet netukšas par `True`.

```python
print(bool(""))
print(bool("False"))
```

Otrais piemērs izvada `True`, jo teksts `"False"` nav tukšs. Ja jāapstrādā lietotāja ievadīts "jā" vai "nē", labāk pārbaudīt konkrētu tekstu:

```python
answer = input("Turpināt? ").strip().lower()
should_continue = answer == "jā"
```
