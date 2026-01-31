#region Using

using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

using PacketManager.Server.Adapters;
using PacketManager.Server.Api;

using Terraria;
using Terraria.Localization;
using TerrariaApi.Server;

#endregion

namespace PacketManager.Server
{
    [ApiVersion(2, 1)]
    public class PacketManagerPlugin(Main game) : TerrariaPlugin(game)
    {
        public override string Name => "PacketManagerAPI";
        public override string Author => "Zoom L1";
        public override Version Version => new(1, 1, 0, 0);

        private Core.Implementations.PacketManager? _manager;
        private TerrariaPacketGenerator? _generator;

        public override void Initialize()
        {
            var network = new TerrariaNetworkService();
            _generator = new TerrariaPacketGenerator();
            _manager = new Core.Implementations.PacketManager(Netplay.Clients.Length, _generator, network);

            Facade.Initialize(_manager);

            Infrastructure.IL.ExtNetMessage.OrigSendData += ILSendData;
            On.Terraria.NetMessage.OnPacketWrite += OnPacketWrite;
            ServerApi.Hooks.NetSendData.Register(this, OnSendData, int.MaxValue);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Infrastructure.IL.ExtNetMessage.OrigSendData -= ILSendData;
                On.Terraria.NetMessage.OnPacketWrite -= OnPacketWrite;
                ServerApi.Hooks.NetSendData.Deregister(this, OnSendData);
                _manager?.Dispose();
                _generator?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void OnSendData(SendDataEventArgs args)
        {
            if (args.Handled) return;

            var data = new Core.Data.PacketData(
                args.remoteClient,
                args.text?.ToString(),
                args.number,
                args.number2,
                args.number3,
                args.number4,
                args.number5,
                args.number6,
                args.number7
            );

            args.Handled = _manager!.ProcessOutgoingPacket((byte)args.MsgId, data, args.ignoreClient, args.remoteClient);
        }

        private void ILSendData(ILContext context)
        {
            ILCursor cursor = new(context);
            cursor.GotoNext(i => i.OpCode.Code == Code.Call
                && i.Operand is MethodReference method
                && method.Name == nameof(NetMessage.OnPacketWrite));

            cursor.Index++;
            Instruction after = cursor.Next;
            
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.Emit(OpCodes.Ldc_I4, TerrariaPacketGenerator.GetNameHash());
            cursor.Emit(OpCodes.Ceq);
            cursor.Emit(OpCodes.Brfalse, after);
            cursor.Emit(OpCodes.Ret);
        }

        private void OnPacketWrite(On.Terraria.NetMessage.orig_OnPacketWrite orig, int num,
            MemoryStream ms, OTAPI.PacketWriter bw, int msgType, int remoteClient, int ignoreClient,
            NetworkText text, int number, float number2, float number3, float number4,
            int number5, int number6, int number7)
        {
            if (ignoreClient == TerrariaPacketGenerator.GetNameHash())
            {
                var buffer = ms.GetBuffer();
                var len = BitConverter.ToInt16(buffer, 0);
                TerrariaPacketGenerator.CaptureBuffer(buffer, len);
            }
            TerrariaPacketGenerator.SetLastNum(num);
            orig(num, ms, bw, msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
        }
    }
}
