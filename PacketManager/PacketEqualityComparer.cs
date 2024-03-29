namespace PacketManager
{
    class PacketEqualityComparer : IEqualityComparer<PacketBuilder?>
    {
        public bool Equals(PacketBuilder? x, PacketBuilder? y) => ReferenceEquals(x, y); // x == y :nerd:
        public int GetHashCode(PacketBuilder? packet) => packet?.GetHashCode() ?? -1; 
    }
}
