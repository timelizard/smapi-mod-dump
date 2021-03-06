﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpaceCore.Events;
using StardewModdingAPI.Events;
using Magic.Game.Interface;
using Magic.Schools;
using Magic.Spells;
using StardewValley;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Magic.Mod;
using SpaceCore;
using Magic.Other;
using StardewModdingAPI;
using StardewValley.Network;
using Newtonsoft.Json;
using System.IO;

namespace Magic
{
    // TODO: Refactor this mess
    static class Magic
    {
        private static Texture2D spellBg;
        private static Texture2D manaBg;
        private static Texture2D manaFg;

        public const string MSG_DATA = "spacechase0.Magic.Data";
        public const string MSG_MINIDATA = "spacechase0.Magic.MiniData";
        public const string MSG_CAST = "spacechase0.Magic.Cast";

        internal static void init()
        {
            loadAssets();

            SpellBook.init();

            SaveEvents.AfterLoad += afterLoad;

            InputEvents.ButtonPressed += onKeyPress;
            InputEvents.ButtonReleased += onKeyRelease;
            
            TimeEvents.AfterDayStarted += onNewDay;
            PlayerEvents.Warped += EvacSpell.onLocationChanged;
            MineEvents.MineLevelChanged += EvacSpell.onMineLevelChanged;
            PlayerEvents.Warped += eventChecks;
            
            SpaceEvents.OnBlankSave += onBlankSave;
            SpaceEvents.ShowNightEndMenus += showMagicLevelMenus;
            SpaceEvents.OnItemEaten += onItemEaten;
            SpaceEvents.ActionActivated += actionTriggered;
            SpaceCore.SpaceCore.RegisterMessageHandler(MSG_DATA, onNetworkData);
            SpaceCore.SpaceCore.RegisterMessageHandler(MSG_MINIDATA, onNetworkMiniData);
            SpaceCore.SpaceCore.RegisterMessageHandler(MSG_CAST, onNetworkCast);
            SpaceEvents.ServerGotClient += onClientConnected;

            GraphicsEvents.OnPostRenderHudEvent += renderHud;

            checkForExperienceBars();

            Command.register("player_addmana", addManaCommand);
            Command.register("player_setmaxmana", setMaxManaCommand);
            Command.register("player_learnschool", learnSchoolCommand);
            Command.register("player_learnspell", learnSpellCommand);
            Command.register("magicmenu", magicMenuCommand);
        }

        private static void onNetworkData(IncomingMessage msg)
        {
            int count = msg.Reader.ReadInt32();
            for ( int i = 0; i < count; ++i )
            {
                Mod.Data.players[msg.Reader.ReadInt64()] = JsonConvert.DeserializeObject<MultiplayerSaveData.PlayerData>(msg.Reader.ReadString());
            }
        }

        private static void onNetworkMiniData(IncomingMessage msg)
        {
            Mod.Data.players[msg.FarmerID].mana = msg.Reader.ReadInt32();
            Mod.Data.players[msg.FarmerID].manaCap = msg.Reader.ReadInt32();
            Mod.Data.players[msg.FarmerID].magicLevel = msg.Reader.ReadInt32();
            Mod.Data.players[msg.FarmerID].magicExp = msg.Reader.ReadInt32();
        }

        private static void onNetworkCast( IncomingMessage msg )
        {
            Game1.getFarmer(msg.FarmerID).castSpell(msg.Reader.ReadString(), msg.Reader.ReadInt32());
        }

        private static void onClientConnected(object sender, EventArgsServerGotClient args)
        {
            if ( !Data.players.ContainsKey( args.FarmerID ) )
                Data.players[args.FarmerID] = new MultiplayerSaveData.PlayerData();

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((int)Data.players.Count);
                foreach (var entry in Data.players)
                {
                    writer.Write(entry.Key);
                    writer.Write(JsonConvert.SerializeObject(entry.Value, MultiplayerSaveData.networkSerializerSettings));
                }
                SpaceCore.SpaceCore.BroadcastMessage(Magic.MSG_DATA, stream.ToArray());
            }
        }

        private static void onBlankSave( object sender, EventArgs args )
        {
            placeAltar("FarmCave", 5, 2, 54 * 3, SchoolId.Toil);
            placeAltar("Woods", 49, 26, 54 * 2, SchoolId.Nature);
            placeAltar("SeedShop", 36, 16, 54 * 4, SchoolId.Life);
            placeAltar("WizardHouseBasement", 8, 3, 54 * 1, SchoolId.Elemental);
            placeAltar("WitchHut", 6, 8, 54 * 7, SchoolId.Eldritch);
        }

