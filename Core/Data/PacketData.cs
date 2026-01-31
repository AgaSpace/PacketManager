namespace PacketManager.Core.Data;

/// <summary>
/// Структура данных, содержащая параметры исходящего сетевого пакета.
/// Соответствует аргументам метода NetMessage.SendData.
/// </summary>
public readonly record struct PacketData(
    /// <summary>
    /// Идентификатор удаленного клиента (-1 для отправки всем).
    /// </summary>
    int RemoteClient,

    /// <summary>
    /// Текстовое сообщение (для чатов, имен NPC и т.д.).
    /// </summary>
    string? Text,

    /// <summary>
    /// Основной числовой параметр (обычно ID игрока или объекта).
    /// </summary>
    int Number,

    /// <summary>
    /// Дополнительный параметр 1 (обычно float: позиция X, скорость и т.д.).
    /// </summary>
    float Number2,

    /// <summary>
    /// Дополнительный параметр 2 (обычно float: позиция Y).
    /// </summary>
    float Number3,

    /// <summary>
    /// Дополнительный параметр 3 (обычно float: позиция Z или скорость).
    /// </summary>
    float Number4,

    /// <summary>
    /// Дополнительный параметр 4 (целое).
    /// </summary>
    int Number5,

    /// <summary>
    /// Дополнительный параметр 5 (целое).
    /// </summary>
    int Number6,

    /// <summary>
    /// Дополнительный параметр 6 (целое).
    /// </summary>
    int Number7
);