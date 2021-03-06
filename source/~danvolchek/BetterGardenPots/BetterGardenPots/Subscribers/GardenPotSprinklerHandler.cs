﻿using System;
using System.Collections.Generic;
using System.Linq;
using BetterGardenPots.APIs;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace BetterGardenPots.Subscribers
{
    internal class GardenPotSprinklerHandler : IEventSubscriber
    {
        private ISimpleSprinklersAPI simpleSprinklersAPI;
        private IBetterSprinklersAPI betterSprinklersAPI;

        private readonly IModHelper helper;

        public GardenPotSprinklerHandler(IModHelper helper)
        {
            this.helper = helper;
            GameEvents.FirstUpdateTick += this.GameEvents_FirstUpdateTick;
        }

        private bool isSubscribed;

        public void Subscribe()
        {
            if (this.isSubscribed)
                return;

            TimeEvents.AfterDayStarted += this.TimeEvents_AfterDayStarted;

            this.isSubscribed = false;
        }

        public void Unsubscribe()
        {
            if (!this.isSubscribed)
                return;

            TimeEvents.AfterDayStarted -= this.TimeEvents_AfterDayStarted;

            this.isSubscribed = true;
        }

        private void GameEvents_FirstUpdateTick(object sender, EventArgs e)
        {
            if (this.helper.ModRegistry.IsLoaded("tZed.SimpleSprinkler"))
            {
                this.simpleSprinklersAPI = this.helper.ModRegistry.GetApi<ISimpleSprinklersAPI>("tZed.SimpleSprinkler");
            }

            if (this.helper.ModRegistry.IsLoaded("Speeder.BetterSprinklers"))
            {
                this.betterSprinklersAPI = this.helper.ModRegistry.GetApi<IBetterSprinklersAPI>("Speeder.BetterSprinklers");
            }
        }

        private void TimeEvents_AfterDayStarted(object sender, EventArgs e)
        {
            foreach (GameLocation location in GetMainAndInnerLocations(Game1.locations))
                foreach (Vector2 wateredTile in this.GetWateredTiles(location))
                {
                    if (location.Objects.TryGetValue(wateredTile, out SObject item) && item is IndoorPot pot && pot.hoeDirt.Value != null)
                    {
                        pot.hoeDirt.Value.state.Value = 1;
                        pot.showNextIndex.Value = true;
                    }
                }
        }

        private static IEnumerable<GameLocation> GetMainAndInnerLocations(IEnumerable<GameLocation> locations)
        {
            foreach (GameLocation loc in locations)
            {
                yield return loc;
                if (loc is BuildableGameLocation buildableLoc)
                    foreach (GameLocation l in buildableLoc.buildings.Select(item => item.indoors.Value))
                        yield return l;
            }
        }

        private IEnumerable<Vector2> GetWateredTiles(GameLocation location)
        {
            if (location == null)
                yield break;

            foreach (KeyValuePair<Vector2, SObject> item in location.Objects.Pairs)
                if (item.Value.Name.ToLower().Contains("sprinkler"))
                    foreach (Vector2 pos in this.GetRange(item.Value, item.Key))
                        yield return pos;
        }


        public IEnumerable<Vector2> GetRange(SObject obj, Vector2 position)
        {
            if (this.betterSprinklersAPI == null)
            {
                foreach (Vector2 pos in this.GetDefaultRange(obj, position))
                    yield return pos;
            }
            else if (this.betterSprinklersAPI.GetSprinklerCoverage().TryGetValue(obj.ParentSheetIndex, out Vector2[] bExtra))
                foreach (Vector2 extraPos in bExtra)
                    yield return extraPos + position;

            if (this.simpleSprinklersAPI != null && this.simpleSprinklersAPI.GetNewSprinklerCoverage().TryGetValue(obj.ParentSheetIndex, out Vector2[] sExtra))
                foreach (Vector2 extraPos in sExtra)
                    yield return extraPos + position;
        }

        public IEnumerable<Vector2> GetDefaultRange(SObject obj, Vector2 position)
        {
            string objectName = obj.Name.ToLower();

            yield return new Vector2(position.X - 1, position.Y);
            yield return new Vector2(position.X + 1, position.Y);
            yield return new Vector2(position.X, position.Y - 1);
            yield return new Vector2(position.X, position.Y + 1);

            if (objectName.Contains("quality") || objectName.Contains("iridium"))
            {
                yield return new Vector2(position.X - 1, position.Y - 1);
                yield return new Vector2(position.X + 1, position.Y - 1);
                yield return new Vector2(position.X - 1, position.Y + 1);
                yield return new Vector2(position.X + 1, position.Y + 1);

                if (objectName.Contains("iridium"))
                {
                    for (int i = -2; i < 3; i++)
                    {
                        for (int j = -2; j < 3; j++)
                        {
                            if (Math.Abs(i) == 2 || Math.Abs(j) == 2)
                            {
                                yield return new Vector2(position.X + i, position.Y + j);
                            }
                        }
                    }
                }
            }
        }
    }
}
