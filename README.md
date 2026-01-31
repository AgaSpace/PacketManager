# PacketManager

[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![TShock](https://img.shields.io/badge/TShock-5.x-blue)](https://github.com/Pryaxis/TShock)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

**PacketManager** — это высокопроизводительная библиотека для перехвата и модификации исходящих сетевых пакетов в серверах Terraria (TShock). Позволяет плагинам изменять содержимое пакетов "на лету" без изменения исходного кода игры.

## 🏗️ Архитектура

Проект разделён на два слоя:

```
┌─────────────────────────────────────┐
│         PacketManager.Core          │
│    (Чистая логика, netstandard2.1)  │
│                                     │
│  • Абстракции сетевых сервисов      │
│  • Реестр билдеров с приоритетами   │
│  • Группировка клиентов             │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│      PacketManager.Server           │
│   (Адаптер для Terraria)            │
│                                     │
│  • IL-хуки (MonoMod)                │
│  • Перехват orig_SendData           │
│  • Интеграция с RemoteClient        │
└─────────────────────────────────────┘
```

## 🚀 Быстрый старт

### 1. Создание билдера

Создайте класс, реализующий `IPacketBuilder`:

```csharp
using PacketManager.Core.Abstractions;
using System.IO;

namespace MyPlugin.Builders
{
    /// <summary>
    /// Подменяет здоровье игрока на фейковые значения
    /// </summary>
    public class FakeHealthBuilder : IPacketBuilder
    {
        // Приоритет выше = выполнится первым (если несколько билдеров на один пакет)
        public int Priority => 100;
        
        // PacketTypes.PlayerHP = 16
        public byte MessageId => 16;

        public void Build(IPacketBuildContext context)
        {
            // Структура пакета 16:
            // byte - PlayerID
            // short - Current HP (statLife)
            // short - Max HP (statLifeMax)
            
            byte playerId = (byte)context.OriginalData.Number;
            short fakeCurrent = 500;
            short fakeMax = 200;
            
            context.Writer.Write(playerId);
            context.Writer.Write(fakeCurrent);
            context.Writer.Write(fakeMax);
        }
    }
}
```

### 2. Регистрация билдера

```csharp
using PacketManager.Server.Extensions;

public override void Initialize()
{
    // Получаем менеджер для конкретного игрока
    var player = TShock.Players[0];
    var manager = player.GetPacketManager();
    
    // Добавляем билдер
    manager.Add(new FakeHealthBuilder());
    
    // Проверяем наличие
    if (manager.Has(16)) // 16 = PlayerHP
    {
        Console.WriteLine("Билдер активен!");
    }
}
```

## 📚 API Reference

### Основные интерфейсы

#### `IPacketBuilder`
Контракт для модификации пакетов:

```csharp
public interface IPacketBuilder
{
    int Priority { get; }      // Чем больше, тем выше приоритет
    byte MessageId { get; }    // ID пакета (из PacketTypes)
    void Build(IPacketBuildContext context);
}
```

#### `IPacketBuildContext`
Контекст сборки пакета:

```csharp
public interface IPacketBuildContext
{
    byte MessageId { get; }                    // Тип пакета
    BinaryWriter Writer { get; }               // Писатель потока
    PacketData OriginalData { get; }           // Оригинальные аргументы SendData
    IReadOnlyCollection<INetworkClient> Targets { get; } // Получатели
}
```

### Расширения для TSPlayer

```csharp
// Получение менеджера пакетов
var manager = player.GetPacketManager();

// Методы:
bool Add(IPacketBuilder builder);           // Добавить билдер
bool Remove(IPacketBuilder builder);        // Удалить билдер  
bool Has(byte messageId);                   // Проверить наличие билдера для типа
```

### Глобальный доступ через Facade

```csharp
using PacketManager.Server.Api;

// Добавление удалённо (без ссылки на TSPlayer)
Facade.AddBuilder(playerId, new MyBuilder());

// Проверка
bool hasBuilder = Facade.HasBuilder(playerId, messageId: 16);

// Доступ к реестру напрямую
IPacketBuilderRegistry registry = Facade.Registry;
```

## ⚙️ Как это работает

### 1. Перехват пакетов (IL Injection)
PacketManager использует **MonoMod.RuntimeDetour** для модификации IL-кода метода `Terraria.NetMessage.orig_SendData`:

```csharp
// Псевдокод логики:
if (ignoreClient == NameHash) 
{
    // Перехватываем байты в статическую переменную
    CaptureBuffer(ms.ToArray());
    return; // Не отправляем реально
}
```

### 2. Группировка клиентов
Для оптимизации производительности клиенты группируются по билдерам:

```
Клиенты: [A, B, C, D]
Билдеры: A→BuilderX, B→BuilderX, C→null, D→BuilderY

Результат:
- Группа 1 (BuilderX): [A, B] → Генерируем 1 пакет, шлём обоим
- Группа 2 (null): [C] → Оригинальный пакет
- Группа 3 (BuilderY): [D] → Кастомный пакет для D
```

### 3. Выбор билдера
Если у игрока несколько билдеров на один `MessageId`, выполняется **только один** — с максимальным `Priority`.

## 🛠️ Расширенные сценарии

### Seek/Modify (частичная модификация)
Изменение только части пакета:

```csharp
public void Build(IPacketBuildContext context)
{
    // Сначала пишем оригинальные данные
    context.Writer.Write((byte)context.OriginalData.Number);
    context.Writer.Write((short)100); // HP placeholder
    
    // Запоминаем позицию HP
    long hpPosition = context.Writer.BaseStream.Position - 2;
    
    // Пишем остальное
    context.Writer.Write((short)500); // MaxHP
    
    // Возвращаемся и патчим HP
    context.Writer.BaseStream.Position = hpPosition;
    context.Writer.Write((short)999); // Новое HP
}
```

### Условная логика по клиентам
Разные данные для разных получателей:

```csharp
public void Build(IPacketBuildContext context)
{
    // Targets содержит список получателей этого пакета
    var playerIds = context.Targets.Select(t => t.Id).ToList();
    
    if (playerIds.Contains(0)) // Если среди получателей есть админ (ID 0)
    {
        // Показываем реальные данные админу
        context.Writer.Write(realData);
    }
    else
    {
        // Фейковые данные для остальных
        context.Writer.Write(fakeData);
    }
}
```

## 📁 Структура проекта

```
PacketManager/
├── Core/                           # Чистая логика (без Terraria)
│   ├── Abstractions/               # Интерфейсы (IPacketBuilder и т.д.)
│   ├── Data/                       # DTO (PacketData, Result)
│   └── Implementations/            # Реализации (PacketBuilderRegistry)
├── Server/                         # Адаптер TShock
│   ├── Adapters/                   # TerrariaNetworkClient, TerrariaPacketGenerator
│   ├── Api/                        # Facade, PlayerPacketManager
│   ├── Extensions/                 # TSPlayerExtensions
│   ├── Infrastructure/             # IL-хуки
│   │   ├── IL/                     # ExtNetMessage (IL модификации)
│   │   └── On/                     # OnExtNetMessage (On хуки)
│   └── PacketManagerPlugin.cs      # Entry point
└── README.md
```

## ⚠️ Важные замечания

1. **Потокобезопасность**: Все методы `PacketManager` потокобезопасны. `Build()` вызывается в контексте игрового потока (Main thread).

2. **Производительность**: Группировка клиентов минимизирует вызовы `GenerateOriginal`. Не создавайте билдеры с высокой частотой (не в цикле каждого тика).

3. **Ограничения**: Нельзя модифицировать пакеты, которые не проходят через `NetMessage.SendData` (например, некоторые внутренние пакеты vanilla).

## 📝 Лицензия

MIT License - свободное использование в коммерческих и некоммерческих проектах.

---

**Разработано с ❤️ для сообщества Terraria**