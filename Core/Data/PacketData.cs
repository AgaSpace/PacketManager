namespace PacketManager.Core.Data;

public readonly record struct PacketData(
    int RemoteClient,
    string? Text,
    int Number,
    float Number2,
    float Number3,
    float Number4,
    int Number5,
    int Number6,
    int Number7
);
