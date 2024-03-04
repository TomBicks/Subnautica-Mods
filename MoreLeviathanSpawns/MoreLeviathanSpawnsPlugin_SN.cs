﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;
using Nautilus.Json;
using Nautilus.Json.Attributes;
using Nautilus.Options.Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable IDE1006 // Suppress warnings related to "Naming Styles"

namespace MoreLeviathanSpawns
{
    [BepInPlugin(myGUID, pluginName, versionString)]
    public class MoreLeviathanSpawnsPlugin_SN : BaseUnityPlugin
    {
        private const string myGUID = "com.haulinoats.moreleviathanspawnssn";
        private const string pluginName = "More Leviathan Spawns";
        private const string versionString = "2.0.0";

        private static readonly Harmony harmony = new Harmony(myGUID);

        internal static ManualLogSource logger { get; private set; }

        internal static Config config { get; } = OptionsPanelHandler.RegisterModOptions<Config>();

        internal static SaveCoords saveCoords { get; } = SaveDataHandler.RegisterSaveDataCache<SaveCoords>();

        //SAVEDATACACHE TESTING!!
        [FileName("leviathans_coords")]
        internal class SaveCoords : SaveDataCache
        {
            public int ReaperSpawnIntensity { get; set; }
            public int GhostSpawnIntensity { get; set; }

            public List<Vector3> ReaperCoords { get; set; }
            public List<GhostCoordsAndType> GhostCoords { get; set; }
        }

        public void Awake()
        {
            harmony.PatchAll();
            Logger.LogInfo(pluginName + " " + versionString + " " + "loaded.");
            logger = Logger;

            /*OnStartLoading, check whether the coord file has null values;
             - if it does, it means the save and coord files are being created for the first time and that we need
               to populate the coord file with a random selection of leviathan coordinates, based on the intensity selected
             - if it doesn't have null values, it means this is not the first time the save has been loaded and we can skip
               the process of populating the coord file with levaithan coordinates*/
            saveCoords.OnStartedLoading += (object sender, JsonFileEventArgs e) =>
            {
                SaveCoords coords = e.Instance as SaveCoords;

                logger.LogInfo($"loading from filepath: {coords.JsonFilePath}");

                if(coords.ReaperCoords is null)
                {
                    logger.LogInfo("Coord File is null! Preparing to populate Coord File...");
                    PopulateCoordArray();
                }
                else
                {
                    logger.LogInfo("Coord File is already populated!");
                }

                //NOTE!! [0] works to display each slot! use for loop to display them all!!!
                logger.LogInfo($"loading reaper coords from save slot: {coords.ReaperCoords[0]}");
                ErrorMessage.AddMessage($"loading reaper coords from save slot: {coords.ReaperCoords[0]}");

                logger.Log(LogLevel.Info, $"Reaper spawn intensity is set to: {coords.ReaperSpawnIntensity}");
                logger.Log(LogLevel.Info, $"Ghost spawn intensity is set to: {coords.GhostSpawnIntensity}");

                if (coords.ReaperSpawnIntensity != 0)
                {
                    for (int i = 0; i < coords.ReaperCoords.Count; i++)
                    {
                        logger.Log(LogLevel.Info, $"Reaper spawn #{i + 1} - Coords: {coords.ReaperCoords[i]}");
                        CoordinatedSpawnsHandler.RegisterCoordinatedSpawn(new SpawnInfo(TechType.ReaperLeviathan, coords.ReaperCoords[i]));
                    }
                }
            };

            //Display coords upon loading a save file
            saveCoords.OnFinishedLoading += (object sender, JsonFileEventArgs e) =>
            {
                SaveCoords coords = e.Instance as SaveCoords;

                logger.LogInfo($"loaded from filepath: {coords.JsonFilePath}");

                logger.LogInfo($"loaded reaper coords from save slot: {coords.ReaperCoords}");
                //logger.LogInfo($"loaded ghost coords from save slot: {coords.GhostCoords}");
                ErrorMessage.AddMessage($"loaded reaper coords from save slot: {coords.ReaperCoords}");
                //ErrorMessage.AddMessage($"loaded ghost coords from save slot: {coords.GhostCoords}");
            };

            saveCoords.OnFinishedSaving += (object sender, JsonFileEventArgs e) =>
            {
                SaveCoords coords = e.Instance as SaveCoords;
                logger.LogInfo($"saved reaper coords from save slot: {coords.ReaperCoords}");
                //logger.LogInfo($"saved ghost coords from save slot: {coords.GhostCoords}");
                ErrorMessage.AddMessage($"saved reaper coords from save slot: {coords.ReaperCoords}");
                //ErrorMessage.AddMessage($"saved ghost coords from save slot: {coords.GhostCoords}");
            };
        }

