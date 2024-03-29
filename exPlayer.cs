// This file is part of Mystery Dungeon eXtended.

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

using System;
using System.Collections.Generic;
using System.Text;

using Server.Scripting;
using Server.IO;
using Server.Players;
using System.Xml;
using Server;
using Server.Network;
using Server.Maps;
using Server.Database;
using Server.Database.MySql;
using Server.Fly;

namespace Script
{
    public class exPlayer : IExPlayer
    {
        public Client Client { get; set; }

        public DateTime LastGiftTime { get; set; }

        public string SpawnMap { get; set; }
        public int SpawnX { get; set; }
        public int SpawnY { get; set; }

        public int DungeonID { get; set; } //is this being used?
        public int DungeonX { get; set; }
        public int DungeonY { get; set; }
        public int DungeonMaxX { get; set; }
        public int DungeonMaxY { get; set; }
        public int DungeonSeed { get; set; }
        public PitchBlackAbyss.Room[,] DungeonMap { get; set; } //note: not saved to DB, re-made each time needed
        public bool DungeonGenerated { get; set; }
        public bool FirstMapLoaded { get; set; }

        public string HousingCenterMap { get; set; }
        public int HousingCenterX { get; set; }
        public int HousingCenterY { get; set; }
        
        public string PlazaEntranceMap { get; set; }
        public int PlazaEntranceX { get; set; }
        public int PlazaEntranceY { get; set; }

        //Move statuses
        //public bool[] MoveUsed { get; set; } ~may not need this; Last Resort, the only move that references this, can be re-coded to be based on moves with PP run out.
        public int LastMoveUsed { get; set; }
        public int TimesUsed { get; set; }

        public bool VoltorbFlip { get; set; }
        public bool WillBeCascoon { get; set; }

        public bool StoryEnabled { get; set; }
        public bool AnniversaryEnabled {
        	get {
        		return true;
        	}
        }
        //Buffs ~obsolete
        //public int StrBuffCount { get; set; }
        //public int DefBuffCount { get; set; }
        //public int SpeedBuffCount { get; set; }
        //public int SpclAtkBuffCount { get; set; }
        //public int SpclDefBuffCount { get; set; }


        //public int ToxicDamage { get; set; }

        //Transform
        //Aqua Ring
        //Wish
        //Counter

        public bool EvolutionActive { get; set; }

        public bool PermaMuted { get; set; }
        public bool DamageTurn { get; set; }

        #region CTF Vars

        public bool InCTF;
        public CTF.Teams CTFSide;
        public CTF.PlayerState CTFState;

        #endregion
        
        #region Snowball Game Vars
        
        public bool InSnowballGame;
        public SnowballGame.Teams SnowballGameSide;
        public int SnowballGameLives;
        public SnowballGame SnowballGameInstance;
        
        #endregion

        #region Electrostasis Tower

        public int ElectrolockCharge { get; set; }
        public int ElectrolockLevel { get; set; }
        public List<int> ElectrolockSublevel { get; set; }

        public List<string> ElectrolockSublevelTriggersActive { get; set; }


        #endregion

        public static exPlayer Get(Client client) {
            exPlayer exPlayer = client.Player.ExPlayer as exPlayer;
            if (exPlayer == null) {
                client.Player.ExPlayer = new exPlayer(client);
                exPlayer = client.Player.ExPlayer as exPlayer;
                exPlayer.Load();
            }
            return exPlayer;
        }

        public exPlayer(Client client) {
            Client = client;

            ElectrolockSublevel = new List<int>();

            ElectrolockSublevelTriggersActive = new List<string>();

            LastGiftTime = DateTime.UtcNow;
        }

        public void Load() {
            string query = "SELECT script_extras_general.SpawnMap, script_extras_general.SpawnX, script_extras_general.SpawnY, " +
                "script_extras_general.DungeonID, script_extras_general.DungeonX, script_extras_general.DungeonY, " +
                "script_extras_general.TurnsTaken, script_extras_general.EvolutionActive, script_extras_general.Permamuted, " +
                "script_extras_general.HousingCenterMap, script_extras_general.HousingCenterX, script_extras_general.HousingCenterY, " +
                "script_extras_general.DungeonMaxX, script_extras_general.DungeonMaxY, script_extras_general.DungeonSeed, " +
                "script_extras_general.PlazaEntranceMap, script_extras_general.PlazaEntranceX, script_extras_general.PlazaEntranceY " +
                "FROM script_extras_general " +
                "WHERE script_extras_general.CharID = \'" + Client.Player.CharID + "\'";

            DataColumnCollection row = null;
            using (DatabaseConnection dbConnection = new DatabaseConnection(DatabaseID.Players)) {
                row = dbConnection.Database.RetrieveRow(query);
            }
            if (row != null) {
                SpawnMap = row["SpawnMap"].ValueString;
                SpawnX = row["SpawnX"].ValueString.ToInt();
                SpawnY = row["SpawnY"].ValueString.ToInt();
                DungeonID = row["DungeonID"].ValueString.ToInt();
                DungeonX = row["DungeonX"].ValueString.ToInt();
                DungeonY = row["DungeonY"].ValueString.ToInt();
                //TurnsTaken = row["TurnsTaken"].ValueString.ToInt();
                EvolutionActive = row["EvolutionActive"].ValueString.ToBool();
                PermaMuted = row["Permamuted"].ValueString.ToBool();
                HousingCenterMap = row["HousingCenterMap"].ValueString;
                HousingCenterX = row["HousingCenterX"].ValueString.ToInt();
                HousingCenterY = row["HousingCenterY"].ValueString.ToInt();
                DungeonMaxX = row["DungeonMaxX"].ValueString.ToInt();
                DungeonMaxY = row["DungeonMaxY"].ValueString.ToInt();
                DungeonSeed = row["DungeonSeed"].ValueString.ToInt();
                
                PlazaEntranceMap = row["PlazaEntranceMap"].ValueString;
                PlazaEntranceX = row["PlazaEntranceX"].ValueString.ToInt();
                PlazaEntranceY = row["PlazaEntranceY"].ValueString.ToInt();
            }
            DungeonGenerated = false;
            DungeonMap = new PitchBlackAbyss.Room[1,1];
        }

