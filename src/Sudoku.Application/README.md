# Sudoku.Application

Слой **Application**: сценарии игры поверх домена.

**Зависит от:** `Sudoku.Domain`.
**Запрещено:** Avalonia, `System.IO`, файловая система, сеть, зависимость от `Sudoku.Infrastructure`
и `Sudoku.App`.

Слой содержит:

- порты (абстракции), которые реализует `Sudoku.Infrastructure`;
- сценарии (use cases), которые вызывает `Sudoku.App`;
- модели, описывающие игровые опции для слоя представления.

На текущем этапе реализован `GameOptions` — единственная точка входа Presentation-слоя
в домен (уровни сложности и режимы игры).

Правила слоя закреплены тестами `Sudoku.Application.Tests/LayerBoundaryTests.cs`.