        public static void afterLoad( object sender, EventArgs args )
        {
            Data.players[ Game1.player.UniqueMultiplayerID ].spellBook.Owner = Game1.player;
            foreach ( var farmer in Game1.otherFarmers )
            {
                Data.players[farmer.Key].spellBook.Owner = farmer.Value;
            }
        }

        public static void renderHud( object sender, EventArgs args )
        {
            if (Game1.activeClickableMenu != null || Game1.eventUp) return;
            if (Game1.player.getMaxMana() == 0) return;
            SpriteBatch b = Game1.spriteBatch;
            
            Vector2 manaPos = new Vector2(20, Game1.viewport.Height - manaBg.Height * 4 - 20);
            b.Draw(manaBg, manaPos, new Rectangle(0, 0, manaBg.Width, manaBg.Height), Color.White, 0, new Vector2(), 4, SpriteEffects.None, 1);
            if (Game1.player.getCurrentMana() > 0)
            {
                Rectangle targetArea = new Rectangle(3, 13, 6, 41);
                float perc = Game1.player.getCurrentMana() / (float)Game1.player.getMaxMana();
                int h = (int)(targetArea.Height * perc);
                targetArea.Y += targetArea.Height - h;
                targetArea.Height = h;

                targetArea.X *= 4;
                targetArea.Y *= 4;
                targetArea.Width *= 4;
                targetArea.Height *= 4;
                targetArea.X += (int)manaPos.X;
                targetArea.Y += (int)manaPos.Y;
                b.Draw(manaFg, targetArea, new Rectangle(0, 0, 1, 1), Color.White);
            }

            Point[] spots =
            new Point[4]/*
            {
                new Point((int)manaPos.X + manaBg.Width * 4 + 20 + 40, Game1.viewport.Height - 20 - 50 - 30 - 50 - 25),
                new Point((int)manaPos.X + manaBg.Width * 4 + 20 + 50 + 30, Game1.viewport.Height - 20 - 50 - 40 - 25),
                new Point((int)manaPos.X + manaBg.Width * 4 + 20 + 40, Game1.viewport.Height - 20 - 50 - 25 ),
                new Point((int)manaPos.X + manaBg.Width * 4 + 20, Game1.viewport.Height - 20 - 50 - 40 - 25 ),
            };*/
            {
                new Point( (int)manaPos.X + manaBg.Width * 4 + 20 + 60 * 0, Game1.viewport.Height - 20 - 50 - 60 * 3 ),
                new Point( (int)manaPos.X + manaBg.Width * 4 + 20 + 60 * 0, Game1.viewport.Height - 20 - 50 - 60 * 2 ),
                new Point( (int)manaPos.X + manaBg.Width * 4 + 20 + 60 * 0, Game1.viewport.Height - 20 - 50 - 60 * 1 ),
                new Point( (int)manaPos.X + manaBg.Width * 4 + 20 + 60 * 0, Game1.viewport.Height - 20 - 50 - 60 * 0 ),
            };

            SpellBook book = Game1.player.getSpellBook();
            if (book == null || book.selectedPrepared >= book.prepared.Length)
                return;
            PreparedSpell[] prepared = book.getPreparedSpells();

            for (int i = 0; i < spots.Length; ++i)
            {
                b.Draw(spellBg, new Rectangle(spots[i].X, spots[i].Y, 50, 50), Color.White);
            }

            string prepStr = (book.selectedPrepared + 1) + "/" + book.prepared.Length;
            b.DrawString(Game1.dialogueFont, prepStr, new Vector2(spots[Game1.down].X + 25 + 2, spots[Game1.up].Y - 35 + 0), Color.Black, 0, new Vector2(Game1.dialogueFont.MeasureString(prepStr).X / 2, 0), 0.6f, SpriteEffects.None, 0);
            b.DrawString(Game1.dialogueFont, prepStr, new Vector2(spots[Game1.down].X + 25 - 2, spots[Game1.up].Y - 35 + 0), Color.Black, 0, new Vector2(Game1.dialogueFont.MeasureString(prepStr).X / 2, 0), 0.6f, SpriteEffects.None, 0);
            b.DrawString(Game1.dialogueFont, prepStr, new Vector2(spots[Game1.down].X + 25 + 0, spots[Game1.up].Y - 35 + 2), Color.Black, 0, new Vector2(Game1.dialogueFont.MeasureString(prepStr).X / 2, 0), 0.6f, SpriteEffects.None, 0);
            b.DrawString(Game1.dialogueFont, prepStr, new Vector2(spots[Game1.down].X + 25 + 0, spots[Game1.up].Y - 35 - 2), Color.Black, 0, new Vector2(Game1.dialogueFont.MeasureString(prepStr).X / 2, 0), 0.6f, SpriteEffects.None, 0);
            b.DrawString(Game1.dialogueFont, prepStr, new Vector2(spots[Game1.down].X + 25, spots[Game1.up].Y - 35), Color.White, 0, new Vector2(Game1.dialogueFont.MeasureString(prepStr).X / 2, 0), 0.6f, SpriteEffects.None, 0);

            for (int i = 0; i < Math.Min(spots.Length, prepared.Length); ++i)
            {
                PreparedSpell prep = prepared[i];
                if (prep == null)
                    continue;

                Spell spell = SpellBook.get(prep.SpellId);
                if (spell == null || spell.Icons.Length <= prep.Level || spell.Icons[prep.Level] == null)
                    continue;
                
                b.Draw(spell.Icons[prep.Level], new Rectangle(spots[i].X, spots[i].Y, 50, 50), Game1.player.canCastSpell(spell, prep.Level) ? Color.White : new Color(128, 128, 128));
            }
        }

