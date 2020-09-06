using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataManager.Players;
using Script.Events;
using Script.Models;
using Server;
using Server.Database;
using Server.Discord;
using Server.Events;
using Server.Events.World;
using Server.Leaderboards;
using Server.Network;
using Server.Stories;

namespace Script
{
    public partial class Main
    {
        public static readonly int EventHubMap = 153;
        public static readonly int EventHubMapX = 27;
        public static readonly int EventHubMapY = 14;

        public static IEvent ActiveEvent { get; set; }
        public static bool IsTestingEvent { get; set; }
        public static bool EventIsScheduled { get; set; }

        public static List<string> Events { get; set; }

        public static void InitializeEvents()
        {
            Events = new List<string>();

            // Events.Add("treasurehunt");
            Events.Add("shinyspectacular");
            Events.Add("paintball");
            // Events.Add("werewolf");
            Events.Add("bossrush");
        }

        public static string SelectNextEvent()
        {
            var eventDate = GetEventDate();

            var slot = eventDate.DayOfYear % Events.Count;

            return Events[slot];
        }   

        public static bool IsEventScheduled()
        {
            return EventIsScheduled;
        }

        public static DateTime GetEventDate()
        {
            var weekday = GetNextWeekday(DateTime.UtcNow, DayOfWeek.Sunday);

            var eventDate = new DateTime(weekday.Year, weekday.Month, weekday.Day, 17, 0, 0, DateTimeKind.Utc);

            if (eventDate < DateTime.UtcNow)
            {
                weekday = GetNextWeekday(DateTime.UtcNow.AddDays(1), DayOfWeek.Sunday);
                eventDate = new DateTime(weekday.Year, weekday.Month, weekday.Day, 17, 0, 0, DateTimeKind.Utc);
            }

            return eventDate;
        }

        public static DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            // The (... + 7) % 7 ensures we end up with a value in the range [0, 6]
            int daysToAdd = ((int) day - (int) start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd);
        }

        public static IEvent BuildEvent(string identifier)
        {
            switch (identifier)
            {
                case "treasurehunt":
                    return new TreasureHuntEvent();
                case "shinyspectacular":
                    return new ShinySpectacular();
                case "paintball":
                    return new Paintball();
                case "werewolf":
                    return new WerewolfEvent();
                case "bossrush":
                    return new BossRushEvent();
                default:
                    return null;
            }
        }

        public static bool SetEvent(Client client, string identifier, bool isTesting, bool allowOverride = false)
        {
            if (ActiveEvent != null && !allowOverride)
            {
                if (client != null)
                {
                    Messenger.PlayerMsg(client, "An event has already been set.", Text.BrightRed);
                }
                return false;
            }

            Main.IsTestingEvent = isTesting;
                                
            var eventInstance = BuildEvent(identifier);

            if (eventInstance == null)
            {
                if (client != null)
                {
                    Messenger.PlayerMsg(client, $"Invalid event type: {identifier}", Text.BrightRed);
                }
                return false;
            }

            EventManager.ActiveEventIdentifier = eventInstance.Identifier;
            ActiveEvent = eventInstance;

            if (client != null)
            {
                Messenger.PlayerMsg(client, $"The event has been set to {ActiveEvent.Name}!", Text.BrightGreen);
            }

            return true;
        }

        public static void StartEvent() 
        {
            if (ActiveEvent == null) 
            {
                return;
            }

            foreach (var registeredClient in EventManager.GetRegisteredClients())
            {
                Story story = new Story(Guid.NewGuid().ToString());
                StoryBuilderSegment segment = StoryBuilder.BuildStory();
                StoryBuilder.AppendSaySegment(segment, $"This event is... {ActiveEvent.Name}!", -1, 0, 0);
                StoryBuilder.AppendSaySegment(segment, ActiveEvent.IntroductionMessage, -1, 0, 0);

                foreach (var rule in ActiveEvent.Rules)
                {
                    StoryBuilder.AppendSaySegment(segment, rule, -1, 0, 0);
                }

                if (ActiveEvent.Duration.HasValue) 
                {
                    StoryBuilder.AppendSaySegment(segment, $"The event will end in {ActiveEvent.Duration.Value.TotalMinutes} minutes.", -1, 0, 0);
                }
                if (Main.IsTestingEvent)
                {
                    StoryBuilder.AppendSaySegment(segment, $"This event is currently being tested and winners will not receive any prizes.", -1, 0, 0);
                } 
                else if (!string.IsNullOrEmpty(ActiveEvent.RewardMessage))
                {
                    StoryBuilder.AppendSaySegment(segment, ActiveEvent.RewardMessage, -1, 0, 0);
                }

                StoryBuilder.AppendSaySegment(segment, "The event has now begun!", -1, 0, 0);
                segment.AppendToStory(story);
                StoryManager.PlayStory(registeredClient, story);
            }

            ActiveEvent.Start();

            var eventStartMessage = new StringBuilder();
            if (Main.IsTestingEvent) 
            {
                eventStartMessage.Append("[Testing] ");
            }
            eventStartMessage.Append($"{ActiveEvent.Name} has started!");

            Task.Run(() => DiscordManager.Instance.SendAnnouncement(eventStartMessage.ToString()));
            Messenger.SendAnnouncement("Weekly Event", eventStartMessage.ToString());

            if (ActiveEvent.Duration.HasValue) 
            {
                var endTime = DateTime.UtcNow.Add(ActiveEvent.Duration.Value);

                SetGlobalCountdown(new Countdown("The event ends in...", endTime));
                TimedEventManager.CreateTimer("endevent", endTime, null);
            }
        }

