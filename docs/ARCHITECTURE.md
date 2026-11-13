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
| `Grid` / `Cell` | Доска 9×9; значение 0 = пусто; неизменяемый снаружи | ⬜ этап 2 |
| `Puzzle` | Головоломка: начальные подсказки + единственное решение + рейтинг сложности | ⬜ этап 2 |
| `DifficultyRating` | Числовая оценка + ранг «потолка техник» | ⬜ этап 2 |
| `GameSession` | Состояние захода: режим, сложность, текущая головоломка, введённые значения, счёт, таймер | ⬜ этап 2 |

### 3.3. Что находится в `Sudoku.Application`

**Прикладная логика (чистые функции, детерминированные)**

| Тип | Назначение | Статус |
|---|---|---|
| `GameOptions` | Опции игры для слоя представления: уровни сложности и режимы | ✅ реализован |
| `ISudokuSolver` | Решение головоломки; `Solve()` и `CountSolutions(limit: 2)` | ⬜ этап 3 |
| `MrvSudokuSolver` | Реализация: backtracking + constraint propagation + выбор ячейки по MRV | ⬜ этап 3 |
| `PuzzleGenerator` | Генерация головоломки заданной сложности с seed | ⬜ этап 4 |
| `IPuzzleGenerator` | Абстракция (нужна для подмены в тестах и для банка) | ⬜ этап 4 |
| `IDifficultyRater` | Оценка сложности по потолку техник | ⬜ этап 4 |
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
| `MainWindow` + `MainWindowViewModel` | Минимальное окно приложения (на текущем этапе — проверка запуска UI) | ✅ реализован |
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

1. `Solve(grid)` — единственное решение.
2. `CountSolutions(grid, limit: 2)` — уникальность (прерывается на втором решении).
3. Пошаговое применение техник (для подсказок).

Реализация: битовые маски кандидатов + constraint propagation (naked single, hidden single)
+ DFS с выбором ячейки по **MRV** (minimum remaining values). Для Sudoku этого достаточно;
эталон — реализация Norvig (https://norvig.com/sudoku.html).

DLX / Algorithm X (729×324 exact cover) — **не нужен**: он оправдан, когда требуется перечисление
всех решений. Проверка «решений ровно одно» делается тем же MRV-решателем с лимитом 2.

### 4.2. Генерация

1. Сгенерировать полное заполнение: случайный MRV-backfill по возрастанию сложности рандомизации.
2. Удалять подсказки в случайном порядке; после каждого удаления проверять, что решение
   остаётся **единственным** (`CountSolutions(limit: 2) == 1`).
3. Остановиться, когда достигнут целевой порог рейтинга.
4. Зафиксировать seed.

Альтернатива (быстрее и стабильнее по качеству): **генерация с целевой техникой** —
удалять подсказки, пока решатель «без догадок» находит очередную технику нужного уровня
(это то, что делает tdoku). Требует реализации лестницы техник.

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

Подсказка = следующий шаг логического решателя: применяется самая простая доступная техника,
подсвечивается затронутая ячейка/пара и объясняется правило. Неограниченность — следствие
отсутствия счётчика в модели `GameSession` (учёт подсказок ведётся только в статистике).

---

## 5. Режимы и таймер

### 5.1. Endless

Лимита времени нет. Каждая решённая головоломка порождает следующую той же сложности.
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
| `Sudoku.Domain.Tests` | 4 | Состав и порядок уровней сложности и режимов; отсутствие ссылок на UI и другие слои |
| `Sudoku.Application.Tests` | 3 | Состав игровых опций; зависимость Application только от Domain |
| `Sudoku.Infrastructure.Tests` | 1 | Отсутствие ссылок на Avalonia и UI |
| `Sudoku.App.Tests` | 2 | Headless: окно открывается, отображает название и данные из доменного слоя |

Итого: **10 тестов**, все зелёные.

Тест-проекты используют xUnit v3 в режиме Microsoft.Testing.Platform
(`TestingPlatformDotnetTestSupport=true`), поэтому `dotnet test` работает без
`Microsoft.NET.Test.Sdk` и `xunit.runner.visualstudio`.

---

## 9. Порядок внедрения

См. [docs/TASKS.md](TASKS.md). Кратко: инфраструктура решения → домен → решатель → генератор
и рейтинг → хранение → UI меню → игровое поле → подсказки → режимы → упаковка → полировка.

Каждый этап заканчивается работающей сборкой и зелёными тестами, этапы не объединяются.
