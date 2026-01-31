#region Using

using PacketManager.Core.Data;

#endregion

namespace PacketManager.Core.Abstractions;

public interface IPacketBuildContext
{
    byte MessageId { get; }
    BinaryWriter Writer { get; }
    IReadOnlyCollection<INetworkClient> Targets { get; }
    PacketData OriginalData { get; }
}
