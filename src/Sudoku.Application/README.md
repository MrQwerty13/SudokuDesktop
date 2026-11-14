# Sudoku.Application

Слой **Application**: сценарии игры поверх домена.

**Зависит от:** `Sudoku.Domain`.
**Запрещено:** Avalonia, `System.IO`, файловая система, сеть, зависимость от `Sudoku.Infrastructure`
и `Sudoku.App`.

Слой содержит:

- порты (абстракции), которые реализует `Sudoku.Infrastructure`;
- сценарии (use cases), которые вызывает `Sudoku.App`;
- модели, описывающие игровые опции для слоя представления.

## Содержимое

| Тип | Назначение |
|---|---|
| `GameOptions` | Опции игры для слоя представления (уровни сложности, режимы) |
| `ISudokuSolver` / `MrvSudokuSolver` | Решение головоломки, подсчёт решений с пределом |
| `SolveResult` / `SolveOutcome` | Результат решения: нет решения / единственное / несколько |
| `IPuzzleGenerator` / `PuzzleGenerator` | Генерация головоломки заданного уровня по seed |
| `IDifficultyRater` / `LogicalDifficultyRater` | Оценка сложности по потолку логических приёмов |
| `DifficultyBands` / `DifficultyBand` | Калибровка уровней: полосы потолка приёмов, минимум подсказок |
| `IRandomSource` / `SystemRandomSource` | Источник случайности с явным seed |
| `PuzzleGenerationException` | Не удалось построить головоломку заданного уровня |
| `IGameSessionFactory` / `GameSessionFactory` | Новая игра: генерация головоломки выбранной сложности по seed и создание сессии |

Внутренние (не публичные) типы: `LogicalSolver` — лестница приёмов v1, на которой строится
оценка сложности; `RandomGridFiller` — случайное корректное заполнение доски.

Алгоритм генерации, калибровка уровней и измеренные ограничения описаны в
[ADR-0009](../docs/ADR/0009-puzzle-generation-algorithm.md) и
[ADR-0003](../docs/ADR/0003-difficulty-rating.md).

Правила слоя закреплены тестами `Sudoku.Application.Tests/LayerBoundaryTests.cs`.
