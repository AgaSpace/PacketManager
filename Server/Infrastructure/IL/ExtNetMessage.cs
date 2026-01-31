#region Using

using System.Reflection;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;

using PacketManager.Server.Infrastructure.On;

#endregion

namespace PacketManager.Server.Infrastructure.IL;

public static class ExtNetMessage
{
    public static event ILContext.Manipulator OrigSendData
    {
        add
        {
            HookEndpointManager.Modify<OnExtNetMessage.HookOrigSendData>(
                MethodBase.GetMethodFromHandle(OnExtNetMessage.OrigSendDataHandler),
                value);
        }
        remove
        {
            HookEndpointManager.Unmodify<OnExtNetMessage.HookOrigSendData>(
                MethodBase.GetMethodFromHandle(OnExtNetMessage.OrigSendDataHandler),
                value);
        }
    }
}
