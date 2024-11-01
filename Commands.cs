﻿// This file is part of Mystery Dungeon eXtended.

// Copyright (C) 2015 Pikablu, MDX Contributors, PMU Staff

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Affero General Public License for more details.

// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.

namespace Script
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;

    using Server;
    using Server.Maps;
    using Server.Players;
    using Server.Dungeons;
    using Server.RDungeons;
    using Server.Combat;
    using Server.Pokedex;
    using Server.Evolutions;
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
    using Server.SecretBases;
    using Server.Discord;
    using System.Threading.Tasks;
    using Script.Models;
    using Server.Events.World;
    using Server.Quests;

    public partial class Main
    {
        //public static void ProcessServerCommand(Server.Forms.MainUI mainUI, Command fullCommand, string fullArgs)
        //{
        //    try
        //    {
        //        //mainUI.AddCommandLine("Command not found: " + fullCommand.CommandArgs[0]);
        //    }
        //    catch (Exception ex)
        //    {
        //        Messenger.AdminMsg("Error: ProcessServerCommand", Text.Black);
        //    }

        //}

        public static bool IsValidServerCommand(string header, string command)
        {
            try
            {
                switch (header.ToLower())
                {
                    case "/gmmode":
                        {
                            return true;
                        }
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                Messenger.AdminMsg("Error: IsValidServerCommand", Text.Black);
                return false;
            }
        }

        //public static void DisplayServerCommandHelp(Server.Forms.MainUI mainUI)
        //{
        //    try
        //    {

        //    }
        //    catch (Exception ex)
        //    {
        //        Messenger.AdminMsg("Error: DisplayServerCommandHelp", Text.Black);
        //    }
        //}

        public static bool InQuiz;
        public static bool QuestionReady;
        public static string QuizAnswer { get; set; }
        public static bool CanAnswer = false;

        public static List<Tuple<string, int, int>> positions = new List<Tuple<string, int, int>>();
        public static List<string> votersRestart = new List<string>();

        public static bool IsValidDeathCommand(Client client, Command command)
        {
            switch (command[0]) 
            {
                case "/g":
                    return true;
                default:
                    return false;
            }
        }

        public static void Commands(Client client, Command command)
        {
            try
            {
                string joinedArgs = JoinArgs(command.CommandArgs);
                PacketHitList hitlist = null;
                PacketHitList.MethodStart(ref hitlist);


                switch (command[0])
                {
                    case "/setsprite":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                client.Player.GetActiveRecruit().Sprite = command[1].ToInt();
                                client.Player.GetActiveRecruit().Shiny = (Enums.Coloration)command[2].ToInt();
                                Messenger.SendPlayerData(client);
                            }
                        }
                        break;
                    case "/debug":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Mapper))
                            {
                                if (!client.Player.Dead)
                                {
                                    Messenger.PlayerWarp(client, "s1", 16, 21);
                                }   
                            }
                        }
                        break;
                    case "/raven":
                        {
                            foreach (var ravenClient in ClientManager.GetClients())
                            {
                                if (ravenClient.Player.Name.StartsWith("Raven"))
                                {
                                    ravenClient.Player.GetActiveRecruit().Sprite = 198;
                                    Messenger.SendPlayerData(ravenClient);
                                }
                            }
                        }       
                        break;
                    case "/spawnswarm":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                for (var i = 0; i < 10; i++)
                                {
                                    MapNpcPreset npc = new MapNpcPreset();
                                    npc.SpawnX = -1;
                                    npc.SpawnY = -1;
                                    npc.NpcNum = command[1].ToInt();
                                    npc.MinLevel = command[2].ToInt();
                                    npc.MaxLevel = command[2].ToInt();
                                    client.Player.Map.SpawnNpc(npc, false, false);
                                }

                                Messenger.PlayerMsg(client, "Swarm spawned.", Text.BrightGreen);
                            }
                        }
                        break;
                    case "/testquest":
                        {
                            var task = 0;
                            if (command.CommandArgs.Count > 2)
                            {
                                task = command[2].ToInt();
                            }
                            client.Player.TestQuest(command[1].ToInt(), task);
                        }
                        break;
                    case "/myoutlawpoints":
                        {
                            var lockedPoints = client.Player.PlayerData.PendingOutlawPoints - (client.Player.PlayerData.PendingOutlawPoints % OutlawPointInterval);

                            Messenger.PlayerMsg(client, $"You have {client.Player.PlayerData.LockedOutlawPoints} outlaw points locked.", Text.BrightGreen);
                            Messenger.PlayerMsg(client, $"You have {client.Player.PlayerData.PendingOutlawPoints} outlaw points this round. ({lockedPoints} will be locked)", Text.BrightGreen);
                        }
                        break;
                    case "/makemenormal":
                        {
                            if (client.Player.OutlawRole == Enums.OutlawRole.Hunter)
                            {
                                client.Player.OutlawRole = Enums.OutlawRole.None;

                                Messenger.SendPlayerData(client);
                            }
                        }   
                        break;
                    case "/makemehunter":
                        {
                            if (client.Player.OutlawRole == Enums.OutlawRole.None)
                            {
                                client.Player.OutlawRole = Enums.OutlawRole.Hunter;

                                Messenger.SendPlayerData(client);
                            }
                        }
                        break;
                    case "/copymissionclients":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                var missionPool = WonderMailManager.Missions[(int)Enums.JobDifficulty.S - 1];

                                var initial = (int)Enums.JobDifficulty.Star;
                                var final = (int)Enums.JobDifficulty.NineStar;

                                for (var i = initial; i <= final; i++)
                                {
                                    var secondMissionPool = WonderMailManager.Missions[i - 1];

                                    secondMissionPool.MissionClients.Clear();
                                    foreach (var missionClient in missionPool.MissionClients)
                                    {
                                        secondMissionPool.MissionClients.Add(new MissionClientData()
                                        {
                                            Species = missionClient.Species,
                                            Form = missionClient.Form
                                        });
                                    }

                                    using (var databaseConnection = new DatabaseConnection(DatabaseID.Data))
                                    {
                                        WonderMailManager.SaveMissionPool(databaseConnection, i - 1);
                                    }
                                }

                                Messenger.PlayerMsg(client, "Done", Text.BrightGreen);
                            }
                        }
                        break;
                    case "/countdown":
                        {
                            var targetTime = DateTime.UtcNow.AddMinutes(1);

                            Messenger.DisplayCountdown(client, "The event starts in...", targetTime);

                            GlobalCountdown = new Countdown("The event starts in...", targetTime);

                            TimedEventManager.CreateTimer("countdown", targetTime, null);
                        }
                        break;
                    case "/announce":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Task.Run(() => DiscordManager.Instance.SendAnnouncement($"**ANNOUNCEMENT**: {joinedArgs}"));

                                Messenger.SendAnnouncement("Announcement", joinedArgs);
                                Messenger.GlobalMsg($"ANNOUNCEMENT: {joinedArgs}", Text.BrightGreen);
                            }
                        }   
                        break;
                    case "/dmsg":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Task.Run(() => DiscordManager.Instance.SendAnnouncement(joinedArgs));
                            }
                        }
                        break;
                    case "/srnew":
                        {
                            var chapter = command[1].ToInt() - 1;
                            
                            var recordingStarted = client.Player.StoryRecorder.StartNewRecording(chapter, StoryRecorderMode.Create);
                            if (!recordingStarted)
                            {
                                Messenger.PlayerMsg(client, "Unable to start recording.", Text.BrightRed);
                                return;
                            }

                            Messenger.PlayerMsg(client, "Recording started.", Text.BrightGreen);
                        }
                        break;
                    case "/srsave":
                        {
                            if (!client.Player.StoryRecorder.IsRunning)
                            {
                                Messenger.PlayerMsg(client, "Recording not running.", Text.BrightRed);
                                return;
                            }

                            client.Player.StoryRecorder.SaveRecording();
                            Messenger.PlayerMsg(client, "Recording saved!", Text.BrightGreen);
                        }
                        break;
                    case "/setupfly":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                InitializeFlyPoints();
                            }
                        }
                        break;
                    case "/setupquests":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                InitializeIncompleteRegionQuests();
                            }
                        }
                        break;
                    case "/closebase":
                        {
                            if (SecretBaseManager.HasSecretBase(client))
                            {
                                SecretBaseManager.DeleteSecretBase(client);
                                Messenger.PlayerMsg(client, "Your secret base has been removed from the overworld!", Text.BrightGreen);
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You don't have a secret base anywhere!", Text.BrightRed);
                            }
                        }
                        break;
                    case "/closeguildbase":
                        {
                            if (client.Player.GuildAccess != Enums.GuildRank.Founder)
                            {
                                Messenger.PlayerMsg(client, "You can't use this command!", Text.BrightRed);
                                return;
                            }

                            if (SecretBaseManager.HasGuildSecretBase(client.Player.GuildId))
                            {
                                SecretBaseManager.DeleteGuildSecretBase(client.Player.GuildId);
                                Messenger.PlayerMsg(client, "Your guild base has been removed from the overworld!", Text.BrightGreen);
                            } 
                            else 
                            {
                                Messenger.PlayerMsg(client, "You don't have a guild base anywhere!", Text.BrightRed);
                            }
                        }   
                        break;
                    case "/play":
                        {
                            if (client.Player.MapID == "s604" || client.Player.MapID == "s999")
                            {
                                client.Player.Map.YouTubeMusicID = joinedArgs;

                                foreach (var i in client.Player.Map.GetClients())
                                {
                                    Messenger.RefreshMap(i);
                                }
                            }
                        }
                        break;
                    case "/setcostume":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client n;
                                string[] subCommand = command[0].Split('*');
                                if (subCommand.Length > 1)
                                {
                                    n = ClientManager.FindClient(command[1], true);
                                }
                                else
                                {
                                    n = ClientManager.FindClient(command[1]);
                                }

                                if (n != null)
                                {
                                    n.Player.GetActiveRecruit().Costume = command[2].ToInt();
                                    Messenger.SendPlayerData(n);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Player is offline.", Text.Grey);
                                }
                            } 
                            else
                            {
                                client.Player.GetActiveRecruit().Costume = joinedArgs.ToInt();
                                Messenger.SendPlayerData(client);
                            }
                        }
                        break;
                    case "/saveposition":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                positions.Add(Tuple.Create(client.Player.MapID, client.Player.X, client.Player.Y));

                                using (var fileStream = new System.IO.FileStream(System.IO.Path.Combine(Server.IO.Paths.ScriptsIOFolder, "positions.txt"), System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite))
                                {
                                    using (var streamWriter = new System.IO.StreamWriter(fileStream))
                                    {
                                        foreach (var position in positions)
                                        {
                                            streamWriter.WriteLine($"new TreasureHuntData.TreasureData() {{ MapID = \"{position.Item1}\", X = {position.Item2}, Y = {position.Item3}, Claimed = false }},");
                                        }
                                    }
                                }

                                Messenger.PlayerMsg(client, "Position saved!", Text.BrightGreen);
                            }
                        }
                        break;
                    case "/clearidlemessage":
                        {
                            client.Player.PlayerData.IdleMessage = "";

                            Messenger.PlayerMsg(client, "Your idle message has been cleared!", Text.BrightGreen);
                        }
                        break;
                    case "/idlemessage":
                        {
                            if (string.IsNullOrEmpty(joinedArgs))
                            {
                                var story = new Story();
                                var segment = StoryBuilder.BuildStory();

                                StoryBuilder.AppendSaySegment(segment, $"{client.Player.DisplayName}: {client.Player.PlayerData.IdleMessage}", client.Player.GetActiveRecruit().Species, 0, 0);

                                segment.AppendToStory(story);

                                StoryManager.PlayStory(client, story);
                            }
                            else
                            {
                                client.Player.PlayerData.IdleMessage = joinedArgs;

                                Messenger.PlayerMsg(client, "Your idle message has been updated!", Text.BrightGreen);
                            }
                        }
                        break;
                    case "/recruitbonus":
                        {
                            var bonus = Main.ScriptedRecruitBonus(client.Player, null);

                            Messenger.PlayerMsg(client, $"Your recruit bonus is {bonus / 10f}%", Text.BrightGreen);
                        }
                        break;
                    case "/abilities":
                        {
                            var activeRecruit = client.Player.GetActiveRecruit();

                            Messenger.PlayerMsg(client, $"Abilities for {activeRecruit.Name}:", Text.BrightGreen);

                            if (!string.IsNullOrEmpty(activeRecruit.Ability1))
                            {
                                Messenger.PlayerMsg(client, $"Ability 1: {activeRecruit.Ability1}", Text.BrightGreen);
                            }
                            if (!string.IsNullOrEmpty(activeRecruit.Ability2))
                            {
                                Messenger.PlayerMsg(client, $"Ability 2: {activeRecruit.Ability2}", Text.BrightGreen);
                            }
                            if (!string.IsNullOrEmpty(activeRecruit.Ability3))
                            {
                                Messenger.PlayerMsg(client, $"Ability 3: {activeRecruit.Ability3}", Text.BrightGreen);
                            }
                        }
                        break;
                    case "/charid":
                        {
                            Messenger.PlayerMsg(client, "Your character ID is: " + client.Player.CharID, Text.BrightGreen);
                        }
                        break;
                    case "/fetchscripts":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Messenger.PlayerMsg(client, "Pulling scripts...", Text.BrightGreen);
                                Server.Scripting.ScriptRepoHelper.PullChanges();
                                Messenger.PlayerMsg(client, "Scripts fetched!", Text.BrightGreen);
                            }
                        }
                        break;
                    case "/pullscripts":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter) || client.Player.Name == "ArtMax")
                            {
                                Messenger.PlayerMsg(client, "Pulling scripts...", Text.BrightGreen);
                                Server.Scripting.ScriptRepoHelper.PullChanges();
                                Messenger.PlayerMsg(client, "Reloading scripts...", Text.BrightGreen);
                                Server.Scripting.ScriptManager.Reload();
                                Messenger.PlayerMsg(client, "Scripts updated!", Text.BrightGreen);
                            }
                        }
                        break;
                    case "/eventdate":
                        {
                            var eventDate = GetEventDate();

                            Messenger.PlayerMsg(client, $"The next event date is {eventDate.ToShortDateString()} at {eventDate.ToShortTimeString()} UTC.", Text.BrightGreen);
                        }
                        break;
                    case "/scheduleweeklyevent":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                var startTime = new DateTime(DateTime.UtcNow.Year, 6, 7, 17, 0, 0, DateTimeKind.Utc);

                                if (Main.SetEvent(client, joinedArgs, false))
                                {
                                    TimedEventManager.CreateTimer("eventreminder", startTime.AddMinutes(-15), null);
                                    TimedEventManager.CreateTimer("eventintro", startTime, null);
                                }
                            }
                        }   
                        break;
                    case "/retryevent":
                        {
                            if (ActiveEvent != null && ActiveEvent.IsStarted)
                            {
                                ActiveEvent.OnboardNewPlayer(client);
                                ActiveEvent.ConfigurePlayer(client);
                            }
                        }
                        break;
                    case "/testeventintro":
                        {
                            var story = Main.BuildEventIntroStory();

                            StoryManager.PlayStory(client, story);
                        }
                        break;
                    case "/warneventstart":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Main.RunEventReminder();
                            }
                        }
                        break;
                    case "/finishevent":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Main.FinishEvent();
                            }
                        }
                        break;
                    case "/setevent":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Main.SetEvent(client, joinedArgs, false, allowOverride: true);
                            }
                        }
                        break;
                    case "/testevent":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Main.SetEvent(client, joinedArgs, true, allowOverride: true);
                            }
                        }
                        break;
                    case "/startevent":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Main.StartEvent();
                            }
                        }
                        break;
                    case "/endevent":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Main.EndEvent();
                            }
                        }
                        break;
                    case "/introevent":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Main.RunEventIntro();
                            }
                        }       
                        break;
                    case "/transferzone":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                client.Player.Map.ZoneID = joinedArgs.ToInt();

                                client.Player.Map.Save();

                                Messenger.PlayerMsg(client, "Zone transferred.", Text.BrightGreen);
                            }
                        }
                        break;
                    case "/shrug":
                        {
                            Messenger.MapMsg(client.Player.MapID, client.Player.Name + @": ¯\_(ツ)_/¯", Text.White);
                        }
                        break;
                    case "/zone":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Mapper))
                            {
                                var zoneID = client.Player.Map.ZoneID;

                                Messenger.PlayerMsg(client, $"Zone: {zoneID}", Text.BrightGreen);
                            }
                        }
                        break;
                    case "/testdungeon":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Mapper))
                            {
                                if (command[1].IsNumeric())
                                {
                                    int floor = 1;
                                    if (command.CommandArgs.Count > 2 && command[2].IsNumeric())
                                    {
                                        floor = command[2].ToInt();
                                    }

                                    var level = 1;
                                    if (command.CommandArgs.Count > 3 && command[3].IsNumeric())
                                    {
                                        level = command[3].ToInt();
                                    }

                                    var rdungeonNumber = command[1].ToInt();
                                    var rdungeon = Server.RDungeons.Modern.ModernRDungeonManager.Instance.Resources.Where(x => x.Id == rdungeonNumber).First();

                                    if (rdungeon.IsZoneOrObjectSandboxed() && client.Player.CanViewZone(rdungeon.ZoneId))
                                    {
                                        client.Player.WarpToModernRDungeon(rdungeon, floor - 1);
                                    }
                                }
                            }
                        }
                        break;
                    case "/testrdungeon":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Mapper))
                            {
                                if (command[1].IsNumeric())
                                {
                                    int floor = 1;
                                    if (command.CommandArgs.Count > 2 && command[2].IsNumeric())
                                    {
                                        floor = command[2].ToInt();
                                    }

                                    var level = 1;
                                    if (command.CommandArgs.Count > 3 && command[3].IsNumeric())
                                    {
                                        level = command[3].ToInt();
                                    }

                                    var rdungeonNumber = command[1].ToInt() - 1;
                                    var rdungeon = RDungeonManager.RDungeons[rdungeonNumber];

                                    if (rdungeon.IsZoneOrObjectSandboxed() && client.Player.CanViewZone(rdungeon.ZoneId))
                                    {
                                        client.Player.BeginTempStatMode(level, true);
                                        client.Player.WarpToRDungeon(command[1].ToInt() - 1, floor - 1);
                                    }
                                }
                            }
                        }
                        break;
                    case "/sandbox":
                        {
                            // TODO: Only allow entering sandbox mode if assigned to at least one zone
                            // TODO: Only allow entering sandbox mode if on the overworld
                            client.Player.PlayerData.IsSandboxed = !client.Player.PlayerData.IsSandboxed;
                            Messenger.PlayerMsg(client, "Sandbox mode is now " + (client.Player.PlayerData.IsSandboxed ? "enabled." : "disabled."), Text.BrightGreen);
                        }
                        break;
                    case "/enablediscord":
                        {
                            client.Player.PlayerData.CanLinkDiscord = true;
                            Messenger.PlayerMsg(client, "Discord connecting enabled!", Text.BrightGreen);
                        }
                        break;
                    case "/disablediscord":
                        {
                            client.Player.PlayerData.CanLinkDiscord = false;
                            Messenger.PlayerMsg(client, "Discord connecting disabled!", Text.BrightGreen);
                        }
                        break;
                    case "/resetdiscord":
                        {
                            client.Player.PendingDiscordId = 0;
                            Messenger.PlayerMsg(client, "Discord connecting reset!", Text.BrightGreen);
                        }
                        break;
                    case "/discordinfo":
                        {
                            if (client.Player.PlayerData.LinkedDiscordId > 0)
                            {
                                Messenger.PlayerMsg(client, "Your account is linked with Discord.", Text.BrightGreen);
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "Your account is NOT linked with Discord.", Text.BrightGreen);
                            }
                        }
                        break;
                    case "/savelogs":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Logger.SaveLogs();
                                Messenger.PlayerMsg(client, "Logs have been saved.", Text.BrightGreen);
                            }
                        }
                        break;
                    case "/textstory":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Story story = new Story();

                                StoryBuilderSegment segment = new StoryBuilderSegment();

                                StoryBuilder.AppendSaySegment(segment, joinedArgs, 25, 0, 0);

                                segment.AppendToStory(story);

                                foreach (Client i in client.Player.Map.GetClients())
                                {
                                    StoryManager.PlayStory(i, story);
                                }
                            }
                        }
                        break;
                    case "/spawnminions":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                for (int i = 0; i < joinedArgs.ToInt(); i++)
                                {
                                    MapNpcPreset npc = new MapNpcPreset();
                                    npc.SpawnX = 22; //-1;
                                    npc.SpawnY = 32; //-1;
                                    npc.NpcNum = 1368;
                                    npc.MinLevel = 90;
                                    npc.MaxLevel = 100;
                                    client.Player.Map.SpawnNpc(npc);
                                }
                            }
                        }
                        break;
                    case "/serverstatus":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {
                                Server.Globals.ServerStatus = joinedArgs;
                                Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Server Status] " + client.Player.Name + " changed the server status to: '" + joinedArgs + "'");
                            }
                        }
                        break;
                    case "/togglequiz":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {
                                InQuiz = !InQuiz;
                                QuestionReady = false;
                                Messenger.AdminMsg("[Staff] In Quiz: " + InQuiz.ToString(), Text.BrightBlue);
                            }
                        }
                        break;
                    case "/questionready":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {
                                QuestionReady = true;
                                Messenger.AdminMsg("[Staff] Question Ready: " + QuestionReady.ToString(), Text.BrightGreen);
                                for (int a = 8; a >= 0; a--)
                                {
                                    Messenger.MapMsg(client.Player.MapID, "You can answer in: " + a, Text.BrightGreen);
                                    System.Threading.Thread.Sleep(1000);
                                    CanAnswer = false;
                                }

                                Messenger.MapMsg(client.Player.MapID, "You can now buzz in!", Text.BrightGreen);
                                CanAnswer = true;
                            }
                        }
                        break;
                    case "/yatterman":
                        {
                            if (client.Player.CharID.Substring(1).ToInt() % 2 == 0)
                            {
                                Messenger.PlayerMsg(client, "All of the PINK", System.Drawing.Color.LimeGreen);
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "Slightly less PINK", Text.White);
                            }
                        }
                        break;
                    case "/glomp":
                        {
                            if (client.Player.Muted == false)
                            {
                                if (client.Player.CharID.Substring(1).ToInt() % 2 == 0)
                                {
                                    Messenger.MapMsg(client.Player.MapID, "Plusle Power! " + client.Player.Name + " used Glomp!", Text.Red);
                                }
                                else
                                {
                                    Messenger.MapMsg(client.Player.MapID, "Minun Power! " + client.Player.Name + " used Glomp!", Text.Cyan);
                                }

                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                            }

                        }
                        break;

                    case "/setquizanswer":
                        {
                            if (InQuiz)
                            {
                                if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                                {
                                    QuizAnswer = joinedArgs.ToLower();
                                    Messenger.AdminMsg("[Staff] Quiz answer set to: " + QuizAnswer, Text.BrightBlue);
                                }
                            }

                        }
                        break;
                    case "/buzz":
                        {
                            if (InQuiz && QuestionReady && CanAnswer)
                            {
                                //QuestionReady = false;
                                foreach (Client i in client.Player.Map.GetClients())
                                {
                                    if (i.IsPlaying())
                                    {

                                        Messenger.BattleMsg(i, client.Player.Name + " has answered with: " + joinedArgs, Text.BrightGreen);


                                        /* Story story = new Story();

                                         StoryBuilderSegment segment = new StoryBuilderSegment();

                                         StoryBuilder.AppendSaySegment(segment, client.Player.Name + " has buzzed in! " + client.Player.Name + "'s answer is...", -1, 0, 0);
                                         StoryBuilder.AppendSaySegment(segment, joinedArgs, -1, 0, 0);

                                         segment.AppendToStory(story);

                                         StoryManager.PlayStory(i, story);*/
                                    }


                                }
                                if (CanAnswer && joinedArgs.ToLower() == QuizAnswer)
                                {
                                    foreach (Client i in client.Player.Map.GetClients())
                                    {
                                        Messenger.PlayerMsg(i, client.Player.Name + " has answered correctly! The answer was: " + QuizAnswer, Text.Yellow);

                                    }
                                    QuestionReady = false;
                                    QuizAnswer = "";
                                }
                            }
                        }
                        break;
                    case "/d":
                    case "/roll":
                        {
                            int roll = 0;
                            if (int.TryParse(joinedArgs, out roll) && roll > 0)
                            {
                                Messenger.MapMsg(client.Player.MapID, $"{client.Player.Name} rolled {Server.Math.Rand(1, roll + 1)} (1-{roll})", Text.Teammate);
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "That is not a valid roll.", Text.Unselectable);
                            }
                        }
                        break;
                    case "/endgame":
                        {
                            if (exPlayer.Get(client).SnowballGameInstance.GameLeader == client)
                            {
                                exPlayer.Get(client).SnowballGameInstance.EndGame();
                                Messenger.PlayerMsg(client, "You have ended the game.", Text.Yellow);
                            }
                        }
                        break;
                    case "/snowballplayers":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {
                                if (Main.ActiveSnowballGames.Count > 0)
                                {
                                    Messenger.PlayerMsg(client, "Blue Team:", Text.Yellow);
                                    foreach (Client teamClient in Main.ActiveSnowballGames.Values[0].BlueTeam)
                                    {
                                        Messenger.PlayerMsg(client, teamClient.Player.Name, Text.Yellow);
                                    }
                                    Messenger.PlayerMsg(client, "Green Team:", Text.Yellow);
                                    foreach (Client teamClient in Main.ActiveSnowballGames.Values[0].GreenTeam)
                                    {
                                        Messenger.PlayerMsg(client, teamClient.Player.Name, Text.Yellow);
                                    }
                                }
                            }
                        }
                        break;
                    case "/gmmode":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Server.Globals.GMOnly = !Server.Globals.GMOnly;
                                Messenger.PlayerMsg(client, "GM Only Mode Active: " + Server.Globals.GMOnly, Text.Yellow);
                            }
                        }
                        break;
                    case "/copymap":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                IMap baseMap = MapManager.RetrieveMap(command[1]);
                                IMap destinationMap = MapManager.RetrieveMap(command[2], true);
                                MapCloner.CloneMapTileProperties(baseMap, destinationMap);
                                MapCloner.CloneMapTiles(baseMap, destinationMap);
                                destinationMap.Revision++;
                                destinationMap.Save();
                                Messenger.PlayerWarp(client, destinationMap, 25, 25);
                            }
                        }
                        break;
                    case "/currentsection":
                        {
                            if (exPlayer.Get(client).StoryEnabled)
                            {
                                if (command.CommandArgs.Count == 2)
                                {
                                    client.Player.StoryHelper.SaveSetting("[MainStory]-CurrentSection", joinedArgs.ToInt().ToString());
                                }
                                Messenger.PlayerMsg(client, "Current section: " + client.Player.StoryHelper.ReadSetting("[MainStory]-CurrentSection").ToInt().ToString(), Text.BrightGreen);
                            }
                        }
                        break;
                    case "/resetstory":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {
                                StoryHelper.ResetStory(client);
                            }
                        }
                        break;
                    case "/storymode":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {
                                exPlayer.Get(client).StoryEnabled = !exPlayer.Get(client).StoryEnabled;
                                Messenger.PlayerMsg(client, "Story mode is now " + (exPlayer.Get(client).StoryEnabled ? "on!" : "off!"), Text.BrightGreen);
                            }
                        }
                        break;

                    case "/staffauction":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Mapper))
                            {
                                if (Auction.StaffAuction == false)
                                {
                                    Auction.StaffAuction = true;
                                    Messenger.AdminMsg("[Staff] Staff-only auction mode is now active.", Text.BrightBlue);
                                }
                                else if (Auction.StaffAuction == true)
                                {
                                    Auction.StaffAuction = false;
                                    Messenger.AdminMsg("[Staff] Staff-only auction mode is now disabled.", Text.BrightBlue);
                                }
                            }
                        }

                        break;
                    case "/rdstartcheck":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {
                                for (int i = 0; i < 10; i++)
                                {
                                    RDungeonMap map = RDungeonFloorGen.GenerateFloor(client, 54, 49, RDungeonManager.RDungeons[54].Floors[49].Options);
                                    Messenger.PlayerMsg(client, i.ToString(), Text.Black);
                                }
                            }
                        }
                        break;
                    case "/daynight":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Server.Events.World.TimedEventManager.TimedEvents["DayCycle"].OnTimeElapsed(Server.Core.GetTickCount());
                                Messenger.PlayerMsg(client, Server.Globals.ServerTime.ToString(), Text.BrightGreen);
                                //}
                            }
                        }
                        break;
                    case "/tourneyplayers":
                        {
                            Tournament tourney = client.Player.Tournament;
                            if (tourney != null)
                            {
                                if (tourney.RegisteredMembers[client] != null)
                                {
                                    if (tourney.RegisteredMembers[client].Admin)
                                    {
                                        tourney.PlayersNeeded = joinedArgs.ToInt();
                                        Messenger.PlayerMsg(client, "The current player requirement is: " + tourney.PlayersNeeded.ToString(), Text.BrightGreen);
                                    }
                                }
                            }
                        }
                        break;
                    case "/createtourney":
                        {
                            Messenger.PlayerMsg(client, "You are making a tourney!", Text.BrightRed);
                            Tournament tourney = TournamentManager.CreateTournament(client, joinedArgs, "s1193", 10, 10);
                            tourney.AddCombatMap("s1194");
                        }
                        break;
                    case "/jointourney":
                        {
                            Tournament tourney = TournamentManager.Tournaments[joinedArgs.ToInt()];
                            tourney.RegisterPlayer(client);
                        }
                        break;
                    case "/estlevel":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                exPlayer.Get(client).ElectrolockLevel = joinedArgs.ToInt();
                                Messenger.PlayerMsg(client, "EST level set to: " + joinedArgs.ToInt(), Text.BrightGreen);
                            }
                        }
                        break;
                    case "/voterestart":
                        {
                            if(!votersRestart.Contains(client.Player.Name))
                            {
                                votersRestart.Add(client.Player.Name);
                                Messenger.GlobalMsg(client.Player.Name + " has voted to restart the server.", Text.BrightGreen);
                            }
                            foreach (Client i in ClientManager.GetClients())
                                if (i.IsPlaying() && !votersRestart.Contains(i.Player.Name))
                                    return;
                            Messenger.GlobalMsg("All online players have voted to restart the server.", Text.Red);
                            Task.Run(ServerEnvironment.RestartAsync);
                        }
                        break;
                    case "/cancelvoterestart":
                        {
                            if(votersRestart.Contains(client.Player.Name))
                            {
                                votersRestart.Remove(client.Player.Name);
                                Messenger.GlobalMsg(client.Player.Name + " has cancelled their vote to restart the server.", Text.BrightGreen);
                            }
                        }
                        break;
                    case "/checktime":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {
                                Client n = ClientManager.FindClient(joinedArgs);
                                if (n != null)
                                {
                                    Messenger.PlayerMsg(client, joinedArgs + "'s total play time: " + n.Player.Statistics.TotalPlayTime.ToString(), Text.BrightGreen);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Player is offline.", Text.Grey);
                                }
                            }
                        }
                        break;
                    case "/warn*":
                    case "/warn":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {
                                Client n;
                                string[] subCommand = command[0].Split('*');
                                if (subCommand.Length > 1)
                                {
                                    n = ClientManager.FindClient(command[1], true);
                                }
                                else
                                {
                                    n = ClientManager.FindClient(command[1]);
                                }
                                if (n != null)
                                {
                                    Messenger.PlayerMsg(n, "You have been warned by a staff member: " + command[2] /* + "\n-" + client.Player.Name*/, System.Drawing.Color.Orange);
                                    Messenger.AdminMsg("[Staff] " + client.Player.Name + " has warned " + n.Player.Name + ": " + command[2], Text.BrightBlue);
                                    Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Warning Issued] " + client.Player.Name + " warned " + n.Player.Name + " with message: " + command[2]);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Player is offline.", Text.Grey);
                                }
                            }
                        }
                        break;
                    case "/getcharinfo":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                using (DatabaseConnection dbConnection = new DatabaseConnection(DatabaseID.Players))
                                {
                                    CharacterInformation charInfo = PlayerManager.RetrieveCharacterInformation(dbConnection, joinedArgs);
                                    if (charInfo != null)
                                    {
                                        Messenger.PlayerMsg(client, "Info for " + charInfo.Name + ":", Text.Yellow);
                                        Messenger.PlayerMsg(client, "Account: " + charInfo.Account, Text.Yellow);
                                        Messenger.PlayerMsg(client, "CharID: " + charInfo.ID, Text.Yellow);
                                        Messenger.PlayerMsg(client, "Char Slot: " + charInfo.Slot, Text.Yellow);
                                        Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Info Request] " + client.Player.Name + " checked the character information for: " + command[1]);
                                    }
                                }
                            }
                        }
                        break;
                    case "/clearjoblist":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client n = ClientManager.FindClient(joinedArgs);
                                if (n != null)
                                {
                                    n.Player.JobList.Clear();
                                    Messenger.SendJobList(n);
                                    Messenger.PlayerMsg(n, "Your job list has been cleared!", Text.BrightGreen);
                                    Messenger.PlayerMsg(client, "You have cleared " + joinedArgs + "'s job list!", Text.BrightGreen);
                                    //Messenger.PlayerMsg(client, joinedArgs + " ID is: " + n.Player.CharID, Text.BrightGreen);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Player is offline.", Text.Grey);
                                }
                            }
                        }
                        break;
                    case "/void":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Messenger.PlayerWarpToVoid(client);
                            }
                        }
                        break;
                    case "/voidplayer*":
                    case "/voidplayer":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                string playerName = command[1];
                                Client n;
                                string[] subCommand = command[0].Split('*');
                                if (subCommand.Length > 1)
                                {
                                    n = ClientManager.FindClient(playerName, true);
                                }
                                else
                                {
                                    n = ClientManager.FindClient(playerName);
                                }

                                if (n != null)
                                {
                                    Messenger.PlayerWarpToVoid(n);
                                    Messenger.GlobalMsg(n.Player.Name + " has been swallowed by the void...", Text.Red);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, playerName + " could not be found.", Text.Green);
                                }
                            }
                        }
                        break;
                    case "/unvoid":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {

                                string playerName = command[1];
                                Client n;
                                n = ClientManager.FindClient(playerName);
                                IMap map = n.Player.Map;
                                if (map.MapType == Enums.MapType.Void)
                                {
                                    Server.Maps.Void @void = map as Server.Maps.Void;
                                    @void.SafeExit = true;
                                    Messenger.PlayerWarp(n, 1015, 25, 25);
                                    Messenger.PlayerMsg(n, "You have been unvoided.", Text.BrightGreen);
                                    Messenger.PlayerMsg(client, playerName + " has been unvoided.", Text.BrightGreen);
                                }
                            }
                        }
                        break;
                    case "/getid":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client n = ClientManager.FindClient(joinedArgs);
                                if (n != null)
                                {
                                    Messenger.PlayerMsg(client, joinedArgs + " ID is: " + n.Player.CharID, Text.BrightGreen);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Player is offline.", Text.Grey);
                                }
                            }
                        }
                        break;
                    case "/who":
                        {
                            int count = 0;
                            foreach (Client i in ClientManager.GetClients())
                            {
                                if (i.TcpClient.Socket.Connected && i.IsPlaying())
                                {
                                    count++;
                                }
                            }
                            Messenger.PlayerMsg(client, "Players online: " + count, Text.Yellow);
                        }
                        break;
                    case "/saveall":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {
                                using (DatabaseConnection dbConnection = new DatabaseConnection(DatabaseID.Players))
                                {
                                    try
                                    {
                                        foreach (Client i in ClientManager.GetClients())
                                        {
                                            i.Player.SaveCharacterData(dbConnection);
                                            Messenger.PlayerMsg(i, "You saved the game!", Text.BrightGreen);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Messenger.PlayerMsg(client, ex.ToString(), Text.BrightRed);
                                    }
                                }
                                Messenger.PlayerMsg(client, "Everyone has been saved!", Text.Yellow);
                            }
                        }
                        break;
                    case "/explode":
                        {
                            Messenger.GlobalMsg(client.Player.Name + " has exploded.", Text.BrightRed);
                        }
                        break;

                    case "/silentkick*":
                    case "/silentkick":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                string playerName = command[1];
                                Client kickedClient;
                                string[] subCommand = command[0].Split('*');
                                if (subCommand.Length > 1)
                                {
                                    kickedClient = ClientManager.FindClient(playerName, true);
                                }
                                else
                                {
                                    kickedClient = ClientManager.FindClient(playerName);
                                }
                                if (kickedClient != null)
                                {
                                    if (command.CommandArgs.Count > 2 && !String.IsNullOrEmpty(command[2]))
                                    {
                                        Messenger.AdminMsg("[Staff] " + kickedClient.Player.Name + " has been disconnected silently from the server by " + client.Player.Name + "! " + "Reason: " + command[2], Text.BrightBlue);
                                        kickedClient.CloseConnection();
                                        Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Kick] " + client.Player.Name + " silently kicked " + playerName + " from the server with reason: " + command[2]);
                                    }
                                    else
                                    {
                                        Messenger.AdminMsg("[Staff] " + kickedClient.Player.Name + " has been disconnected silently from the server by " + client.Player.Name + "!", Text.BrightBlue);
                                        Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Kick] " + client.Player.Name + " silently kicked " + playerName + " from the server.");
                                        kickedClient.CloseConnection();
                                    }
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Unable to find player!", Text.BrightRed);
                                }
                            }
                        }
                        break;
                    case "/kick*":
                    case "/kick":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                string playerName = command[1];
                                Client kickedClient;
                                string[] subCommand = command[0].Split('*');
                                if (subCommand.Length > 1)
                                {
                                    kickedClient = ClientManager.FindClient(playerName, true);
                                }
                                else
                                {
                                    kickedClient = ClientManager.FindClient(playerName);
                                }
                                if (kickedClient != null)
                                {
                                    if (command.CommandArgs.Count > 2 && !String.IsNullOrEmpty(command[2]))
                                    {
                                        Messenger.AdminMsg(kickedClient.Player.Name + " has been kicked from the server by " + client.Player.Name + "!" + " Reason: " + command[2], Text.BrightBlue);
                                        Messenger.PlainMsg(kickedClient, "You have been kicked from the server!  Reason: " + command[2], Enums.PlainMsgType.MainMenu);
                                        kickedClient.CloseConnection();
                                        Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Kick] " + client.Player.Name + " kicked " + playerName + " from the server with reason: " + command[2]);
                                    }
                                    else
                                    {
                                        Messenger.AdminMsg("[Staff] " + kickedClient.Player.Name + " has been kicked from the server by " + client.Player.Name, Text.BrightBlue);
                                        Messenger.PlainMsg(kickedClient, "You have been kicked from the server!", Enums.PlainMsgType.MainMenu);
                                        kickedClient.CloseConnection();
                                        Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Kick] " + client.Player.Name + " kicked " + playerName + " from the server.");
                                    }
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Unable to find player!", Text.BrightRed);
                                }
                            }
                        }
                        break;
                    case "/ban*":
                    case "/ban":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                string playerName = command[1];
                                string banTimeDays = "-----";
                                Client bannedClient;
                                string[] subCommand = command[0].Split('*');
                                if (subCommand.Length > 1)
                                {
                                    bannedClient = ClientManager.FindClient(playerName, true);
                                }
                                else
                                {
                                    bannedClient = ClientManager.FindClient(playerName);
                                }

                                if (command.CommandArgs.Count > 2 && command[2].IsNumeric())
                                {
                                    banTimeDays = DateTime.Now.AddDays(Convert.ToDouble(command[2])).ToString();
                                }

                                if (bannedClient != null)
                                {
                                    using (DatabaseConnection dbConnection = new DatabaseConnection(DatabaseID.Players))
                                    {
                                        Bans.BanPlayer(dbConnection, bannedClient.IP.ToString(), bannedClient.Player.CharID,
                                            bannedClient.Player.AccountName + "/" + bannedClient.Player.Name, bannedClient.MacAddress, bannedClient.BiosId,
                                            client.Player.CharID, client.IP.ToString(), banTimeDays, Enums.BanType.Ban);
                                        Messenger.AdminMsg("[Staff] " + bannedClient.Player.Name + " has been banned by " + client.Player.Name + "!", Text.BrightBlue);
                                        Messenger.PlainMsg(bannedClient, "You have been banned!", Enums.PlainMsgType.MainMenu);
                                        bannedClient.CloseConnection();
                                        Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Ban] " + client.Player.Name + " banned " + playerName + ".");
                                    }
                                }
                                else
                                {
                                    using (DatabaseConnection dbConnection = new DatabaseConnection(DatabaseID.Players))
                                    {
                                        IDataColumn[] columns = dbConnection.Database.RetrieveRow("characteristics", "CharID", "Name=\"" + playerName + "\"");
                                        if (columns != null)
                                        {
                                            string charID = (string)columns[0].Value;
                                            string foundIP = (string)dbConnection.Database.RetrieveRow("character_statistics", "LastIPAddressUsed", "CharID=\"" + charID + "\"")[0].Value;
                                            string foundMac = (string)dbConnection.Database.RetrieveRow("character_statistics", "LastMacAddressUsed", "CharID=\"" + charID + "\"")[0].Value;
                                            string storedUUID = (string)dbConnection.Database.RetrieveRow("character_statistics", "StoredUUID", "CharID=\"" + charID + "\"")[0].Value;
                                            //get previous IP and mac
                                            Bans.BanPlayer(dbConnection, foundIP, charID, playerName, foundMac, storedUUID,
                                                client.Player.CharID, client.IP.ToString(), banTimeDays, Enums.BanType.Ban);
                                            Messenger.AdminMsg("[Staff] " + bannedClient.Player.Name + " has been banned by " + client.Player.Name + "!", Text.BrightBlue);
                                            Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Ban] " + client.Player.Name + " banned " + playerName + ".");
                                        }
                                        else
                                        {
                                            Messenger.PlayerMsg(client, "Unable to find player!", Text.BrightRed);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case "/emptyhouse":
                        {
                            IMap map = client.Player.Map;
                            if (map.MapType == Server.Enums.MapType.House && ((House)map).OwnerID == client.Player.CharID)
                            {
                                List<Client> clientList = new List<Client>();

                                foreach (Client n in client.Player.Map.GetClients())
                                {
                                    if (n != client && Ranks.IsDisallowed(client, Enums.Rank.Monitor))
                                    {
                                        clientList.Add(n);
                                    }
                                }

                                foreach (Client n in clientList)
                                {
                                    if (!string.IsNullOrEmpty(exPlayer.Get(n).HousingCenterMap))
                                    {
                                        Messenger.PlayerWarp(n, exPlayer.Get(n).HousingCenterMap, exPlayer.Get(n).HousingCenterX, exPlayer.Get(n).HousingCenterY);
                                    }
                                }
                                Messenger.PlayerMsg(client, "All visitors have been kicked from your house!", Text.Yellow);
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You aren't in your house!", Text.BrightRed);
                            }
                        }
                        break;
                    case "/leavehouse":
                        {
                            IMap map = client.Player.Map;
                            if (map.MapType == Server.Enums.MapType.House || map.MapType == Server.Enums.MapType.GuildBase)
                            {
                                if (!string.IsNullOrEmpty(exPlayer.Get(client).HousingCenterMap))
                                {
                                    Messenger.PlayerWarp(client, exPlayer.Get(client).HousingCenterMap, exPlayer.Get(client).HousingCenterX, exPlayer.Get(client).HousingCenterY);
                                }
                            }
                        }
                        break;
                    case "/houseentrance":
                        {
                            IMap map = client.Player.Map;
                            if (map.MapType == Server.Enums.MapType.House && ((House)map).OwnerID == client.Player.CharID)
                            {
                                //Messenger.AskQuestion(client, "HouseSpawn", "Will you set your house's entrance here?", -1);
                                Messenger.AskQuestion(client, "HouseSpawn", "Will you set your house's entrance here?  It will cost 500 Poké.", -1);
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You can't set your house entrance here!", Text.BrightRed);
                            }
                        }
                        break;
                    case "/baseentrance":
                        {
                            IMap map = client.Player.Map;
                            if (map.MapType == Server.Enums.MapType.GuildBase && ((GuildBase)map).Owner == client.Player.GuildId)
                            {
                                Messenger.AskQuestion(client, "BaseSpawn", "Will you set your base's entrance here?  It will cost 500 Poké.", -1);
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You can't set your base entrance here!", Text.BrightRed);
                            }
                        }
                        break;
                    case "/houseroof":
                        {
                            IMap map = client.Player.Map;
                            if (map.MapType == Server.Enums.MapType.House && ((House)map).OwnerID == client.Player.CharID)
                            {
                                if (map.Indoors)
                                {
                                    Messenger.AskQuestion(client, "HouseRoof", "Will you open your house's roof and expose it to time and weather conditions?  It will cost 500 Poké.", -1);
                                }
                                else
                                {
                                    Messenger.AskQuestion(client, "HouseRoof", "Will you close your house to time and weather conditions?  It will cost 500 Poké.", -1);
                                }
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You can't set your house roof here!", Text.BrightRed);
                            }
                        }
                        break;
                    case "/houseweather":
                        {
                            IMap map = client.Player.Map;
                            if (client.Player.ExplorerRank >= Enums.ExplorerRank.Silver)
                            {
                                if (map.MapType == Server.Enums.MapType.House && ((House)map).OwnerID == client.Player.CharID)
                                {
                                    if (map.Indoors)
                                    {
                                        Messenger.PlayerMsg(client, "You can't set your house weather unless you open your house with /houseroof", Text.BrightRed);
                                    }
                                    else
                                    {
                                        Messenger.OpenChangeWeatherMenu(client);
                                    }
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "You can't set your house weather here!", Text.BrightRed);
                                }
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You can't expand your house until your Explorer Rank is Silver or higher.", Text.BrightRed);
                            }
                        }
                        break;
                    case "/houselight":
                        {
                            IMap map = client.Player.Map;
                            if (client.Player.ExplorerRank >= Enums.ExplorerRank.Silver)
                            {
                                if (map.MapType == Server.Enums.MapType.House && ((House)map).OwnerID == client.Player.CharID)
                                {
                                    Messenger.OpenChangeDarknessMenu(client);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "You can't set your house lights here!", Text.BrightRed);
                                }
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You can't expand your house until your Explorer Rank is Silver or higher.", Text.BrightRed);
                            }
                        }
                        break;
                    case "/houseexpand":
                        {
                            IMap map = client.Player.Map;
                            if (client.Player.ExplorerRank >= Enums.ExplorerRank.Gold)
                            {
                                if (map.MapType == Server.Enums.MapType.House && ((House)map).OwnerID == client.Player.CharID)
                                {
                                    Messenger.OpenChangeBoundsMenu(client);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "You can't expand your house here!", Text.BrightRed);
                                }
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You can't expand your house until your Explorer Rank is Gold or higher.", Text.BrightRed);
                            }
                        }
                        break;
                    case "/houseshop":
                        {
                            IMap map = client.Player.Map;
                            if (client.Player.ExplorerRank >= Enums.ExplorerRank.Bronze)
                            {
                                if (map.MapType == Server.Enums.MapType.House && ((House)map).OwnerID == client.Player.CharID)
                                {
                                    Messenger.OpenAddShopMenu(client);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "You can't place a shop here!", Text.BrightRed);
                                }
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You can't expand your house until your Explorer Rank is Bronze or higher.", Text.BrightRed);
                            }
                        }
                        break;
                    case "/housesign":
                        {
                            IMap map = client.Player.Map;
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                                return;
                            }
                            if (client.Player.ExplorerRank >= Enums.ExplorerRank.Bronze)
                            {
                                if (map.MapType == Server.Enums.MapType.House && ((House)map).OwnerID == client.Player.CharID)
                                {
                                    Messenger.OpenAddSignMenu(client);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "You can't place a sign here!", Text.BrightRed);
                                }
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You can't expand your house until your Explorer Rank is Bronze or higher.", Text.BrightRed);
                            }
                        }
                        break;
                    case "/housesound":
                        {
                            IMap map = client.Player.Map;
                            if (client.Player.ExplorerRank >= Enums.ExplorerRank.Bronze)
                            {
                                if (map.MapType == Server.Enums.MapType.House && ((House)map).OwnerID == client.Player.CharID)
                                {
                                    Messenger.OpenAddSoundMenu(client);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "You can't place a sound tile here!", Text.BrightRed);
                                }
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You can't expand your house until your Explorer Rank is Bronze or higher.", Text.BrightRed);
                            }
                        }
                        break;
                    case "/housenotice":
                        {
                            IMap map = client.Player.Map;
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                                return;
                            }
                            if (client.Player.ExplorerRank >= Enums.ExplorerRank.Bronze)
                            {
                                if (map.MapType == Server.Enums.MapType.House && ((House)map).OwnerID == client.Player.CharID)
                                {
                                    Messenger.OpenAddNoticeMenu(client);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "You can't place a notice tile here!", Text.BrightRed);
                                }
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You can't expand your house until your Explorer Rank is Bronze or higher.", Text.BrightRed);
                            }
                        }
                        break;

                    case "/findaccount":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                foreach (Client i in ClientManager.GetClients())
                                {
                                    if (i.Player.AccountName.ToLower() == joinedArgs.ToLower())
                                    {
                                        Messenger.PlayerMsg(client, "Found account! [" + i.Player.AccountName + "/" + i.Player.Name + "]", Text.BrightGreen);
                                    }
                                }
                            }
                        }
                        break;
                    case "/cc":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client n = ClientManager.FindClient(joinedArgs);
                                n.CloseConnection();
                                Messenger.PlayerMsg(client, "CC/ " + n.Player.Name, Text.Blue);
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "That is not a valid command!", Text.BrightRed);
                            }
                        }
                        break;
                    case "/regenlotto":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Lottery.ForceGenLottoNumbers();
                                Messenger.PlayerMsg(client, "Lottery numbers regenerated!", Text.Yellow);
                            }
                        }
                        break;
                    case "/lottostats":
                        {
                            // TODO: /lottostats lottery
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Messenger.PlayerMsg(client, "Lottery Stats:", Text.BrightGreen);
                                Messenger.PlayerMsg(client, "Lottery Payout: " + Lottery.LotteryPayout, Text.BrightGreen);
                                Messenger.PlayerMsg(client, "Lottery Earnings: " + Lottery.LotteryEarnings, Text.BrightGreen);
                                Messenger.PlayerMsg(client, "Last Lottery Earnings: " + Lottery.LastLotteryEarnings, Text.BrightGreen);
                                Messenger.PlayerMsg(client, "Total Lottery Earnings: " + Lottery.TotalLotteryEarnings, Text.BrightGreen);
                            }
                        }
                        break;
                    //debug
                    //testbuff
                    case "/motd":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Server.Settings.MOTD = joinedArgs;
                                Messenger.GlobalMsg("MOTD changed to: " + joinedArgs, Text.BrightCyan);
                                Server.Settings.SaveMOTD();
                                Server.Logging.ChatLogger.AppendToChatLog("Staff", "[MOTD] " + client.Player.Name + " set the MOTD to: " + joinedArgs);
                            }
                        }
                        break;

                    #region CTF Commands
                    case "/ctfcreate":
                        {
                            if (client.Player.MapID == MapManager.GenerateMapID(CTF.HUBMAP))
                            {
                                if (ActiveCTF == null)
                                {
                                    ActiveCTF = new CTF(CTF.CTFGameState.NotStarted);
                                }
                                if (ActiveCTF.GameState == CTF.CTFGameState.NotStarted)
                                {
                                    ActiveCTF.CreateGame(client);
                                    return;
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "A game of Capture The Flag is already started!", Text.BrightRed);
                                }
                            }
                        }
                        break;
                    case "/ctfjoin":
                        {
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You cannot join the game because you are muted!", Text.BrightRed);
                            }
                            if (client.Player.MapID == MapManager.GenerateMapID(CTF.HUBMAP))
                            {
                                if (ActiveCTF == null)
                                {
                                    Messenger.PlayerMsg(client, "No game has been started yet!", Text.BrightRed);
                                }
                                else if (ActiveCTF.GameState == CTF.CTFGameState.WaitingForPlayers)
                                {
                                    if (exPlayer.Get(client).InCTF == false)
                                    {
                                        ActiveCTF.AddToGame(client);
                                    }
                                    else
                                    {
                                        Messenger.PlayerMsg(client, "You have already joined the game!", Text.BrightRed);
                                    }
                                }
                                else
                                {
                                    if (ActiveCTF.GameState == CTF.CTFGameState.Started)
                                    {
                                        Messenger.PlayerMsg(client, "There is already a game of Capture The Flag that has been started!", Text.BrightRed);
                                    }
                                    else
                                    {
                                        Messenger.PlayerMsg(client, "No game of Capture The Flag has been created yet!", Text.BrightRed);
                                    }
                                }
                            }
                        }
                        break;
                    case "/ctfleave":
                        {
                            if (client.Player.MapID == MapManager.GenerateMapID(CTF.REDMAP) || client.Player.MapID == MapManager.GenerateMapID(CTF.BLUEMAP))
                            {
                                ActiveCTF.RemoveFromGame(client);
                            }
                        }
                        break;
                    case "/ctfstart":
                        {
                            if (ActiveCTF.GameLeader == client)
                            {
                                ActiveCTF.StartGame();
                                ActiveCTF.CTFMsg("This game of Capture The Flag has started.", Text.Yellow);
                                ActiveCTF.CTFMsg("This game will have " + ActiveCTF.BlueFlags + " flags!", Text.Yellow);
                            }
                        }
                        break;
                    case "/ctfflags":
                        {
                            if (ActiveCTF.GameLeader == client && ActiveCTF.GameState == CTF.CTFGameState.WaitingForPlayers)
                            {
                                ActiveCTF.RedFlags = joinedArgs.ToInt();
                                ActiveCTF.BlueFlags = joinedArgs.ToInt();
                                Messenger.PlayerMsg(client, "This game will have " + ActiveCTF.BlueFlags + " flags!", Text.Yellow);
                            }
                        }
                        break;
                    case "/ctfend":
                        {
                            if (ActiveCTF.GameLeader == client)
                            {
                                ActiveCTF.EndGame(client);
                                Messenger.PlayerMsg(client, "You have ended the game.", Text.Yellow);
                            }
                        }
                        break;
                    case "/ctfforceend":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {
                                ActiveCTF.EndGame(client.Player.Name);
                                Messenger.PlayerMsg(client, "You have ended the game.", Text.Yellow);
                            }
                        }
                        break;
                    case "/ctf":
                        {
                            if (ActiveCTF.GameState == CTF.CTFGameState.Started)
                            {
                                if (exPlayer.Get(client).InCTF)
                                {
                                    ActiveCTF.CTFMsg(client.Player.Name + " [CTF]: " + joinedArgs, Text.Grey);
                                }
                            }
                        }
                        break;
                    case "/ctft":
                        {
                            if (ActiveCTF.GameState == CTF.CTFGameState.Started)
                            {
                                if (exPlayer.Get(client).InCTF)
                                {
                                    ActiveCTF.CTFTMsg(client, client.Player.Name + " [CTF Team]: " + joinedArgs, Text.Grey);
                                }
                            }
                        }
                        break;
                    case "/ctfgen":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                RDungeonMap dungeonMap = RDungeonFloorGen.GenerateFloor(client, 39, 0, RDungeonManager.RDungeons[39].Floors[0].Options);
                                Messenger.PlayerWarp(client, dungeonMap, dungeonMap.StartX, dungeonMap.StartY);
                            }
                        }
                        break;
                    #endregion CTF Commands
                    case "/checkstack":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client n = ClientManager.FindClient(command[1]);
                                if (n == null)
                                {
                                    Messenger.PlayerMsg(client, "Player is offline.", Text.Grey);
                                }
                                else if (command[2].ToInt() <= 0)
                                {
                                    Messenger.PlayerMsg(client, "Invalid item number.", Text.BrightRed);
                                }
                                else if (n == client)
                                {
                                    Messenger.PlayerMsg(client, "Your amount of " + ItemManager.Items[command[2].ToInt()].Name + ": " + n.Player.HasItem(command[2].ToInt()).ToString(), Text.Yellow);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, n.Player.Name + "'s amount of " + ItemManager.Items[command[2].ToInt()].Name + ": " + n.Player.HasItem(command[2].ToInt()).ToString(), Text.Yellow);
                                }
                            }
                        }
                        break;
                    case "/checkinv*":
                    case "/checkinv":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client n;
                                string[] subCommand = command[0].Split('*');
                                if (subCommand.Length > 1)
                                {
                                    n = ClientManager.FindClient(joinedArgs, true);
                                }
                                else
                                {
                                    n = ClientManager.FindClient(joinedArgs);
                                }
                                Messenger.PlayerMsg(client, n.Player.Name + "'s Inventory:", Text.Yellow);
                                InventoryItem item;
                                for (int i = 1; i <= n.Player.MaxInv; i++)
                                {
                                    item = n.Player.Inventory[i];
                                    int amount = 0;
                                    string msg = item.Num + " ";
                                    if (item.Num > 0)
                                    {
                                        msg += ItemManager.Items[item.Num].Name;
                                        amount = item.Amount;
                                    }
                                    if (amount > 0)
                                    {
                                        msg += " (" + amount.ToString() + ")";
                                    }
                                    if (item.Tag != "")
                                    {
                                        msg += " [" + item.Tag + "]";
                                    }
                                    if (msg != "")
                                    {
                                        Messenger.PlayerMsg(client, msg, Text.Yellow);
                                    }
                                }
                            }
                        }
                        break;
                    case "/clearinv":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                for (int i = 1; i <= client.Player.MaxInv; i++)
                                {
                                    client.Player.TakeItemSlot(i, client.Player.Inventory[i].Amount, true);
                                }
                                Messenger.PlayerMsg(client, "Inventory Cleared", Text.Yellow);
                            }
                        }
                        break;
                    case "/checkbank*":
                    case "/checkbank":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client n;
                                string[] subCommand = command[0].Split('*');
                                if (subCommand.Length > 1)
                                {
                                    n = ClientManager.FindClient(joinedArgs, true);
                                }
                                else
                                {
                                    n = ClientManager.FindClient(joinedArgs);
                                }
                                Messenger.PlayerMsg(client, n.Player.Name + "'s Bank:", Text.Yellow);
                                InventoryItem item;
                                for (int i = 1; i <= n.Player.MaxBank; i++)
                                {
                                    item = n.Player.Bank[i];
                                    int amount = 0;
                                    string msg = "";
                                    if (item.Num > 0)
                                    {
                                        msg = ItemManager.Items[item.Num].Name;
                                        amount = item.Amount;
                                    }
                                    if (amount > 0)
                                    {
                                        msg += " (" + amount.ToString() + ")";
                                    }
                                    if (msg != "")
                                    {
                                        Messenger.PlayerMsg(client, msg, Text.Yellow);
                                    }
                                }
                            }
                        }
                        break;
                    case "/checkmoves":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client n = ClientManager.FindClient(joinedArgs);
                                Messenger.PlayerMsg(client, n.Player.Name + "'s Moves:", Text.Yellow);
                                for (int i = 0; i < Constants.MAX_ACTIVETEAM; i++)
                                {
                                    if (n.Player.Team[i] != null && n.Player.Team[i].Loaded)
                                    {
                                        Messenger.PlayerMsg(client, "Team #" + i + ": " + Pokedex.GetPokemon(n.Player.Team[i].Species).Name, Text.Yellow);
                                        for (int j = 0; j < 4; j++)
                                        {
                                            if (n.Player.Team[i].Moves[j].MoveNum > 0)
                                            {
                                                Messenger.PlayerMsg(client, MoveManager.Moves[n.Player.Team[i].Moves[j].MoveNum].Name, Text.Yellow);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    #region Auction Commands
                    case "/masswarpauction":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                foreach (Client i in ClientManager.GetClients())
                                {
                                    Messenger.AskQuestion(i, "MassWarpAuction", client.Player.Name + " is inviting you to join an auction!  Would you like to play?", -1);
                                }
                            }
                        }
                        break;
                    case "/createauction":
                        {
                            if (client.Player.MapID == Auction.AUCTION_MAP)
                            {
                                if (!Auction.StaffAuction)
                                {
                                    Auction.CreateAuction(client);
                                }
                                else if (Auction.StaffAuction && Ranks.IsAllowed(client, Enums.Rank.Mapper))
                                {
                                    Auction.CreateAuction(client);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "A staff held auction is in progress. You may not create an auction at this time!", Text.BrightRed);
                                }

                            }

                        }
                        break;
                    case "/startauction":
                        {
                            Auction.StartAuction(client);
                        }
                        break;
                    case "/endauction":
                        {
                            Auction.EndAuction(client);
                        }
                        break;
                    case "/auctionadminhelp":
                        {
                            Auction.SayHelp(client);
                        }
                        break;
                    case "/auctionhelp":
                        {
                            Auction.SayPlayerHelp(client);
                        }
                        break;
                    case "/checkbidder":
                        {
                            Auction.CheckBidder(client);
                        }
                        break;
                    case "/setauctionitem":
                        {
                            Auction.SetAuctionItem(client, joinedArgs);
                        }
                        break;
                    case "/setauctionminbid":
                        {
                            Auction.SetAuctionMinBid(client, joinedArgs.ToInt());
                        }
                        break;
                    case "/setbidincrement":
                        {
                            Auction.SetBidIncrement(client, joinedArgs.ToInt());
                        }
                        break;
                    case "/bid":
                        {
                            if (client.Player.HasItem(1) >= joinedArgs.ToInt())
                            {
                                Auction.UpdateBids(client, joinedArgs.ToInt());
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You don't have enough Poké!", Text.BrightRed);
                            }
                        }
                        break;

                    #endregion Auction Commands
                    case "/givetokens":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client n;
                                n = ClientManager.FindClient(command[1]);
                                int numTokens = command[2].ToInt();

                                n.Player.GiveItem(133, numTokens);
                                Messenger.PlayerMsg(n, client.Player.Name + " has awarded you " + numTokens + " Arcade Tokens!", Text.BrightGreen);
                                Messenger.AdminMsg("[Staff] " + client.Player.Name + " has given " + n.Player.Name + " " + numTokens + " Arcade Tokens!", Text.BrightBlue);
                            }
                        }
                        break;
                    case "/setname*":
                    case "/setname":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Admin))
                            {
                                Client n;
                                string newName = command[2];
                                string[] subCommand = command[0].Split('*');
                                if (subCommand.Length > 1)
                                {
                                    n = ClientManager.FindClient(command[1], true); // Attempt to find our player if they have a difficult name
                                }
                                else
                                {
                                    n = ClientManager.FindClient(command[1]);
                                }

                                n.Player.Name = newName;
                                Messenger.SendPlayerData(n);
                            }
                        }
                        break;
                    case "/hb":
                        {
                            string playerMap = client.Player.MapID;
                            Messenger.MapMsg(playerMap, "H", Text.Blue);
                            Messenger.MapMsg(playerMap, "A", Text.Green);
                            Messenger.MapMsg(playerMap, "P", Text.Cyan);
                            Messenger.MapMsg(playerMap, "P", Text.Red);
                            Messenger.MapMsg(playerMap, "Y", Text.Magenta);
                            Messenger.MapMsg(playerMap, "-", Text.Grey);
                            Messenger.MapMsg(playerMap, "B", Text.Brown);
                            Messenger.MapMsg(playerMap, "I", Text.BrightBlue);
                            Messenger.MapMsg(playerMap, "R", Text.BrightGreen);
                            Messenger.MapMsg(playerMap, "T", Text.BrightCyan);
                            Messenger.MapMsg(playerMap, "H", Text.BrightRed);
                            Messenger.MapMsg(playerMap, "D", Text.Pink);
                            Messenger.MapMsg(playerMap, "A", Text.Yellow);
                            Messenger.MapMsg(playerMap, "Y", Text.Blue);
                            Messenger.MapMsg(playerMap, joinedArgs + "!", Text.White);
                            Messenger.PlaySoundToMap(client.Player.MapID, "magic7.wav");
                        }
                        break;
                    case "/eat":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Admin))
                            {
                                if (command.CommandArgs.Count >= 2)
                                {
                                    Client n = ClientManager.FindClient(joinedArgs);
                                    if (n != null)
                                    {
                                        Messenger.GlobalMsg(client.Player.Name + " has eaten " + n.Player.Name + "!", Text.Yellow);
                                        Messenger.PlayerWarp(n, 509, 11, 8);
                                        //} else if (n == index) {
                                        //    NetScript.PlayerMsg(index, "You cant eat yourself!", Text.BrightRed);
                                    }
                                    else if (n == null)
                                    {
                                        Messenger.PlayerMsg(client, "Player is offline.", Text.Grey);
                                    }
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "You have to pick somebody to eat!", Text.Black);
                                }
                            }
                        }
                        break;
                    case "/._.":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Admin))
                            {
                                if (command.CommandArgs.Count >= 2)
                                {
                                    Client n = ClientManager.FindClient(joinedArgs);
                                    if (n != null)
                                    {
                                        Messenger.GlobalMsg(client.Player.Name + " has stared into the eternal soul of " + n.Player.Name + "!", System.Drawing.Color.MidnightBlue);
                                        Messenger.PlayerWarp(n, 2000, 9, 6);
                                        //} else if (n == index) {
                                        //    NetScript.PlayerMsg(index, "You cant eat yourself!", Text.BrightRed);
                                    }
                                    else if (n == null)
                                    {
                                        Messenger.PlayerMsg(client, "Player is offline.", Text.Grey);
                                    }
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "You have to pick somebody to stare into the eternal soul of!", Text.Black);
                                }
                            }
                        }
                        break;
                    case "/reloadnews":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Server.Settings.LoadNews();
                                Messenger.PlayerMsg(client, "News have been reloaded!", Text.Yellow);
                            }
                        }
                        break;
                    case "/news":
                        {
                            Messenger.PlayerMsg(client, "Latest News:", Text.Yellow);
                            for (int i = 0; i < Server.Settings.News.Count; i++)
                            {
                                Messenger.PlayerMsg(client, Server.Settings.News[i], Text.Yellow);
                            }

                        }
                        break;
                    case "/givemove":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client n = ClientManager.FindClient(command[1]);
                                if (n != null)
                                {
                                    int moveNum;
                                    if (command[2].IsNumeric() && command[2].ToInt() > 1 && command[2].ToInt() <= MoveManager.Moves.MaxMoves)
                                    {
                                        moveNum = command[2].ToInt();
                                        n.Player.GetActiveRecruit().LearnNewMove(moveNum);
                                        Messenger.PlayerMsg(client, "You have taught " + n.Player.Name + " the move " + MoveManager.Moves[moveNum].Name, Text.Yellow);
                                        Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Move Given] " + client.Player.Name + " gave " + n.Player.Name + " the move " + MoveManager.Moves[moveNum].Name);

                                    }
                                    else
                                    {
                                        moveNum = -1;
                                        for (int i = 1; i <= MoveManager.Moves.MaxMoves; i++)
                                        {
                                            if (MoveManager.Moves[i].Name.ToLower().StartsWith(command[2].ToLower()))
                                            {
                                                moveNum = i;
                                            }
                                        }
                                        if (moveNum > -1)
                                        {

                                            n.Player.GetActiveRecruit().LearnNewMove(moveNum);
                                            Messenger.PlayerMsg(client, "You have taught " + n.Player.Name + " the move " + MoveManager.Moves[moveNum].Name, Text.Yellow);
                                            Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Move Given] " + client.Player.Name + " gave " + n.Player.Name + " the move " + MoveManager.Moves[moveNum].Name);
                                        }
                                    }
                                    Messenger.SendPlayerMoves(n);
                                }
                            }
                        }
                        break;
                    //global
                    //fakeadmin
                    //serverontime, obsolete
                    case "/hunt":
                        {
                            var map = client.Player.Map;

                            if (Ranks.IsAllowed(client, Enums.Rank.Mapper))
                            {
                                if (map.IsZoneOrObjectSandboxed() && client.Player.CanEditZone(map.ZoneID))
                                {
                                    if (client.Player.ProtectionOff)
                                    {
                                        client.Player.ProtectionOff = false;
                                        client.Player.Hunted = false;
                                        Messenger.PlayerMsg(client, "You are no longer hunted.", Text.BrightGreen);
                                    }
                                    else
                                    {
                                        client.Player.ProtectionOff = true;
                                        client.Player.Hunted = true;
                                        Messenger.PlayerMsg(client, "You are now hunted.", Text.BrightGreen);
                                    }
                                    PacketBuilder.AppendHunted(client, hitlist);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "You can't change hunted mode here.", Text.BrightRed);
                                }
                            }
                        }
                        break;
                    case "/learnmove":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter) || (client.Player.Map.IsZoneOrObjectSandboxed() && client.Player.GetActiveRecruit().InTempMode))
                            {
                                int move = command[1].ToInt();
                                if (move <= MoveManager.Moves.MaxMoves)
                                {
                                    client.Player.GetActiveRecruit().LearnNewMove(move);
                                    Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Move Given] " + client.Player.Name + " gave themselves the move: " + MoveManager.Moves[move].Name);
                                }
                                Messenger.SendPlayerMoves(client);
                            }
                        }
                        break;
                    case "/give":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client n = ClientManager.FindClient(command[1]);
                                int itemAmount = command[2].ToInt();
                                string item = command[3];
                                if (itemAmount == 0)
                                {
                                    Messenger.PlayerMsg(client, "Invalid item amount.", Text.BrightRed);
                                }
                                else if (n == null)
                                {
                                    Messenger.PlayerMsg(client, "Player is offline.", Text.Grey);
                                }
                                else
                                {
                                    if (item.IsNumeric())
                                    {
                                        if (item.ToInt() <= 0 || item.ToInt() > Server.Items.ItemManager.Items.MaxItems)
                                        {
                                            Messenger.PlayerMsg(client, "Invalid item number.", Text.BrightRed);
                                        }
                                        else
                                        {
                                            if (ItemManager.Items[item.ToInt()].StackCap <= 1)
                                            {
                                                for (int i = itemAmount; i > 0; i--)
                                                {
                                                    n.Player.GiveItem(item.ToInt(), 1);
                                                }
                                                Messenger.PlayerMsg(client, "You have given " + n.Player.Name + " " + itemAmount.ToString() + " " + ItemManager.Items[item.ToInt()].Name + "!", Text.Yellow);
                                                Messenger.PlayerMsg(n, client.Player.Name + " has given you " + itemAmount.ToString() + " " + ItemManager.Items[item.ToInt()].Name + "!", Text.Yellow);
                                                Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Item Given] " + client.Player.Name + " gave " + n.Player.Name + " " + itemAmount + " " + ItemManager.Items[item.ToInt()].Name);
                                            }
                                            else
                                            {
                                                n.Player.GiveItem(item.ToInt(), itemAmount);
                                                Messenger.PlayerMsg(client, "You have given " + n.Player.Name + " " + itemAmount.ToString() + " " + ItemManager.Items[item.ToInt()].Name + "!", Text.Yellow);
                                                Messenger.PlayerMsg(n, client.Player.Name + " has given you " + itemAmount.ToString() + " " + ItemManager.Items[item.ToInt()].Name + "!", Text.Yellow);
                                                Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Item Given] " + client.Player.Name + " gave " + n.Player.Name + " " + itemAmount + " " + ItemManager.Items[item.ToInt()].Name);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        int itemNum = -1;
                                        for (int i = Server.Items.ItemManager.Items.MaxItems; i > 0; i--)
                                        {
                                            if (ItemManager.Items[i].Name.ToLower().StartsWith(item.ToLower()))
                                            {
                                                itemNum = i;
                                            }
                                        }
                                        if (itemNum == -1)
                                        {
                                            Messenger.PlayerMsg(client, "Unable to find an item that starts with " + item, Text.Yellow);
                                        }
                                        else
                                        {
                                            n.Player.GiveItem(itemNum, itemAmount);
                                            Messenger.PlayerMsg(client, "You have given " + n.Player.Name + " " + itemAmount.ToString() + " " + ItemManager.Items[item.ToInt()].Name + "!", Text.Yellow);
                                            Messenger.PlayerMsg(n, client.Player.Name + " has given you " + itemAmount.ToString() + " " + ItemManager.Items[item.ToInt()].Name + "!", Text.Yellow);
                                            Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Item Given] " + client.Player.Name + " gave " + n.Player.Name + " " + itemAmount + " " + ItemManager.Items[item.ToInt()].Name);
                                        }
                                    }
                                }


                            }
                        }
                        break;
                    case "/take":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client n = ClientManager.FindClient(command[1]);
                                int itemAmount = command[2].ToInt();
                                string item = command[3];
                                if (itemAmount == 0)
                                {
                                    Messenger.PlayerMsg(client, "Invalid item amount.", Text.BrightRed);
                                }
                                else if (n == null)
                                {
                                    Messenger.PlayerMsg(client, "Player is offline.", Text.Grey);
                                }
                                else
                                {
                                    if (item.IsNumeric())
                                    {
                                        if (item.ToInt() <= 0 || item.ToInt() > Server.Items.ItemManager.Items.MaxItems)
                                        {
                                            Messenger.PlayerMsg(client, "Invalid item number.", Text.BrightRed);
                                        }
                                        else
                                        {
                                            n.Player.TakeItem(item.ToInt(), itemAmount);
                                            Messenger.PlayerMsg(client, "You have taken " + itemAmount.ToString() + " " + ItemManager.Items[item.ToInt()].Name + " from " + n.Player.Name + "!", Text.Yellow);
                                            Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Item Taken] " + client.Player.Name + " took " + itemAmount + " " + ItemManager.Items[item.ToInt()].Name + " from " + n.Player.Name);
                                            //NetScript.PlayerMsg(player, NetScript.GetPlayerName(index) + " has given you " +  itemAmount.ToString() + " " + NetScript.GetItemName(item.ToInt()) + "!", Text.Yellow);
                                        }
                                    }
                                    else
                                    {
                                        int itemNum = -1;
                                        for (int i = Server.Items.ItemManager.Items.MaxItems; i > 0; i--)
                                        {
                                            if (ItemManager.Items[i].Name.ToLower().StartsWith(item.ToLower()))
                                            {
                                                itemNum = i;
                                            }
                                        }
                                        if (itemNum == -1)
                                        {
                                            Messenger.PlayerMsg(client, "Unable to find an item that starts with " + item, Text.Yellow);
                                        }
                                        else
                                        {
                                            n.Player.TakeItem(itemNum, itemAmount);
                                            Messenger.PlayerMsg(client, "You have taken " + itemAmount.ToString() + " " + ItemManager.Items[item.ToInt()].Name + " from " + n.Player.Name + "!", Text.Yellow);
                                            Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Item Taken] " + client.Player.Name + " took " + itemAmount + " " + ItemManager.Items[item.ToInt()].Name + " from " + n.Player.Name);
                                            //NetScript.PlayerMsg(player, NetScript.GetPlayerName(index) + " has given you " +  itemAmount.ToString() + " " + NetScript.GetItemName(itemNum) + "!", Text.Yellow);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case "/save":
                        {
                            if (client.Player.SavingLocked == false)
                            {
                                using (DatabaseConnection dbConnection = new DatabaseConnection(DatabaseID.Players))
                                {
                                    client.Player.SaveCharacterData(dbConnection);
                                }
                                Messenger.PlayerMsg(client, "You have saved the game!", Text.BrightGreen);
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You cannot save right now!", Text.BrightRed);
                            }
                        }
                        break;
                    case "/p":
                        {
                            if (!string.IsNullOrEmpty(client.Player.PartyID))
                            {
                                var message = joinedArgs = OnChatMessageRecieved(client, joinedArgs, Enums.ChatMessageType.Guild);

                                var party = PartyManager.FindPlayerParty(client);
                                foreach (var member in party.GetOnlineMemberClients())
                                {
                                    Messenger.PlayerMsg(member, $"[Party] {client.Player.DisplayName}: {message}", System.Drawing.Color.MediumSpringGreen);
                                }
                            }
                        }
                        break;
                    case "/g":
                        {
                            if (!string.IsNullOrEmpty(client.Player.GuildName) && !string.IsNullOrEmpty(joinedArgs) && client.Player.Muted == false)
                            {
                                joinedArgs = OnChatMessageRecieved(client, joinedArgs, Enums.ChatMessageType.Guild);
                                Server.Logging.ChatLogger.AppendToChatLog("Guild Chat/" + client.Player.GuildName, client.Player.Name + ": " + joinedArgs);
                                foreach (Client i in ClientManager.GetClients())
                                {
                                    if (i.IsPlaying() && i.Player.GuildName == client.Player.GuildName)
                                    {
                                        Messenger.PlayerMsg(i, client.Player.Name + " [" + client.Player.GuildName + "]: " + joinedArgs, System.Drawing.Color.MediumSpringGreen);
                                    }
                                }

                            }
                        }
                        break;
                    case "/finditemend":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {
                                int itemsFound = 0;
                                for (int i = 0; i < Server.Items.ItemManager.Items.MaxItems; i++)
                                {
                                    if (ItemManager.Items[i].Name.ToLower().EndsWith(joinedArgs.ToLower()))
                                    {
                                        Messenger.PlayerMsg(client, ItemManager.Items[i].Name + "'s number is " + i.ToString(), Text.Yellow);
                                        itemsFound++;
                                        //return;
                                    }
                                }
                                if (itemsFound == 0)
                                {
                                    Messenger.PlayerMsg(client, "Unable to find an item that starts with '" + joinedArgs + "'", Text.Yellow);
                                }
                            }
                        }
                        break;
                    case "/finditem":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {
                                int itemsFound = 0;
                                if (String.IsNullOrEmpty(joinedArgs))
                                {
                                    Messenger.PlayerMsg(client, "Type in an item name.", Text.Yellow);
                                }
                                else
                                {
                                    for (int i = 0; i < Server.Items.ItemManager.Items.MaxItems; i++)
                                    {
                                        if (ItemManager.Items[i].Name.ToLower().Contains(joinedArgs.ToLower()))
                                        {
                                            Messenger.PlayerMsg(client, ItemManager.Items[i].Name + "'s number is " + i.ToString(), Text.Yellow);
                                            itemsFound++;
                                            //return;
                                        }
                                    }
                                    if (itemsFound == 0)
                                    {
                                        Messenger.PlayerMsg(client, "Unable to find an item that starts with '" + joinedArgs + "'", Text.Yellow);
                                    }
                                }
                            }
                        }
                        break;
                    case "/reloaddex":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Developer))
                            {
                                Messenger.PlayerMsg(client, "Loading Pokedex...", Text.Yellow);
                                Pokedex.Initialize();
                                Pokedex.LoadAllPokemon();
                                Messenger.PlayerMsg(client, "Loading evolutions...", Text.Yellow);
                                EvolutionManager.Initialize();
                                EvolutionManager.LoadEvos(null);
                                Messenger.PlayerMsg(client, "Loading moves...", Text.Yellow);
                                MoveManager.Initialize();
                                MoveManager.LoadMoves(null);
                                Messenger.PlayerMsg(client, "Loading items...", Text.Yellow);
                                ItemManager.Initialize();
                                ItemManager.LoadItems(null);
                                Messenger.PlayerMsg(client, "Reload complete.", Text.Yellow);
                            }

                        }
                        break;
                    case "/findnpc":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {
                                int npcsFound = 0;
                                if (String.IsNullOrEmpty(joinedArgs))
                                {
                                    Messenger.PlayerMsg(client, "Type in an npc name.", Text.Yellow);
                                }
                                else
                                {
                                    for (int i = 1; i <= Server.Npcs.NpcManager.Npcs.MaxNpcs; i++)
                                    {
                                        if (NpcManager.Npcs[i].Name.ToLower().Contains(joinedArgs.ToLower()))
                                        {
                                            Messenger.PlayerMsg(client, NpcManager.Npcs[i].Name + "'s number is " + i.ToString(), Text.Yellow);
                                            npcsFound++;
                                            //return;
                                        }
                                    }
                                    if (npcsFound == 0)
                                    {
                                        Messenger.PlayerMsg(client, "Unable to find an npc that starts with '" + joinedArgs + "'", Text.Yellow);
                                    }
                                }
                            }
                        }
                        break;
                    case "/finddex":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {
                                int npcsFound = 0;
                                for (int i = 1; i <= Constants.TOTAL_POKEMON; i++)
                                {
                                    if (Pokedex.GetPokemon(i).Name.ToLower().Contains(joinedArgs.ToLower()))
                                    {
                                        foreach (PokemonForm form in Pokedex.GetPokemon(i).Forms)
                                        {
                                            Messenger.PlayerMsg(client, Pokedex.GetPokemon(i).Name + "'s dex number is " + i, Text.Yellow);
                                        }
                                        npcsFound++;
                                        //return;
                                    }
                                }
                                if (npcsFound == 0)
                                {
                                    Messenger.PlayerMsg(client, "Unable to find an Pokemon that starts with '" + joinedArgs + "'", Text.Yellow);
                                }
                            }
                        }
                        break;
                    case "/findmove":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {
                                int movesFound = 0;
                                for (int i = 1; i <= MoveManager.Moves.MaxMoves; i++)
                                {
                                    if (MoveManager.Moves[i].Name.ToLower().Contains(joinedArgs.ToLower()))
                                    {
                                        Messenger.PlayerMsg(client, MoveManager.Moves[i].Name + "'s number is " + i.ToString(), Text.Yellow);
                                        movesFound++;
                                        //return;
                                    }
                                }
                                if (movesFound == 0)
                                {
                                    Messenger.PlayerMsg(client, "Unable to find a move that starts with '" + joinedArgs + "'", Text.Yellow);
                                }
                            }
                        }
                        break;
                    case "/findmoverange":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter) && joinedArgs.IsNumeric())
                            {
                                Enums.MoveRange range = (Enums.MoveRange)joinedArgs.ToInt();
                                int movesFound = 0;
                                Messenger.PlayerMsg(client, "Moves with " + range + " range", Text.Yellow);
                                for (int i = 1; i <= MoveManager.Moves.MaxMoves; i++)
                                {
                                    if (MoveManager.Moves[i].RangeType == range && !String.IsNullOrEmpty(MoveManager.Moves[i].Name))
                                    {
                                        Messenger.PlayerMsg(client, "#" + i + ": " + MoveManager.Moves[i].Name, Text.Yellow);
                                        movesFound++;
                                        //return;
                                    }
                                }
                                if (movesFound == 0)
                                {
                                    Messenger.PlayerMsg(client, "[None]", Text.Yellow);
                                }
                            }
                        }
                        break;
                    case "/findrdungeon":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                int itemsFound = 0;
                                for (int i = 0; i < RDungeonManager.RDungeons.Count; i++)
                                {
                                    if (RDungeonManager.RDungeons[i].DungeonName.ToLower().Contains(joinedArgs.ToLower()))
                                    {
                                        Messenger.PlayerMsg(client, RDungeonManager.RDungeons[i].DungeonName + "'s number is " + (i + 1).ToString(), Text.Yellow);
                                        itemsFound++;
                                        //return;
                                    }
                                }
                                if (itemsFound == 0)
                                {
                                    Messenger.PlayerMsg(client, "Unable to find a random dungeon that starts with '" + joinedArgs + "'", Text.Yellow);
                                }
                            }
                        }
                        break;
                    case "/finddungeon":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                int itemsFound = 0;
                                for (int i = 1; i < DungeonManager.Dungeons.Count; i++)
                                {
                                    if (DungeonManager.Dungeons[i].Name.ToLower().Contains(joinedArgs.ToLower()))
                                    {
                                        Messenger.PlayerMsg(client, DungeonManager.Dungeons[i].Name + "'s number is " + i.ToString(), Text.Yellow);
                                        itemsFound++;
                                        //return;
                                    }
                                }
                                if (itemsFound == 0)
                                {
                                    Messenger.PlayerMsg(client, "Unable to find a dungeon that starts with '" + joinedArgs + "'", Text.Yellow);
                                }
                            }
                        }
                        break;
                    case "/addfriend":
                        {
                            client.Player.AddFriend(joinedArgs);
                        }
                        break;
                    case "/removefriend":
                        {
                            client.Player.RemoveFriend(joinedArgs);
                        }
                        break;
                    case "/block":
                        {
                            client.Player.BlockPlayer(joinedArgs);
                        }
                        break;
                    case "/unblock":
                        {
                            client.Player.UnblockPlayer(joinedArgs);
                        }
                        break;
                    //storytile
                    //clearstorytile
                    case "/unlockstory":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Mapper))
                            {
                                client.Player.SetStoryState(command[1].ToInt() - 1, false);
                                Messenger.PlayerMsg(client, "Chapter " + (command[1].ToInt()) + " has been unlocked!", Text.Yellow);
                            }
                        }
                        break;
                    case "/storystate":
                        {
                            var inputChapter = command[1].ToInt();
                            var chapter = inputChapter - 1;

                            var story = StoryManager.Stories[chapter];
                            var chapterState = client.Player.GetStoryState(chapter);

                            Messenger.PlayerMsg(client, $"Chapter #{inputChapter} ({story.Name}) is currently {(chapterState ? "locked" : "unlocked")}.", Text.Green);
                        }
                        break;
                    case "/setstorystate":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Mapper))
                            {
                                client.Player.SetStoryState(command[1].ToInt() - 1, command[2].ToBool());
                                Messenger.PlayerMsg(client, "Chapter " + (command[1].ToInt()) + " has been set!", Text.Yellow);
                            }
                        }
                        break;
                    case "/resetstats":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client n = ClientManager.FindClient(joinedArgs);
                                if (n != null)
                                {
                                    n.Player.Team[0].AtkBonus = 0;
                                    n.Player.Team[0].DefBonus = 0;
                                    n.Player.Team[0].SpclAtkBonus = 0;
                                    n.Player.Team[0].SpclDefBonus = 0;
                                    n.Player.Team[0].SpdBonus = 0;
                                    Messenger.PlayerMsg(client, "Stats have been reset for " + n.Player.Name + "!", Text.Green);
                                    Messenger.PlayerMsg(n, "Your stats have been reset!", Text.Green);
                                }
                            }
                        }
                        break;
                    case "/teststory":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Mapper))
                            {
                                client.Player.SetStoryState(joinedArgs.ToInt() - 1, false);

                                StoryManager.PlayStory(client, joinedArgs.ToInt() - 1);
                            }
                        }
                        break;
                    case "/teststorybreak":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Mapper))
                            {
                                Messenger.ForceEndStoryTo(client);
                                client.Player.MovementLocked = false;
                            }
                        }
                        break;
                    case "/mute*":
                    case "/mute":
                        {
                            Client n;
                            string[] subCommand = command[0].Split('*');
                            if (subCommand.Length > 1)
                            {
                                n = ClientManager.FindClient(joinedArgs, true);
                            }
                            else
                            {
                                n = ClientManager.FindClient(joinedArgs);
                            }
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter) && n != null)
                            {
                                n.Player.Muted = true;
                                n.Player.Status = "MUTED";
                                Messenger.PlayerMsg(n, "You have been muted.", Text.Green);
                                Messenger.AdminMsg("[Staff] " + client.Player.Name + " has muted " + n.Player.Name + ".", Text.BrightBlue);
                                Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Mute Issued] " + client.Player.Name + " muted " + n.Player.Name);

                                Messenger.SendPlayerData(n);
                            }
                        }
                        break;
                    case "/permamute*":
                    case "/permamute":
                        {
                            try
                            {
                                if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                                {
                                    string playerName = command[1];
                                    string muteTimeDays = "-----";
                                    Client bannedClient;

                                    string[] subCommand = command[0].Split('*');

                                    if (subCommand.Length > 1)
                                    {
                                        bannedClient = ClientManager.FindClient(playerName, true);
                                    }
                                    else
                                    {
                                        bannedClient = ClientManager.FindClient(playerName);
                                    }

                                    if (command.CommandArgs.Count > 2 && command[2].IsNumeric())
                                    {
                                        muteTimeDays = DateTime.Now.AddDays(Convert.ToDouble(command[2])).ToString();
                                    }

                                    if (bannedClient != null)
                                    {
                                        bannedClient.Player.Muted = true;
                                        bannedClient.Player.Status = "MUTED";
                                        Messenger.PlayerMsg(bannedClient, "You have been permamuted.", Text.Green);
                                        Messenger.AdminMsg("[Staff] " + client.Player.Name + " has permamuted " + bannedClient.Player.Name + ".", Text.BrightBlue);
                                        Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Permamute Issued] " + client.Player.Name + " muted " + bannedClient.Player.Name);
                                        Messenger.SendPlayerData(bannedClient);
                                        using (DatabaseConnection dbConnection = new DatabaseConnection(DatabaseID.Players))
                                        {
                                            Bans.BanPlayer(dbConnection, bannedClient.IP.ToString(), bannedClient.Player.CharID,
                                                bannedClient.Player.AccountName + "/" + bannedClient.Player.Name, bannedClient.MacAddress, bannedClient.BiosId,
                                                client.Player.CharID, client.IP.ToString(), muteTimeDays, Enums.BanType.Mute);
                                        }
                                    }
                                    else
                                    {
                                        using (DatabaseConnection dbConnection = new DatabaseConnection(DatabaseID.Players))
                                        {
                                            IDataColumn[] columns = dbConnection.Database.RetrieveRow("characteristics", "CharID", "Name=\"" + playerName + "\"");
                                            if (columns != null)
                                            {
                                                string charID = (string)columns[0].Value;
                                                string foundIP = (string)dbConnection.Database.RetrieveRow("character_statistics", "LastIPAddressUsed", "CharID=\"" + charID + "\"")[0].Value;
                                                string foundMac = (string)dbConnection.Database.RetrieveRow("character_statistics", "LastMacAddressUsed", "CharID=\"" + charID + "\"")[0].Value;
                                                string storedUUID = (string)dbConnection.Database.RetrieveRow("character_statistics", "StoredUUID", "CharID=\"" + charID + "\"")[0].Value;
                                                //get previous IP and mac
                                                Bans.BanPlayer(dbConnection, foundIP, charID, playerName, foundMac, storedUUID,
                                                    client.Player.CharID, client.IP.ToString(), muteTimeDays, Enums.BanType.Mute);
                                                Messenger.AdminMsg("[Staff] " + client.Player.Name + " has permamuted " + bannedClient.Player.Name + ".", Text.BrightBlue);
                                                Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Permamute Issued] " + client.Player.Name + " muted " + bannedClient.Player.Name);
                                            }
                                            else
                                            {
                                                Messenger.PlayerMsg(client, "Unable to find player!", Text.BrightRed);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Messenger.AdminMsg("Error: Permamute", Text.White);
                                Messenger.AdminMsg(ex.ToString(), Text.White);
                            }
                        }
                        break;
                    case "/unmute*":
                    case "/unmute":
                        {
                            Client n;
                            string[] subCommand = command[0].Split('*');
                            if (subCommand.Length > 1)
                            {
                                n = ClientManager.FindClient(joinedArgs, true);
                            }
                            else
                            {
                                n = ClientManager.FindClient(joinedArgs);
                            }
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter) && n != null)
                            {
                                n.Player.Muted = false;
                                n.Player.Status = "";
                                Messenger.PlayerMsg(n, "You have been unmuted.", Text.Green);
                                Messenger.AdminMsg("[Staff] " + client.Player.Name + " has unmuted " + n.Player.Name + ".", Text.BrightBlue);
                                Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Unmute] " + client.Player.Name + " unmuted " + n.Player.Name);
                                Messenger.SendPlayerData(n);
                                using (DatabaseConnection dbConnection = new DatabaseConnection(DatabaseID.Players))
                                {
                                    Bans.RemoveBan(dbConnection, Enums.BanMethod.PlayerID, n.Player.CharID);
                                }
                            }
                        }
                        break;
                    case "/tostart*":
                    case "/tostart":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                string playerName = joinedArgs;
                                Client n;
                                string[] subCommand = command[0].Split('*');
                                if (subCommand.Length > 1)
                                {
                                    n = ClientManager.FindClient(playerName, true);
                                }
                                else
                                {
                                    n = ClientManager.FindClient(playerName);
                                }
                                if (n != null)
                                {
                                    Messenger.PlayerWarp(n, 1015, 25, 25);
                                    n.Player.Dead = false;
                                    PacketBuilder.AppendDead(n, hitlist);
                                    Messenger.PlayerMsg(n, "You have been warped to the crossroads by " + client.Player.Name + "!", Text.BrightGreen);
                                    Messenger.PlayerMsg(client, n.Player.Name + " has been warped to the crossroads!", Text.BrightGreen);
                                    Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Warp Event] " + client.Player.Name + " warped " + n.Player.Name + " to The Crossroads.");
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Player is offline.", Text.Grey);
                                }
                            }
                        }
                        break;
                    case "/warpto":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Mapper))
                            {
                                var map = MapManager.RetrieveMap(command[1].ToInt());

                                if ((map.IsZoneOrObjectSandboxed() && client.Player.CanViewZone(map.ZoneID)) || Ranks.IsAllowed(client, Enums.Rank.Scripter))
                                {
                                    Messenger.PlayerWarp(client, command[1].ToInt(), client.Player.X, client.Player.Y);
                                    Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Warp Event] " + client.Player.Name + " warped to map " + command[1] + " - " + client.Player.Map.Name);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Unable to warp. The destination must be a sandboxed map that you can access.", Text.BrightRed);
                                }
                            }
                        }
                        break;
                    case "/warpmeto*":
                    case "/warpmeto":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Mapper))
                            {
                                Client target;
                                string[] subCommand = command[0].Split('*');
                                if (subCommand.Length > 1)
                                {
                                    target = ClientManager.FindClient(joinedArgs, true);
                                }
                                else
                                {
                                    target = ClientManager.FindClient(joinedArgs);
                                }
                                if (target != null)
                                {
                                    var targetMap = target.Player.GetCurrentMap();
                                    if ((targetMap.IsZoneOrObjectSandboxed() && client.Player.CanViewZone(targetMap.ZoneID) || Ranks.IsAllowed(client, Enums.Rank.Scripter)) /*|| Ranks.IsAllowed(client, Enums.Rank.Mapper)*/)
                                    {
                                        Messenger.PlayerWarp(client, targetMap, target.Player.X, target.Player.Y);
                                        Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Warp Event] " + client.Player.Name + " warped to " + target.Player.Name + " on map: " + target.Player.MapID + " - " + target.Player.Map.Name);
                                    }
                                    else
                                    {
                                        Messenger.PlayerMsg(client, "Unable to warp. The destination must be a sandboxed map that you can access.", Text.BrightRed);
                                    }
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Player could not be found.", Text.Green);
                                }
                            }
                        }
                        break;
                    case "/warptome*":
                    case "/warptome":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client target;
                                string[] subCommand = command[0].Split('*');
                                if (subCommand.Length > 1)
                                {
                                    target = ClientManager.FindClient(joinedArgs, true);
                                }
                                else
                                {
                                    target = ClientManager.FindClient(joinedArgs);
                                }
                                if (target != null)
                                {
                                    Messenger.PlayerWarp(target, client.Player.GetCurrentMap(), client.Player.X, client.Player.Y);
                                    Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Warp Event] " + client.Player.Name + " warped " + target.Player.Name + " to them on map:" + client.Player.MapID + " - " + client.Player.Map.Name);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Player could not be found.", Text.Green);
                                }
                            }
                        }
                        break;
                    case "/map*":
                    case "/map":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                string playerName = command[1];
                                Client n;
                                string[] subCommand = command[0].Split('*');
                                if (subCommand.Length > 1)
                                {
                                    n = ClientManager.FindClient(playerName, true);
                                }
                                else
                                {
                                    n = ClientManager.FindClient(playerName);
                                }

                                if (n != null)
                                {
                                    Messenger.PlayerMsg(client, n.Player.Name + " is at Map " + n.Player.MapID, Text.Green);
                                    Messenger.PlayerMsg(client, n.Player.Map.Name, Text.Green);
                                    Messenger.PlayerMsg(client, n.Player.X + ", " + n.Player.Y, Text.Green);
                                    Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Info Request] " + client.Player.Name + " checked " + n.Player.Name + "'s map.");
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, playerName + " could not be found.", Text.Green);
                                }
                            }
                        }
                        break;
                    case "/playerindungeon":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                if (command[1].IsNumeric())
                                {
                                    foreach (Client i in ClientManager.GetClients())
                                    {
                                        if (i.IsPlaying() && i.Player.Map.MapType == Enums.MapType.RDungeonMap
                                            && ((RDungeonMap)i.Player.Map).RDungeonIndex == command[1].ToInt() - 1)
                                        {
                                            Messenger.PlayerMsg(client, i.Player.Name + " is on Floor " + (((RDungeonMap)i.Player.Map).RDungeonFloor + 1), Text.BrightCyan);
                                        }
                                    }
                                }
                                else
                                {

                                }
                            }
                        }
                        break;
                    case "/playerindungeons":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                int total = 0;
                                foreach (Client i in ClientManager.GetClients())
                                {
                                    if (i.IsPlaying() && i.Player.Map.Moral == Enums.MapMoral.None)
                                    {

                                        Messenger.PlayerMsg(client, i.Player.Name + " is at " + i.Player.Map.Name, Text.BrightCyan);
                                        total++;
                                    }
                                }
                                Messenger.PlayerMsg(client, "Total: " + total, Text.BrightCyan);

                            }
                        }
                        break;
                    case "/info*":
                    case "/info":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client n;
                                string[] subCommand = command[0].Split('*');
                                if (subCommand.Length > 1)
                                {
                                    n = ClientManager.FindClient(joinedArgs, true);
                                }
                                else
                                {
                                    n = ClientManager.FindClient(joinedArgs);
                                }

                                if (n != null)
                                {
                                    Messenger.PlayerMsg(client, "Account: " + n.Player.AccountName + ", Name: " + n.Player.Name, Text.Yellow);
                                    for (int i = 0; i < Constants.MAX_ACTIVETEAM; i++)
                                    {
                                        if (n.Player.Team[i] != null && n.Player.Team[i].Loaded)
                                        {
                                            Messenger.PlayerMsg(client, "Team #" + i + ": " + Pokedex.GetPokemon(n.Player.Team[i].Species).Name + " Lv." + n.Player.Team[i].Level, Text.Yellow);
                                            Messenger.PlayerMsg(client, "HP: " + n.Player.Team[i].HP + "/" + n.Player.Team[i].MaxHP, Text.Yellow);
                                            Messenger.PlayerMsg(client, "Exp: " + n.Player.Team[i].Exp + "/" + n.Player.Team[i].GetNextLevel(), Text.Yellow);
                                            Messenger.PlayerMsg(client, "Atk/Sp.Atk: " + n.Player.Team[i].Atk + "/" + n.Player.Team[i].SpclAtk, Text.Yellow);
                                            Messenger.PlayerMsg(client, "Def/Sp.Def: " + n.Player.Team[i].Def + "/" + n.Player.Team[i].SpclDef, Text.Yellow);
                                            Messenger.PlayerMsg(client, "Speed: " + n.Player.Team[i].Spd, Text.Yellow);
                                        }
                                    }
                                    Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Info Request] " + client.Player.Name + " checked " + n.Player.Name + "'s team information.");
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Player is offline", Text.Grey);
                                }
                            }
                        }
                        break;
                    case "/statusinfo":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {

                                Client n = ClientManager.FindClient(joinedArgs);
                                if (n != null)
                                {

                                    for (int i = 0; i < Constants.MAX_ACTIVETEAM; i++)
                                    {
                                        if (n.Player.Team[i] != null && n.Player.Team[i].Loaded)
                                        {
                                            Messenger.PlayerMsg(client, "Team #" + i + ": " + Pokedex.GetPokemon(n.Player.Team[i].Species).Name + "/" + n.Player.Team[i].StatusAilment, Text.Yellow);
                                            for (int j = 0; j < n.Player.Team[i].VolatileStatus.Count; j++)
                                            {
                                                Messenger.PlayerMsg(client, n.Player.Team[i].VolatileStatus[j].Name +
                                                    "/" + n.Player.Team[i].VolatileStatus[j].Counter + "/" + n.Player.Team[i].VolatileStatus[j].Tag, Text.Yellow);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Player is offline", Text.Grey);
                                }
                            }
                        }
                        break;
                    case "/getip*":
                    case "/getip":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {

                                string playerName = command[1];
                                Client n;
                                string[] subCommand = command[0].Split('*');
                                if (subCommand.Length > 1)
                                {
                                    n = ClientManager.FindClient(playerName, true);
                                }
                                else
                                {
                                    n = ClientManager.FindClient(playerName);
                                }

                                if (n != null)
                                {
                                    if (Ranks.IsAllowed(n, Enums.Rank.ServerHost))
                                    {
                                        Messenger.PlayerMsg(client, n.Player.Name + "'s IP: 46.4.166.141", Text.Yellow);
                                    }
                                    else
                                    {
                                        Messenger.PlayerMsg(client, n.Player.Name + "'s IP: " + n.IP.ToString(), Text.Yellow);
                                    }
                                    Messenger.PlayerMsg(client, n.Player.Name + "'s MAC: " + n.MacAddress, Text.Yellow);
                                    Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Info Request] " + client.Player.Name + " checked " + n.Player.Name + "'s IP/MAC.");

                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Player is offline", Text.Grey);
                                }
                            }
                        }
                        break;
                    case "/findip":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Messenger.PlayerMsg(client, "Searching for players with the IP: \"" + joinedArgs + "\"", Text.BrightBlue);
                                foreach (Client n in ClientManager.GetClients())
                                {
                                    if (n.IsPlaying())
                                    {
                                        if (n.IP.ToString().StartsWith(joinedArgs))
                                        {
                                            Messenger.PlayerMsg(client, n.Player.AccountName + "/" + n.Player.Name + ": " + n.IP.ToString(), Text.BrightGreen);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    //getindex; no indexes
                    //poke, hug, praise, yawn, wave
                    case "/praise":
                        {
                            if (client.Player.Muted == false)
                            {
                                Messenger.MapMsg(client.Player.MapID, client.Player.Name + " gave praise to " + joinedArgs + "!", Text.Green);
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You have been muted!", Text.BrightRed);
                            }
                        }
                        break;
                    case "/hug":
                        {
                            if (client.Player.Muted == false)
                            {
                                if (command.CommandArgs.Count >= 2)
                                {
                                    Messenger.MapMsg(client.Player.MapID, client.Player.Name + " has hugged " + command[1] + "!", Text.White);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "You have to pick somebody to hug!", Text.BrightRed);
                                }
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You have been muted!", Text.BrightRed);
                            }
                        }
                        break;

                    case "/notepad":
                        {
                            if (client.Player.Muted == false)
                            {
                                if (command.CommandArgs.Count >= 2)
                                {
                                    if (command[1].ToLower() != "artmax")
                                    {
                                        Messenger.MapMsg(client.Player.MapID, client.Player.Name + " threw a notepad at " + command[1] + "!", Text.Yellow);
                                    }
                                    else
                                    {
                                        Messenger.MapMsg(client.Player.MapID, client.Player.Name + " threw a notepad at " + command[1] + " (nuclear strike!)", Text.Yellow);
                                    }
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "You have to pick somebody to throw a notepad at!", Text.BrightRed);
                                }
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You have been muted!", Text.BrightRed);
                            }
                        }
                        break;
                    case "/time":
                        {
                            Messenger.PlayerMsg(client, "It is currently: " + Server.Globals.ServerTime, Text.BrightGreen);
                        }
                        break;

                    case "/me":
                        {
                            if (client.Player.Muted == false)
                            {
                                if (command.CommandArgs.Count >= 2)
                                {
                                    Messenger.MapMsg(client.Player.MapID, client.Player.Name + " " + joinedArgs, Text.BrightBlue);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "You have to include something to say besides your name!", Text.BrightRed);
                                }
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You have been muted!", Text.BrightRed);
                            }
                        }
                        break;

                    case "/poke":
                        {
                            if (client.Player.Muted == false)
                            {
                                if (command.CommandArgs.Count >= 2)
                                {
                                    Messenger.MapMsg(client.Player.MapID, client.Player.Name + " poked " + command[1] + ".", Text.Yellow);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "You have to pick somebody to poke!", Text.BrightRed);
                                }
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You have been muted!", Text.BrightRed);
                            }
                        }
                        break;
                    case "/yawn":
                        {
                            if (client.Player.Muted == false)
                            {
                                Messenger.MapMsg(client.Player.MapID, client.Player.Name + " let out a loud yawn " + joinedArgs + "~", Text.BrightBlue);
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You have been muted!", Text.BrightRed);
                            }
                        }
                        break;
                    case "/wave":
                        {
                            if (client.Player.Muted == false)
                            {
                                Messenger.MapMsg(client.Player.MapID, client.Player.Name + " waved at " + joinedArgs + ".", Text.BrightGreen);
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You have been muted!", Text.BrightRed);
                            }
                        }
                        break;
                    case "/away":
                        {
                            if (client.Player.Muted == true)
                            {
                                Messenger.PlayerMsg(client, "You have been muted!", Text.BrightRed);
                            }
                            else if (client.Player.MapID == "s791" || client.Player.MapID == "s792")
                            {
                                Messenger.PlayerMsg(client, "You cannot be away while playing Capture The Flag!", Text.BrightRed);
                            }
                            else if (client.Player.Status.ToLower() == "away")
                            {
                                client.Player.Status = "";
                                Messenger.GlobalMsg(client.Player.Name + " has returned from being away.", Text.Yellow);
                                Messenger.SendPlayerData(client);
                            }
                            else
                            {
                                client.Player.Status = "Away";
                                Messenger.GlobalMsg(client.Player.Name + " is now away.", Text.Yellow);
                                Messenger.SendPlayerData(client);
                            }
                        }
                        break;
                    case "/wb*":
                    case "/wb":
                        {
                            if (client.Player.Muted == false)
                            {
                                if (command.CommandArgs.Count >= 2)
                                {
                                    Client n;
                                    string[] subCommand = command[0].Split('*');
                                    if (subCommand.Length > 1)
                                    {
                                        n = ClientManager.FindClient(joinedArgs, true);
                                    }
                                    else
                                    {
                                        n = ClientManager.FindClient(joinedArgs);
                                    }
                                    if (n != null)
                                    {
                                        Messenger.MapMsg(client.Player.MapID, client.Player.Name + " welcomes " + n.Player.Name + " back to Shift!", Text.White);
                                    }
                                    else if (n == null)
                                    {
                                        Messenger.PlayerMsg(client, "Player is offline.", Text.Grey);
                                    }
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Pick someone to welcome back.", Text.Black);
                                }
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You have been muted!", Text.BrightRed);
                            }
                        }
                        break;
                    case "/pichu!":
                        {
                            if (client.Player.Muted == false)
                            {
                                if (client.Player.GetActiveRecruit().Species == 172)
                                {
                                    Messenger.PlaySoundToMap(client.Player.MapID, "Pichu!.wav");
                                }
                            }
                        }
                        break;
                    case "/muwaha":
                        {
                            if (client.Player.Muted == false)
                            {
                                Messenger.PlaySoundToMap(client.Player.MapID, "magic1268.wav");
                            }
                        }
                        break;
                    case "/status":
                        {
                            if (client.Player.Muted == true)
                            {
                                Messenger.PlayerMsg(client, "You have been muted!", Text.BrightRed);
                            }
                            else if (exPlayer.Get(client).InCTF == false && exPlayer.Get(client).InSnowballGame == false
                                       && joinedArgs != "MUTED")
                            {
                                if (!string.IsNullOrEmpty(joinedArgs))
                                {
                                    string status = joinedArgs;
                                    if (joinedArgs.Length > 10)
                                    {
                                        status = joinedArgs.Substring(0, 10);
                                    }
                                    client.Player.Status = status;
                                    Messenger.SendPlayerData(client);
                                }
                                else
                                {
                                    client.Player.Status = "";
                                    Messenger.SendPlayerData(client);
                                }
                            }
                        }
                        break;
                    case "/gup":
                    case "/giveup":
                        {
                            GiveUp(client);
                        }
                        break;
                    case "/watch":
                        {
                            if (client.Player.MapID == MapManager.GenerateMapID(660) || client.Player.MapID == MapManager.GenerateMapID(1718))
                            {
                                TcpPacket packet = new TcpPacket("focusonpoint");
                                packet.AppendParameters(15, 15);
                                Messenger.SendDataTo(client, packet);
                                client.Player.MovementLocked = true;
                            }
                        }
                        break;
                    case "/setspawn":
                        {
                            if (exPlayer.Get(client).IsValidPlayerSpawn(client.Player.MapID) == true
                                && client.Player.Map.Tile[client.Player.X, client.Player.Y].Type != Enums.TileType.Blocked)
                            {
                                exPlayer.Get(client).SpawnMap = client.Player.MapID;
                                exPlayer.Get(client).SpawnX = client.Player.X;
                                exPlayer.Get(client).SpawnY = client.Player.Y;
                                Messenger.PlayerMsg(client, "Spawn point saved!", Text.Yellow);
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "This is not a valid spawn point!", Text.BrightRed);
                            }
                        }
                        break;
                    case "/rstart":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                if (command[1].IsNumeric())
                                {
                                    int floor = 1;
                                    if (command.CommandArgs.Count > 2 && command[2].IsNumeric())
                                    {
                                        floor = command[2].ToInt();
                                    }
                                    //RDungeonManager.LoadRDungeon(command[1].ToInt() - 1);
                                    client.Player.WarpToRDungeon(command[1].ToInt() - 1, floor - 1);
                                    Server.Logging.ChatLogger.AppendToChatLog("Staff", "[RDungeon Warp] " + client.Player.Name + " warped to " + RDungeonManager.RDungeons[command[1].ToInt() - 1].DungeonName + ", Floor: " + floor);

                                }
                            }
                        }
                        break;
                    case "/nextfloor":
                        {
                            try
                            {
                                if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                                {
                                    if (client.Player.Map.MapType == Enums.MapType.RDungeonMap && ((RDungeonMap)client.Player.Map).RDungeonIndex > -1)
                                    {
                                        client.Player.WarpToRDungeon(((RDungeonMap)client.Player.Map).RDungeonIndex, ((RDungeonMap)client.Player.Map).RDungeonFloor + 1);
                                    }

                                }
                            }
                            catch (Exception ex)
                            {
                                Messenger.AdminMsg("nextfloor error", Text.Pink);
                                Messenger.AdminMsg(ex.ToString(), Text.Pink);
                            }
                        }
                        break;
                    case "/visible":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                PacketBuilder.AppendVisibility(client, hitlist, false);
                            }
                        }
                        break;
                    case "/infoexp":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Admin))
                            {
                                Messenger.PlayerMsg(client, client.Player.ExplorerRank.ToString(), Text.BrightRed);
                            }
                        }
                        break;
                    case "/testexp":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Admin))
                            {
                                client.Player.MissionExp += command[1].ToInt();
                                MissionManager.ExplorerRankUp(client);
                            }
                        }
                        break;
                    case "/fixrank":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor))
                            {
                                Client n = ClientManager.FindClient(joinedArgs);
                                if (n != null)
                                {
                                    n.Player.ExplorerRank = Enums.ExplorerRank.Normal;
                                    MissionManager.ExplorerRankUp(n);
                                    Messenger.PlayerMsg(client, n.Player.Name + "'s rank is fixed now!", Text.Yellow);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Player is offline", Text.Grey);
                                }
                            }
                        }
                        break;
                    case "/;":
                        {
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                                return;
                            }
                            hitlist.AddPacketToMap(client.Player.Map, PacketBuilder.CreateSoundPacket("Emote_Sweat"), client.Player.X, client.Player.Y, 10);
                            PacketBuilder.AppendEmote(client, 9, 1, 2, hitlist);
                        }
                        break;
                    case "/'":
                        {
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                                return;
                            }
                            hitlist.AddPacketToMap(client.Player.Map, PacketBuilder.CreateSoundPacket("Emote_Sweatdrop"), client.Player.X, client.Player.Y, 10);
                            PacketBuilder.AppendEmote(client, 6, 2, 1, hitlist);
                        }
                        break;
                    case "/*":
                        {
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                                return;
                            }
                            hitlist.AddPacketToMap(client.Player.Map, PacketBuilder.CreateSoundPacket("Emote_Laugh.wav"), client.Player.X, client.Player.Y, 10);
                            PacketBuilder.AppendEmote(client, 5, 2, 1, hitlist);
                        }
                        break;
                    case "/)":
                        {
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                                return;
                            }
                            hitlist.AddPacketToMap(client.Player.Map, PacketBuilder.CreateSoundPacket("Emote_Applause"), client.Player.X, client.Player.Y, 10);
                            PacketBuilder.AppendEmote(client, 7, 2, 3, hitlist);
                        }
                        break;
                    case "/))":
                        {
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                                return;
                            }
                            hitlist.AddPacketToMap(client.Player.Map, PacketBuilder.CreateSoundPacket("Emote_Chatter"), client.Player.X, client.Player.Y, 10);
                            PacketBuilder.AppendEmote(client, 7, 2, 7, hitlist);
                        }
                        break;
                    case "/)))":
                        {
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                                return;
                            }
                            hitlist.AddPacketToMap(client.Player.Map, PacketBuilder.CreateSoundPacket("Emote_Cheer"), client.Player.X, client.Player.Y, 10);
                            PacketBuilder.AppendEmote(client, 7, 2, 8, hitlist);
                        }
                        break;
                    case "/.":
                        {
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                                return;
                            }
                            hitlist.AddPacketToMap(client.Player.Map, PacketBuilder.CreateSoundPacket("Emote_Pause"), client.Player.X, client.Player.Y, 10);
                            PacketBuilder.AppendEmote(client, 12, 2, 1, hitlist);
                        }
                        break;
                    case "/..":
                        {
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                                return;
                            }
                            hitlist.AddPacketToMap(client.Player.Map, PacketBuilder.CreateSoundPacket("Emote_Pause"), client.Player.X, client.Player.Y, 10);
                            PacketBuilder.AppendEmote(client, 12, 2, 2, hitlist);
                        }
                        break;
                    case "/...":
                        {
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                                return;
                            }
                            hitlist.AddPacketToMap(client.Player.Map, PacketBuilder.CreateSoundPacket("Emote_Pause"), client.Player.X, client.Player.Y, 10);
                            PacketBuilder.AppendEmote(client, 12, 2, 3, hitlist);
                        }
                        break;
                    case "/!":
                        {
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                                return;
                            }
                            hitlist.AddPacketToMap(client.Player.Map, PacketBuilder.CreateSoundPacket("Emote_Surprise"), client.Player.X, client.Player.Y, 10);
                            PacketBuilder.AppendEmote(client, 13, 2, 1, hitlist);
                        }
                        break;
                    case "/?":
                        {
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                                return;
                            }
                            hitlist.AddPacketToMap(client.Player.Map, PacketBuilder.CreateSoundPacket("Emote_Question"), client.Player.X, client.Player.Y, 10);
                            PacketBuilder.AppendEmote(client, 8, 2, 1, hitlist);
                        }
                        break;
                    case "/??":
                        {
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                                return;
                            }
                            hitlist.AddPacketToMap(client.Player.Map, PacketBuilder.CreateSoundPacket("Emote_Confused"), client.Player.X, client.Player.Y, 10);
                            PacketBuilder.AppendEmote(client, 8, 2, 2, hitlist);
                        }
                        break;
                    case "/!?":
                        {
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                                return;
                            }
                            hitlist.AddPacketToMap(client.Player.Map, PacketBuilder.CreateSoundPacket("Emote_Shock"), client.Player.X, client.Player.Y, 10);
                            PacketBuilder.AppendEmote(client, 11, 2, 1, hitlist);
                        }
                        break;
                    case "/+":
                        {
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                                return;
                            }
                            hitlist.AddPacketToMap(client.Player.Map, PacketBuilder.CreateSoundPacket("Emote_Anger"), client.Player.X, client.Player.Y, 10);
                            PacketBuilder.AppendEmote(client, 10, 2, 1, hitlist);
                        }
                        break;
                    case "/lvl":
                        {
                            if (client.Player.Muted)
                            {
                                Messenger.PlayerMsg(client, "You are muted!", Text.BrightRed);
                                return;
                            }
                            hitlist.AddPacketToMap(client.Player.Map, PacketBuilder.CreateSoundPacket("Emote_Anger"), client.Player.X, client.Player.Y, 10);
                            PacketBuilder.AppendEmote(client, 97, 2, 2, hitlist);
                        }
                        break;
                    case "/testailment":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                if (command[1].IsNumeric() && command[1].ToInt() >= 0 && command[1].ToInt() < 6)
                                {

                                    SetStatusAilment(client.Player.GetActiveRecruit(), client.Player.Map, (Enums.StatusAilment)(command[1].ToInt()), 1, null);
                                    //    for (int i = 0; i < Constants.MAX_MAP_NPCS; i++)
                                    //    {
                                    //        if (client.Player.Map.ActiveNpc[i].Num > 0)
                                    //        {
                                    //            client.Player.Map.ActiveNpc[i].ChangeStatusAilment((Enums.StatusAilment)(command[1].ToInt()), 1);
                                    //        }
                                    //    }
                                }
                            }
                        }
                        break;
                    case "/addvstatus":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                if (command[1].IsNumeric())
                                {
                                    ExtraStatus status = new ExtraStatus();
                                    status.Name = client.Player.GetActiveRecruit().VolatileStatus.Count.ToString();
                                    status.Emoticon = command[1].ToInt();
                                    client.Player.GetActiveRecruit().VolatileStatus.Add(status);
                                    PacketBuilder.AppendVolatileStatus(client, hitlist);

                                    IMap clientMap = client.Player.Map;
                                    for (int i = 0; i < Constants.MAX_MAP_NPCS; i++)
                                    {
                                        if (clientMap.ActiveNpc[i].Num > 0)
                                        {
                                            clientMap.ActiveNpc[i].VolatileStatus.Add(status);
                                            PacketBuilder.AppendNpcVolatileStatus(MapManager.RetrieveActiveMap(clientMap.ActiveNpc[i].MapID), hitlist, i);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case "/removevstatus":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                client.Player.GetActiveRecruit().VolatileStatus.Clear();
                                PacketBuilder.AppendVolatileStatus(client, hitlist);

                                IMap clientMap = client.Player.Map;
                                for (int i = 0; i < Constants.MAX_MAP_NPCS; i++)
                                {
                                    if (clientMap.ActiveNpc[i].Num > 0)
                                    {
                                        clientMap.ActiveNpc[i].VolatileStatus.Clear();
                                        PacketBuilder.AppendNpcVolatileStatus(MapManager.RetrieveActiveMap(clientMap.ActiveNpc[i].MapID), hitlist, i);
                                    }
                                }
                            }
                        }
                        break;
                    /*case "/diagonal": {
                            if (Ranks.IsAllowed(client, Enums.Rank.Monitor)) {
                                IMap clientMap = client.Player.Map;
                                for (int i = 0; i < Constants.MAX_MAP_NPCS; i++) {
                                    if (clientMap.ActiveNpc[i].Num > 0) {
                                        clientMap.ActiveNpc[i].X += 3;
                                        clientMap.ActiveNpc[i].Y += 3;
                                        PacketBuilder.AppendNpcXY(MapManager.RetrieveActiveMap(clientMap.ActiveNpc[i].MapID), hitlist, i);
                                    }
                                }
                            }
                        }
                        break;*/
                    case "/checkailment":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Messenger.PlayerMsg(client, client.Player.GetActiveRecruit().StatusAilment.ToString() + client.Player.GetActiveRecruit().StatusAilmentCounter.ToString(), Text.BrightRed);
                            }
                        }
                        break;
                    case "/checkdungeons":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client n = ClientManager.FindClient(joinedArgs);
                                if (n != null)
                                {
                                    Messenger.PlayerMsg(client, n.Player.Name + "'s completed dungeons:", Text.Yellow);
                                    for (int i = 0; i < Server.Dungeons.DungeonManager.Dungeons.Count; i++)
                                    {
                                        Messenger.PlayerMsg(client, Server.Dungeons.DungeonManager.Dungeons[i].Name + ": " + n.Player.GetDungeonCompletionCount(i), Text.Yellow);
                                    }
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Player is offline", Text.Grey);
                                }
                            }
                        }
                        break;
                    case "/testegg":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Admin))
                            {

                            }
                        }
                        break;
                    case "/speedlimit":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                if (command[1].IsNumeric() && command[1].ToInt() >= 0 && command[1].ToInt() < 7)
                                {

                                    client.Player.GetActiveRecruit().SpeedLimit = (Enums.Speed)(command[1].ToInt());
                                    PacketBuilder.AppendSpeedLimit(client, hitlist);
                                }
                            }
                        }
                        break;
                    case "/testdeath":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                client.Player.Hunted = false;
                                PacketBuilder.AppendHunted(client, hitlist);
                                client.Player.Dead = true;
                                PacketBuilder.AppendDead(client, hitlist);

                                AskAfterDeathQuestion(client);
                            }
                        }
                        break;
                    case "/mobile":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Admin))
                            {
                                if (command[1].IsNumeric())
                                {
                                    client.Player.GetActiveRecruit().Mobility[command[1].ToInt()] = true;
                                    PacketBuilder.AppendMobility(client, hitlist);
                                }
                            }
                        }
                        break;
                    case "/immobile":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Admin))
                            {
                                if (command[1].IsNumeric())
                                {
                                    client.Player.GetActiveRecruit().Mobility[command[1].ToInt()] = false;
                                    PacketBuilder.AppendMobility(client, hitlist);
                                }
                            }
                        }
                        break;
                    case "/createparty":
                        {
                            PartyManager.CreateNewParty(client);
                        }
                        break;
                    case "/joinparty":
                        {
                            if (client.Player.PartyID == null)
                            {
                                Party party = PartyManager.FindPlayerParty(ClientManager.FindClient(joinedArgs));
                                PartyManager.JoinParty(party, client);
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You are already in a party!", Text.BrightRed);
                            }
                        }
                        break;
                    case "/leaveparty":
                        {
                            if (client.Player.PartyID != null)
                            {
                                if (client.Player.Map.Moral == Enums.MapMoral.None)
                                {
                                    Messenger.PlayerMsg(client, "You can't leave the party here!", Text.BrightRed);
                                }
                                else
                                {
                                    Party party = PartyManager.FindPlayerParty(client);
                                    PartyManager.RemoveFromParty(party, client);
                                }
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You are not in a party!", Text.BrightRed);
                            }
                        }
                        break;
                    case "/myparty":
                        {
                            if (client.Player.PartyID != null)
                            {
                                Party party = PartyManager.FindPlayerParty(client);
                                Messenger.PlayerMsg(client, "Players in your party:", Text.Black);
                                foreach (Client i in party.GetOnlineMemberClients())
                                {
                                    Messenger.PlayerMsg(client, i.Player.Name, Text.White);
                                }
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "You are not in a party!", Text.BrightRed);
                            }
                        }
                        break;
                    case "/kickparty":
                        {
                            try
                            {
                                if (client.Player.PartyID != null)
                                {
                                    Client targetPlayer = ClientManager.FindClient(command[1]);
                                    if (targetPlayer.Player.PartyID == client.Player.PartyID)
                                    {

                                        Party party = PartyManager.FindPlayerParty(client);
                                        if (party.GetLeader() == client)
                                        {
                                            Client n = ClientManager.FindClient(joinedArgs);
                                            if (n != null)
                                            {
                                                if (n.Player.Map.Moral == Enums.MapMoral.None)
                                                {
                                                    Messenger.PlayerMsg(client, "The party member can't be kicked there!", Text.BrightRed);
                                                }
                                                else
                                                {
                                                    PartyManager.RemoveFromParty(party, n);
                                                }
                                            }
                                            else
                                            {
                                                Messenger.PlayerMsg(client, "Unable to find player.", Text.BrightRed);
                                            }
                                        }
                                        else
                                        {
                                            Messenger.PlayerMsg(client, "You are not the party leader!", Text.BrightRed);
                                        }
                                    }
                                    else
                                    {
                                        Messenger.PlayerMsg(client, "Player is not in your party!", Text.BrightRed);
                                    }
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "You are not in a party!", Text.BrightRed);
                                }
                            }
                            catch (Exception ex)
                            {
                                Messenger.AdminMsg(ex.ToString(), Text.White);
                            }
                        }
                        break;
                    case "/moduleswitch":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                client.Player.SetActiveExpKitModule(Enums.ExpKitModules.Counter);
                            }
                        }
                        break;
                    case "/sticky":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Admin))
                            {
                                if (command[1].IsNumeric())
                                {
                                    client.Player.SetItemSticky(command[1].ToInt(), true);
                                }
                            }
                        }
                        break;
                    case "/thticky":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Admin))
                            {
                                if (command[1].IsNumeric())
                                {
                                    client.Player.SetItemSticky(command[1].ToInt(), false);
                                }
                            }
                        }
                        break;
                    case "/trade":
                        {
                            Client n = ClientManager.FindClient(joinedArgs);
                            if (n != null)
                            {
                                if (n.Player.MapID == client.Player.MapID)
                                {
                                    client.Player.RequestTrade(n);
                                    Messenger.PlayerMsg(client, "You have asked " + n.Player.Name + " to trade with you!", Text.BrightGreen);
                                }
                            }
                            else
                            {
                                Messenger.PlayerMsg(client, "Player is offline.", Text.Grey);
                            }
                        }
                        break;
                    case "/editemoticon":
                    case "/editemotions":
                        {
                            // Prevent Hacking
                            if (Ranks.IsDisallowed(client, Enums.Rank.Developer))
                            {
                                Server.Network.Messenger.HackingAttempt(client, "Admin Cloning");
                            }
                            else
                            {
                                Messenger.SendDataTo(client, TcpPacket.CreatePacket("emoticoneditor"));
                            }
                        }
                        break;
                    case "/edititem":
                        {
                            // Prevent Hacking
                            if (Ranks.IsDisallowed(client, Enums.Rank.Developer))
                            {
                                Server.Network.Messenger.HackingAttempt(client, "Admin Cloning");
                            }
                            else
                            {
                                if (command[1].IsNumeric())
                                {
                                    int n = command[1].ToInt();

                                    // Prevent hacking
                                    if (n < 0 | n > Server.Items.ItemManager.Items.MaxItems)
                                    {
                                        Messenger.HackingAttempt(client, "Invalid Item Index");
                                        return;
                                    }

                                    Messenger.SendEditItemTo(client, n);
                                }

                            }
                        }
                        break;
                    case "/edititems":
                        {
                            // Prevent Hacking
                            if (Ranks.IsDisallowed(client, Enums.Rank.Developer))
                            {
                                Server.Network.Messenger.HackingAttempt(client, "Admin Cloning");
                            }
                            else
                            {
                                Messenger.SendItemEditor(client);
                            }
                        }
                        break;
                    case "/editmove":
                        {
                            // Prevent Hacking
                            if (Ranks.IsDisallowed(client, Enums.Rank.Developer))
                            {
                                Server.Network.Messenger.HackingAttempt(client, "Admin Cloning");
                            }
                            else
                            {
                                if (command[1].IsNumeric())
                                {
                                    int n = command[1].ToInt(-1);

                                    // Prevent hacking
                                    if (n < 0 | n > MoveManager.Moves.MaxMoves)
                                    {
                                        Messenger.HackingAttempt(client, "Invalid Move Index");
                                        return;
                                    }

                                    Messenger.SendEditMoveTo(client, n);
                                }

                            }
                        }
                        break;
                    case "/editmoves":
                        {
                            // Prevent Hacking
                            if (Ranks.IsDisallowed(client, Enums.Rank.Developer))
                            {
                                Server.Network.Messenger.HackingAttempt(client, "Admin Cloning");
                            }
                            else
                            {
                                Messenger.SendDataTo(client, TcpPacket.CreatePacket("moveeditor"));
                            }
                        }
                        break;
                    case "/editdungeon":
                        {
                            // Prevent Hacking
                            if (Ranks.IsDisallowed(client, Enums.Rank.Mapper))
                            {
                                Server.Network.Messenger.HackingAttempt(client, "Admin Cloning");
                            }
                            else
                            {
                                if (command[1].IsNumeric())
                                {
                                    if (command[1].ToInt() < 0 && command[1].ToInt() >= DungeonManager.Dungeons.Count)
                                    {
                                        Server.Network.Messenger.HackingAttempt(client, "Invalid Dungeon Number");
                                    }
                                    Messenger.SendEditDungeonTo(client, command[1].ToInt());
                                }
                            }
                        }
                        break;
                    case "/editdungeons":
                        {
                            // Prevent Hacking
                            if (Ranks.IsDisallowed(client, Enums.Rank.Mapper))
                            {
                                Server.Network.Messenger.HackingAttempt(client, "Admin Cloning");
                            }
                            else
                            {
                                Messenger.SendDataTo(client, TcpPacket.CreatePacket("dungeoneditor"));
                            }
                        }
                        break;
                    case "/editnpc":
                        {
                            // Prevent Hacking
                            if (Ranks.IsDisallowed(client, Enums.Rank.Developer))
                            {
                                Server.Network.Messenger.HackingAttempt(client, "Admin Cloning");
                            }
                            else
                            {
                                if (command[1].IsNumeric())
                                {
                                    int n = command[1].ToInt();

                                    // Prevent hacking
                                    /*if (n < 0 || n > Server.Npcs.NpcManager.Npcs.MaxNpcs) {
                                        Messenger.HackingAttempt(client, "Invalid Npc Index");
                                        return;
                                    }*/
                                    Messenger.SendNpcAiTypes(client);
                                    Messenger.SendEditNpcTo(client, n);
                                }

                            }
                        }
                        break;
                    case "/editnpcs":
                        {
                            // Prevent Hacking
                            if (Ranks.IsDisallowed(client, Enums.Rank.Mapper))
                            {
                                Server.Network.Messenger.HackingAttempt(client, "Admin Cloning");
                            }
                            else
                            {
                                Messenger.SendDataTo(client, TcpPacket.CreatePacket("npceditor"));
                            }
                        }
                        break;
                    case "/editrdungeon":
                        {
                            // Prevent Hacking
                            if (Ranks.IsDisallowed(client, Enums.Rank.Mapper))
                            {
                                Server.Network.Messenger.HackingAttempt(client, "Admin Cloning");
                            }
                            else
                            {
                                if (command[1].IsNumeric())
                                {
                                    int n = command[1].ToInt();
                                    if (n < 0 || n > RDungeonManager.RDungeons.Count - 1)
                                    {
                                        Messenger.PlayerMsg(client, "Invalid dungeon client", Text.BrightRed);
                                        return;
                                    }

                                    Messenger.SendEditRDungeonTo(client, n);
                                }

                            }
                        }
                        break;
                    case "/editrdungeons":
                        {
                            // Prevent Hacking
                            if (Ranks.IsDisallowed(client, Enums.Rank.Mapper))
                            {
                                Server.Network.Messenger.HackingAttempt(client, "Admin Cloning");
                            }
                            else
                            {
                                Messenger.SendDataTo(client, TcpPacket.CreatePacket("rdungeoneditor"));
                            }
                        }
                        break;
                    case "/editstory":
                    case "/editstories":
                        {
                            // Prevent Hacking
                            if (Ranks.IsDisallowed(client, Enums.Rank.Mapper))
                            {
                                Server.Network.Messenger.HackingAttempt(client, "Admin Cloning");
                            }
                            else
                            {
                                Messenger.SendDataTo(client, TcpPacket.CreatePacket("storyeditor"));
                            }
                        }
                        break;
                    case "/mapreport":
                        {
                            // Prevent hacking
                            //if (Ranks.IsDisallowed(client, Enums.Rank.Mapper)) {
                            //Messenger.HackingAttempt(client, "Admin Cloning");
                            //return;
                            //}
                            // TODO: Fix MapReport to work with on-demand map loading system
                            //TcpPacket packet = new TcpPacket("mapreport");
                            //for (int i = 1; i <= Server.Settings.MaxMaps; i++) {
                            //packet.AppendParameter(MapManager.Maps[i].Name);
                            //packet.AppendParameter("-Not Implemented-");
                            //}

                            //Messenger.SendDataTo(client, packet);
                        }
                        break;
                    case "/editevolutions":
                    case "/editevolution":
                        {
                            // Prevent Hacking
                            if (Ranks.IsDisallowed(client, Enums.Rank.Mapper))
                            {
                                Server.Network.Messenger.HackingAttempt(client, "Admin Cloning");
                            }
                            else
                            {
                                Messenger.SendDataTo(client, TcpPacket.CreatePacket("evolutioneditor"));
                            }
                        }
                        break;
                    case "/editshop":
                    case "/editshops":
                        {
                            // Prevent Hacking
                            if (Ranks.IsDisallowed(client, Enums.Rank.Mapper))
                            {
                                Server.Network.Messenger.HackingAttempt(client, "Admin Cloning");
                            }
                            else
                            {
                                Messenger.SendDataTo(client, TcpPacket.CreatePacket("shopeditor"));
                            }
                        }
                        break;
                    case "/testrecall":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Messenger.SendRecallMenu(client, false);
                            }

                        }
                        break;
                    case "/test":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Debug.RunTest(client);
                            }
                        }
                        break;
                    case "/setform":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                client.Player.GetActiveRecruit().SetForm(command[1].ToInt());
                                Messenger.SendPlayerData(client);
                                Messenger.SendActiveTeam(client);
                                Messenger.SendStats(client);
                            }
                        }
                        break;
                    case "/setgender":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client n = ClientManager.FindClient(command[1]);
                                if (n != null)
                                {
                                    n.Player.GetActiveRecruit().Sex = (Enums.Sex)command[2].ToInt();
                                    RefreshCharacterTraits(n.Player.GetActiveRecruit(), n.Player.Map, hitlist);
                                    Messenger.SendPlayerData(n);
                                    Messenger.SendActiveTeam(n);
                                    Messenger.SendStats(n);
                                    Messenger.PlayerMsg(client, n.Player.Name + "'s " + n.Player.GetActiveRecruit().Name + "'s gender was set to " + ((Enums.Sex)command[2].ToInt()).ToString(), Text.Pink);
                                    Server.Logging.ChatLogger.AppendToChatLog("Staff", "[Characteristics] " + client.Player.Name + " changed the gender of " + n.Player.Name + "'s " + n.Player.GetActiveRecruit().Name + " to " + ((Enums.Sex)command[2].ToInt()).ToString());
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Player is offline", Text.Grey);
                                }
                            }
                        }
                        break;
                    case "/offlinetostart":
                        {
                            try
                            {
                                if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                                {
                                    using (DatabaseConnection dbConnection = new DatabaseConnection(DatabaseID.Players))
                                    {
                                        dbConnection.Database.ExecuteNonQuery("UPDATE mdx_players.location " +
                                            "JOIN mdx_players.characteristics ON mdx_players.characteristics.CharID = mdx_players.location.CharID " +
                                            "SET mdx_players.location.Map = \'s1015\' " +
                                            "WHERE characteristics.Name = \'" + command[1] + "\';");
                                        Messenger.PlayerMsg(client, "Character has been offline-warped to the crossroads", Text.Yellow);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Messenger.PlayerMsg(client, ex.ToString(), Text.Black);
                            }
                        }
                        break;
                    case "/fixhouse":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                Client n = ClientManager.FindClient(command[1]);
                                if (n != null)
                                {
                                    string houseID = MapManager.GenerateHouseID(n.Player.CharID, 0);
                                    // Make an empty house
                                    DataManager.Maps.HouseMap rawHouse = new DataManager.Maps.HouseMap(houseID);
                                    rawHouse.Owner = n.Player.CharID;
                                    rawHouse.Room = 0;
                                    IMap map = new House(rawHouse);
                                    map.Moral = Enums.MapMoral.House;
                                    map.Name = n.Player.Name + "'s House";
                                    map.Save();
                                    Messenger.PlayerMsg(client, n.Player.Name + "'s house is cleared.", Text.Pink);
                                }
                                else
                                {
                                    Messenger.PlayerMsg(client, "Player is offline", Text.Grey);
                                }
                            }
                        }
                        break;
                    case "/addmaps":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Admin))
                            {

                                for (int i = 2002; i <= 3000; i++)
                                {
                                    // Make an empty house
                                    DataManager.Maps.Map rawMap = new DataManager.Maps.Map("s" + i);
                                    Server.Maps.Map map = new Server.Maps.Map(rawMap);
                                    map.Save();
                                    Messenger.PlayerMsg(client, "Map " + i + " added.", Text.Pink);
                                }
                                Messenger.PlayerMsg(client, "Maps added.", Text.Pink);
                            }
                        }
                        break;
                    case "/copycharacter":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                //try {
                                //    using (DatabaseConnection dbConnection = new DatabaseConnection(DatabaseID.Players)) {
                                //	
                                //        PlayerDataManager.CopyCharacter(dbConnection.Database, command[1], command[2]);
                                //        Messenger.PlayerMsg(client, "Character copied from " + command[1] + " to " + command[2], Text.Black);
                                //    }
                                //} catch (Exception ex) {
                                //    Messenger.PlayerMsg(client, ex.ToString(), Text.Black);
                                //}
                            }

                        }
                        break;
                    case "/addnpcs":
                        {
                            if (Ranks.IsAllowed(client, Enums.Rank.Scripter))
                            {
                                for (int i = 4000; i <= 6000; i++)
                                {
                                    NpcManager.SaveNpc(i);
                                    Messenger.PlayerMsg(client, "NPC " + i + " added.", Text.BrightGreen);
                                }
                            }
                        }
                        break;
                    default:
                        {
                            var processed = false;
                            if (ActiveEvent != null && ActiveEvent.IsStarted)
                            {
                                if (ActiveEvent.ProcessCommand(client, command, joinedArgs))
                                {
                                    processed = true;
                                }
                            }

                            if (!processed)
                            {
                                Messenger.PlayerMsg(client, "That is not a valid command.", Text.BrightRed);
                            }
                        }
                        break;
                }

                PacketHitList.MethodEnded(ref hitlist);
            }
            catch (Exception ex)
            {
                Messenger.AdminMsg("Error: Command (" + command.CommandArgs[0] + ")" + ex.ToString(), Text.Black);
                //Messenger.AdminMsg(ex.ToString(), Text.Black);
            }
        }

        public static string JoinArgs(List<string> args)
        {
            if (args.Count > 1)
            {
                StringBuilder joinedArgs = new StringBuilder();
                for (int i = 1; i < args.Count; i++)
                {
                    joinedArgs.Append(args[i]);
                    joinedArgs.Append(" ");
                }
                return joinedArgs.ToString().Trim();
            }
            else
            {
                return "";
            }
        }

    }
}
