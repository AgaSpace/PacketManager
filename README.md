# PacketManager

Простенький плагин упрощающий работу с заменой отправляемых пакетов. 
Идея реализации взята с репозитория https://github.com/AnzhelikaO/FakeProvider.

# В чём преимущество от уже существующего хука SendBytes?

Хук [SendBytes](https://github.com/Pryaxis/TSAPI/blob/8a3fffd71db401736ea80619122c70c449c10ff3/TerrariaServerAPI/TerrariaApi.Server/HookManager.cs#L531)  [TSAPI](https://github.com/Pryaxis/TSAPI) заменят уже сгенерированные данные для каждого игрока индивидуально. Да, по сути, просто замена байт, но я считаю, что это не очень удобно.

Для решения проблемы "удобности" я разработал данную библиотеку, чтобы она заменяла генерацию пакетов, а одинаково сгенерированные пакеты отправляла группе игроков, без какой либо повторной проверки.