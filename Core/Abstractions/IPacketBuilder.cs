namespace PacketManager.Core.Abstractions;

public interface IPacketBuilder
{
    int Priority { get; }
    byte MessageId { get; }
    void Build(IPacketBuildContext context);
}
