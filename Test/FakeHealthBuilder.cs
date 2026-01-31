using System.IO;
using PacketManager.Core.Abstractions;

namespace FakeHealthPlugin.Builders
{
    // Первый билдер (приоритет 0): устанавливает statLifeMax = 500
    public class MaxHealthBuilder : IPacketBuilder
    {
        public int Priority => 0;
        public byte MessageId => 16;

        public void Build(IPacketBuildContext context)
        {
            // Пишем полную структуру пакета:
            // [0] byte - PlayerID
            // [1-2] short - statLife (временное значение)
            // [3-4] short - statLifeMax (целевое 500)

            context.Writer.Write((byte)context.OriginalData.Number); // ID игрока
            context.Writer.Write((short)100); // Заглушка для statLife (будет перезаписана)
            context.Writer.Write((short)500); // statLifeMax = 500
        }
    }

    // Второй билдер (приоритет 100): перезаписывает statLife на 200
    public class CurrentHealthBuilder : IPacketBuilder
    {
        public int Priority => 100; // Выполняется ПОСЛЕ MaxHealthBuilder
        public byte MessageId => 16;

        public void Build(IPacketBuildContext context)
        {
            // Seek к позиции statLife (после 1 байта playerId)
            // Позиция сейчас в конце (5), нам нужно отступить назад на 4 байта (2 для max + 2 для life)
            // Или точнее: позиция 1 (второй байт пакета)
            context.Writer.BaseStream.Position = 3;

            // Перезаписываем statLife
            context.Writer.Write((short)200);

            // Возвращаем позицию в конец, чтобы не нарушить работу следующих билдеров (если появятся)
            context.Writer.BaseStream.Position = context.Writer.BaseStream.Length;
        }
    }
}