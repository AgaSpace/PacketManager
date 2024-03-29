namespace PacketManager
{
    public class PlayerPacketManager
    {
        public readonly int Index = -1;
        public readonly List<PacketBuilder>[] builders;

        public PlayerPacketManager(byte whoAmI)
        {
            Index = whoAmI;
            builders = Enumerable.Range(0, PacketManagerAPI.MaxPacketCount)
                .Select(i => new List<PacketBuilder>()).ToArray();
        }

        public bool Add(PacketBuilder builder)
        {
            ref List<PacketBuilder> builders = ref this.builders[(int)builder.Packet];
            if (builders.Contains(builder))
                return false;
            builders.Add(builder);
            Sort(builder.Packet);
            return true;
        }
        public bool Remove(PacketBuilder builder)
        {
            return builders[(int)builder.Packet].Remove(builder);
        }
        public void Sort(PacketTypes packet)
        {
            builders[(int)packet].Sort((i, j) => i.Layer.CompareTo(j.Layer));
        }
    }
}