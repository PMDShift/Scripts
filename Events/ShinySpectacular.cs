using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server;
using Server.Npcs;
using Server.Combat;
using Server.Database;
using Server.Events;
using Server.Maps;
using Server.Network;
using Server.Players;
using Server.Pokedex;

namespace Script.Events
{
    public class ShinySpectacular : AbstractEvent<ShinySpectacular.ShinySpectacularData>
    {
        private readonly int ScoreThreshold = 15;

        public class ShinySpectacularData : AbstractEventData
        {
            public Dictionary<string, UserScore> Scores { get; set; }

            public ShinySpectacularData()
            {
                this.Scores = new Dictionary<string, UserScore>();
            }
        }

        public class UserScore
        {
            public int Score { get; set; }
            public HashSet<int> FoundSpecies { get; set; }

            public UserScore()
            {
                this.FoundSpecies = new HashSet<int>();
            }
        }

        public override string Identifier => "shinyspectacular";

        public override string Name => "Shiny Spectacular";

        public override string IntroductionMessage => "Swarms of shiny Pokemon have been spotted in dungeons! Defeat the most to win!";
        public override string RewardMessage => "The top three players with the most points will receive one of the shiny Pokemon they defeated.";

        public override string[] Rules => new string[] 
        {
            "You will get one point for every shiny Pokemon defeated.",
            $"You need to collect a minimum of {ScoreThreshold} points to be eligible for a prize.",
            "Only regular wild Pokemon count, bosses will not give points."
        };

        public override TimeSpan? Duration => new TimeSpan(0, 30, 0);

        protected override List<EventRanking> DetermineRankings()
        {
            var rankings = new List<EventRanking>();

            foreach (var client in EventManager.GetRegisteredClients())
            {
                if (Data.Scores.TryGetValue(client.Player.CharID, out var score))
                {
                    if (score.Score >= ScoreThreshold) 
                    {
                        rankings.Add(new EventRanking(client, score.Score));
                    }
                }
            }

            return rankings;
        }

        public override string HandoutReward(EventRanking eventRanking, int position, bool isTesting)
        {
            base.HandoutReward(eventRanking, position, isTesting);

            if (!Data.Scores.TryGetValue(eventRanking.Client.Player.CharID, out var userScore))
            {
                return "";
            }

            var availableSpecies = userScore.FoundSpecies.ToList();
            var selectedIndex = Server.Math.Rand(0, availableSpecies.Count);
            var selectedSpecies = Pokedex.GetPokemon(availableSpecies[selectedIndex]);

            if (!isTesting) 
            {
                var recruit = new Recruit(eventRanking.Client);
                //recruit.SpriteOverride = -1;
                recruit.Level = 1;
                recruit.Species = selectedSpecies.ID;
                recruit.Sex = Pokedex.GetPokemonForm(selectedSpecies.ID).GenerateLegalSex();
                recruit.Name = Pokedex.GetPokemon(selectedSpecies.ID).Name;
                recruit.Shiny = Enums.Coloration.Shiny;
                recruit.NpcBase = 0;

                recruit.GenerateMoveset();

                using (var dbConnection = new DatabaseConnection(DatabaseID.Players))
                {
                    eventRanking.Client.Player.AddToRecruitmentBank(dbConnection, recruit);
                }
            }

            return $"a shiny {selectedSpecies.Name}";
        }

        public override void OnNpcSpawn(IMap map, MapNpcPreset npc, MapNpc spawnedNpc, PacketHitList hitlist)
        {
            base.OnNpcSpawn(map, npc, spawnedNpc, hitlist);

            if (Data.Started)
            {
                if (!map.IsZoneOrObjectSandboxed())
                {
                    spawnedNpc.Unrecruitable = true;
                    spawnedNpc.Shiny = Server.Enums.Coloration.Shiny;

                    PacketBuilder.AppendNpcSprite(map, hitlist, spawnedNpc.MapSlot);
                }
            }
        }

        public override void OnNpcDeath(PacketHitList hitlist, ICharacter attacker, MapNpc npc)
        {
            base.OnNpcDeath(hitlist, attacker, npc);

            if (Data.Started)
            {
                var map = MapManager.RetrieveMap(attacker.MapID);
                if (!map.IsZoneOrObjectSandboxed())
                {
                    if (attacker.CharacterType == Enums.CharacterType.Recruit)
                    {
                        var npcDefinition = NpcManager.Npcs[npc.Species];
                        var owner = ((Recruit)attacker).Owner;

                        if (npc.Shiny == Enums.Coloration.Shiny && 
                            (npcDefinition.Behavior == Enums.NpcBehavior.AttackOnSight || npcDefinition.Behavior == Enums.NpcBehavior.AttackWhenAttacked || npcDefinition.Behavior == Enums.NpcBehavior.FleeOnsight))
                        {
                            if (Data.Scores.TryGetValue(owner.Player.CharID, out var userScore))
                            {
                                userScore.Score += 1;
                            }
                            else
                            {
                                userScore = new UserScore()
                                {
                                    Score = 1
                                };
                                Data.Scores.Add(owner.Player.CharID, userScore);
                            }

                            if (!userScore.FoundSpecies.Contains(npc.Species))
                            {
                                userScore.FoundSpecies.Add(npc.Species);
                            }

                            Messenger.PlayerMsg(owner, "You got a point!", Text.BrightGreen);
                        }
                    }
                }
            }
        }
    }
}
