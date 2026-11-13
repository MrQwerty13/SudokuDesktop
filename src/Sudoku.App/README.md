# Sudoku.App

Слой **Presentation**: UI на Avalonia, ViewModels и композиция зависимостей.

**Зависит от:** `Sudoku.Application`, `Sudoku.Domain`, `Sudoku.Infrastructure` (когда появится).
**Запрещено:** игровая логика (генерация, решение, оценка сложности), прямой доступ
к файловой системе мимо `Sudoku.Infrastructure`.

Состав:

- `Program.cs` — точка входа, `AppBuilder`;
- `App.axaml(.cs)` — корневой `Avalonia.Application`, тема Fluent, запуск окна;
- `Views/MainWindow.axaml(.cs)` — окно приложения;
- `ViewModels/` — ViewModels на CommunityToolkit.Mvvm;
- `ViewLocator.cs` — маршрутизация ViewModel → View по соглашению имён.

Запуск: `dotnet run --project src/Sudoku.App`.
