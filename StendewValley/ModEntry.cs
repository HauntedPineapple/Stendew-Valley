using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Monsters;

namespace StendewValley
{
    /// <summary>The mod entry point</summary>
    internal sealed class ModEntry : Mod
    {


        #region Entry Method
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // Hook up Global Classes
            Globals.Helper = this.Helper;           // Can be referenced by Globals.Helper
            Globals.Monitor = this.Monitor;         // Can be referenced by Globals.Logger.Log(...)
            Globals.Manifest = this.ModManifest;    // Can be referenced by Globals.Manifest
            Globals.Info = new ModInfo();           // Can be referenced by Globals.Info

            // Set the intial ModInfo variable states
            Globals.Info.monstersPeaceful = true;

            // Hook up button press event
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;

            // Instantiate harmony patcher
            var harmony = new Harmony(ModManifest.UniqueID);
        }
        #endregion


        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;


            // print button presses to the console window
            this.Monitor.Log($"{Game1.player.Name} pressed {e.Button}.", LogLevel.Debug);
        }

        // Monster methods

        /// <summary>
        /// Stops the slime from jumping towards the player if Sten's bald cap is being worn
        /// </summary>
        /// <param name="__instance">Slime monster instance</param>
        /// <param name="time">In game time in ticks (required parameter for base)</param>
        /// <param name="___readyToJump">Reference to the readyToJump varaible in base</param>
        private static void GreenSlime_behaviorAtGameTick_prefix(GreenSlime __instance, GameTime time, ref int ___readyToJump)
        {
            if (Globals.Info.monstersPeaceful && __instance.Health == __instance.MaxHealth)
            {
                ___readyToJump = -1; // Slime will not attack
            }
        }

        /// <summary>
        /// Stops the dust spirit from attacking or fleeing from player if Sten's bald cap is being worn
        /// </summary>
        /// <param name="__instance">Dust Spirit instanc</param>
        /// <param name="___runningAwayFromFarmer">Reference to the runningAwayFromFarmer variable in base</param>
        /// <param name="___chargingFarmer">Reference to the chargingFarmer variable in base</param>
        private static void DustSpirit_behaviorAtGameTick_postfix(DustSpirit __instance, ref bool ___runningAwayFromFarmer, ref bool ___chargingFarmer)
        {
            if (Globals.Info.monstersPeaceful && __instance.Health == __instance.MaxHealth)
            {
                ___runningAwayFromFarmer = false;   // Dust spirit won't flee
                ___chargingFarmer = false;          // Dust spirit won't charge
            }
        }
    }
}