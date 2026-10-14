# EngMemAssist

Личный помощник по запоминанию английских слов (WPF, .NET 10, MVVM Toolkit, DI, SQLite + Dapper).

## Проекты

| Проект | Назначение |
|---|---|
| `EngMemAssist` | WPF-приложение (окна, ViewModel, навигация) |
| `EngMemAssist.Core` | Модели, интерфейсы, бизнес-логика (синхронизация слов, квиз), настройки |
| `EngMemAssist.Data` | SQLite, Dapper-репозитории, миграции DbUp |
| `EngMemAssist.Nlp` | Лемматизация (Catalyst, запасной вариант на правилах) |
| `EngMemAssist.Integrations` | Google Sheets (OAuth) и Tatoeba (предложения) |

## Первый запуск

1. В [Google Cloud Console](https://console.cloud.google.com/) создайте проект и включите **Google Sheets API**.
2. Создайте **OAuth client ID** типа **Desktop app** и скачайте JSON.
3. Положите файл как `%AppData%\EngMemAssist\client_secret.json`.
4. Запустите приложение и вставьте ссылку на таблицу. Откроется браузер для входа в Google.

Формат таблицы (без заголовков): `[A: английское слово] [B: перевод] [C: разделитель] [D: слово] [E: перевод] [F: разделитель] …`

Первый запуск лемматизатора долгий: модели Catalyst распаковываются в `%AppData%\EngMemAssist\catalyst-models`.

## Файлы приложения (`%AppData%\EngMemAssist`)

`settings.json`, `engmemassist.db`, `client_secret.json`, `google-token\`, `catalyst-models\`, `sync.log`

`sync.log` — журнал загрузки предложений: по каждому слову видно, сколько нашлось кандидатов, сколько содержат слово и сколько сохранено. Если у слова нет предложений, начните с него.

Предложения берутся из Tatoeba (с русскими переводами) и из английского Викисловаря (примеры употребления, в том числе для редких слов). Слова без предложений повторно запрашиваются не чаще раза в сутки.
