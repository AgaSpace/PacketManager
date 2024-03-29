#region Using

using Terraria;
using Terraria.Localization;

#endregion

namespace PacketManager
{
    public class PacketPackBytesArgs
    {
        #region Data

        /// <summary>
        /// <see cref="Stream"/> для генерации пакета.
        /// </summary>
        public MemoryStream stream;
        /// <summary>
        /// <see cref="BinaryWriter"/> для генерации пакета.
        /// </summary>
        public BinaryWriter writer;
        // Список получателей данного PacketBuilder'а.
        public IEnumerable<RemoteClient> recievers;

        // Аргумент из SendDataArgs.
        public int remoteClient;
        // Аргумент из SendDataArgs.
        public NetworkText? text;

        // Аргумент из SendDataArgs.
        public int number;
        // Аргумент из SendDataArgs.
        public float number2;
        // Аргумент из SendDataArgs.
        public float number3;
        // Аргумент из SendDataArgs.
        public float number4;
        // Аргумент из SendDataArgs.
        public int number5;
        // Аргумент из SendDataArgs.
        public int number6;
        // Аргумент из SendDataArgs.
        public int number7;

        #endregion
        #region Constructor

        public PacketPackBytesArgs(MemoryStream stream, BinaryWriter writer, IEnumerable<RemoteClient> recievers,
            int remoteClient, NetworkText? text, int number, float number2, float number3,
            float number4, int number5, int number6, int number7)
        {
            this.stream = stream;
            this.writer = writer;
            this.recievers = recievers;

            this.remoteClient = remoteClient;
            this.text = text;

            this.number = number;
            this.number2 = number2;
            this.number3 = number3;
            this.number4 = number4;
            this.number5 = number5;
            this.number6 = number6;
            this.number7 = number7;
        }

        #endregion
    }
}
