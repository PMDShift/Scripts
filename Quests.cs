// This file is part of PMD: Shift!.

// Copyright (C) 2019 BurningBlaze

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
using System.Linq;
using System.Text;
using Server.Fly;
using Server.Network;

namespace Script
{
    public partial class Main
    {
        public static List<int> IncompleteRegionQuests { get; set; }

        public static void InitializeIncompleteRegionQuests()
        {
            IncompleteRegionQuests = new List<int>();

            IncompleteRegionQuests.Add(2);
            IncompleteRegionQuests.Add(4);
            IncompleteRegionQuests.Add(5);
        }

        public static int GetNextIncompleteQuestId(Client client)
        {
            foreach (var questId in IncompleteRegionQuests) 
            {
                if (!client.Player.QuestLog.Where(x => x.QuestId == questId).Any())
                {
                    return questId;
                }
            }

            return -1;
        }
        
    }
}

