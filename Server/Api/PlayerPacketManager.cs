#region Using

using PacketManager.Core.Abstractions;

#endregion

namespace PacketManager.Server.Api;

public class PlayerPacketManager(int id)
{
    private readonly int _id = id;

    public bool Add(IPacketBuilder builder) => Facade.AddBuilder(_id, builder);
    public bool Remove(IPacketBuilder builder) => Facade.RemoveBuilder(_id, builder);
    public bool Has(byte messageId) => Facade.HasBuilder(_id, messageId);
}
