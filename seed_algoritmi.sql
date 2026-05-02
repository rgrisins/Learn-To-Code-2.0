-- ===========================================================================
-- LearnToCode — "Algoritmi" sadaļas seed
--
-- Pievieno:
--   1) Algoritmi (kā jauna TheoryLanguage — tādas pašas tabulas, tā pati
--      struktūra kā Python/Java)
--   2) Tēma "Big O" (SortOrder = 1) — sarežģītības analīze, 3 lapas
--   3) Tēma "Kārtošanas algoritmi" (SortOrder = 2) — Bubble, Quick, Merge Sort
--   4) Atbilstoši TheoryContent metadati
--
-- MinIO objektiem jābūt šādā formā:
--   learn-to-code/theory/algoritmi/0001_big_o/page-01.md       (jauns)
--   learn-to-code/theory/algoritmi/0001_big_o/page-02.md       (jauns)
--   learn-to-code/theory/algoritmi/0001_big_o/page-03.md       (jauns)
--   learn-to-code/theory/algoritmi/0001_kartosanas_algoritmi/page-01.md  (esošais)
--   learn-to-code/theory/algoritmi/0001_kartosanas_algoritmi/page-02.md  (esošais)
--   learn-to-code/theory/algoritmi/0001_kartosanas_algoritmi/page-03.md  (esošais)
--
-- Piezīme: Kārtošanas algoritmu MinIO ceļš paliek 0001_kartosanas_algoritmi
-- (numerējums prefiksā nav saistīts ar SortOrder DB tabulā).
-- ===========================================================================

BEGIN;

-- 1) Algoritmi valoda (kategorijas ietvars)
INSERT INTO "TheoryLanguages" ("Title", "Description", "SortOrder")
VALUES (
    'Algoritmi',
    'Klasiskie algoritmi un datu struktūras — kārtošana, meklēšana, grafi un dinamiskā programmēšana ar piemēriem un sarežģītības analīzi.',
    99
)
ON CONFLICT ("Title") DO UPDATE
SET
    "Description" = EXCLUDED."Description",
    "SortOrder" = EXCLUDED."SortOrder";


-- 2) Tēmas "Big O" un "Kārtošanas algoritmi"
WITH lang AS (
    SELECT "Id" FROM "TheoryLanguages" WHERE "Title" = 'Algoritmi' LIMIT 1
),
topic_data ("Title", "Difficulty", "Description", "EstimatedMinutes", "SortOrder", "MarkdownObjectName", "PageCount") AS (
    VALUES
        ('Big O', 'Iesācējs',
         'Algoritmu sarežģītības analīze: kas ir Big O, sarežģītības klases (O(1), O(log n), O(n), O(n log n), O(n²)) un kā analizēt kodu.',
         35, 1, 'algoritmi/0001_big_o', 3),
        ('Kārtošanas algoritmi', 'Vidējs',
         'Trīs klasiskie kārtošanas algoritmi: Bubble Sort (vienkāršs O(n²)), Quick Sort (ātrs O(n log n) ar pivot stratēģiju) un Merge Sort (stabils O(n log n) ar sadali-un-valdi).',
         45, 2, 'algoritmi/0001_kartosanas_algoritmi', 3)
),
updated_topics AS (
    UPDATE "TheoryTopics" topic
    SET
        "Title" = topic_data."Title",
        "Difficulty" = topic_data."Difficulty",
        "Description" = topic_data."Description",
        "EstimatedMinutes" = topic_data."EstimatedMinutes"
    FROM topic_data
    CROSS JOIN lang
    WHERE topic."LanguageId" = lang."Id"
      AND topic."SortOrder" = topic_data."SortOrder"
    RETURNING topic."Id", topic."SortOrder"
),
inserted_topics AS (
    INSERT INTO "TheoryTopics" (
        "LanguageId",
        "Title",
        "Difficulty",
        "Description",
        "EstimatedMinutes",
        "SortOrder"
    )
    SELECT
        lang."Id",
        topic_data."Title",
        topic_data."Difficulty",
        topic_data."Description",
        topic_data."EstimatedMinutes",
        topic_data."SortOrder"
    FROM topic_data
    CROSS JOIN lang
    WHERE NOT EXISTS (
        SELECT 1 FROM updated_topics u WHERE u."SortOrder" = topic_data."SortOrder"
    )
    ON CONFLICT ("LanguageId", "Title") DO UPDATE
    SET
        "Difficulty" = EXCLUDED."Difficulty",
        "Description" = EXCLUDED."Description",
        "EstimatedMinutes" = EXCLUDED."EstimatedMinutes",
        "SortOrder" = EXCLUDED."SortOrder"
    RETURNING "Id", "SortOrder"
),
all_topics AS (
    SELECT "Id", "SortOrder" FROM updated_topics
    UNION ALL
    SELECT "Id", "SortOrder" FROM inserted_topics
)
INSERT INTO "TheoryContents" (
    "TopicId",
    "MarkdownObjectName",
    "PageCount",
    "Version"
)
SELECT
    all_topics."Id",
    topic_data."MarkdownObjectName",
    topic_data."PageCount",
    1
FROM all_topics
JOIN topic_data ON topic_data."SortOrder" = all_topics."SortOrder"
ON CONFLICT ("TopicId") DO UPDATE
SET
    "MarkdownObjectName" = EXCLUDED."MarkdownObjectName",
    "PageCount" = EXCLUDED."PageCount",
    "Version" = GREATEST("TheoryContents"."Version", 1);

COMMIT;
