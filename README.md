# Sudoku Desktop

Кроссплатформенная одиночная офлайн-игра Sudoku для **macOS**, **Linux** и **Windows**.
Три уровня сложности, неограниченные подсказки, режимы **Endless** (без ограничения времени)
и **Time Attack** (10 минут на 1 судоку, 20 минут на 2 судоку, 30 минут на 3 судоку).

Приложение полностью офлайн: сеть не используется, данные хранятся локально.

## Стек

| Компонент | Выбор | Обоснование |
|---|---|---|
| Язык | C# 14 / .NET 10 (LTS) | Активный LTS до 14.11.2028; рекомендован самим Avalonia |
| UI | Avalonia UI 12.1.6 | Единый код для трёх ОС, нативный вид на каждой платформе |
| MVVM | CommunityToolkit.Mvvm 8.4.2 | Source generators, без рантайм-рефлексии |
| Тесты | xUnit v3 + Avalonia.Headless | Headless-тесты UI без запуска окна |

Подробности и альтернативы — в [docs/ADR](docs/ADR/) и [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Текущее состояние репозитория

Этап 1 завершён: собран **компилируемый каркас** решения с разделением по слоям.
Игровой логики пока нет (по требованию этапа генератор, игровая механика, таймер
и сохранения не реализовывались).

Git: ветка `main`, базовое состояние зафиксировано коммитом `5455959` ("Created Base")
и отправлено в `origin/main`. Изменения этапа 1 в коммит не включены — коммит создаёт владелец.

## Структура решения

```
src/
├── Sudoku.Domain/          домен: чистые типы, без UI и файловой системы
├── Sudoku.Application/     сценарии игры и порты
├── Sudoku.Infrastructure/  реализации портов, файловая система
└── Sudoku.App/             Avalonia UI, ViewModels, композиция
tests/
├── Sudoku.Domain.Tests/
├── Sudoku.Application.Tests/
├── Sudoku.Infrastructure.Tests/
└── Sudoku.App.Tests/        headless-тесты UI
```

Правило зависимостей: `App → Application → Domain`, `Infrastructure → Application`.
Нарушение границ невозможно незаметно — это ошибка компиляции, а не замечание на ревью.

## Требования

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) или новее
  (проверено на SDK 10.0.403 / macOS arm64)
- Для запуска в Linux (X11) требуются системные библиотеки:
  `libx11-6 libice6 libsm6 libfontconfig1`
  ([Avalonia: Linux dependencies](https://docs.avaloniaui.net/docs/platform-specific-guides/linux))

## Команды для разработчика

```bash
# Восстановление зависимостей
dotnet restore

# Сборка всего решения (0 warnings, 0 errors)
dotnet build

# Запуск приложения
dotnet run --project src/Sudoku.App

# Тесты (xUnit v3, Microsoft.Testing.Platform)
dotnet test

# Проверка единого стиля форматирования
dotnet format --verify-no-changes
```

Полезное:

```bash
# Только тесты одного слоя
dotnet test tests/Sudoku.Domain.Tests
dotnet test tests/Sudoku.App.Tests

# Сборка в Release
dotnet build -c Release
```

Опубликованный самодостаточный билд для конкретной ОС:

```bash
# Пример для macOS (Apple Silicon)
dotnet publish src/Sudoku.App -c Release -r osx-arm64 --self-contained true \
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
