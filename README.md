# Sudoku Desktop

Кроссплатформенная одиночная офлайн-игра Sudoku для **macOS**, **Linux** и **Windows**.
Три уровня сложности, неограниченные подсказки, режимы **Endless** (без ограничения времени)
и **Time Attack** (10 минут на 1 судоку, 20 минут на 2 судоку, 30 минут на 3 судоку).

Приложение полностью офлайн: сеть не используется, данные хранятся локально.

## Стек

| Компонент | Выбор | Обоснование |
|---|---|---|
| Язык | C# 14 / .NET 10 (LTS) | Активный LTS до 14.11.2028; рекомендован самим Avalonia |
| UI | Avalonia UI 12.x | Единый код для трёх ОС, нативный вид на каждой платформе |
| MVVM | CommunityToolkit.Mvvm | Source generators, без рантайм-рефлексии |

Подробности и альтернативы — в [docs/ADR](docs/ADR/) и [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Текущее состояние репозитория

Репозиторий **не пустой**: в нём лежит рабочий скаффолд Avalonia-приложения
(проект `Sudoku/` с окном-заглушкой «Welcome to Avalonia!»). Игровой функциональности нет.

Git-репозиторий инициализирован, но **коммитов ещё нет** (ветка `main` пуста).

Подробное описание обнаруженного состояния — в [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md#2-обнаруженное-состояние-репозитория)
и в разделе «Обнаруженное состояние» файла [Card.md](Card.md).

## Требования

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) или новее
  (проверено на SDK 10.0.403 / macOS arm64)
- Для запуска в Linux (X11) требуются системные библиотеки:
  `libx11-6 libice6 libsm6 libfontconfig1`
  ([Avalonia: Linux dependencies](https://docs.avaloniaui.net/docs/platform-specific-guides/linux))

## Сборка и запуск

```bash
# Восстановление зависимостей
dotnet restore

# Сборка (Debug)
dotnet build

# Запуск
dotnet run --project Sudoku
```

Опубликованный самодостаточный билд для конкретной ОС:

```bash
# Пример для macOS (Apple Silicon)
dotnet publish Sudoku -c Release -r osx-arm64 --self-contained true \
  -p:PublishSingleFile=true -o out/osx-arm64
```

Полная инструкция по упаковке и подписанию — в [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md#7-упаковка-и-публикация)
и в ADR [0005](docs/ADR/0005-cross-platform-packaging.md).

## Документация

| Документ | Назначение |
|---|---|
| [Card.md](Card.md) | Продуктовая карточка: назначение, аудитория, ограничения, открытые вопросы |
| [docs/REQUIREMENTS.md](docs/REQUIREMENTS.md) | Функциональные и нефункциональные требования (FR-*/NFR-*) |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Архитектура решения, слои, модель данных, риски |
| [docs/TASKS.md](docs/TASKS.md) | План этапов, статусы, критерии завершения |
| [docs/ADR/](docs/ADR/) | Записи архитектурных решений |
| [AGENTS.md](AGENTS.md) | Инструкции для AI-агентов, работающих с репозиторием |
