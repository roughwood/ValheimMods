﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Durability
{
    [BepInPlugin("aedenthorn.Durability", "Durability", "0.5.2")]
    public class Durability : BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<float> torchDurabilityDrain;
        public static ConfigEntry<float> weaponDurabilityLoss;
        public static ConfigEntry<float> bowDurabilityLoss;

        public static ConfigEntry<float> toolDurabilityLoss;
        public static ConfigEntry<float> torchDurabilityLoss;
        public static ConfigEntry<float> hammerDurabilityLoss;
        public static ConfigEntry<float> hoeDurabilityLoss;
        public static ConfigEntry<float> pickaxeDurabilityLoss;
        public static ConfigEntry<float> axeDurabilityLoss;

        public static ConfigEntry<float> shieldDurabilityLossMult;
        public static ConfigEntry<float> armorDurabilityLossMult;
       
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(Durability ).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            torchDurabilityDrain = Config.Bind<float>("Durability", "TorchDurabilityDrain", 0.033f, "Torch durability drain over time.");
            torchDurabilityLoss = Config.Bind<float>("Durability", "TorchDurabilityLoss", 1f, "Torch durability loss when used to attack.");
            weaponDurabilityLoss = Config.Bind<float>("Durability", "WeaponDurabilityLoss", 1f, "Weapon durability loss per use.");
            bowDurabilityLoss = Config.Bind<float>("Durability", "BowDurabilityLoss", 1f, "Bow durability loss per use.");
            hammerDurabilityLoss = Config.Bind<float>("Durability", "HammerDurabilityLoss", 1f, "Hammer durability loss per use.");
            hoeDurabilityLoss = Config.Bind<float>("Durability", "HoeDurabilityLoss", 1f, "Hoe durability loss per use.");
            pickaxeDurabilityLoss = Config.Bind<float>("Durability", "PickaxeDurabilityLoss", 1f, "Pickaxe durability loss per use.");
            axeDurabilityLoss = Config.Bind<float>("Durability", "AxeDurabilityLoss", 1f, "Axe durability loss per use.");
            toolDurabilityLoss = Config.Bind<float>("Durability", "ToolDurabilityLoss", 1f, "Other tool durability loss per use.");

            shieldDurabilityLossMult = Config.Bind<float>("Durability", "ShieldDurabilityLossMult", 1f, "Shield durability loss multiplier.");
            armorDurabilityLossMult = Config.Bind<float>("Durability", "ArmorDurabilityLossMult", 1f, "Armor durability loss multiplier.");


            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 17, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(ItemDrop), "Awake")]
        static class ItemDrop_Patch
        {

            static void Postfix(ItemDrop __instance)
            {
                if (modEnabled.Value && __instance.name != null && __instance.m_itemData?.m_shared != null)
                {
                    //Dbgl($"{__instance.name}, type: {Enum.GetName(typeof(ItemDrop.ItemData.ItemType), __instance.m_itemData.m_shared.m_itemType)} drain: {__instance.m_itemData.m_shared.m_durabilityDrain}, use: {__instance.m_itemData.m_shared.m_useDurabilityDrain}");

                    if (__instance.name.StartsWith("Pickaxe"))
                        __instance.m_itemData.m_shared.m_useDurabilityDrain = pickaxeDurabilityLoss.Value;
                    else if (__instance.name.StartsWith("Axe"))
                        __instance.m_itemData.m_shared.m_useDurabilityDrain = axeDurabilityLoss.Value;
                    else
                    {
                        switch (__instance.m_itemData.m_shared.m_itemType)
                        {
                            case ItemDrop.ItemData.ItemType.Torch:
                                __instance.m_itemData.m_shared.m_durabilityDrain = torchDurabilityDrain.Value;
                                __instance.m_itemData.m_shared.m_useDurabilityDrain = torchDurabilityLoss.Value;
                                break;
                            case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                            case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                                __instance.m_itemData.m_shared.m_useDurabilityDrain = weaponDurabilityLoss.Value;
                                break;
                            case ItemDrop.ItemData.ItemType.Bow:
                                __instance.m_itemData.m_shared.m_useDurabilityDrain = bowDurabilityLoss.Value;
                                break;
                            case ItemDrop.ItemData.ItemType.Tool:
                                if (__instance.name.StartsWith("Hammer"))
                                    __instance.m_itemData.m_shared.m_useDurabilityDrain = hammerDurabilityLoss.Value;
                                else if (__instance.name.StartsWith("Hoe"))
                                    __instance.m_itemData.m_shared.m_useDurabilityDrain = hoeDurabilityLoss.Value;
                                else
                                    __instance.m_itemData.m_shared.m_useDurabilityDrain = toolDurabilityLoss.Value;
                                break;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), "DamageArmorDurability")]
        static class DamageArmorDurability_Patch
        {
            static void Prefix(Humanoid __instance, ref float[] __state, ItemDrop.ItemData ___m_chestItem, ItemDrop.ItemData ___m_legItem, ItemDrop.ItemData ___m_shoulderItem, ItemDrop.ItemData ___m_helmetItem)
            {
                __state = new float[4];
                if (modEnabled.Value && __instance.IsPlayer())
                {
                    __state[0] = ___m_chestItem?.m_durability ?? -1f;
                    __state[1] = ___m_legItem?.m_durability ?? -1f;
                    __state[2] = ___m_shoulderItem?.m_durability ?? -1f;
                    __state[3] = ___m_helmetItem?.m_durability ?? -1f;
                }
            }
            static void Postfix(Humanoid __instance, float[] __state, ref ItemDrop.ItemData ___m_chestItem, ref ItemDrop.ItemData ___m_legItem, ref ItemDrop.ItemData ___m_shoulderItem, ref ItemDrop.ItemData ___m_helmetItem)
            {
                if (modEnabled.Value && __instance.IsPlayer())
                {
                    if(___m_chestItem != null && __state[0] > ___m_chestItem.m_durability)
                    {
                        Dbgl($"chest old {__state[0]} new {___m_chestItem.m_durability} final {__state[0] - (__state[0] - ___m_chestItem.m_durability) * armorDurabilityLossMult.Value}");
                        ___m_chestItem.m_durability = Mathf.Max(0, __state[0] - (__state[0] - ___m_chestItem.m_durability) * armorDurabilityLossMult.Value);
                    }
                    if (___m_legItem != null && __state[1] > ___m_legItem.m_durability)
                    {
                        Dbgl($"leg old {__state[1]} new {___m_legItem.m_durability} final {__state[1] - (__state[1] - ___m_legItem.m_durability) * armorDurabilityLossMult.Value}");
                        ___m_legItem.m_durability = Mathf.Max(0, __state[1] - (__state[1] - ___m_legItem.m_durability) * armorDurabilityLossMult.Value);
                    }
                    if (___m_shoulderItem != null && __state[2] > ___m_shoulderItem.m_durability)
                    {
                        Dbgl($"shoulder old {__state[2]} new {___m_shoulderItem.m_durability} final {__state[2] - (__state[2] - ___m_shoulderItem.m_durability) * armorDurabilityLossMult.Value}");
                        ___m_shoulderItem.m_durability = __state[2] - (__state[2] - ___m_shoulderItem.m_durability) * armorDurabilityLossMult.Value;
                    }
                    if (___m_helmetItem != null && __state[3] > ___m_helmetItem.m_durability)
                    {
                        Dbgl($"helmet old {__state[3]} new {___m_helmetItem.m_durability} final {__state[3] - (__state[3] - ___m_helmetItem.m_durability) * armorDurabilityLossMult.Value}");

                        ___m_helmetItem.m_durability = __state[3] - (__state[3] - ___m_helmetItem.m_durability) * armorDurabilityLossMult.Value;
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(Humanoid), "BlockAttack")]
        static class BlockAttack_Patch
        {
            static void Prefix(Humanoid __instance, ref float __state, ItemDrop.ItemData ___m_leftItem)
            {
                if (modEnabled.Value && __instance.IsPlayer() && ___m_leftItem != null)
                {
                    __state = ___m_leftItem.m_durability;
                }
            }
            static void Postfix(Humanoid __instance, float __state, ref ItemDrop.ItemData ___m_leftItem)
            {
                if (modEnabled.Value && __instance.IsPlayer())
                {
                    if(__state > 0 && ___m_leftItem != null && __state > ___m_leftItem.m_durability && ___m_leftItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
                    {
                        Dbgl($"shield old {__state} new {___m_leftItem.m_durability} final {__state - (__state - ___m_leftItem.m_durability) * shieldDurabilityLossMult.Value}");

                        ___m_leftItem.m_durability = Mathf.Max(0, __state - (__state - ___m_leftItem.m_durability) * shieldDurabilityLossMult.Value);
                    }
                }
            }
        }


        [HarmonyPatch(typeof(Attack), "DoAreaAttack")]
        static class DoAreaAttack_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling DoAreaAttack");

                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Dup && codes[i + 2].opcode == OpCodes.Ldc_R4 && (float)(codes[i + 2].operand) == 1 && codes[i+3].opcode == OpCodes.Sub)
                    {
                        Dbgl($"got -1, replacing with {weaponDurabilityLoss.Value}");
                        codes[i + 2].operand = weaponDurabilityLoss.Value;
                    }
                }

                return codes.AsEnumerable();
            }
        }
    }
}
