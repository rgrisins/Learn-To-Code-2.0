# Loģiskie operatori

Ar `and`, `or` un `not` var veidot sarežģītākus nosacījumus.

```python
age = 20
has_ticket = True

if age >= 18 and has_ticket:
    print("Drīkst ienākt")
```

`and` ir patiess tikai tad, ja abas puses ir patiesas. `or` ir patiess, ja vismaz viena puse ir patiesa.

```python
is_admin = False
is_teacher = True

if is_admin or is_teacher:
    print("Var rediģēt saturu")
```

`not` apgriež vērtību:

```python
is_blocked = False

if not is_blocked:
    print("Konts aktīvs")
```

Ja nosacījums kļūst grūti lasāms, sadali to pa nosaukumiem:

```python
is_adult = age >= 18
can_enter = is_adult and has_ticket
```

Šādi programma kļūst tuvāka cilvēka valodai.
