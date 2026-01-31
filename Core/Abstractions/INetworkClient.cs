namespace PacketManager.Core.Abstractions;

public interface INetworkClient
{
    int Id { get; }
    bool IsConnected { get; }
    void Send(ReadOnlyMemory<byte> data);
}
