using PacketManager.Core.Data;

namespace PacketManager.Core.Abstractions;

public interface IPacketBuilderRegistry
{
    Result AddBuilder(int playerId, IPacketBuilder builder);
    Result RemoveBuilder(int playerId, IPacketBuilder builder);
    bool HasBuilder(int playerId, byte messageId);
    // ИЗМЕНЕНО: IList для доступа по индексу и сохранения порядка
    IReadOnlyList<IPacketBuilder> GetBuilders(int playerId, byte messageId);
}