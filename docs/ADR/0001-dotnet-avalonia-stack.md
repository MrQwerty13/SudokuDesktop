# ADR-0001. Выбор стека: C# + .NET 10 + Avalonia UI 12

- **Статус:** Принято
- **Дата:** 2026-11-13
- **Связано с:** [ADR-0002](0002-project-and-layer-structure.md), [ADR-0005](0005-cross-platform-packaging.md)

## Контекст

Продукт: кроссплатформенная одиночная офлайн-игра судоку для macOS, Linux и Windows.
Гипотеза владельца — «C# + .NET + Avalonia UI» — требовала проверки по официальной документации:
актуальность версий, поддерживаемые ОС, готовность инструментов сборки/тестирования/упаковки.

Исходное состояние репозитория: скаффолд Avalonia-приложения на `net10.0`
(Avalonia 12.0.4, CommunityToolkit.Mvvm 8.4.1), собирается без ошибок.

## Проверка

| Вопрос | Ответ (на 11.2026) | Источник |
|---|---|---|
| Актуальный .NET | .NET 11 — STS (с 10.11.2026, поддержка до 14.11.2028); **.NET 10 — LTS** (до 14.11.2028) | dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core |
| Рекомендуемый TFM для Avalonia 12 | «The recommended target is .NET 10» | docs.avaloniaui.net/docs/avalonia12-breaking-changes |
| Актуальная версия Avalonia | **12.1.6** (12.0.0 — стабильный релиз 07.04.2026; 12.1.0 добавил нативный Wayland) | github.com/AvaloniaUI/Avalonia/releases |
| Поддержка ОС | Tier 1: macOS 26, Windows 11 24H2+, Ubuntu 25.x, Debian 13, Fedora 43 | docs.avaloniaui.net/docs/supported-platforms |
| Linux | X11 по умолчанию; Wayland — опциональный пакет с 12.1.0; нужны `libx11-6 libice6 libsm6 libfontconfig1`; glibc 2.17+ | docs.avaloniaui.net/docs/deployment/linux |
| macOS | Нативный бэкенд, `MACOSX_DEPLOYMENT_TARGET=12.0` с 12.1.5; нотаризация обязательна вне App Store | docs.avaloniaui.net/docs/deployment/macos |
| Кросс-компиляция | Поддерживается (обычный `net10.0`, без OS-workload) | docs.avaloniaui.net/docs/platform-specific-guides/* |
| AOT / Trimming | Официально поддерживается, требует compiled bindings и `IsAotCompatible` | docs.avaloniaui.net/docs/deployment/native-aot |

## Решение

Стек подтверждён:

- **C#** — .NET 10 (LTS), `TargetFramework: net10.0`.
- **Avalonia UI 12.x** — целевая версия **12.1.6** (с обновлением с 12.0.4 на этапе 1).
- **CommunityToolkit.Mvvm 8.4.x** — MVVM, source generators, без рантайм-рефлексии.
- **xUnit v3** — тестирование; `Avalonia.Headless` + `Avalonia.Headless.XUnit` — UI-тесты
  (Avalonia 12 перешёл с xUnit v2 на v3).
- Тема Fluent (уже в скаффолде), `RequestedThemeVariant="Default"` — следование системной теме.

Целевые ОС: macOS 14+ (x64/arm64), Linux x64 (дистрибутивы Tier 1/2), Windows 10 22H2+ / 11 (x64).

## Последствия

**Положительные**
- Единая кодовая база и единый UI-код для трёх ОС.
- Нативный вид и производительность на каждой платформе.
- Прямой доступ к .NET-экосистеме: JSON, DI, тесты.
- Возможность кросс-компиляции (Windows/macOS сборки делаются с Linux).
- Офлайн-ограничение выполняется естественно: сеть не нужна нигде.

**Отрицательные / риски**
- Avalonia — третья сторона: 12.x — свежая мажорная линия; необходимо следить за changelog.
- DevTools в Avalonia 12 вынесены в коммерческий Avalonia Plus ⇒ в OSS-сборке полноценных
  инструментов инспекции UI нет (остаётся логирование). `AvaloniaUI.DiagnosticsSupport` 2.x —
  тоже продукт Avalonia Plus.
- Поддержка Wayland на Linux — экспериментальная, без авто-фолбэка; по умолчанию XWayland.
- Нет официальных `Avalonia.Distribution.*` пакетов: упаковка — `dotnet publish` + внешние
  инструменты (см. ADR-0005).
- Диагностика UI затруднена без DevTools ⇒ критично логирование и headless-тесты.

## Альтернативы

| Вариант | Почему отклонён |
|---|---|
| **.NET 11 (STS)** | Не LTS: на 1 год меньше бесплатных патчей при той же дате окончания поддержки (14.11.2028). Avalonia рекомендует .NET 10. Пакеты Avalonia публикуются только для `net8.0`/`net10.0` |
| **Electron / Tauri + веб-фронтенд** | Больше рантайм-оверхеда, внешние процессы, слабее контроль над таймером и вводом; Tauri требует связки Rust + веб-стека |
| **Qt (C++) / Qt for Python** | Тяжелее сборка и дистрибуция, три нативных сборки вместо одной managed |
| **WinUI 3 / WPF** | Только Windows (WPF) либо Windows-only (WinUI 3) |
| **JavaFX** | Отдельный рантайм, хуже интеграция с современным .NET-тулингом, своя экосистема упаковки |
| **Unity / Godot** | Избыточны: игровой движок ради логической игры без графики в реальном времени |
| **Avalonia 11.x** | Консервативно, но 12.0 уже в стабильном релизе (апрель 2026), 12.1 добавил Wayland и TableView; оставаться на 11 без причины |

## Связанные документы

- [docs/ARCHITECTURE.md](../ARCHITECTURE.md#1-проверка-гипотезы-о-стеке)
- [docs/REQUIREMENTS.md](../REQUIREMENTS.md#2-нефункциональные-требования)