        private static void loadAssets()
        {
            spellBg = Content.loadTexture("interface/spellbg.png");
            manaBg = Content.loadTexture("interface/manabg.png");

            Color manaCol = new Color(0, 48, 255);
            manaFg = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            manaFg.SetData(new Color[] { manaCol });
        }

        private static bool castPressed = false;
        private static void onKeyPress(object sender, EventArgsInput args)
        {
            if (args.Button == Config.Key_Cast)
            {
                castPressed = true;
            }

            if (Data == null || Game1.activeClickableMenu != null) return;
            if (args.Button == Config.Key_SwapSpells)
            {
                Game1.player.getSpellBook().swapPreparedSet();
            }
            else if (castPressed &&
                      (args.Button == Config.Key_Spell1 || args.Button == Config.Key_Spell2 ||
                        args.Button == Config.Key_Spell3 || args.Button == Config.Key_Spell4))
            {
                int slot = 0;
                if (args.Button == Config.Key_Spell1) slot = 0;
                else if (args.Button == Config.Key_Spell2) slot = 1;
                else if (args.Button == Config.Key_Spell3) slot = 2;
                else if (args.Button == Config.Key_Spell4) slot = 3;

                args.SuppressButton();

                SpellBook book = Game1.player.getSpellBook();
                PreparedSpell[] prepared = book.getPreparedSpells();
                if (prepared[slot] == null)
                    return;
                PreparedSpell prep = prepared[slot];

                Spell toCast = SpellBook.get(prep.SpellId);
                if (toCast == null)
                    return;

                //Log.trace("MEOW " + prep.SpellId + " " + prep.Level + " " + Game1.player.canCastSpell(toCast, prep.Level));
                if (Game1.player.canCastSpell(toCast, prep.Level))
                {
                    Log.trace("Casting " + prep.SpellId);
                    Game1.player.castSpell(toCast, prep.Level);
                    Game1.player.addMana(-toCast.getManaCost(Game1.player, prep.Level));
                }
            }
        }

        private static void onKeyRelease(object sender, EventArgsInput args)
        {
            if (args.Button == Config.Key_Cast)
            {
                castPressed = false;
            }
        }

        private static void onNewDay(object sender, EventArgs args)
        {
            Game1.player.addMana(Game1.player.getMaxMana());
        }

        private static void eventChecks( object sender, EventArgsPlayerWarped args )
        {
            if ( args.NewLocation.Name == "WizardHouse" && !Game1.player.eventsSeen.Contains( 90000 ) &&
                 Game1.player.friendshipData.ContainsKey( "Wizard" ) && Game1.player.friendshipData[ "Wizard" ].Points > 750 )
            {
                args.NewLocation.currentEvent = new Event("WizardSong/0 5/Wizard 8 5 0 farmer 8 15 0/move farmer 0 -8 0/speak Wizard \"TODO#$b#Find one of the five altars and learn some magic.#$b#Q to start casting, then 1-4 to choose the spell.#$b#TAB to switch between spell sets.\"/textAboveHead Wizard \"MAGIC\"/pause 750/fade 750/end", 90000);
                Game1.eventUp = true;
                Game1.displayHUD = false;
                Game1.player.CanMove = false;
                Game1.player.showNotCarrying();
                
                Game1.player.addMagicExp(Game1.player.getMagicExpForNextLevel());
                Game1.player.addMana(Game1.player.getMaxMana());
                Game1.player.eventsSeen.Add(90000);
            }
        }

