-- Migration: Rename all Portuguese table names, column names, and data values to English
-- Run once against the ticketclassifier database

BEGIN;

-- ══════════════════════════════════════════════════════════════
-- 1. Rename columns - Tickets table
-- ══════════════════════════════════════════════════════════════
ALTER TABLE "Tickets" RENAME COLUMN "Assunto" TO "Subject";
ALTER TABLE "Tickets" RENAME COLUMN "Descricao" TO "Description";
ALTER TABLE "Tickets" RENAME COLUMN "Categoria" TO "Category";
ALTER TABLE "Tickets" RENAME COLUMN "Prioridade" TO "Priority";
ALTER TABLE "Tickets" RENAME COLUMN "Departamento" TO "Department";
ALTER TABLE "Tickets" RENAME COLUMN "Resumo" TO "Summary";
ALTER TABLE "Tickets" RENAME COLUMN "Confianca" TO "Confidence";
ALTER TABLE "Tickets" RENAME COLUMN "Justificativa" TO "Justification";
ALTER TABLE "Tickets" RENAME COLUMN "Sentimento" TO "Sentiment";
ALTER TABLE "Tickets" RENAME COLUMN "ProcessadoOk" TO "ProcessedOk";
ALTER TABLE "Tickets" RENAME COLUMN "RegistroModificado" TO "RecordModified";
ALTER TABLE "Tickets" RENAME COLUMN "DataModificacao" TO "ModifiedDate";

-- ══════════════════════════════════════════════════════════════
-- 2. Rename columns - Batches table
-- ══════════════════════════════════════════════════════════════
ALTER TABLE "Batches" RENAME COLUMN "NomeArquivo" TO "FileName";
ALTER TABLE "Batches" RENAME COLUMN "DataCriacao" TO "CreatedDate";

-- ══════════════════════════════════════════════════════════════
-- 3. Rename columns and table - Similaridades → Similarities
-- ══════════════════════════════════════════════════════════════
ALTER TABLE "Similaridades" RENAME COLUMN "TicketOrigemId" TO "SourceTicketId";
ALTER TABLE "Similaridades" RENAME COLUMN "TicketRelacionadoId" TO "RelatedTicketId";
ALTER TABLE "Similaridades" RENAME COLUMN "TagsCompartilhadas" TO "SharedTags";
ALTER TABLE "Similaridades" RENAME COLUMN "DataCriacao" TO "CreatedDate";
ALTER TABLE "Similaridades" RENAME TO "Similarities";

-- ══════════════════════════════════════════════════════════════
-- 4. Rename columns and table - ParametrosClassificacao → ClassificationParameters
-- ══════════════════════════════════════════════════════════════
ALTER TABLE "ParametrosClassificacao" RENAME COLUMN "Tipo" TO "Type";
ALTER TABLE "ParametrosClassificacao" RENAME COLUMN "Termo" TO "Term";
ALTER TABLE "ParametrosClassificacao" RENAME COLUMN "Alvo" TO "Target";
ALTER TABLE "ParametrosClassificacao" RENAME COLUMN "Ativo" TO "Active";
ALTER TABLE "ParametrosClassificacao" RENAME TO "ClassificationParameters";

-- ══════════════════════════════════════════════════════════════
-- 5. Update data values (Target column in ClassificationParameters)
-- ══════════════════════════════════════════════════════════════
UPDATE "ClassificationParameters" SET "Target" = 'Development' WHERE "Target" = 'Desenvolvimento';
UPDATE "ClassificationParameters" SET "Target" = 'Financial' WHERE "Target" = 'Financeiro';
UPDATE "ClassificationParameters" SET "Target" = 'Sales' WHERE "Target" IN ('Comercial', 'Vendas');
UPDATE "ClassificationParameters" SET "Target" = 'Product' WHERE "Target" = 'Produto';
UPDATE "ClassificationParameters" SET "Target" = 'Support' WHERE "Target" = 'Suporte';

COMMIT;
