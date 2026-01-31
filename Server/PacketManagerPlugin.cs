using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using PacketManager.Server.Adapters;
using PacketManager.Server.Api;
using Terraria;
using Terraria.Localization;
using TerrariaApi.Server;

namespace PacketManager.Server;

/// <summary>
/// Основной плагин TShock для PacketManager.
/// Управляет инициализацией системы, перехватывает исходящие пакеты через IL-хуки
/// и диспетчеризирует их обработку билдерам.
/// </summary>
/// <remarks>
/// Создает новый экземпляр плагина.
/// </remarks>
/// <param name="game">Экземпляр Main игры.</param>
[ApiVersion(2, 1)]
public class PacketManagerPlugin(Main game) : TerrariaPlugin(game)
{
    /// <summary>
    /// Получает имя плагина.
    /// </summary>
    public override string Name => "PacketManagerAPI";

    /// <summary>
    /// Получает автора плагина.
    /// </summary>
    public override string Author => "Zoom L1";

    /// <summary>
    /// Получает версию плагина.
    /// </summary>
    public override Version Version => new(1, 1, 0, 0);

    private Core.Implementations.PacketManager? _manager;
    private TerrariaPacketGenerator? _generator;

    /// <summary>
    /// Инициализирует плагин, регистрируя сервисы, хуки и фасад.
    /// </summary>
    public override void Initialize()
    {
        var network = new TerrariaNetworkService();
        _generator = new TerrariaPacketGenerator();
        _manager = new Core.Implementations.PacketManager(_generator, network);

        Facade.Initialize(_manager);

        Infrastructure.IL.ExtNetMessage.OrigSendData += ILSendData;
        On.Terraria.NetMessage.OnPacketWrite += OnPacketWrite;
        ServerApi.Hooks.NetSendData.Register(this, OnSendData, int.MaxValue);
    }

    /// <summary>
    /// Освобождает ресурсы плагина при выгрузке.
    /// </summary>
    /// <param name="disposing">true при явном вызове Dispose; false при финализации.</param>
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

    /// <summary>
    /// Обработчик исходящих пакетов. Перехватывает SendData и делегирует обработку менеджеру.
    /// </summary>
    /// <param name="args">Аргументы события отправки данных.</param>
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

    /// <summary>
    /// Модифицирует IL-код метода orig_SendData, добавляя проверку на специальный ignoreClient.
    /// Если ignoreClient равен NameHash, метод возвращает управление сразу после вызова OnPacketWrite,
    /// предотвращая фактическую отправку пакета (используется для перехвата байтов).
    /// </summary>
    /// <param name="context">Контекст IL для модификации.</param>
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

    /// <summary>
    /// Обработчик, вызываемый после записи пакета в MemoryStream.
    /// Перехватывает сгенерированные байты, если пакет сгенерирован с флагом NameHash.
    /// </summary>
    /// <param name="orig">Оригинальный метод.</param>
    /// <param name="num">Вспомогательный номер.</param>
    /// <param name="ms">Поток памяти с данными пакета.</param>
    /// <param name="bw">Писатель пакета.</param>
    /// <param name="msgType">Тип сообщения.</param>
    /// <param name="remoteClient">Целевой клиент.</param>
    /// <param name="ignoreClient">Клиент для игнорирования (флаг).</param>
    /// <param name="text">Текстовые данные.</param>
    /// <param name="number">Числовой параметр 1.</param>
    /// <param name="number2">Числовой параметр 2.</param>
    /// <param name="number3">Числовой параметр 3.</param>
    /// <param name="number4">Числовой параметр 4.</param>
    /// <param name="number5">Числовой параметр 5.</param>
    /// <param name="number6">Числовой параметр 6.</param>
    /// <param name="number7">Числовой параметр 7.</param>
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