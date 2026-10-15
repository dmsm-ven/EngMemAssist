-- Справочник источников предложений: API создаются один раз, локальные базы — по имени файла.
-- Kind: 0 — внешний API, 1 — локальная текстовая база.
CREATE TABLE SentenceSources (
    Id   INTEGER PRIMARY KEY AUTOINCREMENT,
    Kind INTEGER NOT NULL,
    Name TEXT    NOT NULL COLLATE NOCASE
);

CREATE UNIQUE INDEX UX_SentenceSources_Kind_Name ON SentenceSources (Kind, Name);

INSERT INTO SentenceSources (Kind, Name) VALUES (0, 'Tatoeba'), (0, 'Wiktionary');

-- Локальные базы, предложения из которых уже есть (в Sentences.Source они записаны как «local:Имя»).
INSERT OR IGNORE INTO SentenceSources (Kind, Name)
SELECT DISTINCT 1, substr(Source, 7) FROM Sentences WHERE Source LIKE 'local:%';

ALTER TABLE Sentences ADD COLUMN SourceId INTEGER NULL REFERENCES SentenceSources (Id);

UPDATE Sentences
SET SourceId = (SELECT Id FROM SentenceSources WHERE Kind = 0 AND Name = Sentences.Source)
WHERE Source NOT LIKE 'local:%';

UPDATE Sentences
SET SourceId = (SELECT Id FROM SentenceSources WHERE Kind = 1 AND Name = substr(Sentences.Source, 7))
WHERE Source LIKE 'local:%';

CREATE INDEX IX_Sentences_SourceId ON Sentences (SourceId);