        private static void actionTriggered(object sender, EventArgsAction args)
        {
            string[] actionArgs = args.ActionString.Split(' ');
            if (args.Action == "MagicAltar")
            {
                if ( !Game1.player.eventsSeen.Contains(90000) )
                {
                    Game1.drawObjectDialogue("A glowing altar.");
                }
                else if (!Game1.player.knowsSchool(actionArgs[1]))
                {
                    /*if (Game1.player.getSpellBook().knownSchools.Count >= Game1.player.countStardropsEaten() + 1)
                    {
                        Game1.drawObjectDialogue("You lack the power to use this.");
                    }
                    else*/
                    {
                        Game1.playSound("secret1");
                        Game1.player.getSpellBook().knownSchools.Add(actionArgs[1]);
                        Game1.drawObjectDialogue("You are now attuned to " + actionArgs[1] + ".");
                    }
                }
                else
                {
                    Game1.playSound("secret1");
                    Game1.activeClickableMenu = new MagicMenu(School.getSchool(actionArgs[1]));
                }
            }
        }

        internal static List<int> newMagicLevels = new List<int>();
        private static void showMagicLevelMenus(object sender, EventArgsShowNightEndMenus args)
        {
            if (newMagicLevels.Count() > 0)
            {
                for (int i = newMagicLevels.Count() - 1; i >= 0; --i)
                {
                    int level = newMagicLevels[i];
                    Log.debug("Doing " + i + ": magic level " + level + " screen");

                    if (Game1.activeClickableMenu != null)
                        Game1.endOfNightMenus.Push(Game1.activeClickableMenu);
                    Game1.activeClickableMenu = new MagicLevelUpMenu(level);
                }
                newMagicLevels.Clear();
            }
        }

        private static void onItemEaten(object sender, EventArgs args)
        {
            if (Game1.player.itemToEat.ParentSheetIndex == ja.GetObjectId("Magic Elixir"))
                Game1.player.addMana(Game1.player.getMaxMana());
        }

        public static void placeAltar(string locName, int x, int y, int baseAltarIndex, string school)
        {
            Log.debug("Placing altar @ " + locName + "(" + x + ", " + y + ")");

            // AddTileSheet sorts the tilesheets by ID after adding them.
            // The game sometimes refers to tilesheets by their index (such as in Beach.fixBridge)
            // Prepending this to the ID should ensure that this tilesheet is added to the end,
            // which preserves the normal indices of the tilesheets.
            char comeLast = '\u03a9'; // Omega

            GameLocation loc = Game1.getLocationFromName(locName);

            Dictionary<int, SpaceCore.Content.TileAnimation> anims;
            var tileSheet = Content.loadTilesheet("altarsobjects", loc.Map, out anims);
            tileSheet.Id = comeLast + tileSheet.Id;
            loc.map.AddTileSheet(tileSheet);
            if (Game1.currentLocation == loc)
                loc.map.LoadTileSheets(Game1.mapDisplayDevice);

            var front = loc.Map.GetLayer("Front");
            var buildings = loc.Map.GetLayer("Buildings");

            front.Tiles[x + 0, y - 1] = anims[baseAltarIndex + 0 + 0 * 18].makeTile(tileSheet, front);
            front.Tiles[x + 1, y - 1] = anims[baseAltarIndex + 1 + 0 * 18].makeTile(tileSheet, front);
            front.Tiles[x + 2, y - 1] = anims[baseAltarIndex + 2 + 0 * 18].makeTile(tileSheet, front);
            buildings.Tiles[x + 0, y + 0] = anims[baseAltarIndex + 0 + 1 * 18].makeTile(tileSheet, buildings);
            buildings.Tiles[x + 1, y + 0] = anims[baseAltarIndex + 1 + 1 * 18].makeTile(tileSheet, buildings);
            buildings.Tiles[x + 2, y + 0] = anims[baseAltarIndex + 2 + 1 * 18].makeTile(tileSheet, buildings);
            buildings.Tiles[x + 0, y + 1] = anims[baseAltarIndex + 0 + 2 * 18].makeTile(tileSheet, buildings);
            buildings.Tiles[x + 1, y + 1] = anims[baseAltarIndex + 1 + 2 * 18].makeTile(tileSheet, buildings);
            buildings.Tiles[x + 2, y + 1] = anims[baseAltarIndex + 2 + 2 * 18].makeTile(tileSheet, buildings);
            loc.setTileProperty(x + 0, y + 0, "Buildings", "Action", "MagicAltar " + school);
            loc.setTileProperty(x + 1, y + 0, "Buildings", "Action", "MagicAltar " + school);
            loc.setTileProperty(x + 2, y + 0, "Buildings", "Action", "MagicAltar " + school);
            loc.setTileProperty(x + 0, y + 1, "Buildings", "Action", "MagicAltar " + school);
            loc.setTileProperty(x + 1, y + 1, "Buildings", "Action", "MagicAltar " + school);
            loc.setTileProperty(x + 2, y + 1, "Buildings", "Action", "MagicAltar " + school);
        }

