using Terraria;
using Terraria.Localization;
using PacketManager.Core.Abstractions;
using PacketManager.Core.Data;

namespace PacketManager.Server.Adapters;

public class TerrariaPacketGenerator : IPacketGenerator, IDisposable
{
    private static readonly Lock Lock = new();
    private static byte[] _capturedBuffer = [];
    private static int _lastNum;
    private static readonly int NameHash = "PacketManagerAPI".GetHashCode();

    public byte[] GenerateOriginal(byte messageId, PacketData data)
    {
        lock (Lock)
        {
            NetMessage.orig_SendData(
                messageId,
                data.RemoteClient,
                NameHash,
                data.Text != null ? NetworkText.FromLiteral(data.Text) : null,
                data.Number,
                data.Number2,
                data.Number3,
                data.Number4,
                data.Number5,
                data.Number6,
                data.Number7
            );

            return [.. _capturedBuffer];
        }
    }

    public byte[] GenerateCustom(IPacketBuilder builder, byte messageId, PacketData data,
        IReadOnlyCollection<INetworkClient> targets)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        ms.Position = sizeof(short);
        writer.Write(messageId);

        var terrariaTargets = targets.Cast<TerrariaNetworkClient>().ToList();

        var context = new TerrariaBuildContext(messageId, writer, terrariaTargets, data, _lastNum);
        builder.Build(context);

        var len = (short)ms.Position;
        ms.Position = 0;
        writer.Write(len);

        return ms.ToArray();
    }

    public static void CaptureBuffer(byte[] buffer, int length)
    {
        _capturedBuffer = [.. buffer.Take(length)];
    }

    public static void SetLastNum(int num) => _lastNum = num;
    public static int GetNameHash() => NameHash;

    public void Dispose() { }
}