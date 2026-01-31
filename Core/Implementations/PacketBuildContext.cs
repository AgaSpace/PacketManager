#region Using

using PacketManager.Core.Abstractions;
using PacketManager.Core.Data;

#endregion

namespace PacketManager.Core.Implementations;

internal class PacketBuildContext(byte messageId, BinaryWriter writer,
    IReadOnlyCollection<INetworkClient> targets, PacketData data) : IPacketBuildContext
{
    public byte MessageId { get; } = messageId;
    public BinaryWriter Writer { get; } = writer;
    public IReadOnlyCollection<INetworkClient> Targets { get; } = targets;
    public PacketData OriginalData { get; } = data;
}
