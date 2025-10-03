using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Utility;
using System.Reflection;
using System;
using System.Collections.Generic; // Required for List<int>

// NOTE: The Harmony12 alias is expected to be defined in your main mod file.

namespace SwordSaintFixMod // IMPORTANT: Updated namespace for better consistency!
{
    // HarmonyPatch targets the BlueprintSpellbook class and specifically its SpellsPerDay field accessor/getter.
    [Harmony12.HarmonyPatch(typeof(BlueprintSpellbook), nameof(BlueprintSpellbook.SpellsPerDay), Harmony12.MethodType.Getter)]
    public static class SwordSaintMinimalSpellFix
    {
        // The target GUID for the Magus Spellbook (used by Sword Saint).
        private const string SwordSaintSpellbookGuid = "b4632b73307525f448e89f66cc78280f";

        // This method runs AFTER the original SpellsPerDay data is accessed.
        // We removed 'ref BlueprintSpellsTable __result' to simplify the Postfix and modify the instance directly.
        [Harmony12.HarmonyPostfix]
        private static void Postfix(BlueprintSpellbook __instance)
        {
            // 1. Filter: Only apply this patch to the Sword Saint Spellbook GUID.
            if (!__instance.AssetGuid.Equals(SwordSaintSpellbookGuid))
            {
                return;
            }

            // The SpellsPerDay field holds the BlueprintSpellsTable, which contains the array data.
            BlueprintSpellsTable spellsTable = __instance.SpellsPerDay;

            if (spellsTable == null)
            {
                UberDebug.LogError("[SwordSaintFixMod] SpellsPerDay table is null. Cannot apply fix.");
                return;
            }

            // 2. Use Reflection to access the private array inside BlueprintSpellsTable.
            // In many Pathfinder blueprints, the internal array data is often named 'm_Levels' or 'm_Data'.
            var m_LevelsField = typeof(BlueprintSpellsTable).GetField("m_Levels", BindingFlags.Instance | BindingFlags.NonPublic);

            if (m_LevelsField == null)
            {
                UberDebug.LogError("[SwordSaintFixMod] Failed to find internal 'm_Levels' field in BlueprintSpellsTable. Cannot apply fix.");
                return;
            }

            // The internal table array is typically List<int>[]
            var dataArray = m_LevelsField.GetValue(spellsTable) as List<int>[];

            if (dataArray == null)
            {
                UberDebug.LogError("[SwordSaintFixMod] Internal table data is null or wrong type. Cannot apply fix.");
                return;
            }

            // --- Apply Minimal Fix ---
            const int SpellLevelIndex = 2; // Corresponds to Spell Level 2 (index 0 is level 0 spells)
            const int ClassLevelIndex = 4; // Corresponds to Class Level 5 (index 0 is class level 1)

            // Check array boundaries before modifying
            try
            {
                if (dataArray.Length > SpellLevelIndex &&
                    dataArray[SpellLevelIndex] != null &&
                    dataArray[SpellLevelIndex].Count > ClassLevelIndex)
                {
                    // Increase the slot count by 1 for this specific cell (Level 2 slot at Class Level 5).
                    dataArray[SpellLevelIndex][ClassLevelIndex] += 1;
                    UberDebug.Log("[SwordSaintFixMod] Sword Saint Lvl 2 Slot at Class Lvl 5 (index 4) corrected successfully. Now should be 2+INT Mod.");
                }
                else
                {
                    UberDebug.LogWarning("[SwordSaintFixMod] Sword Saint Spellbook: Target cell for fix out of bounds. Fix skipped.");
                }
            }
            catch (Exception e)
            {
                UberDebug.LogError($"[SwordSaintFixMod] Error applying fix: {e.Message}");
            }
        }
    }
}
