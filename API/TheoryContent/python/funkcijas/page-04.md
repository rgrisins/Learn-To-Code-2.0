# Darbības lauks

Mainīgajiem ir darbības lauks jeb scope. Mainīgais, kas izveidots funkcijas iekšpusē, parasti nav pieejams ārpus tās.

```python
def create_message():
    message = "Sveiki"
    return message

print(create_message())
```

Šeit `message` dzīvo funkcijā. Ārpus funkcijas izmantojam atgriezto vērtību.

Funkcija var lasīt globālus mainīgos, bet tas bieži padara kodu grūtāk testējamu.

```python
tax_rate = 0.21

def calculate_total(price):
    return price + price * tax_rate
```

Mazās programmās tas ir pieņemami, bet lielākās labāk nodot vajadzīgās vērtības kā parametrus:

```python
def calculate_total(price, tax_rate):
    return price + price * tax_rate
```

Jo mazāk funkcija paļaujas uz ārējo stāvokli, jo vieglāk saprast, pārbaudīt un atkārtoti izmantot tās kodu.
