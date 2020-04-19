using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Server;
using Server.Combat;
using Server.Discord;
using Server.Events;
using Server.Maps;
using Server.Network;
using Server.Players;
using Server.Stories;

namespace Script.Events
{
    public abstract class AbstractEvent<TData> : AbstractEvent<TData, AbstractEventData.NullPlayerData> where TData : AbstractEventData<AbstractEventData.NullPlayerData>, new()
    {
    }

    public abstract class AbstractEvent<TData, TPlayerData> : IEvent where TData : AbstractEventData<TPlayerData>, new() where TPlayerData : new()
    {
        public abstract string Identifier { get; }

        public abstract string Name { get; }

        public abstract string IntroductionMessage { get; }
        public virtual string[] Rules { get; }
        public virtual string RewardMessage { get; }
        public abstract TimeSpan? Duration { get; }

        public bool IsStarted => Data.Started;

        public TData Data { get; private set; }

        public AbstractEvent()
        {
            this.Data = new TData();
            this.Rules = Array.Empty<string>();
        }

        protected abstract List<EventRanking> DetermineRankings();

        public void AnnounceWinner()
        {
            var rankings = DetermineRankings();

            var sortedRankings = rankings.OrderByDescending(x => x.Score).ToList();

            var rewards = new string[3];

            if (sortedRankings.Count >= 3)
            {
                rewards[2] = HandoutReward(sortedRankings[2], 3, Main.IsTestingEvent);
            }
            if (sortedRankings.Count >= 2)
            {
                rewards[1] = HandoutReward(sortedRankings[1], 2, Main.IsTestingEvent);
            }
            if (sortedRankings.Count >= 1)
            {
                rewards[0] = HandoutReward(sortedRankings[0], 1, Main.IsTestingEvent);
            }

            foreach (var client in GetRegisteredClients())
            {
                CleanPlayer(client);

                var story = new Story();
                var segment = StoryBuilder.BuildStory();
                StoryBuilder.AppendSaySegment(segment, $"And the winners are...!", -1, 0, 0);

                if (sortedRankings.Count >= 3)
                {
                    StoryBuilder.AppendSaySegment(segment, "In third place...", -1, 0, 0);
                    StoryBuilder.AppendSaySegment(segment, $"{sortedRankings[2].Client.Player.DisplayName}, with a score of {sortedRankings[2].Score}!", -1, 0, 0);
                    if (!string.IsNullOrEmpty(rewards[2]))
                    {
                        StoryBuilder.AppendSaySegment(segment, $"They received {rewards[2]}!", -1, 0, 0);
                    }
                }

                if (sortedRankings.Count >= 2)
                {
                    StoryBuilder.AppendSaySegment(segment, "In second place...", -1, 0, 0);
                    StoryBuilder.AppendSaySegment(segment, $"{sortedRankings[1].Client.Player.DisplayName}, with a score of {sortedRankings[1].Score}!", -1, 0, 0);
                    if (!string.IsNullOrEmpty(rewards[1]))
                    {
                        StoryBuilder.AppendSaySegment(segment, $"They received {rewards[1]}!", -1, 0, 0);
                    }
                }

                if (sortedRankings.Count >= 1)
                {
                    StoryBuilder.AppendSaySegment(segment, "In first place...", -1, 0, 0);
                    StoryBuilder.AppendSaySegment(segment, $"{sortedRankings[0].Client.Player.DisplayName}, with a score of {sortedRankings[0].Score}!", -1, 0, 0);
                    if (!string.IsNullOrEmpty(rewards[0]))
                    {
                        StoryBuilder.AppendSaySegment(segment, $"They received {rewards[0]}!", -1, 0, 0);
                    }
                }

                if (sortedRankings.Count == 0)
                {
                    StoryBuilder.AppendSaySegment(segment, "...no one. Strange?", -1, 0, 0);
                }

                if (!Main.IsTestingEvent)
                {
                    client.Player.GiveItem(133, 1);
                    StoryBuilder.AppendSaySegment(segment, "You were given 1 Arcade Token for participating!", -1, 0, 0);
                }

                segment.AppendToStory(story);
                StoryManager.PlayStory(client, story);
            }

            var announcementMessage = new StringBuilder();
            announcementMessage.AppendLine("The winners are...");
            for (var i = 0; i < System.Math.Min(3, sortedRankings.Count); i++)
            {
                announcementMessage.Append($"{i + 1}. {sortedRankings[i].Client.Player.DisplayName} with a score of {sortedRankings[i].Score}!");
                if (!string.IsNullOrEmpty(rewards[i]))
                {
                    announcementMessage.Append($" They received {rewards[i]}!");
                }
                announcementMessage.AppendLine();
            }

            Task.Run(() => DiscordManager.Instance.SendAnnouncement(announcementMessage.ToString()));
        }

        public virtual string HandoutReward(EventRanking eventRanking, int position, bool isTesting)
        {
            return "";
        }

        public virtual void ConfigurePlayer(Client client)
        {
        }

        public virtual void DeconfigurePlayer(Client client)
        {
        }

        public virtual void CleanPlayer(Client client)
        {
        }

        public virtual void End()
        {
            Data.Started = false;

            foreach (var client in GetRegisteredClients())
            {
                Messenger.PlayerWarp(client, Main.EventHubMap, Main.EventHubMapX, Main.EventHubMapY);
            }
        }

        public virtual void Start()
        {
            Data.Started = true;

            Data.RegisteredCharacters.Clear();
            foreach (var client in EventManager.GetRegisteredClients())
            {
                Data.RegisteredCharacters.Add(client.Player.CharID);
            }

            if (Duration.HasValue)
            {
                Data.CompletionTime = DateTime.UtcNow.Add(Duration.Value);
            }

            PrepareData();

            foreach (var client in GetRegisteredClients())
            {
                OnboardNewPlayer(client);
                ConfigurePlayer(client);
            }
        }

        public void Load(string data)
        {
            this.Data = JsonConvert.DeserializeObject<TData>(data);
        }

        public string Save()
        {
            return JsonConvert.SerializeObject(Data);
        }

        protected virtual void PrepareData()
        {
        }

        public virtual void OnboardNewPlayer(Client client)
        {
        }

        public virtual void OnServerTick(TickCount tickCount)
        {
        }

        public virtual void OnScriptTimer(string identifier, string arguments)
        {
        }

        public virtual void OnMapTick(IMap map)
        {
        }

        public virtual void OnActivateMap(IMap map)
        {
        }

        public virtual void OnDeath(Client client)
        {
        }

        public virtual void OnPickupItem(ICharacter character, int itemSlot, InventoryItem invItem)
        {
        }

        public virtual void OnNpcSpawn(IMap map, MapNpcPreset npc, MapNpc spawnedNpc, PacketHitList hitlist)
        {
        }

        public virtual void OnNpcDeath(PacketHitList hitlist, ICharacter attacker, MapNpc npc)
        {
        }

        public virtual void OnMoveHitCharacter(Client attacker, Client defender)
        {
        }

        public virtual bool ProcessCommand(Client client, Command command, string joinedArgs)
        {
            return false;
        }

        public IEnumerable<Client> GetRegisteredClients() {
            foreach (var registeredCharacter in Data.RegisteredCharacters) {
                var client = ClientManager.FindClientFromCharID(registeredCharacter);

                if (client != null) {
                    yield return client;
                }
            }
        }
    }
}
