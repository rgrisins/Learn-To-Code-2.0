-- ===========================================================================
-- LearnToCode — Users tabulas refaktorings
--
-- Izmaiņas:
--   1. Pārdēvē EducationInstitution → Representation
--   2. Noņem NormalizedUsername (Username turpmāk glabājas jau normalizēts)
--   3. Noņem NormalizedEmail (Email turpmāk glabājas jau normalizēts)
--   4. Noņem FullName (atvasināts no FirstName + LastName C# pusē)
--
-- Visu var atcelt, atjaunojot kolonnas un indeksus no backup.
-- Ieteicams pirms palaišanas izveidot backup: pg_dump ... > backup.sql
-- ===========================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- 1) EducationInstitution → Representation
-- ---------------------------------------------------------------------------
ALTER TABLE "Users" RENAME COLUMN "EducationInstitution" TO "Representation";


-- ---------------------------------------------------------------------------
-- 2) Username — pārveidoju uz lowercase un noņemu NormalizedUsername
-- ---------------------------------------------------------------------------

-- Vispirms pārliecinos, ka Username ir lowercase un trim
UPDATE "Users"
SET "Username" = LOWER(TRIM("Username"))
WHERE "Username" IS NOT NULL
  AND "Username" <> LOWER(TRIM("Username"));

-- Noņemu veco unique indeksu uz NormalizedUsername
DROP INDEX IF EXISTS "IX_Users_NormalizedUsername";

-- Izveidoju jaunu unique indeksu uz Username (case-insensitive caur LOWER())
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Username_lower"
    ON "Users" (LOWER("Username"))
    WHERE "Username" IS NOT NULL;

-- Noņemu NormalizedUsername kolonnu
ALTER TABLE "Users" DROP COLUMN IF EXISTS "NormalizedUsername";


-- ---------------------------------------------------------------------------
-- 3) Email — pārveidoju uz lowercase un noņemu NormalizedEmail
-- ---------------------------------------------------------------------------

UPDATE "Users"
SET "Email" = LOWER(TRIM("Email"))
WHERE "Email" IS NOT NULL
  AND "Email" <> LOWER(TRIM("Email"));

DROP INDEX IF EXISTS "IX_Users_NormalizedEmail";

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email_lower"
    ON "Users" (LOWER("Email"));

ALTER TABLE "Users" DROP COLUMN IF EXISTS "NormalizedEmail";


-- ---------------------------------------------------------------------------
-- 4) FullName — pilnībā noņemu (C# computed: FirstName + ' ' + LastName)
-- ---------------------------------------------------------------------------
ALTER TABLE "Users" DROP COLUMN IF EXISTS "FullName";


COMMIT;

-- ===========================================================================
-- Pārbaude pēc migrēšanas
-- ===========================================================================
-- Pārliecinos, ka kolonnas ir tieši šādas (Users tabulai):
--   Id, Username, FirstName, LastName, BirthDate, Email, PasswordHash,
--   Representation, Rating, Role, CreatedAtUtc, UpdatedAtUtc
--
-- Indeksi:
--   IX_Users_Username_lower (UNIQUE)
--   IX_Users_Email_lower    (UNIQUE)
-- ===========================================================================
