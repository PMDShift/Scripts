namespace Script
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;

    using Server;
    using Server.Maps;
    using Server.Players;
    using Server.RDungeons;
    using Server.Dungeons;
    using Server.Combat;
    using Server.Pokedex;
    using Server.Items;
    using Server.Moves;
    using Server.Npcs;
    using Server.Stories;
    using Server.Exp;
    using Server.Network;
    using Server.Sockets;
    using Server.Players.Parties;
    using Server.Logging;
    using Server.Missions;
    using Server.Events.Player.TriggerEvents;
    using Server.WonderMails;
    using Server.Tournaments;
    using Server.Events;
    using Server.Trading;
    using Server.SecretBases;

    using DataManager.Players;
    using Server.Database;
    using Script.Models;
    using Server.Events.World;
    using Server.Legendaries;

    public partial class Main
    {
        public static readonly int OutlawPointInterval = 4000;
        public static readonly int OutlawPointRewardTimeInterval = 1000;

        private static int lastOutlawPointsTick = 0;

        public static void OutlawDefeated(Client attacker, Client defender)
        {
            if (attacker.Player.OutlawRole == Enums.OutlawRole.Hunter)
            {
                var gainedPoints = defender.Player.PlayerData.PendingOutlawPoints;

                attacker.Player.PlayerData.LockedOutlawPoints += gainedPoints;

                Messenger.PlayerMsg(attacker, $"You defeated an outlaw! You earned {gainedPoints} points!", Text.BrightGreen);
            } 
            else
            {
                Messenger.PlayerMsg(attacker, $"You defeated an outlaw! However, as you were not a hunter, you did not earn any points.", Text.BrightGreen);
            }
        }

        public static void HunterDefeated(Client attacker, Client defender)
        {
            Messenger.PlayerMsg(attacker, "You defeated a hunter!", Text.BrightGreen);
        }

        public static void HandoutOutlawPoints(TickCount tickCount)
        {
            if (tickCount.Elapsed(lastOutlawPointsTick, OutlawPointRewardTimeInterval) || lastOutlawPointsTick == 0)
            {
                lastOutlawPointsTick = tickCount.Tick;

                foreach (var client in ClientManager.GetClients())
                {
                    if (client.Player.OutlawRole == Enums.OutlawRole.Outlaw && !client.Player.Dead) 
                    {
                        if (client.Player.Map.MapType == Enums.MapType.Standard && !client.Player.Map.IsZoneOrObjectSandboxed())
                        {
                            client.Player.PlayerData.PendingOutlawPoints += 1;
                        }
                    }
                }
            }
        }

        public static void HandleOutlawGameOver(Client client, ref PacketHitList hitList)
        {
            PacketHitList.MethodStart(ref hitList);

            if (client.Player.OutlawRole == Enums.OutlawRole.Outlaw)
            {
                var lostPoints = client.Player.PlayerData.PendingOutlawPoints % OutlawPointInterval;
                var gainedPoints = client.Player.PlayerData.PendingOutlawPoints - lostPoints;

                client.Player.PlayerData.LockedOutlawPoints += gainedPoints;

                Messenger.PlayerMsg(client, $"You have been defeated! You gained {gainedPoints} points from this round!", Text.BrightGreen);

                Messaging.SendGlobalMessage($"{client.Player.DisplayName} has been captured!", Settings.DiscordOutlawChannel, Text.BrightGreen);
            }

            client.Player.OutlawRole = Enums.OutlawRole.None;
            client.Player.PlayerData.PendingOutlawPoints = 0;

            PacketBuilder.AppendPlayerData(client, hitList);

            PacketHitList.MethodEnded(ref hitList);
        }

        public static bool IsOutlawPlayerInRange(Client client, int range, Enums.OutlawRole outlawRole)
        {
            return GetOutlawPlayersInRange(client, range, outlawRole).Any();
        }

        public static IEnumerable<Client> GetOutlawPlayersInRange(Client client, int range, Enums.OutlawRole outlawRole)
        {
            var targets = MoveProcessor.GetTargetsInRange(Enums.MoveRange.Room, range, client.Player.Map, client.Player.GetActiveRecruit(), client.Player.X, client.Player.Y, Enums.Direction.Up, true, true, false);

            return targets.Foes.OfType<Recruit>().Where(x => x.Owner.Player.OutlawRole == outlawRole).Select(x => x.Owner);
        }

        public static bool IsValidOutlawMap(IMap map)
        {
            return map.Moral == Enums.MapMoral.Safe;
        }
    }
}