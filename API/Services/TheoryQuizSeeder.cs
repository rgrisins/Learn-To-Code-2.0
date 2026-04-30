using LearnToCode.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnToCode.API.Services;

internal static class TheoryQuizSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext)
    {
        var seeds = GetSeeds();

        foreach (var seed in seeds)
        {
            var topics = await dbContext.TheoryTopics
                .Include(t => t.Language)
                .Include(t => t.Quiz)
                    .ThenInclude(quiz => quiz!.Questions)
                        .ThenInclude(question => question.Options)
                .Where(t => t.Language!.Title.ToLower() == seed.LanguageCode)
                .ToListAsync();

            var topic = topics.FirstOrDefault(t => TheoryTopicKey.Matches(t.Title, seed.TopicSlug));

            if (topic is null)
            {
                continue;
            }

            if (topic.Quiz is null)
            {
                dbContext.TheoryQuizzes.Add(BuildQuiz(topic.Id, seed));
                continue;
            }

            CompleteExistingQuiz(topic.Quiz, seed);
        }

        await dbContext.SaveChangesAsync();
    }

    private static TheoryQuiz BuildQuiz(int topicId, QuizSeed seed)
    {
        return new TheoryQuiz
        {
            TopicId = topicId,
            Title = seed.Title,
            Description = seed.Description,
            Questions = seed.Questions
                .Select((questionSeed, questionIndex) => BuildQuestion(questionSeed, questionIndex))
                .ToList(),
        };
    }

    private static TheoryQuizQuestion BuildQuestion(QuestionSeed questionSeed, int questionIndex)
    {
        return new TheoryQuizQuestion
        {
            OrderIndex = questionIndex,
            Prompt = questionSeed.Prompt,
            Explanation = questionSeed.Explanation,
            Options = questionSeed.Options
                .Select((optionText, optionIndex) => new TheoryQuizOption
                {
                    OrderIndex = optionIndex,
                    Text = optionText,
                    IsCorrect = optionIndex == questionSeed.CorrectIndex,
                })
                .ToList(),
        };
    }

    private static void CompleteExistingQuiz(TheoryQuiz quiz, QuizSeed seed)
    {
        if (string.IsNullOrWhiteSpace(quiz.Title))
        {
            quiz.Title = seed.Title;
        }

        if (string.IsNullOrWhiteSpace(quiz.Description))
        {
            quiz.Description = seed.Description;
        }

        foreach (var questionSeed in seed.Questions.Select((value, index) => new { value, index }))
        {
            var existingQuestion = quiz.Questions.FirstOrDefault(question => question.OrderIndex == questionSeed.index);
            if (existingQuestion is null)
            {
                quiz.Questions.Add(BuildQuestion(questionSeed.value, questionSeed.index));
                continue;
            }

            CompleteExistingQuestion(existingQuestion, questionSeed.value);
        }
    }

    private static bool CompleteExistingQuestion(TheoryQuizQuestion question, QuestionSeed seed)
    {
        var changed = false;

        if (string.IsNullOrWhiteSpace(question.Prompt))
        {
            question.Prompt = seed.Prompt;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(question.Explanation))
        {
            question.Explanation = seed.Explanation;
            changed = true;
        }

        var hasCorrectOption = question.Options.Any(option => option.IsCorrect);

        foreach (var optionSeed in seed.Options.Select((value, index) => new { value, index }))
        {
            var existingOption = question.Options.FirstOrDefault(option => option.OrderIndex == optionSeed.index);
            if (existingOption is not null)
            {
                continue;
            }

            question.Options.Add(new TheoryQuizOption
            {
                OrderIndex = optionSeed.index,
                Text = optionSeed.value,
                IsCorrect = !hasCorrectOption && optionSeed.index == seed.CorrectIndex,
            });
            changed = true;
        }

        return changed;
    }

    private static List<QuizSeed> GetSeeds() =>
    [
        new QuizSeed(
            LanguageCode: "python",
            TopicSlug: "ievads-un-darba-vide",
            Title: "Tests: Ievads un darba vide",
            Description: "Pārbaudi savas zināšanas par Python interpretatoru, REPL un pirmo programmu.",
            Questions:
            [
                new QuestionSeed(
                    "Kāda ir Python pirmkoda failu standarta paplašinājums?",
                    [".py", ".python", ".pyt", ".pys"],
                    CorrectIndex: 0,
                    Explanation: "Python skripti tiek glabāti failos ar paplašinājumu .py."),
                new QuestionSeed(
                    "Ko nozīmē saīsinājums REPL?",
                    [
                        "Read–Evaluate–Print Loop",
                        "Run–Edit–Print Library",
                        "Read–Edit–Process Language",
                        "Recursive Evaluation Pattern Loop",
                    ],
                    CorrectIndex: 0,
                    Explanation: "REPL = Read, Evaluate, Print, Loop — interaktīvā Python sesija."),
                new QuestionSeed(
                    "Ar kuru komandu parasti izvada tekstu uz ekrāna Python valodā?",
                    ["print(\"sveiki\")", "echo \"sveiki\"", "console.log(\"sveiki\")", "System.out.println(\"sveiki\")"],
                    CorrectIndex: 0,
                    Explanation: "print() ir Python iebūvētā funkcija teksta izvadei."),
                new QuestionSeed(
                    "Kā komandrindā palaist failu app.py?",
                    ["python app.py", "run app.py", "py-run app.py", "execute app.py"],
                    CorrectIndex: 0,
                    Explanation: "Palaišanai izmanto interpretatoru: python app.py vai python3 app.py."),
                new QuestionSeed(
                    "Kurš no šiem ir vislabākais veids, kā pārbaudīt Python versiju komandrindā?",
                    ["python --version", "python -ver", "python ?", "python check"],
                    CorrectIndex: 0,
                    Explanation: "python --version (vai python -V) izvada uzstādīto Python versiju."),
                new QuestionSeed(
                    "Kura Python versija šobrīd ir tipiska iesācējam mācīšanas vidē?",
                    ["3.x", "2.x", "1.x", "4.x"],
                    CorrectIndex: 0,
                    Explanation: "Python 3.x ir mūsdienu standarts; Python 2 vairs nav atbalstīts."),
                new QuestionSeed(
                    "Ko Python interpretators dara ar # zīmi koda rindā?",
                    [
                        "Apzīmē komentāru — interpretators pārējo rindu ignorē",
                        "Iezīmē rindu kā galveno",
                        "Izsauc kompilāciju",
                        "Definē jaunu mainīgo",
                    ],
                    CorrectIndex: 0,
                    Explanation: "Viss aiz # līdz rindas beigām ir komentārs un netiek izpildīts."),
                new QuestionSeed(
                    "Kura no šīm ir derīga Python instrukcija?",
                    [
                        "print(\"sveiks, pasaule\")",
                        "PRINT \"sveiks, pasaule\"",
                        "echo(\"sveiks, pasaule\");",
                        "writeln(\"sveiks, pasaule\")",
                    ],
                    CorrectIndex: 0,
                    Explanation: "Python ir reģistrjutīga: tikai print(...) ar mazajiem burtiem ir derīga."),
                new QuestionSeed(
                    "Kurš no apgalvojumiem par REPL ir patiess?",
                    [
                        "Tas ir interaktīvs režīms — katra ievadītā rinda tiek nekavējoties izpildīta",
                        "Tas saglabā kodu failā automātiski",
                        "Tas darbojas tikai Windows operētājsistēmā",
                        "REPL atbalsta tikai print() funkciju",
                    ],
                    CorrectIndex: 0,
                    Explanation: "REPL pieņem vienu izteiksmi, izpilda to un parāda rezultātu."),
                new QuestionSeed(
                    "Kurš ir labs paradums sākot jaunu projektu?",
                    [
                        "Izveidot atsevišķu mapi un virtuālo vidi",
                        "Vienmēr instalēt visas paketes globāli",
                        "Strādāt tieši darbavirsmā bez mapju struktūras",
                        "Lietot tikai REPL bez failiem",
                    ],
                    CorrectIndex: 0,
                    Explanation: "Atsevišķa mape un virtuālā vide novērš atkarību konfliktus."),
            ]),
        new QuizSeed(
            LanguageCode: "python",
            TopicSlug: "mainigie-un-datu-tipi",
            Title: "Tests: Mainīgie un datu tipi",
            Description: "Pārbaudi zināšanas par mainīgajiem, skaitļiem, tekstu, booleans un None.",
            Questions:
            [
                new QuestionSeed(
                    "Kura no šīm ir derīga Python mainīgā piešķiršana?",
                    ["x = 5", "5 = x", "let x = 5", "var x := 5"],
                    CorrectIndex: 0,
                    Explanation: "Python piešķiršana: mainīgā_nosaukums = vērtība."),
                new QuestionSeed(
                    "Kāds tips ir vērtībai 3.14?",
                    ["float", "int", "str", "bool"],
                    CorrectIndex: 0,
                    Explanation: "Skaitļi ar decimālo daļu Python ir float."),
                new QuestionSeed(
                    "Ko izvadīs print(type(\"42\"))?",
                    [
                        "<class 'str'>",
                        "<class 'int'>",
                        "<class 'float'>",
                        "<class 'number'>",
                    ],
                    CorrectIndex: 0,
                    Explanation: "Pēdiņās ietverta vērtība ir teksts (str), neraugoties uz saturu."),
                new QuestionSeed(
                    "Kurš operators veic veselo skaitļu dalīšanu Python valodā?",
                    ["//", "/", "%", "**"],
                    CorrectIndex: 0,
                    Explanation: "// ir veselo skaitļu dalīšana (rezultāts noapaļots uz leju)."),
                new QuestionSeed(
                    "Kā pārvērst tekstu \"7\" par veselu skaitli?",
                    ["int(\"7\")", "str(7)", "float(\"7\")", "bool(\"7\")"],
                    CorrectIndex: 0,
                    Explanation: "int() pieņem virkni un atgriež veselu skaitli (ja iespējams)."),
                new QuestionSeed(
                    "Kurš no šiem ir derīgs mainīgā nosaukums?",
                    ["my_value", "1value", "my-value", "for"],
                    CorrectIndex: 0,
                    Explanation: "Mainīgais nedrīkst sākties ar ciparu, saturēt defisi vai būt atslēgvārds."),
                new QuestionSeed(
                    "Kāda ir vērtības None nozīme?",
                    [
                        "Tas apzīmē vērtības trūkumu",
                        "Tas ir skaitlis nulle",
                        "Tas ir tukša teksta virkne",
                        "Tas ir viltus (False)",
                    ],
                    CorrectIndex: 0,
                    Explanation: "None Python apzīmē \"nekā nav\" — atšķirīgi no 0, '' vai False."),
                new QuestionSeed(
                    "Kura izteiksme atgriež True?",
                    ["bool(\"teksts\")", "bool(\"\")", "bool(0)", "bool(None)"],
                    CorrectIndex: 0,
                    Explanation: "Tukša virkne, 0 un None ir falsy; jebkura ne-tukša virkne ir truthy."),
                new QuestionSeed(
                    "Cik ir 7 % 3?",
                    ["1", "2", "3", "0"],
                    CorrectIndex: 0,
                    Explanation: "% ir atlikuma operators: 7 dalīts ar 3 ir 2 ar atlikumu 1."),
                new QuestionSeed(
                    "Kas notiek izpildot a, b = 1, 2?",
                    [
                        "a iegūst vērtību 1, b iegūst vērtību 2 (kortežu izpakošana)",
                        "Abiem mainīgajiem tiek piešķirts viens kortežs (1, 2)",
                        "Tā ir sintakses kļūda",
                        "Tikai a iegūst 1; b paliek nedefinēts",
                    ],
                    CorrectIndex: 0,
                    Explanation: "Tā ir paralēla piešķiršana — Python sadala vērtības starp mainīgajiem."),
            ]),
        new QuizSeed(
            LanguageCode: "python",
            TopicSlug: "nosacijumi-un-logika",
            Title: "Tests: Nosacījumi un loģika",
            Description: "Pārbaudi if/elif/else lietojumu, salīdzinājumus un loģiskos operatorus.",
            Questions:
            [
                new QuestionSeed(
                    "Ar ko Python valodā parasti uzraksta zaru, ja nosacījums NAV izpildīts?",
                    ["else", "otherwise", "default", "fallback"],
                    CorrectIndex: 0,
                    Explanation: "if ... else: zars izpildās, kad galvenais nosacījums ir False."),
                new QuestionSeed(
                    "Kurš operators pārbauda vienlīdzību?",
                    ["==", "=", "is=", "==="],
                    CorrectIndex: 0,
                    Explanation: "= ir piešķiršana; == ir salīdzinājums."),
                new QuestionSeed(
                    "Ko atgriež izteiksme True and False?",
                    ["False", "True", "None", "Kļūdu"],
                    CorrectIndex: 0,
                    Explanation: "and atgriež True tikai tad, ja abi operandi ir patiesi."),
                new QuestionSeed(
                    "Kura konstrukcija ir pareiza?",
                    [
                        "if x > 0:\\n    print(x)",
                        "if (x > 0)\\n    print(x);",
                        "if x > 0 then print(x)",
                        "if: x > 0 print(x)",
                    ],
                    CorrectIndex: 0,
                    Explanation: "Python prasa kolu un atkāpi; iekavu izteiksmes apkārt nav obligātas."),
                new QuestionSeed(
                    "Ko nozīmē operators not?",
                    [
                        "Apgriež loģisko vērtību (True kļūst False un otrādi)",
                        "Salīdzina divus skaitļus",
                        "Izsauc izņēmumu",
                        "Definē jaunu mainīgo",
                    ],
                    CorrectIndex: 0,
                    Explanation: "not ir vienargumenta loģiskais operators — apgriež patiesumu."),
                new QuestionSeed(
                    "Kāds ir izteiksmes 5 < 3 or 4 == 4 rezultāts?",
                    ["True", "False", "None", "Kļūda"],
                    CorrectIndex: 0,
                    Explanation: "or ir patiess, ja vismaz viena puse ir patiesa; 4 == 4 ir True."),
                new QuestionSeed(
                    "Kurš no šiem ir Python truthy?",
                    ["[1, 2]", "[]", "0", "None"],
                    CorrectIndex: 0,
                    Explanation: "Tukšs saraksts, 0 un None ir falsy; ne-tukšs saraksts ir truthy."),
                new QuestionSeed(
                    "Kā pareizi pievienot nosacījumu \"vai citādi, ja\"?",
                    ["elif", "elseif", "else if", "elsif"],
                    CorrectIndex: 0,
                    Explanation: "Python lieto saliktu atslēgvārdu elif."),
                new QuestionSeed(
                    "Ko atgriež 3 < 5 < 10?",
                    [
                        "True — Python atbalsta ķēdes salīdzinājumus",
                        "Sintakses kļūdu",
                        "False",
                        "None",
                    ],
                    CorrectIndex: 0,
                    Explanation: "Python atļauj 3 < 5 < 10 — abi salīdzinājumi tiek apvienoti ar and."),
                new QuestionSeed(
                    "Kurš operators ir ekvivalents a != b nozīmei?",
                    [
                        "not (a == b)",
                        "a == b",
                        "a is b",
                        "a < b",
                    ],
                    CorrectIndex: 0,
                    Explanation: "!= un not == dod tādu pašu rezultātu."),
            ]),
        new QuizSeed(
            LanguageCode: "python",
            TopicSlug: "cikli",
            Title: "Tests: Cikli",
            Description: "Pārbaudi for, while, range, break un continue lietojumu.",
            Questions:
            [
                new QuestionSeed(
                    "Cik reižu izpildīsies cikls for i in range(5):?",
                    ["5", "4", "6", "Bezgalīgi"],
                    CorrectIndex: 0,
                    Explanation: "range(5) izveido 0..4 — pavisam 5 iterācijas."),
                new QuestionSeed(
                    "Ko dara komanda break?",
                    [
                        "Pārtrauc pašreizējo ciklu",
                        "Izlaiž tikai vienu iterāciju",
                        "Atgriežas no funkcijas",
                        "Pauzē programmu",
                    ],
                    CorrectIndex: 0,
                    Explanation: "break iziet no ietverošā cikla pirms tā dabīgā gala."),
                new QuestionSeed(
                    "Ko dara continue?",
                    [
                        "Izlaiž atlikušo ciklā kodu un pāriet pie nākamās iterācijas",
                        "Pārtrauc visu programmu",
                        "Iziet no funkcijas",
                        "Atkārto pēdējo iterāciju",
                    ],
                    CorrectIndex: 0,
                    Explanation: "continue tikai izlaiž atlikušās rindas un sāk nākamo iterāciju."),
                new QuestionSeed(
                    "Kurš ir cikla galvenais lietošanas gadījums?",
                    [
                        "Atkārtot kodu vairākas reizes vai iterēt pa kolekciju",
                        "Definēt funkciju",
                        "Glabāt datus",
                        "Importēt moduļus",
                    ],
                    CorrectIndex: 0,
                    Explanation: "Cikls ir konstrukcija atkārtošanai vai iterēšanai."),
                new QuestionSeed(
                    "Kāds ir izteiksmes range(2, 10, 2) rezultāts (kā saraksts)?",
                    ["[2, 4, 6, 8]", "[2, 4, 6, 8, 10]", "[2, 3, 4, 5, 6, 7, 8, 9]", "[1, 3, 5, 7, 9]"],
                    CorrectIndex: 0,
                    Explanation: "range(start, stop, step) — sākot ar 2, beidzot pirms 10, ar soli 2."),
                new QuestionSeed(
                    "Kurš cikls ir piemērots, kad atkārtojumu skaits IR zināms iepriekš?",
                    ["for", "while True", "do-while", "loop until"],
                    CorrectIndex: 0,
                    Explanation: "for ir parocīgs, ja zinām vērtību kopu vai diapazonu."),
                new QuestionSeed(
                    "Ko izvadīs šis kods? \\nfor i in range(3):\\n    print(i)",
                    [
                        "0\\n1\\n2",
                        "1\\n2\\n3",
                        "0\\n1\\n2\\n3",
                        "1 2 3",
                    ],
                    CorrectIndex: 0,
                    Explanation: "range(3) ir 0, 1, 2 — katrs uz savas rindas."),
                new QuestionSeed(
                    "Kurš ir BIEŽAKAIS bezgalīgā cikla cēlonis?",
                    [
                        "while-cikla nosacījums nekad nekļūst False",
                        "Pārāk daudz mainīgo",
                        "Nepareiza atkāpe komentārā",
                        "Pārāk maza atmiņa",
                    ],
                    CorrectIndex: 0,
                    Explanation: "Ja while nosacījums nekad nekļūst False, cikls neapstājas."),
                new QuestionSeed(
                    "Ko dara else zars pie cikla (for ... else)?",
                    [
                        "Izpildās, ja cikls beidzās DABĪGI (bez break)",
                        "Izpildās tikai uz pirmās iterācijas",
                        "Izpildās tikai pie break",
                        "Tas ir sintakses kļūda",
                    ],
                    CorrectIndex: 0,
                    Explanation: "for/while ar else: else izpildās, ja cikls nepārtraucās ar break."),
                new QuestionSeed(
                    "Kā iterēt pa sarakstu items un saglabāt indeksu?",
                    [
                        "for index, value in enumerate(items):",
                        "for index in items:",
                        "for items.index(value):",
                        "while items.next():",
                    ],
                    CorrectIndex: 0,
                    Explanation: "enumerate(items) atgriež pārus (indekss, vērtība)."),
            ]),
        new QuizSeed(
            LanguageCode: "java",
            TopicSlug: "java-ievads-un-darba-vide",
            Title: "Tests: Java ievads un darba vide",
            Description: "Pārbaudi savas pamatzināšanas par Java pirmkodu, JVM un kompilēšanu.",
            Questions:
            [
                new QuestionSeed(
                    "Kāds ir Java pirmkoda failu standarta paplašinājums?",
                    [".java", ".class", ".jvm", ".jar"],
                    CorrectIndex: 0,
                    Explanation: "Pirmkods glabājas .java failos; .class ir kompilētais bytecode."),
                new QuestionSeed(
                    "Ar kādu komandu kompilē Main.java?",
                    ["javac Main.java", "java Main.java", "compile Main.java", "javacc Main.java"],
                    CorrectIndex: 0,
                    Explanation: "javac ir Java kompilators; tas ražo Main.class failu."),
                new QuestionSeed(
                    "Kā palaiž jau kompilētu Main.class?",
                    ["java Main", "java Main.class", "run Main", "execute Main"],
                    CorrectIndex: 0,
                    Explanation: "java Main palaiž JVM ar klasi Main; klases nosaukumu lieto bez .class paplašinājuma."),
                new QuestionSeed(
                    "Kura no šīm ir derīga Java pirmkoda fragmenta sākuma daļa?",
                    [
                        "public class Main { public static void main(String[] args) { } }",
                        "class Main: def main(): pass",
                        "function Main() { }",
                        "void main() { }",
                    ],
                    CorrectIndex: 0,
                    Explanation: "Java prasa klasi un public static void main(String[] args) ieejas metodi."),
                new QuestionSeed(
                    "Ko nozīmē saīsinājums JVM?",
                    [
                        "Java Virtual Machine — virtuālā mašīna, kas izpilda bytecode",
                        "Java Variable Manager",
                        "Just Very Modern",
                        "Java Version Manager",
                    ],
                    CorrectIndex: 0,
                    Explanation: "JVM = Java Virtual Machine; tā izpilda .class failus."),
                new QuestionSeed(
                    "Kā izvada teksta rindu uz konsoli?",
                    [
                        "System.out.println(\"sveiki\");",
                        "print(\"sveiki\")",
                        "console.log(\"sveiki\");",
                        "echo \"sveiki\";",
                    ],
                    CorrectIndex: 0,
                    Explanation: "System.out.println ir standarta Java izvades metode."),
                new QuestionSeed(
                    "Kāda klase ļauj nolasīt ievaddatus no stdin?",
                    [
                        "java.util.Scanner",
                        "java.io.Reader",
                        "java.lang.System",
                        "java.util.Input",
                    ],
                    CorrectIndex: 0,
                    Explanation: "Scanner var nolasīt rindas, skaitļus un vārdus no jebkura InputStream."),
                new QuestionSeed(
                    "Kura komandrindas instrukcija parāda Java versiju?",
                    ["java --version", "java -ver", "java check", "java !"],
                    CorrectIndex: 0,
                    Explanation: "java --version (vai -version) izvada uzstādīto Java versiju."),
                new QuestionSeed(
                    "Kāda ir Javas tipu sistēma?",
                    [
                        "Stipri tipizēta — tipu nesaderību kompilators noķer kompilācijas laikā",
                        "Bezveida — viss ir Object",
                        "Dinamiski tipizēta tāpat kā Python",
                        "Bez tipiem",
                    ],
                    CorrectIndex: 0,
                    Explanation: "Java mainīgajiem ir konkrēts tips, kas tiek pārbaudīts kompilācijas laikā."),
                new QuestionSeed(
                    "Kāpēc tas pats Java .class fails strādā uz Linux, Windows un macOS?",
                    [
                        "Tas tiek izpildīts JVM, kas pielāgojas operētājsistēmai",
                        "Java kompilatora rezultāts ir mašīnkods",
                        "Java fails ir vienkārši teksts",
                        "Tas nav iespējams, vajag pārkompilēt katrai OS",
                    ],
                    CorrectIndex: 0,
                    Explanation: "JVM pārvar OS atšķirības — bytecode ir pārnēsājams."),
            ]),
    ];

    private sealed record QuizSeed(
        string LanguageCode,
        string TopicSlug,
        string Title,
        string Description,
        List<QuestionSeed> Questions);

    private sealed record QuestionSeed(
        string Prompt,
        string[] Options,
        int CorrectIndex,
        string? Explanation);
}
