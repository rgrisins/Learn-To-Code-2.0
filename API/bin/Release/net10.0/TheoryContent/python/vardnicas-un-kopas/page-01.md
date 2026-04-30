# Vārdnīcas

Vārdnīca jeb `dict` glabā pārus: atslēga un vērtība. Tā ir piemērota datiem, kurus gribi atrast pēc nosaukuma.

```python
student = {
    "name": "Anna",
    "age": 17,
    "course": "Python"
}
```

Vērtību iegūst pēc atslēgas:

```python
print(student["name"])
```

Ja atslēga neeksistē, šāds pieraksts izmet kļūdu. Drošāk var izmantot `get`:

```python
print(student.get("email"))
print(student.get("email", "Nav norādīts"))
```

Vārdnīcas ir ļoti bieži sastopamas tīmekļa lietotnēs un API datos, jo tās labi atbilst JSON objektu struktūrai.

Labs atslēgas nosaukums ir konkrēts un nemainīgs. Piemēram, `first_name` ir labāks par `x`.
