namespace Sudoku.Application;

/// <summary>
/// Ошибка генерации: за отведённое число попыток не удалось построить
/// головоломку, удовлетворяющую всем требованиям.
/// </summary>
public sealed class PuzzleGenerationException : Exception
{
    /// <summary>
    /// Создаёт исключение без дополнительного сообщения.
    /// </summary>
    public PuzzleGenerationException()
        : base("Не удалось сгенерировать головоломку заданной сложности.")
    {
    }

    /// <summary>
    /// Создаёт исключение с указанным сообщением.
    /// </summary>
    /// <param name="message">Описание причины.</param>
    public PuzzleGenerationException(string message)
        : base(message)
    {
    }
}
