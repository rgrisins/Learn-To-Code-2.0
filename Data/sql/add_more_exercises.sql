-- LearnToCode papildu praktiskie uzdevumi.
-- Izpilde: psql -d learn_to_code -f Data/sql/add_more_exercises.sql
--
-- Katram uzdevumam pirmie divi testpiemēri ir redzami kā paraugi,
-- pārējie ir slēptie testi automātiskajai pārbaudei.

WITH exercise AS (
    INSERT INTO "Exercises"
        ("Title", "Description", "LanguageCode", "LanguageVersion", "Difficulty", "SortOrder", "AuthorId", "CreatedAtUtc")
    VALUES
        (
            'Skaitļu summa intervālā',
            'Dots viens naturāls skaitlis n. Aprēķini visu veselo skaitļu summu no 1 līdz n ieskaitot. Ievadē ir viens skaitlis n, izvadei jābūt vienam skaitlim - summai.',
            'python',
            '3.11',
            'viegls',
            COALESCE((SELECT MAX("SortOrder") FROM "Exercises"), 0) + 1,
            NULL,
            NOW()
        )
    RETURNING "Id"
)
INSERT INTO "ExerciseTestCases" ("ExerciseId", "Input", "ExpectedOutput", "IsHidden", "OrderIndex")
SELECT "Id", data.input, data.expected, data.hidden, data.order_index
FROM exercise
CROSS JOIN (VALUES
    ('5', '15', FALSE, 0),
    ('10', '55', FALSE, 1),
    ('1', '1', TRUE, 2),
    ('100', '5050', TRUE, 3),
    ('250', '31375', TRUE, 4),
    ('999', '499500', TRUE, 5),
    ('1234', '761995', TRUE, 6),
    ('10000', '50005000', TRUE, 7)
) AS data(input, expected, hidden, order_index);

WITH exercise AS (
    INSERT INTO "Exercises"
        ("Title", "Description", "LanguageCode", "LanguageVersion", "Difficulty", "SortOrder", "AuthorId", "CreatedAtUtc")
    VALUES
        (
            'Vārda apgriešana',
            'Dots viens vārds. Izvadi šo vārdu apgrieztā secībā. Ievadē nav atstarpju, izvadei jābūt tikai apgrieztajam vārdam.',
            'python',
            '3.11',
            'viegls',
            COALESCE((SELECT MAX("SortOrder") FROM "Exercises"), 0) + 1,
            NULL,
            NOW()
        )
    RETURNING "Id"
)
INSERT INTO "ExerciseTestCases" ("ExerciseId", "Input", "ExpectedOutput", "IsHidden", "OrderIndex")
SELECT "Id", data.input, data.expected, data.hidden, data.order_index
FROM exercise
CROSS JOIN (VALUES
    ('kods', 'sdok', FALSE, 0),
    ('python', 'nohtyp', FALSE, 1),
    ('a', 'a', TRUE, 2),
    ('ritenis', 'sinetir', TRUE, 3),
    ('algoritms', 'smtirogla', TRUE, 4),
    ('learn', 'nrael', TRUE, 5),
    ('dators', 'srotad', TRUE, 6),
    ('programm', 'mmargorp', TRUE, 7)
) AS data(input, expected, hidden, order_index);

