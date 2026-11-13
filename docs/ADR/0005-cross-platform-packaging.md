# ADR-0005. Кроссплатформенная упаковка и публикация

- **Статус:** Принято
- **Дата:** 2026-11-13
- **Связано с:** [ADR-0001](0001-dotnet-avalonia-stack.md)

## Контекст

Нужно распространять приложение на macOS, Linux и Windows из одного репозитория.
Ключевые вводные из официальной документации Avalonia:

- официальных пакетов `Avalonia.Distribution.*` **не существует** — вся платформенная упаковка
  входит в `Avalonia.Desktop` (`Avalonia.Win32`, `Avalonia.X11`, `Avalonia.Native`,
  `Avalonia.Skia`, `Avalonia.HarfBuzz`);
- основной механизм публикации — `dotnet publish` с RID; установщики собираются внешними
  инструментами;
- `Parcel` (Avalonia Plus, коммерческий) умеет `.deb`, `.rpm`, `.zip`, `.dmg`, `.pkg`,
  `.msix`, `.nsis`, но требует лицензию — как обязательный инструмент он не подходит;
- официальная документация по Windows-деплою отсутствует (страница 404), для Linux описан
  только ручной `.deb` через `dpkg-deb`;
- macOS требует подписи и нотаризации для распространения вне App Store.

Цели: одна команда публикации на RID, отсутствие платных зависимостей, воспроизводимость,
возможность подписать артефакты на соответствующей ОС.

## Решение

### Общий принцип

`dotnet publish` с RID + самодостаточная сборка; платформенный инсталлятор — отдельный
скрипт в репозитории. Публикация воспроизводима и не зависит от лицензий.

```bash
dotnet publish Sudoku -c Release -r <rid> --self-contained true \
  -p:PublishSingleFile=true -o out/<rid>
```

| RID | Платформа | Self-contained |
|---|---|---|
| `osx-x64`, `osx-arm64` | macOS | да (иначе требуется установленный .NET) |
| `linux-x64` | Linux | да (официально «recommended») |
| `win-x64` | Windows | да |

`PublishSingleFile=true` — обязателен на macOS: упрощает `codesign`/`notarytool`
и устраняет проблемы песочницы App Store.

### macOS

1. `dotnet publish -r osx-arm64 --self-contained -p:PublishSingleFile=true`.
2. Сборка `.app` вручную: `Contents/MacOS` ← publish, `Contents/Info.plist`,
   `Contents/Resources/Sudoku.icns`.
3. `Info.plist` содержит `CFBundleExecutable`, `CFBundleName` (≤ 15 символов),
   `CFBundleDisplayName`, `CFBundleIdentifier`, `CFBundleVersion`,
   `CFBundleShortVersionString`, `NSHighResolutionCapable`, `CFBundleIconFile`.
4. `UseAppHost=true` обязателен — иначе нет нативного исполняемого файла.
5. Подпись: `codesign --options=runtime --entitlements entitlements.plist` с обязательным
   entitlement `com.apple.security.cs.allow-jit` (JIT-исключение нужно Avalonia под
   hardened runtime). Подписываются вложенные файлы, затем бандл; `codesign --deep` не используется.
6. Нотаризация: `ditto -c -k --sequesterRsrc --keepParent` → `xcrun notarytool submit --wait`
   → `xcrun stapler staple`. `.dmg` нотаризуется и стаплируется **отдельно** от `.app`.
7. Требуется платная подписка **Apple Developer Program** (Developer ID Application).
   Без неё сборка распространяется неподписанной — Gatekeeper предупредит пользователя.

### Windows

- `dotnet publish -r win-x64 --self-contained` (+ опционально `win-arm64`).
- Инсталлятор: **NSIS** (`.exe`) — прост, бесплавен, поддерживает автообновление через
  winget/scoop; или **MSIX** (через MSIX SDK / WiX) — требует сертификата для установки
  неподписанных пакетов. Выбор — за владельцем (Q-12).
- Подпись: Authenticode (`SignTool` на Windows, `rcodesign`/`JSign` — кроссплатформенно).

### Linux

- `dotnet publish -r linux-x64 --self-contained`.
- Обязательные зависимости: `libx11-6 libice6 libsm6 libfontconfig1`.
- Формат по умолчанию: **`.deb`** (сборка вручную через `dpkg-deb`, как описано в официальной
  документации Avalonia) с корректным `.desktop`-файлом и иконками в `hicolor`.
- Дополнительно возможны `.rpm`, portable `.zip`, AppImage, Flatpak — не обязательны.
- Требование glibc ≥ 2.17 (Skia) ⇒ Alpine/musl не поддерживается без кастомной сборки Skia.

### CI

- Linux-раннер: `linux-x64`, `win-x64` (кросс-компиляция поддерживается).
- macOS-раннер: `osx-arm64`, `osx-x64` + подпись/нотаризация (если секреты доступны).
- Сборка всех RID на одной машине возможна, но нотаризация выполняется только на macOS
  с установленным сертификатом.

### А что насчёт AOT/Trimming?

Native AOT официально поддерживается Avalonia, но **не выбран** для Release 1.0: он требует
compiled bindings, запрета runtime-рефлексии и полного перечня trimmer-конфигураций, а в
проекте используется рефлексивный `ViewLocator`. Включать AOT можно после отказа от
`ViewLocator` (замена на явные `DataTemplate`) — это отдельная задача.

## Последствия

**Положительные**
- Ноль платных инструментов в обязательном пути.
- Воспроизводимая публикация: скрипты в репозитории, версии зафиксированы.
- Кросс-компиляция сокращает число машин в CI.
- Совместимость с дистрибутивами: пользователь не обязан ставить .NET.

**Отрицательные**
- Нотаризация macOS требует платной подписки Apple Developer — либо пользователи видят
  предупреждение Gatekeeper.
- MSIX требует сертификата для «чистой» установки; NSIS проще.
- Сборка `.app`/`.deb` вручную — это скрипты, которые нужно поддерживать; официального
  бесплатного «одного клика» нет.
- Wayland на Linux по умолчанию не используется (XWayland) — приемлемо для Tier 1/2.

## Альтернативы

| Вариант | Оценка |
|---|---|
| **Parcel (Avalonia Plus)** | Отклонён как обязательный: коммерческая лицензия (`AVALONIA_TOOLS_LICENSE_KEY`), закрытый формат сгенерированных пакетов |
| **WiX Toolset / MSIX SDK** | Рассмотрено для Windows; выбор за владельцем (Q-12). Требует Windows-SDK или MSIX SDK в CI |
| **Inno Setup** | Рассмотрено как альтернатива NSIS; аналогичная сложность |
| **`appimage-builder` / Flatpak** | Отложено: не в минимальном объёме; официальная поддержка Avalonia не документирована |
| **Native AOT** | Отложено: конфликт с рефлексивным `ViewLocator`, риск потери функциональности; вернуться после этапа 13 |
| **Framework-dependent publish** | Отклонён: требует установки .NET у пользователя — противоречит «открыл и играешь» |
| **Flatpak как основной Linux-формат** | Отклонён: требует Flatpak-рантайма у пользователя, ограничивает доступ к ФС |
