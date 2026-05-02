# Sarežģītības klases sīkāk

## O(1) — Konstants laiks

Algoritms izpildās **vienā un tajā pašā laikā**, neatkarīgi no ievades izmēra. Vislabākā iespējamā sarežģītība.

```python
def get_element(arr, i):
    return arr[i]      # O(1) — tieša piekļuve

def push(stack, value):
    stack.append(value)  # O(1) — pievieno beigās
```

Vārdnīcas (dict) operācijas Python ir **vidēji O(1)**:

```python
d = {"a": 1, "b": 2}
d["c"] = 3      # O(1) ievietošana
print(d["a"])   # O(1) lasīšana
```

## O(log n) — Logaritmiska

Katrā solī algoritms **uz pusi samazina** problēmas izmēru. Tipisks paterns: bināra meklēšana, balansētu koku operācijas.

```python
def binary_search(arr, target):
    low, high = 0, len(arr) - 1
    while low <= high:
        mid = (low + high) // 2
        if arr[mid] == target:
            return mid
        if arr[mid] < target:
            low = mid + 1
        else:
            high = mid - 1
    return -1
```

Ar n = 1 000 000, vajag tikai ~20 iterāciju (`log₂(10⁶) ≈ 20`).

## O(n) — Lineāra

Algoritma laiks aug **proporcionāli** ievades izmēram. Viens cikls cauri datiem.

```python
def find_max(arr):
    largest = arr[0]
    for x in arr:
        if x > largest:
            largest = x
    return largest        # O(n)

def count_chars(text):
    counter = {}
    for ch in text:
        counter[ch] = counter.get(ch, 0) + 1
    return counter        # O(n)
```

## O(n log n) — Linearitmiska

Lielākā daļa **efektīvo kārtošanas algoritmu** (Merge Sort, Quick Sort, Timsort) ir šeit. Ļoti laba sarežģītība lielākajai daļai praktisko uzdevumu.

```python
sorted_arr = sorted([3, 1, 4, 1, 5, 9])  # Python lieto Timsort: O(n log n)
```

## O(n²) — Kvadrātiska

**Ieguldīti cikli** pa visu sarakstu. Maziem n strādā labi, lieliem — pārāk lēns.

```python
# Visu pāru salīdzināšana
def find_pair_with_sum(arr, target):
    for i in range(len(arr)):
        for j in range(i + 1, len(arr)):
            if arr[i] + arr[j] == target:
                return (i, j)
    return None

# Bubble Sort
def bubble_sort(arr):
    for i in range(len(arr)):
        for j in range(len(arr) - 1):
            if arr[j] > arr[j + 1]:
                arr[j], arr[j + 1] = arr[j + 1], arr[j]
```

Ar n = 10 000: 100 000 000 operāciju — sāk būt manāmi lēns.

## O(2ⁿ) — Eksponenciāla

Katrs papildu elements **dubulto** darba apjomu. Naivais rekursīvais Fibonači:

```python
def fib(n):
    if n <= 1:
        return n
    return fib(n - 1) + fib(n - 2)   # O(2ⁿ)

# fib(40) jau aizņem manāmu laiku
# fib(100) — praktiski neiespējami
```

Risinājums — **memoizācija** padara to par O(n):

```python
from functools import lru_cache

@lru_cache(maxsize=None)
def fib(n):
    if n <= 1:
        return n
    return fib(n - 1) + fib(n - 2)
```

## O(n!) — Faktoriāla

Visu **permutāciju** ģenerēšana. Pat n = 12 ir miljardi kombināciju.

```python
from itertools import permutations

# Visu permutāciju iziešana — O(n!)
for p in permutations([1, 2, 3, 4, 5]):
    print(p)
```

## Salīdzinājums tabulā

n = 100, viena operācija = 1 nanosekunde:

| Sarežģītība | Operācijas | Laiks |
|-------------|-----------|-------|
| O(1) | 1 | <1 µs |
| O(log n) | 7 | <1 µs |
| O(n) | 100 | 0.1 µs |
| O(n log n) | 700 | 0.7 µs |
| O(n²) | 10 000 | 10 µs |
| O(2ⁿ) | 10³⁰ | Vairāk par Visuma vecumu |
| O(n!) | 9.3 × 10¹⁵⁷ | Pilnīgi neiespējami |

## Kopsavilkums

Sarežģītības klases nosaka, kā algoritms uzvedīsies, kad ievades skaits aug. O(1) un O(log n) ir vislabākās, O(n²) un sliktāk — jāizvairās lieliem datiem. Pirms īstenot algoritmu, padomā par tā Big O — tas var izlemt, vai programma ir lietojama vai ne.
