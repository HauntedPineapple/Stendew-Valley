using System;
using System.Collections.Generic;
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

        // Name of the maps in Content Patcher
        private readonly string MAIN_ISLAND_NAME = "Custom_MainIsland";

        private GameLocation mainIsland;

        private CustomLargeObject test_boulder;

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
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;

            // Set the intial ModInfo variable states
            Globals.Info.monstersPeaceful = true;

            // Hook up button press event
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;

            // Instantiate harmony patcher
            var harmony = new Harmony(ModManifest.UniqueID);

            try
            {
                // Patch monster behaviour
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
                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => "Spawn Boulder",
                    getValue: () => Config.TestBoulderSpawn,
                    setValue: value => { Config.TestBoulderSpawn = value; test_boulder.Enabled = value; }
                );
            }
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // Set up the location variables
            mainIsland = GetGameLocationByName(MAIN_ISLAND_NAME);

            // Set up the custom boulders
            InitializeCustomObjects();

            // Boulder follows menu option on initial load
            test_boulder.Enabled = Config.TestBoulderSpawn;
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
        /// Makes monsters passive when wearing the special item
        /// </summary>
        /// <param name="__instance">Instance of a monster</param>
        /// <param name="time">Time variable (required)</param>
        /// <returns>True if continuing to base method, false if skipping it</returns>
        private static bool Monster_updateMovement_prefix(Monster __instance, GameTime time)
        {
            if (Config.PassiveMobs && __instance.Health == __instance.MaxHealth)
            {
                __instance.defaultMovementBehavior(time);
                return false;
            }
            return true;
        }

        // Helper methods
        /// <summary>
        /// Search for a reference of a GameLocation by the map's name.
        /// </summary>
        /// <param name="locationName">Name of the map (Woods, Forest, etc.)</param>
        /// <returns></returns>
        private GameLocation GetGameLocationByName(string locationName)
        {
            // Loop through all locations to get a ref to given map
            IList<GameLocation> locations = Game1.locations;
            for (int i = Game1.locations.Count - 1; i >= 0; i--)
            {
                if (locations[i].Name == locationName)
                {
                    return locations[i];
                }
            }

            // No map was found out of all GameLocations
            throw new KeyNotFoundException(
                "Map \"" + locationName + "\" not found in Game1.locations" +
                " (has this method been called before OnSaveLoaded()?)");
        }

        private void InitializeCustomObjects()
        {
            // Test boulder
            CustomLargeObject temp = new CustomLargeObject("Test", true, mainIsland);
            temp.SetSprite(58, 62, "Buildings", 17, 1);
            temp.SetSprite(59, 62, "Buildings", 17, 1);
            temp.SetSprite(58, 63, "Buildings", 17, 1);
            temp.SetSprite(59, 63, "Buildings", 17, 1);
            temp.Spawn();

            test_boulder = temp;
        }
    }
}