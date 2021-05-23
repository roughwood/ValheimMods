﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DamageMod
{
    [BepInPlugin("aedenthorn.DamageMod", "Damage Mod", "0.2.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;
        private static BepInExPlugin context;
        private Harmony harmony;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<float> tameDamageMult;
        public static ConfigEntry<float> wildDamageMult;
        public static ConfigEntry<float> playerDamageMult;
        
        public static ConfigEntry<string> customAttackerDamageMult;
        public static ConfigEntry<string> customDefenderDamageMult;

        public static Dictionary<string, float> attackerMults = new Dictionary<string, float>();
        public static Dictionary<string, float> defenderMults = new Dictionary<string, float>();


        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 1239, "Nexus mod ID for updates");

            tameDamageMult = Config.Bind<float>("Variables", "TameDamageMult", 1f, "Multiplier for damage taken by tame creatures");
            wildDamageMult = Config.Bind<float>("Variables", "WildDamageMult", 1f, "Multiplier for damage taken by wild creatures");
            playerDamageMult = Config.Bind<float>("Variables", "PlayerDamageMult", 1f, "Multiplier for damage taken by players");
            
            customAttackerDamageMult = Config.Bind<string>("Variables", "CustomAttackerDamageMult", "", "Custom attacker damage multipliers. Use comma-separated list of pairs separated by colon (:), e.g. Boar:1.5,Wolf:0.5");
            customDefenderDamageMult = Config.Bind<string>("Variables", "CustomDefenderDamageMult", "", "Custom defender damage multipliers. Use comma-separated list of pairs separated by colon (:), e.g. Boar:1.5,Wolf:0.5");

            if (!modEnabled.Value)
                return;

            SetCustomDamages();



            harmony = new Harmony(Info.Metadata.GUID);
            harmony.PatchAll();
        }

        private static void SetCustomDamages()
        {
            Dbgl(customAttackerDamageMult.Value);
            Dbgl(customDefenderDamageMult.Value);
            foreach (string pair in customAttackerDamageMult.Value.Split(','))
            {
                if (!pair.Contains(":") || !float.TryParse(pair.Split(':')[1], out float result))
                    continue;

                attackerMults.Add(pair.Split(':')[0], result);
            }

            foreach (string pair in customDefenderDamageMult.Value.Split(','))
            {
                if (!pair.Contains(":") || !float.TryParse(pair.Split(':')[1], out float result))
                    continue;

                defenderMults.Add(pair.Split(':')[0], result);
            }
        }

        [HarmonyPatch(typeof(Character), "RPC_Damage")]
        static class RPC_Damage_Patch
        {
            static void Prefix(Character __instance, ref HitData hit)
            {
                if (!modEnabled.Value)
                    return;

                var attacker = Utils.GetPrefabName(hit.GetAttacker().gameObject);
                var defender = Utils.GetPrefabName(__instance.gameObject);

                Dbgl($"attacker: {attacker} defender {defender} pre dmg {hit.GetTotalDamage()}");

                if (__instance.IsPlayer())
                    hit.ApplyModifier(playerDamageMult.Value);
                else if (__instance.IsTamed())
                    hit.ApplyModifier(tameDamageMult.Value);
                else 
                    hit.ApplyModifier(wildDamageMult.Value);

                if (defenderMults.TryGetValue(defender, out float mult1))
                {
                    Dbgl($"Applying mult of {mult1} for defender {defender}");
                    hit.ApplyModifier(mult1);
                }
                else if (hit.GetAttacker() && attackerMults.TryGetValue(attacker, out float mult2))
                {
                    Dbgl($"Applying mult of {mult2} for attacker {attacker}");
                    hit.ApplyModifier(mult2);
                }

                Dbgl($"post dmg {hit.GetTotalDamage()}");
            }
        }

        [HarmonyPatch(typeof(Console), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Console __instance)
            {
                if (!modEnabled.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"{typeof(BepInExPlugin).Namespace.ToLower()} reset"))
                {
                    context.Config.Reload();
                    context.Config.Save();
                    SetCustomDamages();
                    Traverse.Create(__instance).Method("AddString", new object[] { text }).GetValue();
                    Traverse.Create(__instance).Method("AddString", new object[] { $"{context.Info.Metadata.Name} config reloaded" }).GetValue();
                    return false;
                }
                return true;
            }
        }
    }
}
