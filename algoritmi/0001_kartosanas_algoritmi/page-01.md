# Bubble Sort (burbuļkārtošana)

## Kas ir Bubble Sort?

**Bubble Sort** ir vienkāršākais kārtošanas algoritms — tas atkārtoti iziet cauri sarakstam un samaina vietām divus blakus elementus, ja tie ir nepareizā secībā. Pēc katras iterācijas lielākais elements "izpeld" uz sava pareizā galarezultāta, līdzīgi kā burbulis ūdenī — tāpēc nosaukums.

## Kā tas darbojas?

Iedomājies sarakstu `[5, 2, 8, 1, 9]`. Pirmā iterācija:

| Solis | Saraksts | Salīdzinājums | Darbība |
|-------|----------|---------------|---------|
| 1 | `[5, 2, 8, 1, 9]` | 5 vs 2 | Mainīt → `[2, 5, 8, 1, 9]` |
| 2 | `[2, 5, 8, 1, 9]` | 5 vs 8 | OK |
| 3 | `[2, 5, 8, 1, 9]` | 8 vs 1 | Mainīt → `[2, 5, 1, 8, 9]` |
| 4 | `[2, 5, 1, 8, 9]` | 8 vs 9 | OK |

Pēc pirmās iterācijas `9` ir savā vietā (galā). Process atkārtojas, kamēr neviena pāra apmaiņa nav vajadzīga.

## Implementācija Python

```python
def bubble_sort(arr):
    n = len(arr)
    for i in range(n):
        swapped = False
        for j in range(0, n - i - 1):
            if arr[j] > arr[j + 1]:
                arr[j], arr[j + 1] = arr[j + 1], arr[j]
                swapped = True
        if not swapped:
            break
    return arr

print(bubble_sort([5, 2, 8, 1, 9]))
# [1, 2, 5, 8, 9]
```

## Sarežģītība

| Gadījums | Laika sarežģītība | Atmiņas |
|----------|-------------------|---------|
| Labākais (saraksts jau sakārtots) | O(n) | O(1) |
| Vidējais | O(n²) | O(1) |
| Sliktākais | O(n²) | O(1) |

`swapped` karodziņš ļauj algoritmu pārtraukt agrāk, ja saraksts jau ir sakārtots — labākais gadījums kļūst lineārs.

## Kad lietot?

- Mācību nolūkos (vienkārša implementācija)
- Ļoti maziem sarakstiem (n < 10)
- Gandrīz sakārtotiem sarakstiem (`swapped` optimizācija)

**Praksē** Bubble Sort ir lēns lieliem datiem — labāk lietot Quick Sort vai Merge Sort.

## Kopsavilkums

Bubble Sort ir intuitīvs O(n²) algoritms, kas atkārtoti samaina blakus elementus. Vienkārši ieviest, bet praksē reti lietots lielu datu kārtošanai. Ideāls iesācējiem, lai saprastu kārtošanas algoritmu pamatprincipus.
