# ADR-0006. Разделение Domain и Application на отдельные проекты

- **Статус:** Принято
- **Дата:** 2026-11-13
- **Заменяет:** [ADR-0002](0002-project-and-layer-structure.md)
- **Связано с:** [ADR-0001](0001-dotnet-avalonia-stack.md)

## Контекст

ADR-0002 предлагал объединить Domain и Application в один проект `Sudoku.Core`
с организационным разделением по папкам. Обоснование: предметная область мала,
отдельный слой Application не даёт самостоятельной ценности.

Владелец задал уточнение для этапа 1: «Отдели Domain и Application от Avalonia UI»
и «Подготовь тестовые проекты для Domain и Application». Это означает, что Domain
и Application должны быть **отдельно адресуемы** (у каждого — собственный проект
и собственный тестовый проект), а не просто разложены по папкам.

Дополнительный практический аргумент: отдельные проекты делают границы зависимостей
**проверяемыми компилятором и тестами**, а не «договорённостью». С ProjectReference
нарушение границы (например, `Sudoku.Domain` → `Avalonia`) немедленно даёт ошибку сборки.

Обратная сторона: появляются лишние сборки и файлы проектов. Для Sudoku это приемлемо —
проектов всего четыре, а предметная область невелика.

## Решение

Четыре проекта исходного кода в каталоге `src/` и четыре тестовых проекта в `tests/`:

```
src/
├── Sudoku.Domain/          → Sudoku.Domain            (зависимостей нет)
├── Sudoku.Application/     → Sudoku.Domain
├── Sudoku.Infrastructure/  → Sudoku.Application
└── Sudoku.App/             → Sudoku.Application, Sudoku.Domain
tests/
├── Sudoku.Domain.Tests/
├── Sudoku.Application.Tests/
├── Sudoku.Infrastructure.Tests/
└── Sudoku.App.Tests/       (Avalonia.Headless)
```

Граф зависимостей (стрелка — «зависит от»):

```
Sudoku.App ──► Sudoku.Application ──► Sudoku.Domain
    └──────────────────────────────────┘
Sudoku.Infrastructure ──► Sudoku.Application ──► Sudoku.Domain
```

Инварианты, закреплённые автотестами:

| Проверка | Тест |
|---|---|
| `Sudoku.Domain` не ссылается на Avalonia и другие слои | `Sudoku.Domain.Tests/LayerBoundaryTests` |
| `Sudoku.Application` зависит только от `Sudoku.Domain` | `Sudoku.Application.Tests/LayerBoundaryTests` |
| `Sudoku.Infrastructure` не ссылается на Avalonia и UI | `Sudoku.Infrastructure.Tests/LayerBoundaryTests` |

Дополнительно:

- `Sudoku.Domain` и `Sudoku.Application` — `classlib` с `GenerateDocumentationFile=true`
  (NFR-13: XML-документация публичного API). Для `Sudoku.App` файл документации отключён:
  UI-слой документируется через XAML и README проекта.
- Корневое пространство имён `Sudoku.App` — `Sudoku` (а не `Sudoku.App`), чтобы сохранить
  имена `Sudoku.App` (класс приложения Avalonia), `Sudoku.ViewModels`, `Sudoku.Views`
  из исходного скаффолда и не создавать конфликт «пространство имён `Sudoku.App`
  против типа `Sudoku.App`» в тестовых проектах.
- Из-за namespace `Sudoku.Application` в UI-слое базовый класс указывается полностью:
  `Avalonia.Application` (иначе `Application` разрешается в пространство имён).

## Последствия

**Положительные**
- Границы слоёв проверяются компилятором: добавить ссылку на Avalonia в `Sudoku.Domain`
  нельзя случайно — это ошибка сборки, а не замечание на ревью.
- У Domain и Application есть отдельные тестовые проекты, как того требует владелец.
- Каждый слой можно собирать и тестировать независимо.
- Циклы зависимостей невозможны по построению.

**Отрицательные**
- Лишние `.csproj`, каталоги и время на сборку (8 проектов вместо 3).
- Требуется дисциплина: публичные типы `Sudoku.Domain` «протекают» в `Sudoku.Application`
  через сигнатуры — это ожидаемо и допустимо.
- Проверка «Infrastructure зависит от Application» по скомпилированной сборке работает
  только когда в Infrastructure есть типы, использующие Application; на пустом проекте
  компилятор не записывает ссылку в IL. Проверку нужно будет расширить на этапе 6.

## Альтернативы

| Вариант | Оценка |
|---|---|
| **3 проекта (ADR-0002): Domain+Application в `Sudoku.Core`** | Отклонён по требованию владельца; границы оставались бы непроверяемыми компилятором |
| **1 проект на всё** | Отклонён: игровой код смешался бы с UI и файловой системой |
| **Domain / Application / Infrastructure / App / отдельный CLI-инструмент** | Отложено: CLI (генератор банка головоломок) понадобится на этапе 5/12, добавим по факту |
| **Модульный монолит с проверкой правил через архитектурные тесты (NetArchTest)** | Рассмотрено: мощнее, но требует внешней зависимости; проверка границ через `ProjectReference` уже даёт нужную гарантию |

## Связанные документы

- [docs/ARCHITECTURE.md](../ARCHITECTURE.md#31-решение-по-числу-проектов)
- [AGENTS.md](../../AGENTS.md#4-правила-слоёв-жёсткие)
