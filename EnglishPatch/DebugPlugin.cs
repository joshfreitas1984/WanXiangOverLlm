using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EnglishPatch;

/// <summary>
/// Debug plugin for dumping game data and merging translations.
/// Extracts Chinese text to JSON files and merges English translations back into the game.
/// </summary>
[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.Debug", "DebugGame", MyPluginInfo.PLUGIN_VERSION)]
internal class DebugPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private const string ChineseCharPattern = @".*\p{IsCJKUnifiedIdeographs}.*";
    private const string DumpFolderName = "dumpeddata";
    private const string PatchFolderName = "english";

    public static bool DumpEnabled = true;

    private void Awake()
    {
        Logger = base.Logger;
        Harmony.CreateAndPatchAll(typeof(DebugPlugin));
        Logger.LogInfo("Debug plugin initialized");
    }

    [HarmonyPatch(typeof(ExDataLoader), "LoadAllData")]
    [HarmonyPostfix]
    private static void LoadAllData_Postfix()
    {
        try
        {
            var dataTypes = GetAllDataTypes();

            if (DumpEnabled)
                ExportChineseText(dataTypes);

            MergeTranslations(dataTypes);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to process data: {ex}");
        }
    }

    private static (string Name, Type Type)[] GetAllDataTypes()
    {
        var gameAssembly = typeof(ExDataLoader).Assembly;

        return new[]
        {
            ("Achievement", gameAssembly.GetType("ExAchievement")),
            ("Assist", gameAssembly.GetType("ExAssist")),
            ("Audio", gameAssembly.GetType("ExAudio")),
            ("Battle", gameAssembly.GetType("ExBattle")),
            ("Birth", gameAssembly.GetType("ExBirth")),
            ("Buff", gameAssembly.GetType("ExBuff")),
            ("Condition", gameAssembly.GetType("ExCondition")),
            ("Dice", gameAssembly.GetType("ExDice")),
            ("Dictionary", gameAssembly.GetType("ExDictionary")),
            ("Difficulty", gameAssembly.GetType("ExDifficulty")),
            ("Effect", gameAssembly.GetType("ExEffect")),
            ("Ending", gameAssembly.GetType("ExEnding")),
            ("Equip", gameAssembly.GetType("ExEquip")),
            ("Event", gameAssembly.GetType("ExEvent")),
            ("EventDialog", gameAssembly.GetType("ExEventDialog")),
            ("EventDice", gameAssembly.GetType("ExEventDice")),
            ("EventNormal", gameAssembly.GetType("ExEventNormal")),
            ("EventPuzzle", gameAssembly.GetType("ExEventPuzzle")),
            ("EventResult", gameAssembly.GetType("ExEventResult")),
            ("EventSelection", gameAssembly.GetType("ExEventSelection")),
            ("EventSkillUpdate", gameAssembly.GetType("ExEventSkillUpdate")),
            ("Formula", gameAssembly.GetType("ExFormula")),
            ("Hero", gameAssembly.GetType("ExHero")),
            ("HotKey", gameAssembly.GetType("ExHotKey")),
            ("Item", gameAssembly.GetType("ExItem")),
            ("Map", gameAssembly.GetType("ExMap")),
            ("Monster", gameAssembly.GetType("ExMonster")),
            ("News", gameAssembly.GetType("ExNews")),
            ("PlayerPortrait", gameAssembly.GetType("ExPlayerPortrait")),
            ("Point", gameAssembly.GetType("ExPoint")),
            ("Property", gameAssembly.GetType("ExProperty")),
            ("PuzzleGroup", gameAssembly.GetType("ExPuzzleGroup")),
            ("Relation", gameAssembly.GetType("ExRelation")),
            ("Skill", gameAssembly.GetType("ExSkill")),
            ("SkillCondition", gameAssembly.GetType("ExSkillCondition")),
            ("Switch", gameAssembly.GetType("ExSwitch")),
            ("Talent", gameAssembly.GetType("ExTalent")),
            ("UIText", gameAssembly.GetType("ExUIText"))
        };
    }

    private static void ExportChineseText((string Name, Type Type)[] dataTypes)
    {
        var dumpPath = Path.Combine(Paths.BepInExRootPath, DumpFolderName);
        Directory.CreateDirectory(dumpPath);

        foreach (var (name, type) in dataTypes)
            ExportDataType(name, type, dumpPath);

        Logger.LogInfo($"Export completed to {dumpPath}");
    }

    private static void ExportDataType(string name, Type dataType, string dumpPath)
    {
        try
        {
            if (dataType == null)
            {
                Logger.LogWarning($"Type not found: {name}");
                return;
            }

            var dataDict = GetFactoryData(dataType, name);
            if (dataDict == null) return;

            var exportData = BuildExportData(dataDict, dataType);
            if (exportData.Count == 0)
            {
                Logger.LogDebug($"No Chinese text in {name}");
                return;
            }

            SaveToJson(exportData, Path.Combine(dumpPath, $"{name}.json"));
            Logger.LogInfo($"Exported {name}: {exportData.Count} entries");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Export failed for {name}: {ex.Message}");
        }
    }

    private static List<JObject> BuildExportData(Dictionary<int, object> dataDict, Type dataType)
    {
        var chineseRegex = new Regex(ChineseCharPattern);
        var result = new List<JObject>();
        var properties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var kvp in dataDict)
        {
            var exportObj = new JObject { ["Key"] = kvp.Key };
            var hasChineseProperty = false;

            foreach (var prop in properties)
            {
                if (prop.PropertyType != typeof(string)) continue;

                var value = prop.GetValue(kvp.Value) as string;
                if (!string.IsNullOrEmpty(value) && chineseRegex.IsMatch(value))
                {
                    exportObj[prop.Name] = value;
                    hasChineseProperty = true;
                }
            }

            if (hasChineseProperty)
                result.Add(exportObj);
        }

        return result;
    }

    private static void MergeTranslations((string Name, Type Type)[] dataTypes)
    {
        var patchPath = Path.Combine(Paths.BepInExRootPath, PatchFolderName);
        if (!Directory.Exists(patchPath))
        {
            Logger.LogInfo("No translation folder found, skipping merge");
            return;
        }

        Logger.LogInfo("Merging translations...");

        foreach (var (name, type) in dataTypes)
            MergeDataType(name, type, patchPath);

        Logger.LogInfo($"Merge completed from {patchPath}");
    }

    private static void MergeDataType(string name, Type dataType, string patchPath)
    {
        try
        {
            if (dataType == null) return;

            var filePath = Path.Combine(patchPath, $"{name}.json");
            if (!File.Exists(filePath)) return;

            var dataDict = GetFactoryData(dataType, name);
            if (dataDict == null) return;

            var translatedData = LoadFromJson(filePath);
            if (translatedData == null) return;

            var mergedCount = MergeByKey(dataDict, translatedData, dataType);

            if (mergedCount > 0)
            {
                Logger.LogInfo($"Merged {name}: {mergedCount} translations");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Merge failed for {name}: {ex.Message}");
        }
    }

    private static int MergeByKey(Dictionary<int, object> dataDict, JArray translatedData, Type dataType)
    {
        var mergedCount = 0;

        foreach (JObject translatedItem in translatedData)
        {
            var keyToken = translatedItem["Key"];
            if (keyToken == null) continue;

            var key = keyToken.ToObject<int>();
            if (!dataDict.ContainsKey(key)) continue;

            var existingItem = dataDict[key];
            UpdateStringProperties(existingItem, translatedItem, dataType);
            mergedCount++;
        }

        return mergedCount;
    }

    private static void UpdateStringProperties(object target, JObject source, Type targetType)
    {
        var properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (!prop.CanWrite || prop.PropertyType != typeof(string)) continue;

            var sourceValue = source[prop.Name];
            if (sourceValue == null || sourceValue.Type == JTokenType.Null)
                continue;

            try
            {
                var value = sourceValue.ToObject<string>();
                prop.SetValue(target, value);
            }
            catch
            {
                // Silently ignore properties that can't be set
            }
        }
    }

    private static Dictionary<int, object> GetFactoryData(Type dataType, string typeName)
    {
        var factoryType = typeof(ExDataFactory<>).MakeGenericType(dataType);
        var instanceField = factoryType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);

        if (instanceField == null)
        {
            Logger.LogWarning($"Factory not found: {typeName}");
            return null;
        }

        var factoryInstance = instanceField.GetValue(null);
        if (factoryInstance == null)
        {
            Logger.LogWarning($"Factory instance null: {typeName}");
            return null;
        }

        // Call the public GetData() method instead of reflecting on private fields
        var getDataMethod = factoryType.GetMethod("GetData", BindingFlags.Public | BindingFlags.Instance);
        if (getDataMethod == null)
        {
            Logger.LogWarning($"GetData method not found: {typeName}");
            return null;
        }

        var dataDict = getDataMethod.Invoke(factoryInstance, null);
        if (dataDict is IDictionary dict)
        {
            var result = new Dictionary<int, object>();
            foreach (DictionaryEntry entry in dict)
            {
                result[(int)entry.Key] = entry.Value;
            }
            return result;
        }

        Logger.LogWarning($"GetData did not return a dictionary: {typeName}");
        return null;
    }

    private static void SaveToJson(object data, string filePath)
    {
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    private static JArray LoadFromJson(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<JArray>(json);
    }
}

