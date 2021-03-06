﻿using System;
using System.Reflection.Emit;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace StardewHack.AlwaysScrollMap
{
    public class ModConfig {
        /** Whether the mod is enabled upon load. */
        public bool EnabledIndoors = true;
        public bool EnabledOutdoors = false;
        /** Which key should be used to toggle always scroll map. */
        public SButton ToggleScroll = SButton.OemSemicolon;
    }
    
    static class State {
        public static ModConfig config;
        public static bool Enabled() {
            if (StardewValley.Game1.currentLocation.IsOutdoors)
                return config.EnabledOutdoors;
            else
                return config.EnabledIndoors;
        }
    }

    public class ModEntry : HackWithConfig<ModEntry, ModConfig>
    {
        public override void Entry(IModHelper helper) {
            base.Entry(helper);
            InputEvents.ButtonPressed += ToggleScroll;
            State.config = config;
        }

        private void ToggleScroll(object sender, EventArgs e) {
            var ev = (EventArgsInput)e;
            if (ev.Button.Equals(config.ToggleScroll)) {
                if (StardewValley.Game1.currentLocation.IsOutdoors)
                    config.EnabledOutdoors ^= true;
                else
                    config.EnabledIndoors ^= true;
            }
        }

        [BytecodePatch("StardewValley.Game1::UpdateViewPort")]
        void Game1_UpdateViewPort()
        {
            var range = FindCode(
                // if (!Game1.viewportFreeze ...
                Instructions.Ldsfld(typeof(StardewValley.Game1), "viewportFreeze")
            );
            range.Extend(
                // if (Game1.currentLocation.forceViewportPlayerFollow)
                Instructions.Ldsfld(typeof(StardewValley.Game1), "currentLocation"),
                Instructions.Ldfld(typeof(StardewValley.GameLocation), "forceViewportPlayerFollow"),
                OpCodes.Brfalse
            );
            // Encapsulate with if (!State.Enabled) {
            range.Prepend(
                Instructions.Call(typeof(StardewHack.AlwaysScrollMap.State), "Enabled"),
                Instructions.Brtrue(AttachLabel(range.End[0]))
            );
        }
    }
}