        static void PopulateCoordArray()
        {
            SpawnData spawnData = new SpawnData
            {
                //AlwaysRandomized = config.AlwaysRandomized;
                ReaperSpawnIntensity = config.ReaperSpawnIntensity,
                GhostSpawnIntensity = config.GhostSpawnIntensity
            };

            logger.Log(LogLevel.Info, $"Reaper spawn intensity is set to: {spawnData.ReaperSpawnIntensity}");
            logger.Log(LogLevel.Info, $"Ghost spawn intensity is set to: {spawnData.GhostSpawnIntensity}");

            //this will set a general amount of spawns based on the spawn intensity the player set, defaulting to '3'
            int reaperSpawnTotal = (int)(spawnData.ReaperSpawnIntensity / 6 * spawnData.ReaperCoords.Count);
            int ghostSpawnTotal = (int)(spawnData.GhostSpawnIntensity / 6 * spawnData.GhostCoords.Count);

            logger.Log(LogLevel.Info, $"Loading {reaperSpawnTotal} of {spawnData.ReaperCoords.Count} total reaper spawns");
            logger.Log(LogLevel.Info, $"Loading {ghostSpawnTotal} of {spawnData.GhostCoords.Count} total ghost spawns");

            //Create a list for both sets of coords; we will be adding selected spawns to these lists, for the new save file
            saveCoords.ReaperCoords = new List<Vector3>();
            saveCoords.GhostCoords = new List<GhostCoordsAndType>();

            //Reseed; used to generate random selection of spawns to add to the new save file
            System.Random rnd = new System.Random();

            //Randomly select reaper spawns to add to new save file, amount equal to reaperSpawnTotal
            for (int i = 0; i < reaperSpawnTotal; i++)
            {
                //Select an index of the ReaperCoords list randomly, and add that reaper coord to the new save file
                int j = rnd.Next(0, spawnData.ReaperCoords.Count - 1);
                logger.Log(LogLevel.Info, $"Random selection of Reaper Coord #{j + 1}");
                logger.Log(LogLevel.Info, $"Reaper spawn #{i + 1} - Coords: {spawnData.ReaperCoords[j]}");

                //Add the selected coordinates to the list of coordinates for the new save file
                saveCoords.ReaperCoords.Add(spawnData.ReaperCoords[j]);

                //Remove the added reaper coord afterwards, to ensure it's not accidentally selected twice
                spawnData.ReaperCoords.RemoveAt(j);
            }

            //Randomly select ghost spawns to add to new save file, amount equal to ghostSpawnTotal
            for (int i = 0; i < ghostSpawnTotal; i++)
            {
                //DEBUG!! Log whether the ghost leviathan added is an adult or a juvenile
                string ghostType = "Adult";
                if (spawnData.GhostCoords[i].GhostType == 2) { ghostType = "Juvenile"; }

                //Select an index of the GhostCoords list randomly, and add that ghost coord to the new save file
                int j = rnd.Next(0, spawnData.GhostCoords.Count - 1);
                logger.Log(LogLevel.Info, $"Random selection of Ghost {ghostType} Coord #{j + 1}");
                logger.Log(LogLevel.Info, $"Ghost ({ghostType}) spawn #{i + 1} - Coords: {spawnData.GhostCoords[j].Coords}");

                //Add the selected coordinates to the list of coordinates for the new save file
                saveCoords.GhostCoords.Add(spawnData.GhostCoords[j]);

                //Remove the added ghost coord afterwards, to ensure it's not accidentally selected twice
                spawnData.GhostCoords.RemoveAt(j);
            }
        }
    }

    [Menu("More Leviathan Spawns")]
    public class Config : Nautilus.Json.ConfigFile
    {
        [Slider("Reaper Spawn Intensity", Min = 0F, Max = 6F, DefaultValue = 3F, Step = 1F, Id = "reaperSpawnIntensity", Tooltip = "Defines general intensity of additional reaper leviathan spawns to add to the game. A value of 1 will add roughly 2 - 4 spawns. A value of 6 will add roughly 20 - 23 spawns. A value of 0 will add no additional reaper leviathan spawns to game.")]
        public float ReaperSpawnIntensity = 3F;
        [Slider("Ghost Spawn Intensity", Min = 0F, Max = 6F, DefaultValue = 3F, Step = 1F, Id = "ghostSpawnIntensity", Tooltip = "Defines general intensity of additional ghost leviathan spawns to add to the game. A value of 1 will add roughly 1 - 3 spawns. A value of 6 will add roughly 12 - 14 spawns. A value of 0 will add no additional ghost leviathan spawns to game.")]
        public float GhostSpawnIntensity = 3F;
        //NOTE!! How could this have possible worked, if you can't unspawn leviathans? All this would have done would randomise until all unique spawns were registered
        //In essence, this just eventually hit the max, always.
        //Need to either figure out if leviathans can be unregistered from the world spawn thing, or need to remove this option entirely.
        [Toggle("Always randomize spawns?", Id = "alwaysRandomize", Tooltip = "By default, spawn locations are chosen randomly then saved and remain static for rest of playthrough. If this option is checked, spawns will always randomize when opening that save file.")]
        public bool AlwaysRandomized = false;

        /*OKAY!! So, the plan is (best case; can work down from here);
         * - Create a new json file per save, recognising the identifier for each new save, that includes the randomly generated list of coordinates of leviathans for that world
         * - json file is split between the name of the save, to identify it, and the coordinates used, which are run through the registering code, in case any are missed
         * - Creates if it can find no file with the appropirate naming convention (being the name of the save plus something like "leviathancoords"
         * - Loads from file if it does find it
        */

        /*private void SpawnIntensityChanged(SliderChangedEventArgs e)
        {
            switch (e.Id)
            {
                case "reaperSpawnIntensity":
                    ReaperSpawnIntensity = e.Value;
                    break;
                case "ghostSpawnIntensity":
                    GhostSpawnIntensity = e.Value;
                    break;
            }
        }

        private void ToggleChanged(ToggleChangedEventArgs e)
        {
            switch (e.Id)
            {
                case "alwaysRandomized":
                    AlwaysRandomized = e.Value;
                    break;
            }
        }*/
    }
}