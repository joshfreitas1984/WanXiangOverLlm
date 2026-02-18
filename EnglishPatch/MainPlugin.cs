using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
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

    [HarmonyPatch(typeof(GameTools), nameof(GameTools.ConvertNumberToChineseDate))]
    [HarmonyPrefix]
    public static bool ConvertNumberToChineseDate_Prefix(int number, ref string __result)
    {
        if (number <= 0)
            __result = "0";
        else
            __result = Convert.ToString(number);

        return false;
    }

    [HarmonyPatch(typeof(GameTools), nameof(GameTools.ConvertNumberToChineseNoUnit))]
    [HarmonyPrefix]
    public static bool ConvertNumberToChineseNoUnit_Prefix(int number, ref string __result)
    {
        try
        {
            if (number >= 1_000_000_000)
            {
                double billions = number / 1_000_000_000.0;
                __result = $"{billions:0.##}B";
            }
            else if (number >= 1_000_000)
            {
                double millions = number / 1_000_000.0;
                __result = $"{millions:0.##}M";
            }
            else if (number >= 1_000)
            {
                double thousands = number / 1_000.0;
                __result = $"{thousands:0.##}K";
            }
            else
            {
                __result = number.ToString();
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in ConvertNumberToChineseNoUnit patch: {ex}");
            return true;
        }
    }

    [HarmonyPatch(typeof(TypingLogic), "GetTypeSpeed")]
    [HarmonyPrefix]
    public static bool GetTypeSpeed_Prefix(ref float __result)
    {
        try
        {
            // English text requires faster typing speeds since English uses more characters
            // than Chinese for the same amount of information. Original speeds were:
            // Normal: 0.05f, Fast: 0.02f, Instant: 0.0f
            // We divide by 2.5 to make it feel more responsive for English readers

            switch (GameManager.Instance.TextSpeed)
            {
                case TextSpeed.Normal:
                    __result = 0.02f;  // Original: 0.05f
                    break;
                case TextSpeed.Fast:
                    __result = 0.008f; // Original: 0.02f
                    break;
                default:
                    __result = 0.0f;   // Instant stays the same
                    break;
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in GetTypeSpeed patch: {ex}");
            return true;
        }
    }

    [HarmonyPatch(typeof(CreateMenu), "OnInputChange")]
    [HarmonyPrefix]
    public static bool CreateMenu_OnInputChange_Prefix(string value, CreateMenu __instance)
    {
        if (__instance.InputField.text.Length > 20)
        {
            MenuManager.ShowDictTips(72);
            __instance.InputField.text = __instance.InputField.text.Substring(0, 20);
        }
        __instance.NameText.text = __instance.InputField.text.Length > 0 ? __instance.InputField.text : __instance.DefaultNameText.text;
        __instance.NameTextTc.text = __instance.NameText.text;

        return false;
    }

    [HarmonyPatch(typeof(MainMenu), "UpdateMenpaiAndMoney")]
    [HarmonyPostfix]
    public static void MainMenu_UpdateMenpaiAndMoney_Prefix(MainMenu __instance)
    {
        __instance.NameText.text = GameManager.Instance.Player.Name + " · " + GameTools.GetDictionaryString(GameManager.Instance.Player.MenPai);
    }
}