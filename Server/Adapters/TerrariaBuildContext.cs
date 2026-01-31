#region Using

using PacketManager.Core.Abstractions;
using PacketManager.Core.Data;

#endregion

namespace PacketManager.Server.Adapters;

internal class TerrariaBuildContext(byte messageId, BinaryWriter writer,
    List<TerrariaNetworkClient> targets, PacketData data, int num) : IPacketBuildContext
{
    public byte MessageId { get; } = messageId;
    public BinaryWriter Writer { get; } = writer;
    public IReadOnlyCollection<INetworkClient> Targets { get; } = targets;
    public PacketData OriginalData { get; } = data;
    public int Num { get; } = num;
}
