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
using System.Text;
using Server.Fly;

namespace Script
{
    public partial class Main
    {
        public static void InitializeFlyPoints() {
            FlightManager.ClearFlightPoints();

            FlightManager.AddFlightPoint(new FlightPoint() { ID = 0, Name = "Florea Town", MapNumber = 5, ImageName = "florea.png", Description = "Where everything blooms beautifully." });
            FlightManager.AddFlightPoint(new FlightPoint() { ID = 1, Name = "Newpond Village", MapNumber = 54, ImageName = "newpond.png", Description = "Fresh air and fresh water." });
            FlightManager.AddFlightPoint(new FlightPoint() { ID = 2, Name = "Dream Village", MapNumber = 35, ImageName = "dream village.png", Description = "Hidden within the cherry blossoms." });
            FlightManager.AddFlightPoint(new FlightPoint() { ID = 3, Name = "Borealis City", MapNumber = 224, ImageName = "borealis.png", Description = "A city that rose from a glorious history." });
            FlightManager.AddFlightPoint(new FlightPoint() { ID = 4, Name = "Mysveil Town", MapNumber = 358, ImageName = "mysveil.png", Description = "Beyond the veil of time." });
            FlightManager.AddFlightPoint(new FlightPoint() { ID = 5, Name = "Moonhaven", MapNumber = 490, ImageName = "moonhaven.png", Description = "A friendly bat town located inside a long-dormant ocean volcano." });
            FlightManager.AddFlightPoint(new FlightPoint() { ID = 6, Name = "Bayside Bazaar", MapNumber = 258, Description = "The bazaar down by the bay!" });
            FlightManager.AddFlightPoint(new FlightPoint() { ID = 7, Name = "Aurora Highland", MapNumber = 246, ImageName = "aurora highlands.png", Description = "The mountains of Aurora." });
            FlightManager.AddFlightPoint(new FlightPoint() { ID = 8, Name = "Debug", MapNumber = 1, Description = "Test unreleased content here." });
            FlightManager.AddFlightPoint(new FlightPoint() { ID = 9, Name = "Isle of Life", MapNumber = 477, Description = "A sacred island known to hold mythical powers." });
            FlightManager.AddFlightPoint(new FlightPoint() { ID = 10, Name = "Somoria", MapNumber = 1189, Description = "A home for nocturnal pokemon." });
            FlightManager.AddFlightPoint(new FlightPoint() { ID = 11, Name = "Submersia", MapNumber = 522, Description = "An aquatic city deep beneath the ocean waves." });
            FlightManager.AddFlightPoint(new FlightPoint() { ID = 12, Name = "Dryspring", MapNumber = 1459, Description = "A little dry town, home to many." });
        }
    }
}