        internal static Texture2D expIcon = Content.loadTexture("interface/magicexpicon.png");
        private static void checkForExperienceBars()
        {
            if (!Mod.instance.Helper.ModRegistry.IsLoaded("spacechase0.ExperienceBars"))
            {
                Log.info("Experience Bars not found");
                return;
            }
            
            Log.info("Experience Bars found, adding magic experience bar renderer.");
            GraphicsEvents.OnPostRenderHudEvent += drawExperienceBar;
        }

        private static void drawExperienceBar(object sender, EventArgs args_)
        {
            if (Game1.activeClickableMenu != null)
                return;

            try
            {
                int level = Game1.player.getMagicLevel();
                int exp = Game1.player.getMagicExp();

                int haveExp = exp;
                int needExp = Game1.player.getMagicExpForNextLevel();
                float progress = (float)haveExp / needExp;
                if (level == 50)
                {
                    progress = -1;
                }

                var api = Mod.instance.Helper.ModRegistry.GetApi<ExperienceBarsApi>("spacechase0.ExperienceBars");
                if (api == null)
                {
                    Log.warn("No experience bars API? Turning off");
                    GraphicsEvents.OnPostRenderHudEvent -= drawExperienceBar;
                }
                api.DrawExperienceBar(expIcon, level, progress, new Color(0, 66, 255));
            }
            catch (Exception e)
            {
                Log.error("Exception rendering magic experience bar: " + e);
                GraphicsEvents.OnPostRenderHudEvent -= drawExperienceBar;
            }
        }

        private static void addManaCommand(string[] args)
        {
            Game1.player.addMana(int.Parse(args[0]));
        }
        private static void setMaxManaCommand(string[] args)
        {
            Game1.player.setMaxMana(int.Parse(args[0]));
        }
        private static void learnSchoolCommand(string[] args)
        {
            if (args.Length != 1 || (args.Length > 0 && args[0] == ""))
                Log.info("Usage: player_learnschool <school>");
            else if (args[0] == "all")
            {
                foreach (var school in School.getSchoolList())
                    Game1.player.learnSchool(school);
            }
            else if (!School.getSchoolList().Contains(args[0]))
                Log.error($"School '{args[0]}' does not exist.");
            else if (!Game1.player.knowsSchool(args[0]))
                Game1.player.learnSchool(args[0]);
        }
        private static void learnSpellCommand(string[] args)
        {
            if (args.Length == 1 && args[0] == "all")
            {
                foreach (var spellName in SpellBook.getAll())
                    Game1.player.learnSpell(SpellBook.get(spellName), SpellBook.get(spellName).getMaxCastingLevel(), true);
                return;
            }

            if (args.Length != 2 || (args.Length > 1 && (args[0] == "" || args[1] == "")))
            {
                Log.info("Usage: player_learnspell <spell> <level>");
                return;
            }

            Spell spell = SpellBook.get(args[0]);
            if (spell == null)
            {
                Log.error($"Spell '{args[0]}' does not exist.");
                return;
            }

            int level;
            if (!Int32.TryParse(args[1], out level))
            {
                Log.error($"That spell only casts up to level {spell.getMaxCastingLevel()}.");
                return;
            }

            Game1.player.learnSpell(spell, level, true);
        }
        private static void magicMenuCommand(string[] args)
        {
            Game1.activeClickableMenu = new MagicMenu();
        }
    }
}
