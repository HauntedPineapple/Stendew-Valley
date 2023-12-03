using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.SDKs;

namespace StendewValley
{
    /// <summary>The mod entry point</summary>
    internal sealed class ModEntry : Mod
    {
        // Private mod configuration file
        private static ModConfig Config;

        // Name of the maps in Content Patcher
        private readonly string MAIN_ISLAND_NAME = "Custom_MainIsland";
        private readonly string STEN_HOUSE_NAME = "Custom_StenHouse";
        private readonly string MAIN_ISLAND_CAVE_NAME = "Custom_MainIslandCave";

        // Custom Game Locations
        private GameLocation mainIsland;
        private GameLocation stenHouse;
        private GameLocation mainIslandCave;

        // List of spawnable zones in stens house
        List<Rectangle> stenHouseZones = new List<Rectangle>();

        // Boulder object
        private CustomLargeObject boulder;

        #region Entry Method
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // Set up ModConfig
            Config = this.Helper.ReadConfig<ModConfig>();

            // Hook up events
            Helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            Helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;

            // Spawnable zones in stens house
            stenHouseZones.Add(new Rectangle(7, 5, 9, 3));
            stenHouseZones.Add(new Rectangle(6, 14, 5, 4));

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
                this.Monitor.Log($"Loaded Configurations Menu", LogLevel.Debug);

                // register mod
                configMenu.Register(
                    mod: ModManifest,
                    reset: () => Config = new ModConfig(),
                    save: () => Helper.WriteConfig(Config)
                );

                // Add options
                configMenu.AddNumberOption(
                    mod: ModManifest,
                    name: () => "Min Slimes Per Day",
                    getValue: () => Config.MinSlimesPerDay,
                    setValue: value => Config.MinSlimesPerDay = value
                );
                configMenu.AddNumberOption(
                    mod: ModManifest,
                    name: () => "Max Slimes Per Day",
                    getValue: () => Config.MaxSlimesPerDay,
                    setValue: value => Config.MaxSlimesPerDay = value
                );
                configMenu.AddNumberOption(
                    mod: ModManifest,
                    name: () => "Max Total Slimes Cave",
                    getValue: () => Config.MaxTotalSlimesCave,
                    setValue: value => Config.MaxTotalSlimesCave = value
                );
                configMenu.AddNumberOption(
                    mod: ModManifest,
                    name: () => "Max Total Slimes Sten's House",
                    getValue: () => Config.MaxTotalSlimesHouse,
                    setValue: value => Config.MaxTotalSlimesHouse = value
                );
            }
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // Set up the location variables
            mainIsland = GetGameLocationByName(MAIN_ISLAND_NAME);
            stenHouse = GetGameLocationByName(STEN_HOUSE_NAME);
            mainIslandCave = GetGameLocationByName(MAIN_ISLAND_CAVE_NAME);

            // Set up the custom boulders
            InitializeBoulder();

            // Boulder loaded initially
            boulder.Enabled = true;

            // Spawn extra slimes
            if (Config.MaxSlimesPerDay < Config.MinSlimesPerDay)
            {
                Config.MaxSlimesPerDay = Config.MinSlimesPerDay;
                Helper.WriteConfig(Config);
            }

