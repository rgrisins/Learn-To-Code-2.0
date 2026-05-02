-- ===========================================================================
-- LearnToCode — Pievieno Bio kolonnu Users tabulai
--
-- Bio = lietotāja īss apraksts par sevi (max 500 rakstzīmes).
-- Tiek rādīts profila augšējā joslā un ir rediģējams caur "Rediģēt profilu".
-- ===========================================================================

BEGIN;

ALTER TABLE "Users"
    ADD COLUMN IF NOT EXISTS "Bio" character varying(500) NULL;

COMMIT;
