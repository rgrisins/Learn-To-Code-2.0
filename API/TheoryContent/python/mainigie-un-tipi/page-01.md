# Mainīgie

Mainīgais ir nosaukums, kas norāda uz vērtību. Python nav jāraksta mainīgā tips pirms tā izmantošanas, jo tips tiek noteikts pēc piešķirtās vērtības.

```python
age = 16
name = "Marta"
is_student = True
```

Šeit `age` glabā veselu skaitli, `name` glabā tekstu, bet `is_student` glabā patiesuma vērtību. Mainīgā nosaukumam jābūt saprotamam, jo kodu biežāk lasa nekā raksta.

Python piešķiršana nenozīmē, ka vērtība tiek ielikta kastītē uz visiem laikiem. Drīzāk mainīgais ir etiķete, kas norāda uz objektu. Vienam objektam var būt vairākas etiķetes.

```python
a = [1, 2, 3]
b = a
b.append(4)
print(a)
```

Rezultātā `a` arī saturēs `4`, jo `a` un `b` norāda uz vienu un to pašu sarakstu. Šī doma kļūs īpaši svarīga pie sarakstiem un funkcijām.
