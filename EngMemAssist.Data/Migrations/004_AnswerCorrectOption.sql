-- Верный вариант, показанный в тесте: нужен, чтобы таблица результатов прошлого теста выглядела так же, как сразу после теста.
ALTER TABLE QuizAnswers ADD COLUMN CorrectOption TEXT NULL;
