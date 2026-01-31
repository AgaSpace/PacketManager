using PacketManager.Core.Data;

namespace PacketManager.Core.Abstractions;

public interface IPacketGenerator
{
    byte[] GenerateOriginal(byte messageId, PacketData data);
    // ИЗМЕНЕНО: Принимаем список билдеров для цепочной обработки
    byte[] GenerateCustom(IReadOnlyList<IPacketBuilder> builders, byte messageId, PacketData data,
        IReadOnlyCollection<INetworkClient> targets);
}