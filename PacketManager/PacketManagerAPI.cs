#region Using

using Terraria;
using Terraria.Net.Sockets;
using Terraria.Localization;

using TerrariaApi.Server;

#endregion

namespace PacketManager
{
    [ApiVersion(2, 1)]
    public class PacketManagerAPI : TerrariaPlugin
    {
        #region Data

        public override string Author => "Zoom L1";
        public override string Name => "PacketManager";
        public override Version Version => new Version(2, 0, 1, 2);
        public PacketManagerAPI(Main game) : base(game) { }
        static PacketManagerAPI()
        {
            Players = Enumerable.Range(0, Netplay.Clients.Length)
                .Select(i => new PlayerPacketManager((byte)i)).ToArray();
            MaxPacketCount = (int)Enum.GetValues(typeof(PacketTypes)).Cast<PacketTypes>().Max() + 1;

            _nameHash = "PacketManager".GetHashCode();
            _disposed = false;

            _lock = new object();
            _exception = new PacketCustomGenException();
        }

        public static PlayerPacketManager[] Players;
        public static readonly int MaxPacketCount;

        private static int _nameHash;
        private static bool _disposed;

        private static IEnumerable<byte>? _lastBuffer;
        private static object _lock;
        private static PacketCustomGenException _exception;

        #endregion
        #region Initialize

        public override void Initialize()
        {
            On.Terraria.NetMessage.OnPacketWrite += OnPacketWrite;
            ServerApi.Hooks.NetSendData.Register(this, OnSendData);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                On.Terraria.NetMessage.OnPacketWrite -= OnPacketWrite;
                ServerApi.Hooks.NetSendData.Deregister(this, OnSendData);
                _lastBuffer = Array.Empty<byte>();
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        #endregion

        #region OnSendData

        void OnSendData(SendDataEventArgs args)
        {
            if (args.Handled)
                return;
            args.Handled = Distributor(args.MsgId, args.ignoreClient, args.remoteClient, args.text,
                args.number, args.number2, args.number3, args.number4, args.number5, args.number6,
                args.number7);
        }

        #endregion 

        #region Distributor

        bool Distributor(PacketTypes packet, int ignoreClient, int remoteClient, NetworkText? text, int number,
            float number2, float number3, float number4, int number5, int number6, int number7)
        {
            IEnumerable<IGrouping<PacketBuilder?, RemoteClient>> groupedClients = 
                GroupBy(packet, GetClients(ignoreClient, remoteClient));

            if (groupedClients.Count() <= 1)
                return false;

            foreach (IGrouping<PacketBuilder?, RemoteClient> group in groupedClients)
            {
                PacketBuilder? builder = group.Key;
                IEnumerable<RemoteClient> clients = group;

                byte[] buffer;
                // Если у игроков нет PacketBuilder'а, то мы пишем им "оригинальную" информацию
                if (builder == null)
                {
                    buffer = GenPacket((int)packet, remoteClient, text, number, number2,
                        number3, number4, number5, number6, number7).ToArray();
                }
                // Если же есть PacketBuilder, то мы генерируем им новую информацию
                else
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                        {
                            stream.Position += 2;
                            writer.Write((byte)packet);

                            PacketPackBytesArgs args = new PacketPackBytesArgs(stream, writer, clients, remoteClient, text,
                                number, number2, number4, number4, number5, number6, number7);
                            builder.PackBytes(args);

                            long pos = stream.Position;
                            stream.Position = 0;
                            writer.Write((short)pos);
                            stream.Position = pos;
                        }
                        buffer = stream.ToArray();
                    }
                }

                if (buffer?.Length > 0)
                    SendTo(clients, buffer);
            }

            return true;
        }

        #endregion
        #region GroupBy

        public static IEnumerable<IGrouping<PacketBuilder?, RemoteClient>> GroupBy(PacketTypes packet,
            IEnumerable<RemoteClient> clients)
        {
            return clients.GroupBy(i => Players[i.Id].builders[(int)packet].Max(), new PacketEqualityComparer());
        }

        #endregion
        #region GetClients

        public static IEnumerable<RemoteClient> GetClients(int ignoreClient, int remoteClient)
        {
            IEnumerable<RemoteClient> clients = remoteClient == -1 ? Netplay.Clients :
                new RemoteClient[] { Netplay.Clients[remoteClient] };
            return clients.Where(i => i != null && i.Id != ignoreClient);
        }

        #endregion
        #region SendTo

        // https://github.com/AnzhelikaO/FakeProvider/blob/d762d9e55f56838bc3367e4eaedfd17e0390c509/FakeProvider/FakeProviderPlugin.cs#L223
        void SendTo(IEnumerable<RemoteClient> clients, byte[] data)
        {
            foreach (RemoteClient client in clients)
                try
                {
                    client?.Socket?.AsyncSend(data, 0, data.Length,
                        new SocketSendCallback(client.ServerWriteCallBack), null);
                }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
        }

        #endregion

        #region GenPacket

        public static IEnumerable<byte> GenPacket(int msgType, int remoteClient, NetworkText? text = null, int number = 0,
            float number2 = 0f, float number3 = 0f, float number4 = 0f, int number5 = 0,
            int number6 = 0, int number7 = 0)
        {
            lock (_lock)
            {
                try
                {
                    NetMessage.orig_SendData(msgType, remoteClient, _nameHash, text, number, number2,
                        number3, number4, number5, number6, number7);
                }
                catch (PacketCustomGenException) { }
                catch { throw; }

                return _lastBuffer!;
            }
        }

        public static IEnumerable<byte> GenPacket(PacketTypes msgType, int remoteClient, NetworkText? text = null, int number = 0,
            float number2 = 0f, float number3 = 0f, float number4 = 0f, int number5 = 0,
            int number6 = 0, int number7 = 0)
        {
            return GenPacket((int)msgType, remoteClient, text, number, number2,
                        number3, number4, number5, number6, number7);
        }

        public static IEnumerable<byte> GenPacket(PacketTypes msgType, int remoteClient, string? text = null, int number = 0,
            float number2 = 0f, float number3 = 0f, float number4 = 0f, int number5 = 0,
            int number6 = 0, int number7 = 0)
        {
            return GenPacket((int)msgType, remoteClient, text == null ? null : NetworkText.FromLiteral(text), number, number2,
                        number3, number4, number5, number6, number7);
        }

        public static IEnumerable<byte> GenPacket(int msgType, int remoteClient, string? text = null, int number = 0,
            float number2 = 0f, float number3 = 0f, float number4 = 0f, int number5 = 0,
            int number6 = 0, int number7 = 0)
        {
            return GenPacket(msgType, remoteClient, text == null ? null : NetworkText.FromLiteral(text), number, number2,
                        number3, number4, number5, number6, number7);
        }


        #endregion
        #region OnPacketWrite

        void OnPacketWrite(On.Terraria.NetMessage.orig_OnPacketWrite orig,
            int num, MemoryStream ms, OTAPI.PacketWriter bw, int msgType,
            int remoteClient, int ignoreClient, NetworkText text,
            int number, float number2, float number3, float number4, int number5, int number6, int number7)
        {
            if (!_disposed && ignoreClient == _nameHash)
            {
                byte[] buffer = ms.GetBuffer();
                _lastBuffer = buffer.Take(buffer[0] + buffer[1] * 256);
                throw _exception;
            }

            orig.Invoke(num, ms, bw, msgType, remoteClient, ignoreClient, text, number, number2,
                number3, number4, number5, number6, number7);
        }

        #endregion
    }
}
