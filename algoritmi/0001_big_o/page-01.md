# Kas ir Big O notācija?

## Ievads

**Big O notācija** ir matemātisks veids, kā aprakstīt algoritma **sarežģītību** — cik daudz laika vai atmiņas tas patērē atkarībā no ievades izmēra. Tā parāda, kā algoritma resursu patēriņš mainās, kad ievades skaits aug.

Ar Big O mēs neaprēķinām precīzu sekundes vai baitu skaitu, bet aprakstām **augšanas tendenci** — vai algoritms ir lineārs, kvadrātisks, logaritmisks utt.

## Kāpēc tas ir svarīgi?

Iedomājies, ka tev jāmeklē vārds 100 ierakstu sarakstā. Tas ir ātri. Bet ja saraksts ir 100 miljoni? Algoritma izvēle nosaka, vai meklēšana aizņems sekundes vai stundas.

```python
# Lineārs O(n) — pārbauda katru elementu pēc kārtas
def linear_search(arr, target):
    for item in arr:
        if item == target:
            return True
    return False

# Bināra O(log n) — izmanto, ka saraksts ir sakārtots
def binary_search(arr, target):
    low, high = 0, len(arr) - 1
    while low <= high:
        mid = (low + high) // 2
        if arr[mid] == target:
            return True
        elif arr[mid] < target:
            low = mid + 1
        else:
            high = mid - 1
    return False
```

Sarakstā ar 1 miljardu elementu:
- Linear: līdz 1 miljardam salīdzinājumu
- Binary: ~30 salīdzinājumu (`log₂(10⁹) ≈ 30`)

## Kā lasīt Big O?

`O(n)` nozīmē "augšanas kārtība lineāra attiecībā pret n", kur n ir ievades izmērs. Pieraksts:
- **O** — angļu valodā "order of"
- **n** — ievades skaits (parasti masīva garums vai meklēšanas dziļums)

## Galvenās sarežģītības klases

| Notācija | Nosaukums | Piemērs |
|----------|-----------|---------|
| **O(1)** | Konstanta | Saraksta indekss `arr[5]` |
| **O(log n)** | Logaritmiska | Bināra meklēšana |
| **O(n)** | Lineāra | Lineāra meklēšana |
| **O(n log n)** | Linear-logaritmiska | Quick Sort, Merge Sort |
| **O(n²)** | Kvadrātiska | Bubble Sort, ieguldīti cikli |
| **O(2ⁿ)** | Eksponenciāla | Naivs Fibonači rekursīvi |
| **O(n!)** | Faktoriāla | Visu permutāciju ģenerēšana |

## Vizuāls salīdzinājums

Ja n = 100:

```text
O(1):       1 operācija
O(log n):   ~7 operācijas
O(n):       100 operāciju
O(n log n): ~700 operāciju
O(n²):      10 000 operāciju
O(2ⁿ):      ~10³⁰ operāciju (ļoti daudz!)
```

## Kā noteikt algoritma Big O?

**Pamatprincipi:**

1. **Atmet konstantes** — `O(2n)` = `O(n)`, `O(n + 5)` = `O(n)`
2. **Paturi tikai dominējošo terminu** — `O(n² + n)` = `O(n²)`
3. **Skaiti cikla iterācijas** — viens cikls = `O(n)`, ieguldīti cikli = `O(n²)`
4. **Daloši algoritmi** = `O(log n)` (bināra meklēšana, dažas rekursijas)

```python
# O(1) — konstants laiks neatkarīgi no n
def get_first(arr):
    return arr[0]

# O(n) — viens cikls cauri masīvam
def sum_arr(arr):
    total = 0
    for x in arr:
        total += x
    return total

# O(n²) — divi ieguldīti cikli
def has_duplicate(arr):
    for i in range(len(arr)):
        for j in range(i + 1, len(arr)):
            if arr[i] == arr[j]:
                return True
    return False
```

## Kopsavilkums

Big O notācija apraksta algoritma augšanas tendenci atkarībā no ievades izmēra. Lai analizētu kodu — saskaita cikla iterācijas, atmet konstantes un patur tikai dominējošo terminu. Galvenās klases no labākās uz sliktāko: O(1) → O(log n) → O(n) → O(n log n) → O(n²) → O(2ⁿ) → O(n!).
