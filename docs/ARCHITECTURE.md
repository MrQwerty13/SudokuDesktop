# ARCHITECTURE.md — Sudoku Desktop

Статус: проектная (этап 0 завершён, реализация не начата).
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

## 2. Обнаруженное состояние репозитория

Репозиторий **не пустой** — в нём уже есть скаффолд Avalonia. Ничего не удалено и не переписано.

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

Состояние Git: ветка `main`, **0 коммитов**; `.gitignore`, `Sudoku.sln` и файлы `Sudoku/`
проиндексированы, `Program.cs`, `ViewLocator.cs` и оба ViewModel — изменены после индексации,
`.idea/` — не отслеживается.

Проверка: `dotnet build` → **Build succeeded, 0 warnings, 0 errors** (SDK 10.0.403, macOS arm64).

Изменения, которые будут сделаны на этапе 1 (не входят в текущий этап):
обновление Avalonia 12.0.4 → 12.1.6, `AvaloniaUI.DiagnosticsSupport` 2.2.1 → 2.2.3,
`CommunityToolkit.Mvvm` 8.4.1 → 8.4.2, добавление `.idea/` в `.gitignore`.

---

## 3. Целевая архитектура

### 3.1. Решение по числу проектов

Классическое расщепление на Domain / Application / Infrastructure / Presentation для данного
проекта **избыточно**: домен судоку — это доска, цифры и правила; отдельный слой Application
не даёт самостоятельной ценности. Предлагается **три проекта** (см. [ADR-0002](ADR/0002-project-and-layer-structure.md)):

```
Sudoku.sln
├── Sudoku.Core/          (net10.0, classlib)   ← домен + прикладная логика, НЕТ UI и ФС
├── Sudoku.Infrastructure/(net10.0, classlib)   ← сохранение, банк головоломок, пути ФС
├── Sudoku.App/           (net10.0, WinExe)     ← Avalonia UI, ViewModels, DI
├── Sudoku.Core.Tests/        (xUnit v3)
├── Sudoku.Infrastructure.Tests/
└── Sudoku.App.Tests/         (Avalonia.Headless + Avalonia.Headless.XUnit)
```

Правило зависимостей:

```
        ┌──────────────┐
        │  Sudoku.App  │  (Avalonia)
        └───┬──────┬───┘
            │      │
      ┌─────▼──┐ ┌─▼───────────────┐
      │  Core  │ │ Infrastructure  │
      └────────┘ └─────┬───────────┘
            ▲           │
            └───────────┘
```

`Sudoku.Core` не ссылается ни на Avalonia, ни на `System.IO`. Всё, что связано с файловой
системой, живёт в `Sudoku.Infrastructure`; UI — только в `Sudoku.App`.

### 3.2. Что находится в `Sudoku.Core`

**Домен (чистые типы, без побочных эффектов)**

| Тип | Назначение |
|---|---|
| `Grid` / `Cell` | Доска 9×9; значение 0 = пусто; неизменяемый снаружи |
| `Puzzle` | Головоломка: начальные подсказки + единственное решение + рейтинг сложности |
| `Difficulty` | `Easy`, `Normal`, `Hard` |
| `DifficultyRating` | Числовая оценка + ранг «потолка техник» |
| `GameMode` | `Endless`, `TimeAttack` |
| `GameSession` | Состояние захода: режим, сложность, текущая головоломка, введённые значения, счёт, таймер |

**Прикладная логика (чистые функции, детерминированные)**

| Тип | Назначение |
|---|---|
| `ISudokuSolver` | Решение головоломки; `Solve()` и `CountSolutions(limit: 2)` |
| `MrvSudokuSolver` | Реализация: backtracking + constraint propagation + выбор ячейки по MRV |
| `PuzzleGenerator` | Генерация головоломки заданной сложности с seed |
| `IPuzzleGenerator` | Абстракция (нужна для подмены в тестах и для банка) |
| `IDifficultyRater` | Оценка сложности по потолку техник |
| `IHintProvider` | Следующий логический шаг для подсказки (использует тот же логический решатель) |
| `ITimeProvider` | Абстракция времени (`UtcNow`, `Elapsed`) — для детерминированных тестов таймера |
| `GameSessionFactory` | Создание сессии под режим и сложность |

**Ключевой инвариант детерминизма:** `PuzzleGenerator.Generate(difficulty, seed)` при одинаковом
seed всегда даёт одну и ту же головоломку. `System.Random` создаётся внутри с явным seed;
`Random.Shared` не используется.

### 3.3. Что находится в `Sudoku.Infrastructure`

| Тип | Назначение |
|---|---|
| `IStateStore` | Сохранение/загрузка `GameSession` (JSON) |
| `JsonStateStore` | Реализация на `System.Text.Json` (атомарная запись через temp-файл + replace) |
| `IAppPaths` | Пути к каталогам данных/настроек |
| `AppPaths` | Реализация: `Environment.GetFolderPath(SpecialFolder.LocalApplicationData)` → `~/Library/Application Support/Sudoku` (macOS), `~/.local/share/Sudoku` (Linux), `%LOCALAPPDATA%\Sudoku` (Windows) |
| `IPuzzleBank` | Доступ к заранее сгенерированным головоломкам (в будущем) |
| `PuzzleBank` | Файл/ресурс с банком + рандомизация перестановками цифр/строк/столбцов |

### 3.4. Что находится в `Sudoku.App`

| Тип | Назначение |
|---|---|
| `App`, `Program` | Точка входа, `UsePlatformDetect()`, DI-композиция |
| `MainWindow` + `MainWindowViewModel` | Корень навигации: меню режимов и экран игры |
| `GameView` + `GameViewModel` | Игровое поле, ввод, подсказки, таймер |
| `BoardView` | Рисование поля 9×9 (переиспользуемый `ItemsControl`/`Canvas` с 81 ячейкой) |
| `CellViewModel` | Состояние одной ячейки: значение, заданные/введённые, выделение, конфликт |
| `TimerService` | Периодическое обновление отображения таймера; измеряет интервал через `ITimeProvider`, а не через `DispatcherTimer.Tick` |
| `ThemeManager` | Светлая/тёмная тема (по умолчанию — системная) |

MVVM: CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`), compiled bindings
(уже включены в скаффолде: `AvaloniaUseCompiledBindingsByDefault: true`).

### 3.5. Модель данных сохранения

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
dotnet publish Sudoku -c Release -r osx-arm64 --self-contained true \
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
dotnet publish Sudoku -c Release -r win-x64 --self-contained true -o out/win-x64
```

- Инсталлятор — NSIS (`.exe`) или MSIX (WiX/MSIX SDK). Официальная документация Avalonia
  для Windows-деплоя описывает только сторонний платный инструмент Parcel;
  бесплатные альтернативы (NSIS, Inno Setup, WiX) — приемлемы.
- Подпись: Authenticode (SignTool / JSign / rcodesign). Без подписи — SmartScreen-предупреждение.

### 7.3. Linux

```bash
dotnet publish Sudoku -c Release -r linux-x64 --self-contained true -o out/linux-x64
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

---

## 9. Порядок внедрения

См. [docs/TASKS.md](TASKS.md). Кратко: инфраструктура решения → домен → решатель → генератор
и рейтинг → хранение → UI меню → игровое поле → подсказки → режимы → упаковка → полировка.

Каждый этап заканчивается работающей сборкой и зелёными тестами, этапы не объединяются.
