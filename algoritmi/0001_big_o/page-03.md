# Big O praktiska analīze

## Kā analizēt savu kodu

**Pamata noteikumi:**

1. **Konstantes neskaitās** — `O(2n + 5)` = `O(n)`
2. **Dominējošais termins paliek** — `O(n² + n log n)` = `O(n²)`
3. **Cikli reizinās, ja ieguldīti** — `for...for` = `O(n²)`
4. **Cikli saskaitās, ja secīgi** — divi atsevišķi `for` cikli = `O(n + n)` = `O(n)`
5. **Dalīšana uz pusi** parasti dod `O(log n)`

## Piemēri ar analīzi

### 1) Vienkāršs cikls

```python
def sum_list(arr):
    total = 0           # O(1)
    for x in arr:       # O(n)
        total += x      # O(1)
    return total        # O(1)
# Kopā: O(n)
```

### 2) Divi secīgi cikli

```python
def find_min_max(arr):
    smallest = arr[0]
    for x in arr:           # O(n)
        if x < smallest: smallest = x
    largest = arr[0]
    for x in arr:           # O(n)
        if x > largest: largest = x
    return smallest, largest
# Kopā: O(n + n) = O(2n) = O(n)
```

### 3) Ieguldīti cikli

```python
def all_pairs(arr):
    for i in range(len(arr)):       # O(n)
        for j in range(len(arr)):   # O(n)
            print(arr[i], arr[j])   # O(1)
# Kopā: O(n × n) = O(n²)
```

### 4) Dažādi izmēri

```python
def matrix_search(matrix, target):
    for row in matrix:          # O(m)
        for cell in row:        # O(n)
            if cell == target:
                return True
    return False
# Kopā: O(m × n)
```

Kad `m` un `n` ir dažādi, **abi paliek** notācijā.

### 5) Bināra meklēšana rekursīvi

```python
def binary_search(arr, target, low, high):
    if low > high:
        return -1
    mid = (low + high) // 2
    if arr[mid] == target:
        return mid
    if arr[mid] < target:
        return binary_search(arr, target, mid + 1, high)
    return binary_search(arr, target, low, mid - 1)
# Katrā solī problēma uz pusi mazāka → O(log n)
```

### 6) Saskaitītas struktūras

```python
def is_anagram(a, b):
    return sorted(a) == sorted(b)
# sorted() = O(n log n)
# == = O(n)
# Kopā: O(n log n) — dominē kārtošana
```

## Atmiņas sarežģītība (Space Complexity)

Big O attiecas arī uz **atmiņas patēriņu**, ne tikai laiku.

```python
# O(1) atmiņas — neatkarīgi no n
def sum_arr(arr):
    total = 0
    for x in arr:
        total += x
    return total

# O(n) atmiņas — kopēja masīvu
def double_arr(arr):
    return [x * 2 for x in arr]

# O(n) atmiņas — rekursijas stack
def factorial(n):
    if n <= 1: return 1
    return n * factorial(n - 1)
```

## Big O vs reāls ātrums

Big O ir **augšanas tendence**, ne precīzs ātrums. O(n) ar lielu konstanti var būt lēnāks par O(n²) ar mazu konstanti — bet **tikai maziem n**.

```python
# Hipotētiski:
# Algoritms A: 100n operācijas → O(n)
# Algoritms B: n² operācijas → O(n²)

# n = 10:    A = 1000, B = 100   → B ātrāks
# n = 100:   A = 10000, B = 10000 → vienādi
# n = 1000:  A = 100000, B = 1000000 → A ātrāks
# n = 10⁶:   A = 10⁸, B = 10¹² → A daudz ātrāks
```

Tāpēc Big O ir svarīgs **lielajām vērtībām**.

## Big O nelielas problēmas

Maziem datiem (n < 100) Big O nav tik svarīgs — pat O(n²) var būt lielisks. Tas, ka algoritmam ir laba Big O, nenozīmē, ka tas vienmēr ir labākā izvēle:

- **Konstantes** — Quick Sort un Heap Sort abi ir O(n log n), bet Quick Sort parasti ir ~3× ātrāks praksē
- **Kešs (CPU cache)** — secīgas atmiņas piekļuves ir ātrākas par izlases piekļuvēm
- **Algoritma vienkāršība** — Bubble Sort maziem datiem ir vienkārši laba izvēle

## Big O un Python iebūvētās funkcijas

| Operācija | Laika sarežģītība |
|-----------|-------------------|
| `arr[i]` | O(1) |
| `arr.append(x)` | O(1) amortizēta |
| `arr.insert(0, x)` | O(n) |
| `arr.remove(x)` | O(n) |
| `arr.sort()` | O(n log n) |
| `x in list` | O(n) |
| `x in dict` | O(1) vidēji |
| `x in set` | O(1) vidēji |
| `len(arr)` | O(1) |
| `min(arr)`, `max(arr)` | O(n) |

## Praktiski padomi

1. **Pārveido O(n²) uz O(n)** ar hash map / set:
   ```python
   # Slikti: O(n²)
   def has_duplicate(arr):
       for i in range(len(arr)):
           for j in range(i+1, len(arr)):
               if arr[i] == arr[j]: return True
       return False

   # Labi: O(n)
   def has_duplicate(arr):
       seen = set()
       for x in arr:
           if x in seen: return True
           seen.add(x)
       return False
   ```

2. **Memoizācija** pārvērš eksponenciālus rekursīvus algoritmus par lineāriem:
   ```python
   from functools import lru_cache

   @lru_cache(maxsize=None)
   def fib(n):
       return n if n < 2 else fib(n-1) + fib(n-2)
   ```

3. **Kārto datus**, ja paredzi daudzas meklēšanas — tad bināra meklēšana O(log n) vietā O(n).

## Kopsavilkums

Big O analīze ir prasme, kas nāk ar praksi. Saskaiti ciklus, atmet konstantes, paturi dominējošo termini. Atceries — Big O domāts liela mēroga datiem. Optimizē tur, kur tas svarīgi. Bieži O(n²) → O(n) panākams ar set vai dict, eksponenciāla rekursija → lineāra ar memoizāciju.