            // Hook up hat effect
            Game1.player.hat.fieldChangeEvent += HatChanged;
        }

        private void GameLoop_DayStarted(object sender, DayStartedEventArgs e)
        {
            if (Config.MaxSlimesPerDay < Config.MinSlimesPerDay)
            {
                Config.MaxSlimesPerDay = Config.MinSlimesPerDay;
                Helper.WriteConfig(Config);
            }

            // Spawn custom slimes
            SpawnSlimesStenHouse();
            SpawnSlimesCave();

            // Hat logic
            ApplyHatEffect();

            // Boulder Quest update
            UpdateBoulderQuest();
        }

        private void SpawnSlimesStenHouse()
        {
            int existing = 0;
            if (stenHouse.characters.Count > 0)
            {
                // Looping backwards through list
                for (int i = stenHouse.characters.Count - 1; i >= 0; i--)
                {
                    // Increment existing for every slime
                    if (stenHouse.characters[i] is Monster)
                        existing++;
                }
            }
            int amount = Math.Min(Config.MaxTotalSlimesCave - existing, Game1.random.Next(Config.MinSlimesPerDay, Config.MaxSlimesPerDay + 1));
            if (amount <= 0)
                return;
            List<Vector2> used = new();
            for (int i = 0; i < amount; i++)
            {
                Rectangle rect = stenHouseZones[Game1.random.Next(stenHouseZones.Count)];
                Vector2 pos = new Vector2(rect.X + Game1.random.Next(rect.Width), rect.Y + Game1.random.Next(rect.Height)) * 64;
                if (used.Contains(pos))
                    continue;
                Monster m = new GreenSlime(pos, 0);
                this.Monitor.Log("Added slime to Sten's House", LogLevel.Debug);
                stenHouse.characters.Add(m);
                used.Add(pos);
            }
        }

        private void SpawnSlimesCave()
        {
            int existing = 0;
            if (mainIslandCave.characters.Count > 0)
            {
                // Looping backwards through list
                for (int i = mainIslandCave.characters.Count - 1; i >= 0; i--)
                {
                    // Increment existing for every slime
                    if (mainIslandCave.characters[i] is Monster)
                        existing++;
                }
            }
            int amount = Math.Min(Config.MaxTotalSlimesCave - existing, Game1.random.Next(Config.MinSlimesPerDay, Config.MaxSlimesPerDay + 1));
            if (amount <= 0)
                return;
            List<Vector2> used = new();
            for (int i = 0; i < amount; i++)
            {
                Vector2 pos = new Vector2(6 + Game1.random.Next(14), 6 + Game1.random.Next(16)) * 64;
                if (used.Contains(pos))
                    continue;
                Monster m = new GreenSlime(pos, 0);
                mainIslandCave.characters.Add(m);
                used.Add(pos);
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

            // Check boulder quest
            UpdateBoulderQuest();
        }

        /// <summary>
        /// Makes monsters passive when wearing the special item
        /// </summary>
        /// <param name="__instance">Instance of a monster</param>
        /// <param name="time">Time variable (required)</param>
        /// <returns>True if continuing to base method, false if skipping it</returns>
        private static bool Monster_updateMovement_prefix(Monster __instance, GameTime time)
        {
            if (Config.PassiveMobs)
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

        private void InitializeBoulder()
        {
            // Test boulder
            CustomLargeObject temp = new CustomLargeObject("Test", true, mainIsland);
            temp.SetSprite(58, 62, "Buildings", 898, 5);
            temp.SetSprite(59, 62, "Buildings", 899, 5);
            temp.SetSprite(58, 63, "Buildings", 923, 5);
            temp.SetSprite(59, 63, "Buildings", 924, 5);
            temp.Spawn();

            boulder = temp;
        }

        private void ApplyHatEffect()
        {
            // Cleanup
            this.Monitor.Log("Mods are not peaceful", LogLevel.Debug);
            Config.PassiveMobs = false;

            // Set effect if wearing the right hat
            if (Game1.player.hat.Value != null && Game1.player.hat.Value.Name == "Bald Cap")
            {
                this.Monitor.Log("Bald Cap Equiped. Mobs are now peaceful", LogLevel.Debug);
                Config.PassiveMobs = true;
            }
        }

        // Event to call when hat changes
        public void HatChanged(Netcode.NetRef<StardewValley.Objects.Hat> field, StardewValley.Objects.Hat oldValue, StardewValley.Objects.Hat newValue)
        {
            ApplyHatEffect();
        }

        // Helper method to update the quest status for the boulder
        private void UpdateBoulderQuest()
        {
            if (Game1.player.hasOrWillReceiveMail("Custom_PaulQuest_complete"))
            {
                boulder.Enabled = false;
                // this.Monitor.Log("Boulder removed", LogLevel.Debug);
            }
            else
            {
                boulder.Enabled = true;
                // this.Monitor.Log("Boulder spawned", LogLevel.Debug);
            }
        }
    }
}