using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace EnglishPatch;

/// <summary>
/// Swaps the Text db asset in
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class MainPlugin : BaseUnityPlugin
{
    public const string ChineseCharPattern = @".*\p{IsCJKUnifiedIdeographs}.*";
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;

        // Plugin startup logic
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Harmony.CreateAndPatchAll(typeof(MainPlugin));
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} should be patched!");

        DisableEastAsianTmpSettings();
    }

    public void OnDestroy()
    {
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} is destroyed!");
    }

    private void DisableEastAsianTmpSettings()
    {
        var settings = TMP_Settings.instance;
        if (settings != null)
        {
            SetPrivateField(settings, "m_linebreakingRules", null);
            SetPrivateField(settings, "m_leadingCharacters", new TextAsset("("));
            SetPrivateField(settings, "m_followingCharacters", new TextAsset(")"));
            //SetPrivateField(settings, "m_GetFontFeaturesAtRuntime", false);
            
            TMP_Settings.LoadLinebreakingRules();
            Logger.LogMessage("Disabled East Asian TMP settings.");
        }
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
            field.SetValue(obj, value);
        else
            Logger.LogError($"Field '{fieldName}' not found in {obj.GetType().Name}.");
    }   
}