using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using CreatureManager;
using ServerSync;
using UnityEngine;

namespace CreatureManagerModTemplate
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class CreatureModTemplatePlugin : BaseUnityPlugin
    {
        internal const string ModName = "CreatureManagerModTemplate";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "azumatt";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource CreatureManagerModTemplateLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public void Awake()
        {
            _serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            Creature wereBearBlack = new("werebear", "WereBearBlack")
            {
                Biome = Heightmap.Biome.Meadows,
                GroupSize = new Range(1, 2),
                CheckSpawnInterval = 600,
                RequiredWeather = Weather.Rain | Weather.Fog,
                Maximum = 2
            };
            wereBearBlack.Localize()
                .English("Black Werebear")
                .German("Schwarzer Werbär")
                .French("Ours-Garou Noir");
            wereBearBlack.Drops["Wood"].Amount = new Range(1, 2);
            wereBearBlack.Drops["Wood"].DropChance = 100f;

            Creature wereBearRed = new("werebear", "WereBearRed")
            {
                Biome = Heightmap.Biome.AshLands,
                GroupSize = new Range(1, 1),
                CheckSpawnInterval = 900,
                AttackImmediately = true,
                RequiredGlobalKey = GlobalKey.KilledYagluth,
            };
            wereBearRed.Localize()
                .English("Red Werebear")
                .German("Roter Werbär")
                .French("Ours-Garou Rouge");
            wereBearRed.Drops["Coal"].Amount = new Range(1, 2);
            wereBearRed.Drops["Coal"].DropChance = 100f;
            wereBearRed.Drops["Flametal"].Amount = new Range(1, 1);
            wereBearRed.Drops["Flametal"].DropChance = 10f;

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                CreatureManagerModTemplateLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                CreatureManagerModTemplateLogger.LogError($"There was an issue loading your {ConfigFileName}");
                CreatureManagerModTemplateLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        private static ConfigEntry<bool>? _serverConfigLocked;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }

        #endregion
    }
}