WITH exercise AS (
    INSERT INTO "Exercises"
        ("Title", "Description", "LanguageCode", "LanguageVersion", "Difficulty", "SortOrder", "AuthorId", "CreatedAtUtc")
    VALUES
        (
            'Pāra skaitļu skaits',
            'Pirmajā rindā dots skaitlis n. Otrajā rindā doti n veseli skaitļi. Izvadi, cik no tiem ir pāra skaitļi.',
            'python',
            '3.11',
            'viegls',
            COALESCE((SELECT MAX("SortOrder") FROM "Exercises"), 0) + 1,
            NULL,
            NOW()
        )
    RETURNING "Id"
)
INSERT INTO "ExerciseTestCases" ("ExerciseId", "Input", "ExpectedOutput", "IsHidden", "OrderIndex")
SELECT "Id", data.input, data.expected, data.hidden, data.order_index
FROM exercise
CROSS JOIN (VALUES
    (E'5\n1 2 3 4 5', '2', FALSE, 0),
    (E'4\n2 4 6 8', '4', FALSE, 1),
    (E'3\n1 3 5', '0', TRUE, 2),
    (E'6\n0 -2 7 9 12 13', '3', TRUE, 3),
    (E'1\n10', '1', TRUE, 4),
    (E'8\n11 12 13 14 15 16 17 18', '4', TRUE, 5),
    (E'7\n-3 -2 -1 0 1 2 3', '3', TRUE, 6),
    (E'10\n5 10 15 20 25 30 35 40 45 50', '5', TRUE, 7)
) AS data(input, expected, hidden, order_index);

WITH exercise AS (
    INSERT INTO "Exercises"
        ("Title", "Description", "LanguageCode", "LanguageVersion", "Difficulty", "SortOrder", "AuthorId", "CreatedAtUtc")
    VALUES
        (
            'Lielākais skaitlis sarakstā',
            'Pirmajā rindā dots skaitlis n. Otrajā rindā doti n veseli skaitļi. Izvadi lielāko no dotajiem skaitļiem.',
            'python',
            '3.11',
            'vidējs',
            COALESCE((SELECT MAX("SortOrder") FROM "Exercises"), 0) + 1,
            NULL,
            NOW()
        )
    RETURNING "Id"
)
INSERT INTO "ExerciseTestCases" ("ExerciseId", "Input", "ExpectedOutput", "IsHidden", "OrderIndex")
SELECT "Id", data.input, data.expected, data.hidden, data.order_index
FROM exercise
CROSS JOIN (VALUES
    (E'5\n1 9 3 4 2', '9', FALSE, 0),
    (E'4\n-5 -2 -10 -3', '-2', FALSE, 1),
    (E'1\n42', '42', TRUE, 2),
    (E'6\n7 7 7 7 7 7', '7', TRUE, 3),
    (E'8\n0 15 3 99 4 18 20 1', '99', TRUE, 4),
    (E'5\n-100 0 100 -50 25', '100', TRUE, 5),
    (E'7\n12 18 6 24 30 1 29', '30', TRUE, 6),
    (E'10\n5 4 3 2 1 0 -1 -2 -3 -4', '5', TRUE, 7)
) AS data(input, expected, hidden, order_index);

WITH exercise AS (
    INSERT INTO "Exercises"
        ("Title", "Description", "LanguageCode", "LanguageVersion", "Difficulty", "SortOrder", "AuthorId", "CreatedAtUtc")
    VALUES
        (
            'Pirmskaitļa pārbaude',
            'Dots viens vesels skaitlis n. Izvadi "JA", ja skaitlis ir pirmskaitlis, citādi izvadi "NE".',
            'python',
            '3.11',
            'vidējs',
            COALESCE((SELECT MAX("SortOrder") FROM "Exercises"), 0) + 1,
            NULL,
            NOW()
        )
    RETURNING "Id"
)
INSERT INTO "ExerciseTestCases" ("ExerciseId", "Input", "ExpectedOutput", "IsHidden", "OrderIndex")
SELECT "Id", data.input, data.expected, data.hidden, data.order_index
FROM exercise
CROSS JOIN (VALUES
    ('7', 'JA', FALSE, 0),
    ('9', 'NE', FALSE, 1),
    ('1', 'NE', TRUE, 2),
    ('2', 'JA', TRUE, 3),
    ('49', 'NE', TRUE, 4),
    ('97', 'JA', TRUE, 5),
    ('100', 'NE', TRUE, 6),
    ('997', 'JA', TRUE, 7)
) AS data(input, expected, hidden, order_index);
