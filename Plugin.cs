using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using LocalizationManager;
using PieceManager;
using ServerSync;
using UnityEngine;

namespace PieceManagerModTemplate
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class PieceManagerModTemplatePlugin : BaseUnityPlugin
    {
        internal const string ModName = "PieceManagerModTemplate";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "{azumatt}";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource PieceManagerModTemplateLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
        private FileSystemWatcher _watcher;
        private readonly object _reloadLock = new();
        private DateTime _lastConfigReloadTime;
        private const long RELOAD_DELAY = 10000000; // One second
        
        
        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            bool saveOnSet = Config.SaveOnConfigSet;
            Config.SaveOnConfigSet = false;
            
            // Uncomment the line below to use the LocalizationManager for localizing your mod.
            //Localizer.Load(); // Use this to initialize the LocalizationManager (for more information on LocalizationManager, see the LocalizationManager documentation https://github.com/blaxxun-boop/LocalizationManager#example-project).

            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            // Globally turn off configuration options for your pieces, omit if you don't want to do this.
            BuildPiece.ConfigurationEnabled = false;

            // Format: new("AssetBundleName", "PrefabName", "FolderName");
            BuildPiece examplePiece1 = new("funward_bundle", "funward", "FunWard_BundleFolder");
            examplePiece1.Name.English("Fun Ward"); // Localize the name and description for the building piece for a language.
            examplePiece1.Description.English("Ward For testing the Piece Manager");
            examplePiece1.RequiredItems.Add("FineWood", 20, false); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            examplePiece1.RequiredItems.Add("SurtlingCore", 20, false);
            examplePiece1.Category.Set(PieceManager.BuildPieceCategory.Misc);
            examplePiece1.Crafting.Set(PieceManager.CraftingTable.ArtisanTable); // Set a crafting station requirement for the piece.
            //examplePiece1.Extension.Set(CraftingTable.Forge, 2); // Makes this piece a station extension, can change the max station distance by changing the second value. Use strings for custom tables.

            // Or you can do it for a custom table (### Default maxStationDistance is 5. I used 2 as an example here.)
            //examplePiece1.Extension.Set("MYCUSTOMTABLE", 2); // Makes this piece a station extension, can change the max station distance by changing the second value. Use strings for custom tables.

            //examplePiece1.Crafting.Set("CUSTOMTABLE"); // If you have a custom table you're adding to the game. Just set it like this.

            //examplePiece1.SpecialProperties.NoConfig = true;  // Do not generate a config for this piece, omit this line of code if you want to generate a config.
            examplePiece1.SpecialProperties = new SpecialProperties() { AdminOnly = true, NoConfig = true }; // You can declare multiple properties in one line           


            BuildPiece examplePiece2 = new("bamboo", "Bamboo_Wall"); // Note: If you wish to use the default "assets" folder for your assets, you can omit it!
            examplePiece2.Name.English("Bamboo Wall");
            examplePiece2.Description.English("A wall made of bamboo!");
            examplePiece2.RequiredItems.Add("BambooLog", 20, false);
            examplePiece2.Category.Set(PieceManager.BuildPieceCategory.BuildingWorkbench);
            examplePiece2.Crafting.Set("CUSTOMTABLE"); // If you have a custom table you're adding to the game. Just set it like this.
            examplePiece2.SpecialProperties.AdminOnly = true; // You can declare these one at a time as well!.


            // If you want to add your item to the cultivator or another hammer with vanilla categories
            // Format: (AssetBundle, "PrefabName", addToCustom, "Item that has a piecetable")
            BuildPiece examplePiece3 = new("bamboo", "Bamboo_Sapling");
            examplePiece3.Name.English("Bamboo Sapling");
            examplePiece3.Description.English("A young bamboo tree, called a sapling");
            examplePiece3.RequiredItems.Add("BambooSeed", 20, false);
            examplePiece3.Tool.Add("Cultivator"); // Format: ("Item that has a piecetable")
            examplePiece3.SpecialProperties.NoConfig = true;

            // If you don't want to make an icon inside unity, but want the PieceManager to snag one for you, simply add .Snapshot() to your piece.
            examplePiece3.Snapshot(); // Optionally, you can use the lightIntensity parameter to set the light intensity of the snapshot. Default is 1.3 or the cameraRotation parameter to set the rotation of the camera. Default is null.

            // If you want a more custom piece, below is an example. Including custom category and custom crafting station. Also adding to a custom hammer.
            BuildPiece examplePiece4 = new("bamboo", "Bamboo_Beam_Light");
            examplePiece4.Name.English("Bamboo Beam Light");
            examplePiece4.Description.English("A light made of bamboo!");
            examplePiece4.RequiredItems.Add("BambooLog", 20, false);
            examplePiece4.Category.Set("Custom Category");
            examplePiece4.Crafting.Set("CUSTOMTABLE");
            examplePiece4.Tool.Add("Custom Hammer");
            examplePiece4.SpecialProperties.NoConfig = true;
            examplePiece4.Snapshot(); // Optionally, you can use the lightIntensity parameter to set the light intensity of the snapshot. Default is 1.3 or the cameraRotation parameter to set the rotation of the camera. Default is null.


            // Need to add something to ZNetScene but not the hammer, cultivator or other? 
            PiecePrefabManager.RegisterPrefab("bamboo", "Bamboo_Beam_Light");

            // Does your model need to swap materials with a vanilla material? Format: (GameObject, isJotunnMock)
            MaterialReplacer.RegisterGameObjectForMatSwap(examplePiece3.Prefab, false);

            // Does your model use a shader from the game like Custom/Creature or Custom/Piece in unity? Need it to "just work"?
            //MaterialReplacer.RegisterGameObjectForShaderSwap(examplePiece3.Prefab, MaterialReplacer.ShaderType.UseUnityShader);

            // What if you want to use a custom shader from the game (like Custom/Piece that allows snow!!!) but your unity shader isn't set to Custom/Piece? Format: (GameObject, MaterialReplacer.ShaderType.)
            //MaterialReplacer.RegisterGameObjectForShaderSwap(examplePiece3.Prefab, MaterialReplacer.ShaderType.PieceShader);

            // Detailed instructions on how to use the MaterialReplacer can be found on the current PieceManager Wiki. https://github.com/AzumattDev/PieceManager/wiki

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
            
            Config.Save();
            if (saveOnSet)
            {
                Config.SaveOnConfigSet = saveOnSet;
            }
            
            // If you want to do something once localization completes, LocalizationManager has a hook for that.
            /*Localizer.OnLocalizationComplete += () =>
            {
                // Do something
                ItemManagerModTemplateLogger.LogDebug("OnLocalizationComplete called");
            };*/
        }

        private void OnDestroy()
        {
            SaveWithRespectToConfigSet();
            _watcher?.Dispose();
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
            DateTime now = DateTime.Now;
            long time = now.Ticks - _lastConfigReloadTime.Ticks;
            if (time < RELOAD_DELAY)
            {
                return;
            }

            lock (_reloadLock)
            {
                if (!File.Exists(ConfigFileFullPath))
                {
                    PieceManagerModTemplateLogger.LogWarning("Config file does not exist. Skipping reload.");
                    return;
                }

                try
                {
                    PieceManagerModTemplateLogger.LogDebug("Reloading configuration...");
                    SaveWithRespectToConfigSet(true);
                    PieceManagerModTemplateLogger.LogInfo("Configuration reload complete.");
                }
                catch (Exception ex)
                {
                    PieceManagerModTemplateLogger.LogError($"Error reloading configuration: {ex.Message}");
                }
            }

            _lastConfigReloadTime = now;
        }
        
        private void SaveWithRespectToConfigSet(bool reload = false)
        {
            bool originalSaveOnSet = Config.SaveOnConfigSet;
            Config.SaveOnConfigSet = false;
            if (reload)
                Config.Reload();
            Config.Save();
            if (originalSaveOnSet)
            {
                Config.SaveOnConfigSet = originalSaveOnSet;
            }
        }
        
        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription = new(description.Description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"), description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order = null!;
            [UsedImplicitly] public bool? Browsable = null!;
            [UsedImplicitly] public string? Category = null!;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer = null!;
        }

        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() => "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
        }

        #endregion
    }
    
    public static class KeyboardExtensions
    {
        public static bool IsKeyDown(this KeyboardShortcut shortcut)
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
        }

        public static bool IsKeyHeld(this KeyboardShortcut shortcut)
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKey(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
        }
    }
}