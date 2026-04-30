# Java ievads un darba vide

Java ir stipri tipizēta, objektorientēta programmēšanas valoda, kuras kods tiek
kompilēts uz starpkodu jeb *bytecode* un izpildīts JVM (Java Virtual Machine)
videi. Tas pats `.class` fails strādā Linux, Windows un macOS — JVM izpilda kodu
identiski.

## Galvenās lietas, kas jāzina

- **Failu paplašinājums** — pirmkods glabājas `.java` failos.
- **Klases** — viss kods Javā dzīvo klašu iekšienē. Pat *Hello, World!* vajag
  klasi un metodi `main`.
- **Iekapsulēta ieejas vieta** — programma sākas no `public static void main(String[] args)`.
- **Kompilators un izpilde** — `javac Main.java` izveido `Main.class`,
  ko palaiž ar `java Main`.

```java
public class Main {
    public static void main(String[] args) {
        System.out.println("Sveika, Java!");
    }
}
```

## Kāpēc Java šim kursam?

Java ir izvēlēta kā otrā atbalstītā valoda blakus Python, jo:

- tā ir plaši izmantota industrijā (Android, banku sistēmas, lielas Web lietotnes);
- tās stingrā tipu sistēma palīdz iemācīties domāt par datiem;
- tā darbojas tādā pašā JVM uz visām populārajām OS — neviens vairs neuztraucas
  par "manā datorā strādāja".

Šī tēma ir saīsināts ievads — pietiek, lai sāktu risināt vienkāršus uzdevumus.
