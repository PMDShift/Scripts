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

using Server;
using Server.Scripting;
using System;
using System.Drawing;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Server.Network;
using Server.Maps;
using Server.Database;
using Server.Players;

namespace Script 
{
    public static class Dates
    {
        public static DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            // The (... + 7) % 7 ensures we end up with a value in the range [0, 6]
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd);
        }

        public static DateTime GetNextWeekday(DayOfWeek dayOfWeek, int hour, int minute)
        {
            var weekday = GetNextWeekday(DateTime.UtcNow, dayOfWeek);

            var eventDate = new DateTime(weekday.Year, weekday.Month, weekday.Day, hour, minute, 0, DateTimeKind.Utc);

            if (eventDate < DateTime.UtcNow)
            {
                weekday = GetNextWeekday(DateTime.UtcNow.AddDays(1), dayOfWeek);
                eventDate = new DateTime(weekday.Year, weekday.Month, weekday.Day, hour, minute, 0, DateTimeKind.Utc);
            }

            return eventDate;
        }
	}
}
