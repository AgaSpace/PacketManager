#region Using

using PacketManager.Core.Abstractions;

#endregion

namespace PacketManager.Server.Api;

public static class Facade
{
    private static Core.Implementations.PacketManager? _manager;

    internal static void Initialize(Core.Implementations.PacketManager manager) => _manager = manager;

    public static IPacketBuilderRegistry Registry =>
        _manager?.Registry ?? throw new InvalidOperationException("Not initialized");

    public static bool AddBuilder(int playerId, IPacketBuilder builder) =>
        Registry.AddBuilder(playerId, builder).IsSuccess;

    public static bool RemoveBuilder(int playerId, IPacketBuilder builder) =>
        Registry.RemoveBuilder(playerId, builder).IsSuccess;

    public static bool HasBuilder(int playerId, byte messageId) =>
        Registry.HasBuilder(playerId, messageId);
}