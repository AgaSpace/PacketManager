namespace PacketManager
{
    /// <summary>
    /// Исключение вызывающееся в случае персональной генерации пакета. 
    /// Данное исключение не показывает какой либо ошибки, оно нужно лишь чтобы показать,
    /// что пакет сгенерирован персонально.
    /// </summary>
    public class PacketCustomGenException : Exception
    {
        public PacketCustomGenException() : this(null) { }
        public PacketCustomGenException(Exception? innerException) : base("Amazing packet was brutally generated.", innerException)
        {

        }
    }
}
