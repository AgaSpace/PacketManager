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

    public byte[] GenerateCustom(IReadOnlyList<IPacketBuilder> builders, byte messageId, PacketData data,
        IReadOnlyCollection<INetworkClient> targets)
    {
        if (builders.Count == 0)
            return GenerateOriginal(messageId, data);

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Резерв под длину
        ms.Position = 2;
        writer.Write(messageId);

        // ИЗМЕНЕНО: Вызываем ВСЕХ билдеров по порядку приоритета (от меньшего к большему)
        // Они могут использовать Writer.BaseStream.Position для seek/patch
        foreach (var builder in builders)
        {
            var context = new PacketBuildContext(messageId, writer, targets, data);
            builder.Build(context);
        }

        short len = (short)ms.Position;
        ms.Position = 0;
        writer.Write(len);

        return ms.ToArray();
    }
}