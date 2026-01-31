#region Using

using MonoMod.RuntimeDetour.HookGen;

using System.ComponentModel;
using System.Reflection;

using Terraria.Localization;

#endregion

namespace PacketManager.Server.Infrastructure.On
{
    public static class OnExtNetMessage
    {
        public static RuntimeMethodHandle OrigSendDataHandler => typeof(Terraria.NetMessage)
#if DEBUG
            .GetMethod("mfwh_orig_SendData")!.MethodHandle;
#else
            .GetMethod("orig_SendData")!.MethodHandle;
#endif

        [EditorBrowsable(EditorBrowsableState.Never)]
        public delegate void OrigOrigSendData(int msgType, int remoteClient, int ignoreClient, NetworkText text,
            int number, float number2, float number3, float number4, int number5, int number6, int number7);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public delegate void HookOrigSendData(OrigOrigSendData orig, int msgType, int remoteClient, int ignoreClient,
            NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7);

        public static event HookOrigSendData OrigSendData
        {
            add => HookEndpointManager.Add<HookOrigSendData>(
                MethodBase.GetMethodFromHandle(OrigSendDataHandler), value);
            remove => HookEndpointManager.Remove<HookOrigSendData>(
                MethodBase.GetMethodFromHandle(OrigSendDataHandler), value);
        }
    }
}
