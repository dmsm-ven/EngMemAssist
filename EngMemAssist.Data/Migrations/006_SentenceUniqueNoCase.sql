-- Дубли предложений одного слова (без учёта регистра) удаляем, оставляя самое раннее.
-- Ответы квизов не теряются: QuizAnswers.SentenceId имеет ON DELETE SET NULL.
DELETE FROM Sentences
WHERE Id NOT IN (SELECT MIN(Id) FROM Sentences GROUP BY WordId, Text COLLATE NOCASE);

DROP INDEX IF EXISTS UX_Sentences_Word_Text;
CREATE UNIQUE INDEX UX_Sentences_Word_TextNoCase ON Sentences (WordId, Text COLLATE NOCASE);
