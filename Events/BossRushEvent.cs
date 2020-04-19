using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Server;
using Server.Combat;
using Server.Events;
using Server.Maps;
using Server.Network;
using Server.Players;
using Server.Npcs;
using Server.Pokedex;

namespace Script.Events
{
    public class BossRushEvent : AbstractEvent<BossRushEvent.BossRushData, BossRushEvent.PlayerData>
    {
        private readonly int MaxRoomCount = 999;

        private readonly int CheckpointInterval = 5;

        private readonly int MinionCount = 2;

        private readonly int StartTileX = 12;
        private readonly int StartTileY = 15;
        private readonly int CompleteTileX = 12;
        private readonly int CompleteTileY = 0;

        public override string Identifier => "bossrush";
        public override string Name => "Boss Rush";
        public override string IntroductionMessage => "Bosses have appeared in an endless labyrinth! Defeat the most bosses to win!";
        public override string[] Rules => new string[] 
        {
            $"Your team will be fully healed every {CheckpointInterval} rooms that are completed.",
            "If you are defeated, you may use the /retryevent command to restart.",
            "WARNING: Restarting the event will cause all your current progress to be reset!"
        };

        public override TimeSpan? Duration => new TimeSpan(0, 20, 0);

        private readonly List<int> bossNpcs = new List<int>();
        private readonly List<int> minionNpcs = new List<int>();

        private readonly Dictionary<Enums.PokemonType, int> typeMappings = new Dictionary<Enums.PokemonType, int>() 
        {
            { Enums.PokemonType.Normal, 1301 },
            { Enums.PokemonType.Grass, 1302 },
            { Enums.PokemonType.Water, 1303 },
            { Enums.PokemonType.Fire, 1304 },
            { Enums.PokemonType.Bug, 1305 },
            { Enums.PokemonType.Flying, 1306 },
            { Enums.PokemonType.Fighting, 1307 },
            { Enums.PokemonType.Psychic, 1308 },
            { Enums.PokemonType.Dark, 1309 },
            { Enums.PokemonType.Electric, 1310 },
            { Enums.PokemonType.Ghost, 1311 },
            { Enums.PokemonType.Rock, 1312 }
        };

        public class BossRushData : AbstractEventData<PlayerData>
        {
            public List<Enums.PokemonType> SelectedTypes { get; set; }

            public BossRushData() {
                this.SelectedTypes = new List<Enums.PokemonType>();
            }
        }

        public class PlayerData
        {
            public DateTime? StartTime { get; set; }

            public int CurrentRoom { get; set; }
            public int RoomsCleared { get; set; }
        }

        public BossRushEvent()
        {
            for (var i = 3941; i < 4050; i++)
            {
                this.bossNpcs.Add(i);
                this.minionNpcs.Add(i);
            }
        }

        protected override void PrepareData()
        {
            var availableTypes = typeMappings.Keys.ToArray();

            for (var i = 0; i < MaxRoomCount; i++) {
                var selectedTypeIndex = Server.Math.Rand(0, availableTypes.Length);

                Data.SelectedTypes.Add(availableTypes[selectedTypeIndex]);
            }
        }

        public override void ConfigurePlayer(Client client)
        {
            base.ConfigurePlayer(client);

            if (Data.Started)
            {
                var playerData = Data.ExtendPlayer(client);

                if (!client.Player.IsInTempStatMode()) 
                {
                    client.Player.BeginTempStatMode(50, true);
                }
            }
        }

        public override void DeconfigurePlayer(Client client)
        {
            base.DeconfigurePlayer(client);

            var playerData = Data.ExtendPlayer(client);

            if (client.Player.IsInTempStatMode())
            {
                client.Player.EndTempStatMode();
            }
        }

        public override void OnboardNewPlayer(Client client) 
        {
            base.OnboardNewPlayer(client);

            var playerData = Data.ExtendPlayer(client);

            playerData.CurrentRoom = 0;
            playerData.RoomsCleared = 0;
            playerData.StartTime = DateTime.UtcNow;

            WarpToCurrentRoom(client);
        }

