namespace PacketManager
{
    public abstract class PacketBuilder
    {
        public virtual int Layer { get; set; } = 0;
        public abstract PacketTypes Packet { get; }

        public abstract void PackBytes(PacketPackBytesArgs args);
    }
}
