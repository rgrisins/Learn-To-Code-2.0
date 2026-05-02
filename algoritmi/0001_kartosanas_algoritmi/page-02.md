# Quick Sort (ātrā kārtošana)

## Kas ir Quick Sort?

**Quick Sort** ir viens no ātrākajiem un visizplatītākajiem kārtošanas algoritmiem. Tas izmanto **"sadali un valdi" (divide and conquer)** stratēģiju — izvēlas vienu elementu kā **pivot** (pamatpunktu), sadala sarakstu divās daļās (elementi mazāki par pivot un lielāki) un rekursīvi sakārto katru daļu.

## Kā tas darbojas?

Saraksts: `[5, 2, 8, 1, 9, 3, 7]`

1. Izvēlas pivot — piemēram, pēdējais elements `7`
2. Sadala: kreisā daļa = mazāki par 7, labā daļa = lielāki
3. Pēc partition: `[5, 2, 1, 3] | 7 | [8, 9]`
4. Rekursīvi sakārto kreiso un labo daļu

```text
              [5, 2, 8, 1, 9, 3, 7]
              pivot = 7
              ↓
    [5, 2, 1, 3]    7    [8, 9]
    pivot = 3              pivot = 9
    ↓                      ↓
[2, 1] 3 [5]           [8] 9 []
↓
[1] 2 []

Galarezultāts: [1, 2, 3, 5, 7, 8, 9]
```

## Implementācija Python

```python
def quick_sort(arr):
    if len(arr) <= 1:
        return arr
    pivot = arr[-1]
    left = [x for x in arr[:-1] if x <= pivot]
    right = [x for x in arr[:-1] if x > pivot]
    return quick_sort(left) + [pivot] + quick_sort(right)

print(quick_sort([5, 2, 8, 1, 9, 3, 7]))
# [1, 2, 3, 5, 7, 8, 9]
```

In-place variants (lietderīgāks atmiņas ziņā):

```python
def quick_sort_inplace(arr, low=0, high=None):
    if high is None:
        high = len(arr) - 1
    if low < high:
        pivot_index = partition(arr, low, high)
        quick_sort_inplace(arr, low, pivot_index - 1)
        quick_sort_inplace(arr, pivot_index + 1, high)

def partition(arr, low, high):
    pivot = arr[high]
    i = low - 1
    for j in range(low, high):
        if arr[j] <= pivot:
            i += 1
            arr[i], arr[j] = arr[j], arr[i]
    arr[i + 1], arr[high] = arr[high], arr[i + 1]
    return i + 1
```

## Sarežģītība

| Gadījums | Laika sarežģītība | Atmiņas |
|----------|-------------------|---------|
| Labākais | O(n log n) | O(log n) |
| Vidējais | O(n log n) | O(log n) |
| Sliktākais | O(n²) | O(log n) |

Sliktākais gadījums (O(n²)) notiek, kad pivot vienmēr ir mazākais vai lielākais elements (piem., jau sakārtots saraksts ar pēdējo elementu kā pivot). To var novērst ar **randomizāciju** — nejauši izvēloties pivot.

## Kad lietot?

- **Ļoti bieži** — Python iebūvētā `sorted()` izmanto **Timsort** (Quick Sort hibrīds)
- Vidēji un lieli saraksti
- Kad atmiņa ir ierobežota (in-place versija)

## Kopsavilkums

Quick Sort ir efektīvs O(n log n) algoritms ar zemu atmiņas patēriņu (in-place). Tas izmanto pivot elementu un rekursīvi sadala sarakstu. Pareiza pivot izvēle (randomizācija vai mediāna) izvairās no O(n²) sliktākā gadījuma.
