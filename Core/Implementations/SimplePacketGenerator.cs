using PacketManager.Core.Abstractions;
using PacketManager.Core.Data;

namespace PacketManager.Core.Implementations;

public class SimplePacketGenerator : IPacketGenerator
{
    public virtual byte[] GenerateOriginal(byte messageId, PacketData data)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((short)0);
        writer.Write(messageId);
        return ms.ToArray();
    }

    public byte[] GenerateCustom(IPacketBuilder builder, byte messageId, PacketData data,
        IReadOnlyCollection<INetworkClient> targets)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        ms.Position = 2;
        writer.Write(messageId);

        var context = new PacketBuildContext(messageId, writer, targets, data);
        builder.Build(context);

        short len = (short)ms.Position;
        ms.Position = 0;
        writer.Write(len);

        return ms.ToArray();
    }
}