# Sudoku.Infrastructure

Слой **Infrastructure**: реализации портов, работа с файловой системой и внешними ресурсами.

**Зависит от:** `Sudoku.Application` (и транзитивно от `Sudoku.Domain`).
**Запрещено:** Avalonia, бизнес-правила игры, зависимость от `Sudoku.App`.

Будущее содержимое (этапы 6 и 5):

- `AppPaths` / `IAppPaths` — кроссплатформенные пути к данным приложения;
- `JsonStateStore` / `IStateStore` — сохранение и загрузка состояния захода;
- `PuzzleBank` / `IPuzzleBank` — банк заранее сгенерированных головоломок.

Правила слоя закреплены тестами `Sudoku.Infrastructure.Tests/LayerBoundaryTests.cs`.
