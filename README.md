# PacketManager

[![.NET](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/)
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
        
        // PacketTypes.PlayerHp = Terraria.ID.MessageID.PlayerLifeMana = 16
        public int MessageId => Terraria.ID.MessageID.PlayerLifeMana;

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
	// Или через ID напрямую
	var manager = new PlayerPacketManager(0); // 0 - ID игрока
	
    // Добавляем билдер
	var builder = new FakeHealthBuilder();
    manager.Add(builder);
    
    // Проверяем наличие билдеров на 16 пакете
    if (manager.Has(Terraria.ID.MessageID.PlayerLifeMana)) // 16 = PlayerHp
    {
        Console.WriteLine("На 16 пакете есть какие-то билдеры!");
    }
	
	// Проверяем наличие нашего билдера
	if (manager.Contains(builder))
	{
		Console.WriteLine($"У игрока есть билдер {nameof(FakeHealthBuilder)}");
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
    int Priority { get => 0; } // Default = 0, чем выше - тем важнее
    int MessageId { get; }     // ID пакета (из PacketTypes)
    void Build(IPacketBuildContext context);
}
```

#### `IPacketBuildContext`
Контекст сборки пакета:

```csharp
public interface IPacketBuildContext
{
    int MessageId { get; }                     // Тип пакета
    BinaryWriter Writer { get; }               // Писатель потока
    PacketData OriginalData { get; }           // Оригинальные аргументы SendData
    IReadOnlyCollection<INetworkClient> Targets { get; } // Получатели
}
```

#### `PacketData`
Структура с параметрами исходящего пакета (аргументы `SendData`):

```csharp
public readonly record struct PacketData(
    int RemoteClient,  // Целевой клиент (-1 = всем)
    string? Text,      // Текст (чат, имена NPC)
    int Number,        // Основной параметр (обычно whoAmI)
    float Number2,     // X / скорость / текущее HP
    float Number3,     // Y / мана / rotation  
    float Number4,     // Z / max HP / scale
    int Number5,       // Доп. int
    int Number6,       // Доп. int
    int Number7        // Доп. int
);
```

**Использование в билдере:**
```csharp
public void Build(IPacketBuildContext context)
{
    int playerId = context.OriginalData.Number;  			// whoAmI
	short itemIndex = (short)context.OriginalData.Number2;	// item slot in inventory
    byte prefix = (byte)context.OriginalData.Number3;		// item prefix
    // ... модификация данных пакета
}
```

### Расширения для TSPlayer

```csharp
// Получение менеджера пакетов
var manager = player.GetPacketManager();

// Свойства:
int manager.Index; // ID игрока (whoAmI), удобно для отладки

// Методы:
bool Add(IPacketBuilder builder);           // Добавить билдер
bool Remove(IPacketBuilder builder);        // Удалить билдер  
bool Contains(IPacketBuilder builder);		// Проверить наличие билдера для типа
bool Has(int messageId);                    // Проверить наличие билдеров для типа
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

### Инкапсуляция данных в билдере (Паттерн WorldScene)
Храните nullable-поля в билдере, чтобы перезаписывать только нужные значения, а остальные брать из реального мира (`Main.*`) динамически при генерации пакета.

```csharp
public class TimeSetBuilder : IPacketBuilder
{
    public int MessageId => 18; // TimeSet

    // Nullable поля: null = использовать Main.* при генерации
    public bool? DayTime;
    public int? Time;
    public float? SunModY;
    public float? MoonModY;

    public void Build(IPacketBuildContext context)
    {
        // Fallback на Main.* если значение не задано (??)
        context.Writer.Write((byte)((DayTime ?? Main.dayTime) ? 1 : 0));
        context.Writer.Write(Time ?? (int)Main.time);
        context.Writer.Write(SunModY ?? Main.sunModY);
        context.Writer.Write(MoonModY ?? Main.moonModY);
    }
}
```

**Применение:**

```csharp
// Только ускоряем время для игрока, остальное (солнце/луна) как в Main
var builder = new TimeSetBuilder { Time = 12000 };
player.GetPacketManager().Add(builder);

// Или полностью кастомная сцена
var fullScene = new TimeSetBuilder 
{ 
    DayTime = true, 
    Time = 27000, 
    SunModY = -50, 
    MoonModY = 100 
};
player.GetPacketManager().Add(fullScene);

fullScene.DayTime = false; // Изменяем значение в билдере ПОСЛЕ добавления
// Важно: так как билдер хранится по ссылке, изменение полей сразу влияет 
// на все будущие пакеты для этого игрока (перегенерация при каждом SendData)
player.SendData(fullScene.MessageId);
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

2. **Производительность**: Группировка клиентов минимизирует вызовы `GenerateOriginal`.

## 📝 Лицензия

MIT License - свободное использование в коммерческих и некоммерческих проектах.

---

**Разработано с ❤️ для сообщества Terraria**