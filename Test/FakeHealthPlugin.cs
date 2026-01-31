using System;
using FakeHealthPlugin.Builders;
using PacketManager.Server; // Для доступа к Facade и расширениям
using PacketManager.Server.Extensions;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

using FakeHealthPlugin.Builders;

namespace FakeHealthPlugin
{
    [ApiVersion(2, 1)]
    public class FakeHealthPlugin(Main game) : TerrariaPlugin(game)
    {
        public override string Name => "FakeHealth";
        public override string Author => "Author";
        public override Version Version => new(1, 0, 0, 0);

        private readonly MaxHealthBuilder _maxHealthBuilder = new();
        private readonly CurrentHealthBuilder _curHeatlBuilder = new();

        public override void Initialize()
        {
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit);
        }

        private void OnPostInit(EventArgs args)
        {
            // Добавляем билдер игрокам с 0 по 25
            for (int i = 0; i <= 25; i++)
            {
                if (i >= TShock.Players.Length) break;

                var player = TShock.Players[i];
                if (player?.Active == true)
                {
                    var manager = player.GetPacketManager();
                    manager.Add(_maxHealthBuilder);
                    manager.Add(_curHeatlBuilder);
                }
            }

            // Или можно добавлять при подключении:
            ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
        }

        private void OnJoin(JoinEventArgs args)
        {
            if (args.Who >= 0 && args.Who <= 25)
            {
                var player = TShock.Players[args.Who];
                var manager = player.GetPacketManager();
                manager.Add(_maxHealthBuilder);
                manager.Add(_curHeatlBuilder);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Удаляем билдер при выгрузке плагина
                for (int i = 0; i <= 25; i++)
                {
                    if (i >= TShock.Players.Length) break;

                    var player = TShock.Players[i];
                    if (player?.Active == true)
                    {
                        var manager = player.GetPacketManager();
                        manager.Remove(_maxHealthBuilder);
                        manager.Remove(_curHeatlBuilder);
                    }
                }

                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInit);
                ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
            }
            base.Dispose(disposing);
        }
    }
}