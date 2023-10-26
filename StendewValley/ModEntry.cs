using System;
using System.Runtime.CompilerServices;
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
        private static ModConfig Config;

        #region Entry Method
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // Hook up Global Classes
            Globals.Helper = this.Helper;           // Can be referenced by Globals.Helper
            Globals.Monitor = this.Monitor;         // Can be referenced by Globals.Monitor.Log(...)
            Globals.Manifest = this.ModManifest;    // Can be referenced by Globals.Manifest
            Globals.Info = new ModInfo();           // Can be referenced by Globals.Info

            Config = this.Helper.ReadConfig<ModConfig>();

            Helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;

            // Set the intial ModInfo variable states
            Globals.Info.monstersPeaceful = true;

            // Hook up button press event
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;

            // Instantiate harmony patcher
            var harmony = new Harmony(ModManifest.UniqueID);

            try
            {
                // Monsters damage method
                harmony.Patch(
                   original: AccessTools.Method(typeof(GameLocation), "damageMonster", new Type[] { typeof(Microsoft.Xna.Framework.Rectangle), typeof(int), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(float), typeof(float), typeof(bool), typeof(Farmer) }),
                   postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.damageMonster_Postfix))
                );

                // Individual monster behaviours
                /*
                harmony.Patch(
                   original: AccessTools.Method(typeof(GreenSlime), nameof(GreenSlime.behaviorAtGameTick)),
                   prefix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.GreenSlime_behaviorAtGameTick_prefix))
                );
                harmony.Patch(
                   original: AccessTools.Method(typeof(DustSpirit), nameof(DustSpirit.behaviorAtGameTick)),
                   postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.DustSpirit_behaviorAtGameTick_postfix))
                );
                */
                harmony.Patch(
                   original: AccessTools.Method(typeof(Monster), nameof(Monster.updateMovement)),
                   prefix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Monster_updateMovement_prefix))
                );

                this.Monitor.Log("Patches succeeded", LogLevel.Debug);
            }
            catch (Exception ex) 
            {
                this.Monitor.Log("Something failed", LogLevel.Debug);
            }
        }
        #endregion


        // Game launched event
        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is not null)
            {
                this.Monitor.Log($"WE DID IT REDDIT!", LogLevel.Debug);

                // register mod
                configMenu.Register(
                    mod: ModManifest,
                    reset: () => Config = new ModConfig(),
                    save: () => Helper.WriteConfig(Config)
                );

                // Add options
                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => "Peaceful Enemies",
                    getValue: () => Config.PassiveMobs,
                    setValue: value => Config.PassiveMobs = value
                );
            }
        }


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
        /// 
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        private static bool Monster_updateMovement_prefix(Monster __instance, GameTime time)
        {
            if ((__instance.Health <= 0 && __instance.IsWalkingTowardPlayer) || Config.PassiveMobs)
            {
                __instance.defaultMovementBehavior(time);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Stops the slime from jumping towards the player if Sten's bald cap is being worn
        /// </summary>
        /// <param name="__instance">Slime monster instance</param>
        /// <param name="time">In game time in ticks (required parameter for base)</param>
        /// <param name="___readyToJump">Reference to the readyToJump varaible in base</param>
        private static void GreenSlime_behaviorAtGameTick_prefix(GreenSlime __instance, GameTime time, ref int ___timeSinceLastJump)
        {
            if (Config.PassiveMobs)
            {
                ___timeSinceLastJump = 0; // Slime will not jump???
                __instance.focusedOnFarmers = false;
                // Globals.Monitor.Log("We're so back!", LogLevel.Debug);
            }
            else
            {
                // Globals.Monitor.Log("It's over boys", LogLevel.Debug);
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
            if (Config.PassiveMobs && __instance.Health == __instance.MaxHealth)
            {
                ___runningAwayFromFarmer = false;   // Dust spirit won't flee
                ___chargingFarmer = false;          // Dust spirit won't charge
            }
        }

        /// <summary>
        /// Makes monsters harmless while wearing Sten's hat
        /// </summary>
        /// <param name="__instance">Instance of monster</param>
        private static void damageMonster_Postfix(GameLocation __instance)
        {
            for (int i = __instance.characters.Count - 1; i >= 0; i--)
            {
                Monster monster;
                if ((monster = (__instance.characters[i] as Monster)) != null && Config.PassiveMobs
                                && monster.Health == monster.MaxHealth)
                    monster.farmerPassesThrough = true;
            }
        }
    }
}