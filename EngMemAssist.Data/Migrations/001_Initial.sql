CREATE TABLE Words (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Lemma        TEXT    NOT NULL COLLATE NOCASE,
    English      TEXT    NOT NULL,
    Translation  TEXT    NOT NULL,
    AddedAt      TEXT    NOT NULL,
    IsRemoved    INTEGER NOT NULL DEFAULT 0,
    CorrectCount INTEGER NOT NULL DEFAULT 0,
    WrongCount   INTEGER NOT NULL DEFAULT 0,
    LastAskedAt  TEXT    NULL
);

CREATE UNIQUE INDEX UX_Words_Lemma ON Words (Lemma);

CREATE TABLE Sentences (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    WordId      INTEGER NOT NULL REFERENCES Words (Id) ON DELETE CASCADE,
    Text        TEXT    NOT NULL,
    Translation TEXT    NULL,
    Source      TEXT    NOT NULL DEFAULT ''
);

CREATE UNIQUE INDEX UX_Sentences_Word_Text ON Sentences (WordId, Text);

CREATE TABLE QuizSessions (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    StartedAt     TEXT    NOT NULL,
    FinishedAt    TEXT    NULL,
    QuestionCount INTEGER NOT NULL
);

CREATE TABLE QuizAnswers (
    Id                INTEGER PRIMARY KEY AUTOINCREMENT,
    SessionId         INTEGER NOT NULL REFERENCES QuizSessions (Id) ON DELETE CASCADE,
    WordId            INTEGER NOT NULL REFERENCES Words (Id),
    SentenceId        INTEGER NULL REFERENCES Sentences (Id) ON DELETE SET NULL,
    ChosenTranslation  TEXT   NOT NULL,
    CorrectTranslation TEXT   NOT NULL,
    IsCorrect         INTEGER NOT NULL,
    AnsweredAt        TEXT    NOT NULL
);

CREATE INDEX IX_QuizAnswers_Session ON QuizAnswers (SessionId);
CREATE INDEX IX_QuizAnswers_Word ON QuizAnswers (WordId);
