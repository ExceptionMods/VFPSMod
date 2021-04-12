﻿using HarmonyLib;

namespace VFPSMod
{
    [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
    public static class ObjectDB_CopyOtherDB_Patch
    {
        public static void Postfix()
        {
            VFPSModPlugin.TryRegisterItems();
            VFPSModPlugin.TryRegisterStatusEffects();
        }
    }

    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    public static class ObjectDB_Awake_Patch
    {
        public static void Postfix()
        {
            VFPSModPlugin.TryRegisterItems();
            VFPSModPlugin.TryRegisterStatusEffects();
        }
    }
}
