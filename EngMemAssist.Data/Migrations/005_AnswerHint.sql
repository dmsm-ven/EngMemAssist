-- Подсказка, взятая на вопрос: 0 — нет, 1 — половина вариантов убрана, 2 — дополнительное предложение, 3 — первая буква.
ALTER TABLE QuizAnswers ADD COLUMN Hint INTEGER NOT NULL DEFAULT 0;
