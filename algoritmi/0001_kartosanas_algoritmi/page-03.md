# Merge Sort (sapludināšanas kārtošana)

## Kas ir Merge Sort?

**Merge Sort** ir vēl viens "sadali un valdi" algoritms ar garantētu O(n log n) sarežģītību visos gadījumos. Tas darbojas tā: rekursīvi sadala sarakstu uz pusi, līdz katra daļa satur vienu elementu, un tad **sapludina** (merge) tās atpakaļ kopā sakārtotā secībā.

## Kā tas darbojas?

Saraksts: `[5, 2, 8, 1, 9, 3, 7, 4]`

**Sadalīšanas fāze** (top-down):

```text
        [5, 2, 8, 1, 9, 3, 7, 4]
       /                        \
   [5, 2, 8, 1]            [9, 3, 7, 4]
   /         \              /         \
 [5, 2]    [8, 1]         [9, 3]    [7, 4]
  /  \      /  \           /  \      /  \
[5] [2]   [8] [1]        [9] [3]   [7] [4]
```

**Sapludināšanas fāze** (bottom-up):

```text
[5] [2]      → [2, 5]
[8] [1]      → [1, 8]
[2, 5][1, 8] → [1, 2, 5, 8]

[9] [3]      → [3, 9]
[7] [4]      → [4, 7]
[3, 9][4, 7] → [3, 4, 7, 9]

[1, 2, 5, 8] [3, 4, 7, 9] → [1, 2, 3, 4, 5, 7, 8, 9]
```

## Implementācija Python

```python
def merge_sort(arr):
    if len(arr) <= 1:
        return arr

    mid = len(arr) // 2
    left = merge_sort(arr[:mid])
    right = merge_sort(arr[mid:])

    return merge(left, right)

def merge(left, right):
    result = []
    i = j = 0
    while i < len(left) and j < len(right):
        if left[i] <= right[j]:
            result.append(left[i])
            i += 1
        else:
            result.append(right[j])
            j += 1
    result.extend(left[i:])
    result.extend(right[j:])
    return result

print(merge_sort([5, 2, 8, 1, 9, 3, 7, 4]))
# [1, 2, 3, 4, 5, 7, 8, 9]
```

## Sarežģītība

| Gadījums | Laika sarežģītība | Atmiņas |
|----------|-------------------|---------|
| Labākais | O(n log n) | O(n) |
| Vidējais | O(n log n) | O(n) |
| Sliktākais | O(n log n) | O(n) |

**Stabils** algoritms — vienādu elementu relatīvā secība tiek saglabāta.

## Salīdzinājums ar Quick Sort

| Aspekts | Merge Sort | Quick Sort |
|---------|-----------|------------|
| Garantētais ātrums | O(n log n) vienmēr | O(n²) sliktākajā |
| Atmiņa | O(n) papildu | O(log n) (in-place) |
| Stabils | Jā | Nē |
| Kešs (CPU) | Sliktāk | Labāk |
| Pareizēms paralēli | Viegli | Grūtāk |

## Kad lietot?

- Kad vajag **garantētu** O(n log n) (piem., reāllaika sistēmās)
- Saistītajos sarakstos (linked lists) — Merge Sort ir efektīvs
- Kad vajag **stabilu** kārtošanu
- Liels datu apjoms uz diska (external sorting)

## Kopsavilkums

Merge Sort ir uzticams O(n log n) algoritms, kas garantē veiktspēju visos gadījumos. Lieto papildu O(n) atmiņu sapludināšanai, bet ir stabils un labi piemērots saistītajiem sarakstiem un ārējai kārtošanai. Praksē Quick Sort biežāk dod labāku reālo veiktspēju (kešs), bet Merge Sort ir drošāka izvēle, kad nepieciešama paredzamība.