        public override void OnActivateMap(IMap map)
        {
            var minionXs = new int[] {
                11,
                13
            };

            var mapType = this.typeMappings.Where(x => x.Value == ((InstancedMap)map).MapBase).First().Key;
            var availableMinions = minionNpcs.Where(x => Pokedex.GetPokemon(NpcManager.Npcs[x].Species).Forms[0].Type1 == mapType || Pokedex.GetPokemon(NpcManager.Npcs[x].Species).Forms[0].Type2 == mapType).ToList();
            if (availableMinions.Count == 0)
            {
                availableMinions = minionNpcs;
            }

            var availableBosses = bossNpcs.Where(x => Pokedex.GetPokemon(NpcManager.Npcs[x].Species).Forms[0].Type1 == mapType || Pokedex.GetPokemon(NpcManager.Npcs[x].Species).Forms[0].Type2 == mapType).ToList();
            if (availableBosses.Count == 0)
            {
                availableBosses = bossNpcs;
            }

            for (var i = 0; i < MinionCount; i++)
            {
                var minionSlot = Server.Math.Rand(0, availableMinions.Count);
                var minion = availableMinions[minionSlot];

                var npc = new MapNpcPreset();
                npc.SpawnX = minionXs[i];
                npc.SpawnY = 6;
                npc.NpcNum = minion;
                npc.MaxLevel = 30;
                npc.MinLevel = 30;

                map.SpawnNpc(npc);
            }

            var bossSlot = Server.Math.Rand(0, availableBosses.Count);
            var boss = availableMinions[bossSlot];

            var bossNpc = new MapNpcPreset();
            bossNpc.SpawnX = 12;
            bossNpc.SpawnY = 6;
            bossNpc.NpcNum = boss;
            bossNpc.MinLevel = 35;
            bossNpc.MaxLevel = 35;

            map.SpawnNpc(bossNpc);
        }

        public override void OnNpcSpawn(IMap map, MapNpcPreset npc, MapNpc spawnedNpc, PacketHitList hitlist)
        {
            spawnedNpc.MaxHPBonus = 300;
            spawnedNpc.SpAtkBuff = 1;
            spawnedNpc.SpDefBuff = 1;
            spawnedNpc.HP = spawnedNpc.MaxHP;

            Main.RefreshCharacterTraits(spawnedNpc, map, hitlist);
        }

        public override void OnNpcDeath(PacketHitList hitlist, ICharacter attacker, MapNpc npc)
        {
            var map = MapManager.RetrieveActiveMap(attacker.MapID);

            if (!map.ActiveNpc.Enumerate().Where(x => x.Num > 0).Where(x => x != npc).Any())
            {
                SetCompletionTile(MapManager.RetrieveActiveMap(attacker.MapID));
            }
        }

        private void SetCompletionTile(IMap map)
        {
            Messenger.MapMsg(map.MapID, "The pathway has opened!", Text.BrightGreen);

            map.SetAttribute(CompleteTileX, CompleteTileY, Enums.TileType.Scripted, 85, 0, 0, "", "", "");
            Messenger.SendTile(CompleteTileX, CompleteTileY, map);
        }

        private void WarpToCurrentRoom(Client client) 
        {
            var playerData = Data.ExtendPlayer(client);

            var currentRoomMapId = typeMappings[Data.SelectedTypes[playerData.CurrentRoom]];

            Messenger.PlayerWarp(client, currentRoomMapId, StartTileX, StartTileY);
            Messenger.PlayerMsg(client, $"Welcome to room {playerData.CurrentRoom + 1}!", Text.BrightGreen);
        }

        public void CompleteRoom(Client client)
        {
            var playerData = Data.ExtendPlayer(client);

            if (playerData.CurrentRoom % CheckpointInterval == 0)
            {
                PacketHitList hitlist = null;

                PacketHitList.MethodStart(ref hitlist);
                Main.HealParty(hitlist, client);
                PacketHitList.MethodEnded(ref hitlist);
            }

            playerData.CurrentRoom++;
            playerData.RoomsCleared++;

            WarpToCurrentRoom(client);
        }

        protected override List<EventRanking> DetermineRankings()
        {
            var rankings = new List<EventRanking>();

            foreach (var client in EventManager.GetRegisteredClients())
            {
                var playerData = Data.ExtendPlayer(client);

                rankings.Add(new EventRanking(client, playerData.RoomsCleared));
            }

            return rankings;
        }

        public override string HandoutReward(EventRanking eventRanking, int position, bool isTesting)
        {
            base.HandoutReward(eventRanking, position, isTesting);

            switch (position)
            {
                case 1:
                    {
                        if (!isTesting)
                        {
                            eventRanking.Client.Player.GiveItem(133, 10);
                        }
                        return "10 Arcade Tokens";
                    }
                case 2:
                    {
                        if (!isTesting)
                        {
                            eventRanking.Client.Player.GiveItem(133, 5);
                        }
                        return "5 Arcade Tokens";
                    }
                case 3:
                    {
                        if (!isTesting)
                        {
                            eventRanking.Client.Player.GiveItem(133, 3);
                        }
                        return "3 Arcade Tokens";
                    }
            }

            return "";
        }
    }
}