        public static void EndEvent()
        {
            ActiveEvent.End();
            Task.Run(() => DiscordManager.Instance.SendAnnouncement($"{ActiveEvent.Name} has finished!"));
            Messenger.GlobalMsg($"{ActiveEvent.Name} has finished!", Text.BrightGreen);

            foreach (var registeredClient in EventManager.GetRegisteredClients())
            {
                ActiveEvent.DeconfigurePlayer(registeredClient);

                Story story = new Story(Guid.NewGuid().ToString());
                StoryBuilderSegment segment = StoryBuilder.BuildStory();
                StoryBuilder.AppendSaySegment(segment, $"The event is now finished!", -1, 0, 0);
                StoryBuilder.AppendSaySegment(segment, $"Please wait as a winner is announced...", -1, 0, 0);
                segment.AppendToStory(story);
                StoryManager.PlayStory(registeredClient, story);
            }
        }

        public static void FinishEvent()
        {
            if (ActiveEvent != null)
            {
                ActiveEvent.AnnounceWinner();

                ActiveEvent = null;
                EventManager.ActiveEventIdentifier = null;
            }

            EventManager.RegisteredCharacters.Clear();
        }

        public static Story BuildEventIntroStory()
        {
            return BuildEventIntroForSpring();
        }

        public static Story BuildEventIntroForSpring()
        {
            Story story = new Story(Guid.NewGuid().ToString());
            StoryBuilderSegment segment = StoryBuilder.BuildStory();
            StoryBuilder.AppendCreateFNPCAction(segment, "0", "s153", 24, 6, 169, name: "Eventful", direction: Enums.Direction.Down, isShiny: true);
            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, "Greetings!");
            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, "Welcome to our Weekly Gala!");
            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, "The Gala has three parts.");
            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, "For the first part, I will be handing out Arcade Tokens for the leaderboard.");
            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, "Then, I will be giving out Arcade Tokens for the top outlaws!");
            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, "Finally, we will be having our weekly event!");
            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, $"The event this week will be {ActiveEvent.Name}.");

            AppendLeaderboardEventIntro(segment);
            AppendOutlawEventIntro(segment);

            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, $"Time for the event!");
            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, $"Enjoy {ActiveEvent.Name}!");

            segment.AppendToStory(story);

            return story;
        }

        public static void AppendLeaderboardEventIntro(StoryBuilderSegment segment)
        {
            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, "Lets start the first portion of the Gala...");
            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, "We'll be checking the leaderboards and handing out prizes.");
            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, "First place in each category will receive 3 Arcade Tokens.");
            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, "Second will receive 2 Arcade Tokens.");
            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, "Third will receive 1 Arcade Token.");

            foreach (var leaderboard in LeaderBoardManager.ListLeaderboards()) 
            {
                var leaderboardItems = leaderboard.Load().OrderByDescending(x => x.Value).ToList();

                StoryBuilder.AppendSpeechBubbleSegment(segment, 0, $"In the {leaderboard.Name} category...");

                if (leaderboardItems.Count > 0)
                {
                    StoryBuilder.AppendSpeechBubbleSegment(segment, 0, $"First place goes to {leaderboardItems[0].Name}!");
                }
                if (leaderboardItems.Count > 1)
                {
                    StoryBuilder.AppendSpeechBubbleSegment(segment, 0, $"Second place goes to {leaderboardItems[1].Name}!");
                }
                if (leaderboardItems.Count > 2)
                {
                    StoryBuilder.AppendSpeechBubbleSegment(segment, 0, $"Third place goes to {leaderboardItems[2].Name}!");
                }
            }

            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, "That's every category!");
        }

        public static void AppendOutlawEventIntro(StoryBuilderSegment segment)
        {
            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, "Next, lets see who the top outlaws are this week!");
            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, "The top outlaw will receive 10 Arcade tokens.");
            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, "Second place will receive 5 Arcade Tokens.");
            StoryBuilder.AppendSpeechBubbleSegment(segment, 0, "Third will receive 3 Arcade Tokens.");

            using (var databaseConnection = new DatabaseConnection(DatabaseID.Players))
            {
                var topOutlaws = PlayerDataManager.GetTopOutlaws(databaseConnection.Database).OrderByDescending(x => x.Points).Take(3).ToList();

                if (topOutlaws.Count > 0)
                {
                    StoryBuilder.AppendSpeechBubbleSegment(segment, 0, $"The top outlaw this week is {topOutlaws[0].CharacterName} with {topOutlaws[0].Points} OP!");
                }
                if (topOutlaws.Count > 1)
                {
                    StoryBuilder.AppendSpeechBubbleSegment(segment, 0, $"In second place is {topOutlaws[1].CharacterName} with {topOutlaws[1].Points} OP!");
                }
                if (topOutlaws.Count > 2)
                {
                    StoryBuilder.AppendSpeechBubbleSegment(segment, 0, $"Lastly, in third place is {topOutlaws[2].CharacterName} with {topOutlaws[2].Points} OP!");
                }
            }
        }

        public static void RunEventIntro() 
        {
            foreach (var registeredClient in EventManager.GetRegisteredClients())
            {
                var story = BuildEventIntroStory();
                
                StoryManager.PlayStory(registeredClient, story);
            }

            if (!Main.IsTestingEvent) 
            {
                foreach (var leaderboard in LeaderBoardManager.ListLeaderboards()) 
                {
                    var leaderboardItems = leaderboard.Load().OrderByDescending(x => x.Value).ToList();

                    if (leaderboardItems.Count > 0)
                    {
                        var client = ClientManager.FindClient(leaderboardItems[0].Name);
                        if (client != null)
                        {
                            client.Player.GiveItem(133, 3);
                        }
                    }
                    if (leaderboardItems.Count > 1)
                    {
                        var client = ClientManager.FindClient(leaderboardItems[1].Name);
                        if (client != null)
                        {
                            client.Player.GiveItem(133, 2);
                        }
                    }
                    if (leaderboardItems.Count > 2)
                    {
                        var client = ClientManager.FindClient(leaderboardItems[2].Name);
                        if (client != null)
                        {
                            client.Player.GiveItem(133, 2);
                        }
                    }
                }

                using (var databaseConnection = new DatabaseConnection(DatabaseID.Players))
                {
                    var topOutlaws = PlayerDataManager.GetTopOutlaws(databaseConnection.Database).OrderByDescending(x => x.Points).Take(3).ToList();

                    if (topOutlaws.Count > 0)
                    {
                        var client = ClientManager.FindClient(topOutlaws[0].CharacterName);
                        if (client != null)
                        {
                            client.Player.GiveItem(133, 10);
                        }
                    }
                    if (topOutlaws.Count > 1)
                    {
                        var client = ClientManager.FindClient(topOutlaws[1].CharacterName);
                        if (client != null)
                        {
                            client.Player.GiveItem(133, 5);
                        }
                    }
                    if (topOutlaws.Count > 2)
                    {
                        var client = ClientManager.FindClient(topOutlaws[2].CharacterName);
                        if (client != null)
                        {
                            client.Player.GiveItem(133, 3);
                        }
                    }
                }
            }
        }

        public static void RunEventReminder()
        {
            if (ActiveEvent != null)
            {
                var eventMessage = new StringBuilder();

                var eventDate = GetEventDate();

                eventMessage.AppendLine($"@everyone An event will be starting on {eventDate.DayOfWeek} at {eventDate.ToShortTimeString()} UTC! This event is {ActiveEvent.Name}.");
                eventMessage.AppendLine();
                eventMessage.AppendLine($"**Event rules**: {ActiveEvent.IntroductionMessage}");
                foreach (var rule in ActiveEvent.Rules)
                {
                    eventMessage.AppendLine(rule);
                }
                if (!string.IsNullOrEmpty(ActiveEvent.RewardMessage)) 
                {
                    eventMessage.AppendLine($"**Prizes**: {ActiveEvent.RewardMessage}");
                }

                Task.Run(() => DiscordManager.Instance.SendToChannel(525047030716825630, eventMessage.ToString()));
            }
        }
    }
}
