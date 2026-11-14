# ARCHITECTURE.md — Sudoku Desktop

Статус: этап 1 завершён (каркас решения собран, игровая логика не реализована).
Дата: 2026-11-13.

---

## 1. Проверка гипотезы о стеке

Гипотеза владельца: **C# + .NET + Avalonia UI**. Гипотеза подтверждена, стек зафиксирован
в [ADR-0001](ADR/0001-dotnet-avalonia-stack.md).

| Критерий | Результат проверки |
|---|---|
| Актуальный .NET | .NET 11 — STS (акт. с 10.11.2026), **.NET 10 — LTS** (поддержка до 14.11.2028). Выбран `net10.0` |
| Актуальный Avalonia | **12.1.6** (12.0.0 — стабильный релиз 07.04.2026). В репозитории — 12.0.4 |
| Рекомендация Avalonia по TFM | «The recommended target is .NET 10» (docs.avaloniaui.net/docs/avalonia12-breaking-changes) |
| macOS | Tier 1 — macOS 26 (Tahoe); Tier 2 — macOS 14/15. Нативный бэкенд `libAvaloniaNative.dylib`. Минимальный deployment target нативной библиотеки — 12.0 (с 12.1.5) |
| Windows | Tier 1 — Windows 11 24H2+ (x64, ARM64); Tier 2 — Windows 11 22H2, Windows 10 22H2 |
| Linux | Tier 1 — Ubuntu 25.x, Debian 13, Fedora 43; Tier 2 — Ubuntu 24.x и старше, Debian 12, Fedora 30–42. X11 по умолчанию, нативный Wayland — опционально с 12.1.0 |
| Кросс-компиляция | Поддерживается: `net10.0` без OS-workload ⇒ Windows- и macOS-сборки делаются с Linux/macOS |
| AOT/Trimming | Официально поддерживается (`<PublishAot>true</PublishAot>`, `IsAotCompatible`), но требует compiled bindings и запрета runtime-рефлексии |
| Single-file | Поддерживается, рекомендован для macOS (упрощает нотаризацию) |

