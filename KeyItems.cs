namespace Script
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using Server;
    using Server.Maps;
    using Server.Players;
    using Server.RDungeons;
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
    using Server.Database;
    using Server.Events;
    using DataManager.Players;


    public partial class Main
    {
        public static void ScriptedKeyItem(Client client, KeyItem keyItem, int slot) 
        {
            switch (keyItem.ItemID)
            {
                case 1839: // Costume box
                    {
                        Messenger.SendAvailableCostumes(client);
                    }
                    break;
                case 1842: // Drif-Flute
                    {
                        if (client.Player.Map.Indoors || client.Player.Map.MapType != Enums.MapType.Standard) {
                            var story = new Story();
                            var segment = StoryBuilder.BuildStory();
                            StoryBuilder.AppendSaySegment(segment, "It's probably a bad idea to use that here.", client.Player.GetActiveRecruit().Species, 0, 0);
                            segment.AppendToStory(story);
                            StoryManager.PlayStory(client, story);

                            return;
                        }

                        if (client.Player.OutlawRole == Enums.OutlawRole.Outlaw && IsOutlawPlayerInRange(client, 30, Enums.OutlawRole.Hunter))
                        {
                            Messenger.StoryMessage(client, "It's not safe to use that now! A hunter is nearby!");

                            return;
                        }

                        Messenger.SendAvailableFlyPoints(client);
                    }
                    break;
            }
        }
    }
}