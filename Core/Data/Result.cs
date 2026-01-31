namespace PacketManager.Core.Data;

/// <summary>
/// Представляет результат операции, который может быть успешным или содержать ошибку.
/// </summary>
public readonly record struct Result(bool IsSuccess, string? Error = null)
{
    /// <summary>
    /// Создает успешный результат.
    /// </summary>
    public static Result Success() => new(true);

    /// <summary>
    /// Создает результат с ошибкой.
    /// </summary>
    /// <param name="error">Описание ошибки.</param>
    public static Result Failure(string error) => new(false, error);

    /// <summary>
    /// Получает описание ошибки, если операция не успешна.
    /// </summary>
    public string? Error { get; init; } = Error;

    /// <summary>
    /// Получает значение, указывающее, была ли операция успешной.
    /// </summary>
    public bool IsSuccess { get; init; } = IsSuccess;
}