Источники: [supported platforms](https://docs.avaloniaui.net/docs/supported-platforms),
[deployment/linux](https://docs.avaloniaui.net/docs/deployment/linux),
[deployment/macos](https://docs.avaloniaui.net/docs/deployment/macos),
[breaking changes 12](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes),
[.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

---

## 2. Состояние репозитория

### 2.1. Исходное состояние (этап 0)

Репозиторий **не был пустым** — в нём уже лежал скаффолд Avalonia. Ничего не удалено
и не переписано, существующие решения сохранены.

```
Sudoku.sln                     # решение, 1 проект
Sudoku/Sudoku.csproj           # net10.0, Avalonia 12.0.4, CommunityToolkit.Mvvm 8.4.1
Sudoku/Program.cs              # StartWithClassicDesktopLifetime, UsePlatformDetect, WithInterFont
Sudoku/App.axaml(.cs)          # FluentTheme, RequestedThemeVariant="Default"
Sudoku/ViewLocator.cs          # конвенция ViewModel → View через reflection
Sudoku/ViewModels/              # ViewModelBase : ObservableObject, MainWindowViewModel
Sudoku/Views/MainWindow.axaml(.cs)   # окно-заглушка "Welcome to Avalonia!"
Sudoku/app.manifest            # Windows-манифест (Windows 10)
Sudoku/Assets/avalonia-logo.ico
Sudoku/Models/                 # пустая папка из шаблона
.gitignore                     # без .idea/
```

Состояние Git на начало работы: ветка `main`, **0 коммитов** (базовое состояние зафиксировано
позже владельцем коммитом `5455959` "Created Base").

### 2.2. Текущее состояние (после этапа 1)

```
Sudoku.sln                     # решение, 8 проектов
Directory.Build.props          # общие настройки сборки (net10.0, nullable, warnings-as-errors)
Directory.Packages.props       # централизованное управление версиями пакетов
.editorconfig                  # единый стиль форматирования
src/
├── Sudoku.Domain/             # classlib, без зависимостей
├── Sudoku.Application/        # classlib → Sudoku.Domain
├── Sudoku.Infrastructure/     # classlib → Sudoku.Application
└── Sudoku.App/                # WinExe (Avalonia) → Sudoku.Application, Sudoku.Domain
tests/
├── Sudoku.Domain.Tests/       # xUnit v3
├── Sudoku.Application.Tests/  # xUnit v3
├── Sudoku.Infrastructure.Tests/ # xUnit v3
└── Sudoku.App.Tests/          # xUnit v3 + Avalonia.Headless
```

Скаффолд перенесён без потерь: `Program.cs`, `App.axaml(.cs)`, `ViewLocator.cs`,
`ViewModels/`, `Views/`, `Assets/`, `app.manifest` сохранены; проект переименован
`Sudoku` → `Sudoku.App` и перемещён в `src/` (см. [ADR-0006](ADR/0006-domain-application-projects.md)).

Обновлённые пакеты: Avalonia 12.0.4 → **12.1.6**, `AvaloniaUI.DiagnosticsSupport` 2.2.1 → **2.2.3**,
`CommunityToolkit.Mvvm` 8.4.1 → **8.4.2**. Добавлены `Avalonia.Headless` / `Avalonia.Headless.XUnit`
12.1.6 и `xunit.v3` 3.2.2.

Проверка после этапа 1: `dotnet build` → **0 warnings, 0 errors**; `dotnet test` → **10/10 passed**;
`dotnet format --verify-no-changes` → без замечаний; приложение запускается на macOS
и открывает окно «Sudoku Desktop».

### 2.3. Что добавлено на этапе 3

| Проект | Добавлено |
|---|---|
| `src/Sudoku.Domain` | `Grid` — неизменяемая доска 9×9 с разбором из строки и проверками |
| `src/Sudoku.Application` | `ISudokuSolver`, `MrvSudokuSolver`, `SolveOutcome`, `SolveResult` |
| `tests/Sudoku.Domain.Tests` | 13 тестов `Grid` |
| `tests/Sudoku.Application.Tests` | 14 тестов решателя + эталонные тестовые данные |

Проверка после этапа 3: `dotnet build` → **0 warnings, 0 errors**; `dotnet test` → **37/37 passed**;
`dotnet format --verify-no-changes` → без замечаний.

### 2.4. Что добавлено на этапе 4

| Проект | Добавлено |
|---|---|
| `src/Sudoku.Domain` | `Puzzle`, `DifficultyRating`, `SudokuTechnique` (лестница приёмов v1) |
| `src/Sudoku.Application` | `IPuzzleGenerator`/`PuzzleGenerator`, `IDifficultyRater`/`LogicalDifficultyRater`, `LogicalSolver`, `DifficultyBands`, `IRandomSource`/`SystemRandomSource`, `PuzzleGenerationException` |
| `tests/Sudoku.Application.Tests` | Тесты генератора (корректность решения, единственность, полоса, seed, разнообразие, время), тесты логического решателя, независимый эталонный решатель для перекрёстной проверки |

Проверка после этапа 9: `dotnet build` → **0 warnings, 0 errors**; `dotnet test` → **197 passed**, 1 опциональный тест пропущен (полная выборка);
`dotnet format --verify-no-changes` → без замечаний; приложение запускается, режим Endless доступен из меню.

Проверка после этапа 4: `dotnet build` → **0 warnings, 0 errors**; `dotnet test` → **114 passed**, 1 опциональный тест пропущен (полная выборка);
`dotnet format --verify-no-changes` → без замечаний. Полная выборка из 1002 головоломок
(`SUDOKU_FULL_BATCH=1`) — все головоломки единственны и попадают в заявленную полосу.

---

### 2.5. Что добавлено на этапе 9 (режим Endless)

| Проект | Добавлено |
|---|---|
| `src/Sudoku.Domain` | `GameSession.Mode` — режим задаётся при создании и не меняется |
| `src/Sudoku.Application` | `EndlessGame` — сценарий захода; `IGameSessionFactory.CreateNewEndlessGame` — единственный способ создать сессию |
| `src/Sudoku.App` | `MainWindowViewModel` (меню сложности и переключение экранов), `EndlessGameViewModel`, `CellViewModel`, `EndlessGameView`, `Composition` |
| `tests/Sudoku.Application.Tests` | 15 тестов сценария Endless |
| `tests/Sudoku.App.Tests` | 20 тестов UI и ViewModel режима Endless |

---

## 3. Целевая архитектура

### 3.1. Решение по числу проектов

Domain и Application выделены в **отдельные проекты**, чтобы границы слоёв проверялись
компилятором, а не «договорённостью» ([ADR-0006](ADR/0006-domain-application-projects.md),
заменяет [ADR-0002](ADR/0002-project-and-layer-structure.md)):

```
Sudoku.sln
├── src/Sudoku.Domain/         (net10.0, classlib)  ← домен, НЕТ UI и ФС
├── src/Sudoku.Application/    (net10.0, classlib)  ← сценарии и порты
├── src/Sudoku.Infrastructure/ (net10.0, classlib)  ← сохранение, банк головоломок, пути ФС
├── src/Sudoku.App/            (net10.0, WinExe)    ← Avalonia UI, ViewModels, композиция
├── tests/Sudoku.Domain.Tests/         (xUnit v3)
├── tests/Sudoku.Application.Tests/    (xUnit v3)
├── tests/Sudoku.Infrastructure.Tests/ (xUnit v3)
└── tests/Sudoku.App.Tests/            (xUnit v3 + Avalonia.Headless)
```

Граф зависимостей (стрелка — «зависит от»):

```
Sudoku.App ──────────┬──► Sudoku.Application ──► Sudoku.Domain
                      └───────────────────────────┘
Sudoku.Infrastructure ───► Sudoku.Application ──► Sudoku.Domain
```

`Sudoku.Domain` не ссылается ни на Avalonia, ни на `System.IO`. Всё, что связано с файловой
системой, живёт в `Sudoku.Infrastructure`; UI — только в `Sudoku.App`.

Инварианты границ закреплены автотестами (`LayerBoundaryTests` в каждом тестовом проекте).

**Композиция зависимостей:** DI-контейнер пока не подключён — объекты создаются напрямую
в композиционном корне, внедрение идёт через конструкторы
([ADR-0007](ADR/0007-composition-without-di-container.md)).

### 3.2. Что находится в `Sudoku.Domain`

**Домен (чистые типы, без побочных эффектов)**

| Тип | Назначение | Статус |
|---|---|---|
| `Difficulty` | `Easy`, `Normal`, `Hard` | ✅ реализован |
| `GameMode` | `Endless`, `TimeAttack` | ✅ реализован |
| `Grid` | Доска 9×9; значение 0 = пусто; неизменяемая; разбор из строки, проверки полноты и непротиворечивости | ✅ реализован |
| `Cell` | Отдельный тип ячейки | ⬜ этап 2 |
| `Puzzle` | Головоломка: начальные подсказки + единственное решение + рейтинг сложности | ✅ этап 4 |
| `DifficultyRating` | Числовая оценка + ранг «потолка техник» | ✅ этап 4 |
| `Cell` | Снимок состояния ячейки: значение, исходная/пользовательская, конфликт, корректность | ✅ этап 2 |
| `CellPosition` | Позиция ячейки (строка, столбец) с проверкой диапазона | ✅ этап 2 |
| `MoveOutcome` | Результат действия игрока: применено или отклонено (нет выбора, исходная ячейка, пусто, игра завершена) | ✅ этап 2 |
| `HintResult` / `HintStatus` | Итог подсказки: открыто значение, исправлено значение, отказ для исходной ячейки, нет пустых клеток | ✅ этап 2 |
| `GameSession` | Состояние захода: режим, исходное поле, текущее поле, подтверждённое решение, выбор клетки, ввод/стирание, конфликты, подсказки, победа | ✅ этап 2 |
| `GameSession` — счёт и таймер | Зависят от `ITimeProvider` (этап 10) | ⬜ этап 10 |

### 3.3. Что находится в `Sudoku.Application`

**Прикладная логика (чистые функции, детерминированные)**

| Тип | Назначение | Статус |
|---|---|---|
| `GameOptions` | Опции игры для слоя представления: уровни сложности и режимы | ✅ реализован |
| `ISudokuSolver` | Решение головоломки; `Solve()` и `CountSolutions(limit)` | ✅ реализован |
| `MrvSudokuSolver` | Реализация: backtracking + constraint propagation + выбор ячейки по MRV | ✅ реализован |
| `SolveOutcome` / `SolveResult` | Вид решения и найденные варианты | ✅ реализован |
| `PuzzleGenerator` | Генерация головоломки заданной сложности с seed | ✅ этап 4 |
| `IPuzzleGenerator` | Абстракция (нужна для подмены в тестах и для банка) | ✅ этап 4 |
| `IDifficultyRater` | Оценка сложности по потолку техник | ✅ этап 4 |
| `LogicalSolver` | Логический решатель: лестница приёмов v1 (singles → пары) | ✅ этап 4 |
| `DifficultyBands` / `DifficultyBand` | Калибровка уровней: полосы потолка приёмов и минимум подсказок | ✅ этап 4 |
| `IRandomSource` / `SystemRandomSource` | Источник случайности с явным seed | ✅ этап 4 |
| `PuzzleGenerationException` | Не удалось построить головоломку заданного уровня | ✅ этап 4 |
| `IGameSessionFactory` / `GameSessionFactory` | Новая игра: генерация головоломки выбранной сложности по seed и создание сессии. Метод один на режим (`CreateNewEndlessGame`) — сценарий не может случайно запустить другой режим | ✅ этап 2, 9 |
| `EndlessGame` | Сценарий захода Endless: текущая сессия, сложность, номер головоломки, счётчик решённых, суммарный счётчик подсказок, переход к следующей головоломке | ✅ этап 9 |
| `IHintProvider` | Следующий логический шаг для подсказки (использует тот же логический решатель) | ⬜ этап 5 |
| `ITimeProvider` | Абстракция времени (`UtcNow`, `Elapsed`) — для детерминированных тестов таймера | ⬜ этап 2 |
| `GameSessionFactory` | Создание сессии под режим и сложность | ⬜ этап 2 |

**Ключевой инвариант детерминизма:** `PuzzleGenerator.Generate(difficulty, seed)` при одинаковом
seed всегда даёт одну и ту же головоломку. `System.Random` создаётся внутри с явным seed;
`Random.Shared` не используется.

### 3.4. Что находится в `Sudoku.Infrastructure`

| Тип | Назначение |
|---|---|
| `IStateStore` | Сохранение/загрузка `GameSession` (JSON) |
| `JsonStateStore` | Реализация на `System.Text.Json` (атомарная запись через temp-файл + replace) |
| `IAppPaths` | Пути к каталогам данных/настроек |
| `AppPaths` | Реализация: `Environment.GetFolderPath(SpecialFolder.LocalApplicationData)` → `~/Library/Application Support/Sudoku` (macOS), `~/.local/share/Sudoku` (Linux), `%LOCALAPPDATA%\Sudoku` (Windows) |
| `IPuzzleBank` | Доступ к заранее сгенерированным головоломкам (в будущем) |
| `PuzzleBank` | Файл/ресурс с банком + рандомизация перестановками цифр/строк/столбцов |

### 3.5. Что находится в `Sudoku.App`

| Тип | Назначение | Статус |
|---|---|---|
| `Program` | Точка входа, `AppBuilder`, `UsePlatformDetect()` | ✅ реализован |
| `App` | Корневой `Avalonia.Application`: тема Fluent, создание окна | ✅ реализован |
| `MainWindow` + `MainWindowViewModel` | Меню (выбор режима и сложности) и переключение на экран активного захода | ✅ этап 7, 9 |
| `EndlessGameView` + `EndlessGameViewModel` | Экран режима Endless: сложность, состояние захода, поле 9×9, ввод цифр, стирание, подсказка, новая головоломка, завершение захода | ✅ этап 9 |
| `CellViewModel` | Представление ячейки поля: значение, исходная/выбранная/конфликтная | ✅ этап 9 |
| `Composition` | Композиционный корень: сборка генератора, оценщика и фабрики сессий без DI-контейнера (ADR-0007) | ✅ этап 9 |
| `ViewLocator` | Маршрутизация ViewModel → View по соглашению имён | ✅ из скаффолда |
| `GameView` + `GameViewModel` | Игровое поле, ввод, подсказки, таймер | ⬜ этап 7–8 |
| `BoardView` | Рисование поля 9×9 (переиспользуемый `ItemsControl`/`Canvas` с 81 ячейкой) | ⬜ этап 8 |
| `CellViewModel` | Состояние одной ячейки: значение, заданные/введённые, выделение, конфликт | ⬜ этап 8 |
| `TimerService` | Периодическое обновление отображения таймера; измеряет интервал через `ITimeProvider`, а не через `DispatcherTimer.Tick` | ⬜ этап 10 |
| `ThemeManager` | Светлая/тёмная тема (по умолчанию — системная) | ⬜ этап 7 |

MVVM: CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`), compiled bindings
(включены в скаффолде: `AvaloniaUseCompiledBindingsByDefault: true`).

Пространство имён проекта — `Sudoku` (не `Sudoku.App`): так сохраняются имена
`Sudoku.App` (класс приложения), `Sudoku.ViewModels`, `Sudoku.Views` из скаффолда и
не возникает конфликта «namespace `Sudoku.App` vs type `Sudoku.App»» в тестовых проектах.
Из-за namespace `Sudoku.Application` базовый класс указывается как `Avalonia.Application`.

### 3.6. Модель данных сохранения

```jsonc
// state.json (версионируется полем "version")
{
  "version": 1,
  "mode": "TimeAttack",
  "difficulty": "Normal",
  "timeLimitSeconds": 1200,
  "elapsedSeconds": 412,
  "puzzleIndex": 1,
  "puzzlesCompleted": 1,
  "score": 1000,
  "current": {
    "seed": 123456789,
    "puzzle": "530070000600195000098000060800060003400803001700020006060000280000419005000080079",
    "solution": "534678912672195348198342567859761423426853791713924856961537284287419635345286179",
    "rating": 2.4,
    "entries": "000070000600195000098000060800060003400803001700020006060000280000419005000080079",
    "notes": "000000000000000000000000000000000000000000000000000000000000000000000000000000000"
  },
  "statistics": { "solved": 42, "hintsUsed": 7, "totalPlaySeconds": 9182 }
}
```

- `puzzle` — 81 символ решения-шаблона (только заданные цифры, остальные `0`).
- `entries` — текущее состояние поля пользователя.
- `notes` — пометки (кандидаты) в виде битовых масок по 81 ячейке (опционально, FR-09).
- Запись атомарна: сначала во временный файл, затем `File.Move(..., overwrite: true)` —
  файл не может остаться в «наполовину записанном» состоянии.
- Повреждённый JSON не роняет приложение: ошибка логируется, игра начинается заново.

---

## 4. Алгоритмы

### 4.1. Решатель и проверка уникальности

Один и тот же движок решает три задачи:

1. `Solve(grid)` — единственное решение;
2. `CountSolutions(grid, limit: 2)` — уникальность (прерывается на втором решении);
3. Пошаговое применение техник (для подсказок) — этап 5.

Реализация: битовые маски кандидатов + constraint propagation (naked single, hidden single)
+ DFS с выбором ячейки по **MRV** (minimum remaining values). Для Sudoku этого достаточно;
эталон — реализация Norvig (https://norvig.com/sudoku.html).

DLX / Algorithm X (729×324 exact cover) — **не нужен**: он оправдан, когда требуется перечисление
всех решений. Проверка «решений ровно одно» делается тем же MRV-решателем с лимитом 2.

**Статус: реализовано** ([ADR-0008](ADR/0008-sudoku-solver-algorithm.md)).

| Свойство | Реализация |
|---|---|
| Вход | Неизменяемый `Sudoku.Domain.Grid` |
| Результат | `SolveResult` с `SolveOutcome`: `NoSolution` / `UniqueSolution` / `MultipleSolutions` |
| Остановка | `Solve` останавливается после второго решения; `CountSolutions(grid, limit)` — произвольный предел |
| Детерминизм | Порядок ячеек (наименьший индекс при равенстве) и цифр (по возрастанию) фиксированы |
| Некорректный ввод | `Grid.FromString` → `ArgumentException`; `TryFromString` → `false`; `null` → `ArgumentNullException`; предел < 1 → `ArgumentOutOfRangeException`; противоречивые заданные цифры → `NoSolution` |
| Отсутствие мутаций | `Grid` неизменяем, решатель работает на собственном состоянии с откатом через стек назначений |
| Производительность | Классическая головоломка — ~1 мс; разреженная (40–48 подсказок) — 0–3 мс; поиск двух решений для пустой доски — ~1 мс |

MRV-решатель умеет только одиночные приёмы (singles). Приёмы уровня пар (naked/hidden pair),
X-Wing и цепочки реализованы в отдельном логическом решателе `LogicalSolver`
(лестница v1 для рейтинга сложности, ADR-0003); X-Wing, Swordfish, XY-Wing и цепочки
остаются этапом 5.

### 4.2. Генерация

**Статус: реализовано** ([ADR-0009](ADR/0009-puzzle-generation-algorithm.md)).

1. Сгенерировать полное заполнение: случайный MRV-backfill со случайным порядком цифр
   (`RandomGridFiller`).
2. Применить симметричные перестановки решения (цифры, строки и столбцы внутри блоков,
   блоки между собой) — разнообразие при том же seed.
3. Удалять подсказки в случайном порядке; после каждого удаления проверять, что решение
   остаётся **единственным** (`CountSolutions(limit: 2) == 1`) и что головоломка решается
   приёмами не выше верхней границы полосы сложности. Иначе удаление откатывается.
4. Запоминать последнюю доску, подтверждённую как принадлежащую полосе уровня.
5. Пересчитать оценку оценщиком заново (единственность + потолок приёмов) и принять
   головоломку, только если оценка попадает в полосу заявленного уровня.
6. Зафиксировать seed: один и тот же seed и уровень дают побайтово одну и ту же головоломку.

Калибровка полос живёт в `DifficultyBands` (единый источник правды): Easy — потолок 1–2,
Normal — 3–4, Hard — 5–6; полосы не пересекаются и покрывают всю лестницу v1.
Число подсказок критерием сложности **не является**, минимум подсказок — только страховка.

Измерения (Release, .NET 10, macOS arm64, 300 головоломок на уровень): 0 ошибок,
0 неединственных решений, 0 промахов полосы, 300/300 различных головоломок на уровень;
время генерации p95 — 6 мс (Easy), 58 мс (Normal), 69 мс (Hard) при бюджете NFR-05
1 с / 1 с / 5 с.

### 4.3. Оценка сложности — ключевой технический риск

Исследование ([ADR-0003](ADR/0003-difficulty-rating.md)) установило:

- **Единого стандарта сложности не существует**; оценки разных инструментов не совпадают.
- **Число подсказок — слабый признак** (корреляция с человеческой сложностью ≈ 0.25 по данным
  Pelánek, arXiv:1403.7373). Разница между Easy и Hard часто в *распределении* подсказок, а не в их числе.
- Устойчивая метрика — **потолок техник** (technique ceiling): максимальная сложность приёма,
  необходимого для решения без угадывания. Используется в Sudoku Explainer, HoDoKu, xSudoku.
- Метрика «сколько угадываний нужно решателю» (branch count) — хороший **вторичный** признак
  и прямо отражает, решается ли головоломка логически вообще.

**Предлагаемая стратегия (двухуровневая, с эскалацией):**

| Уровень | Реализация | Что покрывает |
|---|---|---|
| v1 (этап 4) | Потолок техник: naked single, hidden single, pointing/claiming, naked/hidden pair | Easy, Normal, большинство Hard |
| v2 (этап 5, по решению владельца) | Добавить X-Wing, Swordfish, XY-Wing, цепочки (forcing chains) | Сложные Hard |
| Fallback (этап 6, опционально) | Предсгенерированный банк головоломок с внешней оценкой (публичное достояние: [grantm/sudoku-exchange-puzzle-bank](https://github.com/grantm/sudoku-exchange-puzzle-bank)) | Гарантированное качество Hard без сложного рейтера |

> ⚠️ Лицензионная оговорка: банк sudoku-exchange — public domain, но набор Project Euler #96 —
> CC BY-NC-SA (некоммерческое), использовать в коммерческом приложении нельзя. В этапе 5
> эталонные тесты будут строиться на собственном наборе.

### 4.4. Подсказки

**Статус: два уровня.**

1. **Открытие правильного значения (реализовано, этап 2).** `GameSession.RevealHint()`
   берёт значение из подтверждённого решения головоломки (`Puzzle.Solution`) — случайных
   значений не бывает. Правила (зафиксированы владельцем): для исходной ячейки подсказка
   не применяется, но значение показывается; для ячейки с введённым значением значение
   исправляется на правильное; если ячейка не выбрана — сессия сама выбирает первую пустую.
   Подсказки неограниченные, их количество только считается (`HintCount`).
2. **Подсказка с объяснением (этап 5).** Следующий шаг логического решателя: применяется
   самая простая доступная техника, подсвечивается затронутая ячейка/пара и объясняется
   правило. Требует `IHintProvider` и лестницы техник v2.

### 4.5. Игровая механика

**Статус: реализовано** (этап 2). Механика живёт в `Sudoku.Domain.GameSession` и не зависит от UI:
слой представления читает состояние (`State`, `Cells`, `GetCell`) и вызывает методы действия.

| Правило | Реализация | Результат |
|---|---|---|
| Исходное поле | `InitialState` (`Puzzle.Clues`) | Значения менять нельзя |
| Текущее поле | `State` | Заданные значения + введённые игроком |
| Правильное решение | `Solution` (`Puzzle.Solution`) | Источник значений для подсказок |
| Выбор клетки | `SelectCell`, `ClearSelection` | `SelectedCell` + флаг `Cell.IsSelected` |
| Ввод цифры 1–9 | `EnterValue` | `MoveOutcome.Applied` / `Unchanged` / отказ |
| Стирание | `EraseValue` | `MoveOutcome.Applied` / отказ |
| Запрет изменения исходных клеток | `IsGiven` | `MoveOutcome.RejectedGivenCell` |
| Конфликты | `ConflictScanner` | `Cell.HasConflict`, `HasConflict(index)` |
| Подсказка | `RevealHint` | `HintResult` (см. 4.4) |
| Победа | `IsComplete` + `IsSolved` | Только при полном и полностью правильном поле |
| Новая игра | `GameSessionFactory.CreateNewGame(difficulty, seed)` | Новая сессия по сгенерированной головоломке |

Ввод конфликтующего значения разрешён: значение остаётся на доске, ячейка помечается
конфликтной, победа при этом не засчитывается.

---

## 5. Режимы и таймер

### 5.1. Endless

**Статус: реализовано** (этап 9).

Лимита времени нет. Каждая решённая головоломка порождает следующую той же сложности.

| Требование | Реализация |
|---|---|
| Выбор сложности Easy / Normal / Hard | `MainWindowViewModel.SelectedDifficulty` + `GameOptions.Difficulties` |
| Новая головоломка выбранной сложности | `EndlessGame` → `IGameSessionFactory.CreateNewEndlessGame` → `PuzzleGenerator` |
| Время не ограничено | В сценарии захода нет таймера и дедлайна (проверяется тестом) |
| Завершение текущей сессии и новая | `EndlessGame.StartNextPuzzle` (решённая засчитывается) и `EndlessGameViewModel.ExitCommand` (заход завершается) |
| Победа | Общая игровая логика `GameSession.IsSolved` |
| UI показывает сложность и состояние | `MainWindowViewModel.StatusText`, `EndlessGameViewModel.DifficultyText` / `StatusText` / `BoardStateText` |
| Подсказки без ограничений | `EndlessGame.RequestHint` — значение из подтверждённого решения; счётчик только для показа |
| Изоляция режимов | `GameSession.Mode` неизменен; у фабрики один метод на режим, сценарий Time Attack появится отдельно (этап 10) |
Счёт — опционально (решается владельцем).

### 5.2. Time Attack

| Головоломок | Лимит |
|---|---|
| 1 | 10 минут (600 с) |
| 2 | 20 минут (1200 с) |
| 3 | 30 минут (1800 с) |

**Предлагаемая (требует подтверждения) семантика:** единый таймер на весь заход; после решения
головоломки выдаётся следующая, остаток времени переносится. По истечении времени заход
завершается, прогресс сохраняется.

**Реализация таймера:** измерение интервала через монотонный источник (`Stopwatch` / `ITimeProvider`),
а не через подсчёт тиков `DispatcherTimer`. `DispatcherTimer` используется только для
перерисовки отображения (раз в 200–500 мс). Так таймер не «отстаёт» при зависании UI-потока
и корректно считает время в фоне.

---

## 6. Технические риски

| # | Риск | Вероятность | Влияние | Митигация |
|---|---|---|---|---|
| R1 | Оценка сложности не соответствует ожиданиям игрока | Высокая | Среднее | Двухуровневый рейтинг + потолок техник + объяснение в подсказках (ADR-0003) |
| R2 | Генерация Hard-головоломок занимает десятки секунд | Средняя | Высокое | Целевая генерация вместо слепого удаления; предсгенерированный банк; генерация в `Task.Run` с индикатором |
| R3 | Головоломка с несколькими решениями | Низкая | Высокое | Обязательная проверка `CountSolutions(limit: 2) == 1` при генерации; регрессионные тесты |
| R4 | Повреждение/потеря сохранения | Низкая | Высокое | Атомарная запись, версионирование схемы, устойчивость к битому JSON |
| R5 | «Прыгающий» таймер (отставание UI, переход в фон) | Средняя | Среднее | Монотонный `ITimeProvider`, отображение через `DispatcherTimer` |
| R6 | Установка на macOS/Windows блокируется (Gatekeeper/SmartScreen) | Высокая | Высокое | Нотаризация и подпись; требует решения владельца (платная подписка Apple Developer ID) |
| R7 | Wayland на Linux: нет авто-фолбэка | Низкая | Среднее | По умолчанию XWayland; `UseWayland()` только при явном `WAYLAND_DISPLAY` |
| R8 | musl/Alpine (Tier 3) — нужен собственный Skia | Низкая | Среднее | Не входит в объём поддержки; зафиксировано в Card.md |
| R9 | Диагностика UI: в Avalonia 12 DevTools вынесены в платный Avalonia Plus | Средняя | Низкая | `AvaloniaUI.DiagnosticsSupport` 2.2.1+ (2.x — Avalonia Plus). В OSS-сборке оставить только логирование |
| R10 | Рефлексия в `ViewLocator` ломается при AOT/Trimming | Средняя | Среднее | Отказаться от `ViewLocator` в пользу явного `DataTemplate`-маршрутизирования до включения AOT |

---

## 7. Упаковка и публикация

Общий принцип: `dotnet publish` с RID + самодостаточная сборка, затем платформенный
инсталлятор. Официального `Avalonia.Distribution.*` в экосистеме нет.

### 7.1. macOS

```bash
dotnet publish src/Sudoku.App -c Release -r osx-arm64 --self-contained true \
  -p:PublishSingleFile=true -o out/osx-arm64
```

- Собирается вручную `.app`: `Contents/MacOS` (publish), `Contents/Info.plist`,
  `Contents/Resources/*.icns`.
- `Info.plist` обязан содержать `CFBundleExecutable`, `CFBundleName` (≤ 15 символов),
  `CFBundleIdentifier`, `CFBundleVersion`, `CFBundleShortVersionString`,
  `NSHighResolutionCapable`, `CFBundleIconFile`.
- `UseAppHost=true` обязателен, иначе не будет нативного исполняемого файла.
- Подпись: `codesign --options=runtime` + entitlement `com.apple.security.cs.allow-jit`
  (JIT-исключение обязательно для Avalonia под hardened runtime).
- Нотаризация: `ditto` → `xcrun notarytool submit --wait` → `xcrun stapler staple`.
  `.dmg` нотаризуется и стаплируется **отдельно**.
- Требует **платной подписки Apple Developer Program** → продуктовый вопрос.
- Кросс-компиляция с Linux/Windows возможна (обычный `net10.0`), но подписывать всё равно на macOS.

### 7.2. Windows

```bash
dotnet publish src/Sudoku.App -c Release -r win-x64 --self-contained true -o out/win-x64
```

- Инсталлятор — NSIS (`.exe`) или MSIX (WiX/MSIX SDK). Официальная документация Avalonia
  для Windows-деплоя описывает только сторонний платный инструмент Parcel;
  бесплатные альтернативы (NSIS, Inno Setup, WiX) — приемлемы.
- Подпись: Authenticode (SignTool / JSign / rcodesign). Без подписи — SmartScreen-предупреждение.

### 7.3. Linux

```bash
dotnet publish src/Sudoku.App -c Release -r linux-x64 --self-contained true -o out/linux-x64
```

- Обязательные зависимости: `libx11-6 libice6 libsm6 libfontconfig1`.
- Форматы: `.deb` (ручная сборка через `dpkg-deb` — описан в официальной документации Avalonia),
  `.rpm`, portable `.zip`, AppImage, Flatpak (вне обязательного объёма).
- glibc 2.17+ (Skia собран под него) — Alpine/musl не поддерживается без кастомной сборки Skia.

### 7.4. CI

Сборка всех RID на одной машине возможна (кросс-компиляция), но подпись macOS — только на macOS
с установленным сертификатом. Рекомендуемая схема: Linux-раннер собирает linux+win,
macOS-раннер — osx (+ подпись/нотаризация при наличии секретов).

---

## 8. Тестирование

| Уровень | Инструмент | Что покрывает |
|---|---|---|
| Unit | xUnit v3 | Решатель, генератор, рейтинг, таймер, сериализация |
| Property/fuzz | xUnit v3 + детерминированный seed | Инварианты: уникальность решения, корректность подсказок, симметрия save/load |
| Интеграционные | xUnit v3 + temp-каталог | Round-trip сохранения, устойчивость к битому JSON |
| UI | `Avalonia.Headless` + `Avalonia.Headless.XUnit` (версия 12.1.6) | Ввод цифр, навигация, отображение таймера |

Эталонные наборы: собственные 50 головоломок в формате Project Euler #96 (собственный
генератор — из-за лицензии), плюс 17-подсказочные головоломки Royle (для проверки минимальности).

### 8.1. Текущее состояние тестов

| Проект | Тестов | Что проверяет |
|---|---|---|
| `Sudoku.Domain.Tests` | 56 | `Grid`: разбор строки, ошибки ввода, полнота, непротиворечивость, равенство, индексация; `Puzzle` и `DifficultyRating`: состояния оценки, некорректные аргументы; `GameSession`: ввод, стирание, выбор клетки, запрет изменения исходных клеток, конфликты, подсказки, победа |
| `Sudoku.Application.Tests` | 119 | Решатель: известная головоломка, отсутствие решения (в том числе у непротиворечивой неразрешимой головоломки), несколько решений, уже решённая доска, некорректный ввод, неизменность входа, детерминизм, предел поиска, полнота и непротиворечивость найденного решения. Генератор: корректность и полнота решения, отсутствие повторов в исходной сетке, единственность решения (проверяется и production-решателем, и независимым эталонным решателем), соответствие заявленному уровню по потолку приёмов, воспроизводимость по seed, разнообразие, попадание в бюджет времени. Логический решатель: корректность выводов (логическое решение совпадает с истинным), минимальность уровня, обработка противоречий. Фабрика сессий: новая игра по сложности и seed, независимость сессий, подсказка по подтверждённому решению. Сценарий Endless: выбранная сложность, новая головоломка, переход к следующей, засчитана решённая или брошенная головоломка, неограниченные подсказки, отсутствие ограничения по времени, изоляция режимов |
| `Sudoku.Infrastructure.Tests` | 1 | Отсутствие ссылок на Avalonia и UI |
| `Sudoku.App.Tests` | 22 | Headless: окно открывается, показывает режим и выбранную сложность, экран захода Endless отображает состояние и поле, стили ячеек (исходная/конфликт) применяются; unit-тесты ViewModel: выбор ячейки, ввод цифры, стирание, подсказка, новая головоломка, завершение захода, старт нового захода |

Итого: **198 тестов** (197 выполняются в обычном прогоне, 1 — по требованию), все зелёные.

Полная выборка генератора (1002 головоломки, проверка единственности и попадания в полосу)
вынесена в отдельный тест и запускается по требованию:
`SUDOKU_FULL_BATCH=1 dotnet test`.

Тестовые данные решателя — опубликованные головоломки (статья «Sudoku» в Википедии),
неоднозначная головоломка, полученная из опубликованного решения удалением четырёх ячеек,
и непротиворечивая неразрешимая головоломка («смертельный узор» 2×2 в блоке), построенная
вручную. Ни один набор не получен от собственного решателя.
Данные не получены от собственного решателя.

Тест-проекты используют xUnit v3 в режиме Microsoft.Testing.Platform
(`TestingPlatformDotnetTestSupport=true`), поэтому `dotnet test` работает без
`Microsoft.NET.Test.Sdk` и `xunit.runner.visualstudio`.

---

## 9. Порядок внедрения

См. [docs/TASKS.md](TASKS.md). Кратко: инфраструктура решения → домен → решатель → генератор
и рейтинг → хранение → UI меню → игровое поле → подсказки → режимы → упаковка → полировка.

Каждый этап заканчивается работающей сборкой и зелёными тестами, этапы не объединяются.