        public void Save() {
            using (DatabaseConnection dbConnection = new DatabaseConnection(DatabaseID.Players)) { 
                dbConnection.Database.UpdateOrInsert("script_extras_general", new IDataColumn[] {
                    dbConnection.Database.CreateColumn(true, "CharID", Client.Player.CharID),
                    dbConnection.Database.CreateColumn(false, "SpawnMap", SpawnMap),
                    dbConnection.Database.CreateColumn(false, "SpawnX", SpawnX.ToString()),
                    dbConnection.Database.CreateColumn(false, "SpawnY", SpawnY.ToString()),
                    dbConnection.Database.CreateColumn(false, "DungeonID", DungeonID.ToString()),
                    dbConnection.Database.CreateColumn(false, "DungeonX", DungeonX.ToString()),
                    dbConnection.Database.CreateColumn(false, "DungeonY", DungeonY.ToString()),
                    //dbConnection.Database.CreateColumn(false, "TurnsTaken", TurnsTaken.ToString()),
                    dbConnection.Database.CreateColumn(false, "EvolutionActive", EvolutionActive.ToIntString()),
                    dbConnection.Database.CreateColumn(false, "Permamuted", PermaMuted.ToIntString()),
                    dbConnection.Database.CreateColumn(false, "HousingCenterMap", HousingCenterMap),
                    dbConnection.Database.CreateColumn(false, "HousingCenterX", HousingCenterX.ToString()),
                    dbConnection.Database.CreateColumn(false, "HousingCenterY", HousingCenterY.ToString()),
                    dbConnection.Database.CreateColumn(false, "DungeonMaxX", DungeonMaxX.ToString()),
                    dbConnection.Database.CreateColumn(false, "DungeonMaxY", DungeonMaxY.ToString()),
                    dbConnection.Database.CreateColumn(false, "DungeonSeed", DungeonSeed.ToString()),
                    dbConnection.Database.CreateColumn(false, "PlazaEntranceMap", PlazaEntranceMap),
                    dbConnection.Database.CreateColumn(false, "PlazaEntranceX", PlazaEntranceX.ToString()),
                    dbConnection.Database.CreateColumn(false, "PlazaEntranceY", PlazaEntranceY.ToString())
                });
            }
        }

        public bool VerifySpawnPoint() {
            //Messenger.PlayerMsg(Client, "1", Text.Black);
            try {
	            
	            if (Client.Player.MapID == MapManager.GenerateMapID(469) || Client.Player.MapID == MapManager.GenerateMapID(470)) {
	                if (Client.Player.HasItem(152) > 0) {
	                    Client.Player.TakeItem(152, 1);
	                }
	            }
	            return IsValidPlayerSpawn(SpawnMap);
            } catch (Exception ex) {
            	Messenger.AdminMsg("Error: VerifySpawnPoint", Text.Black);
            	return false;
            }
            
            
        }

        public bool IsValidPlayerSpawn(string mapID) {
            //Messenger.PlayerMsg(Client, "2", Text.Black);
            if (string.IsNullOrEmpty(mapID)) {
                return false;
            }
            var map = MapManager.RetrieveMap(mapID);
            switch (map.MapType)
            {
                case Enums.MapType.House:
                    {
                        var houseMap = (House)map;

                        return houseMap.OwnerID == Client.Player.CharID;
                    }
                case Enums.MapType.GuildBase:
                    {
                        var guildBaseMap = (GuildBase)map;

                        return Client.Player.GuildId == guildBaseMap.Owner;
                    }
                case Enums.MapType.Standard:
                    {
                        if (!mapID.StartsWith("s")) 
                        {
                            return false;
                        }

                        var mapNumber = mapID.Substring(1, mapID.Length - 1).ToInt();

                        var flightPoint = FlightManager.FindFlightPoint(mapNumber);

                        return flightPoint != null;
                    }
            }

            return false;
        }

        public void WarpToSpawn(bool playSound) {
            if (VerifySpawnPoint() == true) {
                Messenger.PlayerWarp(Client, MapManager.RetrieveMap(SpawnMap), SpawnX, SpawnY);
                //Messenger.AdminMsg(Client.Player.Name + " warped to spawn with X:" + SpawnX + ", Y:" + SpawnY, Text.Black);
            } else {
                SpawnMap = Main.Crossroads;
                SpawnX = 27;
                SpawnY = 28;
                Messenger.PlayerWarp(Client, Main.Crossroads, 27, 28, true, playSound);
            }
        }

        public void UnloadCTF() {
            InCTF = false;
            CTFSide = CTF.Teams.Red;
            CTFState = CTF.PlayerState.Free;
        }
        
        public void UnloadSnowballGame() {
        	InSnowballGame = false;
        	SnowballGameLives = 0;
        	SnowballGameInstance = null;
        	SnowballGameSide = SnowballGame.Teams.Green;
        }

    }
}
