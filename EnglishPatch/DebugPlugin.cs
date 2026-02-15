using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;

namespace EnglishPatch;

/// <summary>
/// Put dicey stuff in here that might crash the plugin - so it doesnt crash the existing plugins
/// </summary>
[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.Debug", "DebugGame", MyPluginInfo.PLUGIN_VERSION)]
internal class DebugPlugin: BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    public const string ChineseCharPattern = @".*\p{IsCJKUnifiedIdeographs}.*";

    private void Awake()
    {
        Logger = base.Logger;
        Harmony.CreateAndPatchAll(typeof(DebugPlugin));
        Logger.LogWarning($"Debug Game Plugin should be patched!");
    }
    
    private void Update()
    {
    }

    [HarmonyPatch(typeof(ExDataLoader), "LoadAllData")]
    [HarmonyPostfix]
    private static void LoadAllData_Postfix()
    {
        try
        {
            var dumpPath = Path.Combine(BepInEx.Paths.PluginPath, "dumped_data");
            Directory.CreateDirectory(dumpPath);

            var gameAssembly = typeof(ExDataLoader).Assembly;

            // Access data from ExDataFactory instances
            DumpFactory("Achievement", gameAssembly.GetType("ExAchievement"));
            DumpFactory("Assist", gameAssembly.GetType("ExAssist"));
            DumpFactory("Audio", gameAssembly.GetType("ExAudio"));
            DumpFactory("Battle", gameAssembly.GetType("ExBattle"));
            DumpFactory("Birth", gameAssembly.GetType("ExBirth"));
            DumpFactory("Buff", gameAssembly.GetType("ExBuff"));
            DumpFactory("Condition", gameAssembly.GetType("ExCondition"));
            DumpFactory("Dice", gameAssembly.GetType("ExDice"));
            DumpFactory("Dictionary", gameAssembly.GetType("ExDictionary"));
            DumpFactory("Difficulty", gameAssembly.GetType("ExDifficulty"));
            DumpFactory("Effect", gameAssembly.GetType("ExEffect"));
            DumpFactory("Ending", gameAssembly.GetType("ExEnding"));
            DumpFactory("Equip", gameAssembly.GetType("ExEquip"));
            DumpFactory("Event", gameAssembly.GetType("ExEvent"));
            DumpFactory("EventDialog", gameAssembly.GetType("ExEventDialog"));
            DumpFactory("EventDice", gameAssembly.GetType("ExEventDice"));
            DumpFactory("EventNormal", gameAssembly.GetType("ExEventNormal"));
            DumpFactory("EventPuzzle", gameAssembly.GetType("ExEventPuzzle"));
            DumpFactory("EventResult", gameAssembly.GetType("ExEventResult"));
            DumpFactory("EventSelection", gameAssembly.GetType("ExEventSelection"));
            DumpFactory("EventSkillUpdate", gameAssembly.GetType("ExEventSkillUpdate"));
            DumpFactory("Formula", gameAssembly.GetType("ExFormula"));
            DumpFactory("Hero", gameAssembly.GetType("ExHero"));
            DumpFactory("HotKey", gameAssembly.GetType("ExHotKey"));
            DumpFactory("Item", gameAssembly.GetType("ExItem"));
            DumpFactory("Map", gameAssembly.GetType("ExMap"));
            DumpFactory("Monster", gameAssembly.GetType("ExMonster"));
            DumpFactory("News", gameAssembly.GetType("ExNews"));
            DumpFactory("PlayerPortrait", gameAssembly.GetType("ExPlayerPortrait"));
            DumpFactory("Point", gameAssembly.GetType("ExPoint"));
            DumpFactory("Property", gameAssembly.GetType("ExProperty"));
            DumpFactory("PuzzleGroup", gameAssembly.GetType("ExPuzzleGroup"));
            DumpFactory("Relation", gameAssembly.GetType("ExRelation"));
            DumpFactory("Skill", gameAssembly.GetType("ExSkill"));
            DumpFactory("SkillCondition", gameAssembly.GetType("ExSkillCondition"));
            DumpFactory("Switch", gameAssembly.GetType("ExSwitch"));
            DumpFactory("Talent", gameAssembly.GetType("ExTalent"));
            DumpFactory("UIText", gameAssembly.GetType("ExUIText"));

            Logger.LogInfo($"All data dumped to {dumpPath}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to dump data: {ex}");
        }
    }

    private static void DumpFactory(string name, Type dataType)
    {
        try
        {
            if (dataType == null)
            {
                Logger.LogWarning($"Failed to find type for {name}");
                return;
            }

            Logger.LogInfo($"Processing {name}, type: {dataType.FullName}");

            var dumpPath = Path.Combine(BepInEx.Paths.PluginPath, "dumped_data");
            var factoryType = typeof(ExDataFactory<>).MakeGenericType(dataType);
            Logger.LogInfo($"Factory type created: {factoryType.FullName}");

            // List all static properties and fields
            var properties = factoryType.GetProperties(BindingFlags.Public | BindingFlags.Static);
            var fields = factoryType.GetFields(BindingFlags.Public | BindingFlags.Static);
            Logger.LogInfo($"Static properties: {string.Join(", ", properties.Select(p => p.Name))}");
            Logger.LogInfo($"Static fields: {string.Join(", ", fields.Select(f => f.Name))}");

            // Try to get instance from property or field
            object instance = null;
            var instanceProperty = factoryType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
            if (instanceProperty != null)
            {
                instance = instanceProperty.GetValue(null);
                Logger.LogInfo($"Got instance from property");
            }
            else
            {
                var instanceField = factoryType.GetField("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
                if (instanceField != null)
                {
                    instance = instanceField.GetValue(null);
                    Logger.LogInfo($"Got instance from field");
                }
                else
                {
                    Logger.LogWarning($"Instance property/field not found for {name}");
                    return;
                }
            }

            if (instance == null)
            {
                Logger.LogWarning($"Instance is null for {name}");
                return;
            }

            Logger.LogInfo($"Instance retrieved for {name}, type: {instance.GetType().FullName}");

            // Inspect instance members to find the actual data (including private/protected)
            var instanceType = instance.GetType();
            var allInstanceProperties = instanceType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var allInstanceFields = instanceType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Logger.LogInfo($"All instance properties: {string.Join(", ", allInstanceProperties.Select(p => $"{p.Name}:{p.PropertyType.Name}"))}");
            Logger.LogInfo($"All instance fields: {string.Join(", ", allInstanceFields.Select(f => $"{f.Name}:{f.FieldType.Name}"))}");

            // Try to find the data collection - common names are: Data, Items, List, Dictionary, Map, All
            object data = null;
            foreach (var prop in allInstanceProperties)
            {
                if (prop.Name.Contains("Data") || prop.Name.Contains("List") || prop.Name.Contains("Dictionary") || 
                    prop.Name.Contains("Map") || prop.Name == "All" || prop.Name == "Items" ||
                    prop.PropertyType.IsGenericType && (
                        prop.PropertyType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>) ||
                        prop.PropertyType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.Dictionary<,>)))
                {
                    data = prop.GetValue(instance);
                    Logger.LogInfo($"Found data in property: {prop.Name} (type: {prop.PropertyType.Name})");
                    break;
                }
            }

            if (data == null)
            {
                foreach (var field in allInstanceFields)
                {
                    if (field.Name.Contains("Data") || field.Name.Contains("List") || field.Name.Contains("Dictionary") || 
                        field.Name.Contains("Map") || field.Name == "All" || field.Name == "Items" ||
                        field.FieldType.IsGenericType && (
                            field.FieldType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>) ||
                            field.FieldType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.Dictionary<,>)))
                    {
                        data = field.GetValue(instance);
                        Logger.LogInfo($"Found data in field: {field.Name} (type: {field.FieldType.Name})");
                        break;
                    }
                }
            }

            var objectToSerialize = data ?? instance;
            var json = JsonConvert.SerializeObject(objectToSerialize, Formatting.Indented);
            Logger.LogInfo($"JSON serialized for {name}, length: {json.Length}");

            var filePath = Path.Combine(dumpPath, $"{name}.json");
            File.WriteAllText(filePath, json);
            Logger.LogInfo($"Dumped {name} to {filePath}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to dump {name}: {ex.Message}\n{ex.StackTrace}");
        }
    }
}

