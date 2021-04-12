using HarmonyLib;

namespace VFPSMod
{
    [HarmonyPatch(typeof(WearNTear), "Awake")]
    public static class WearNTear_Awake_Patch
    {
        public static bool Prefix(ref WearNTear __instance)
        {
            return false;
        }
    }

    // [HarmonyPatch(typeof(WearNTear), "UpdateWear")]
    // public static class WearNTear_UpdateWear_Patch
    // {
    //     public static bool Prefix(ref WearNTear __instance)
    //     {
    //         if (!__instance.m_nview.IsValid())
    //         {
    //             return false;
    //         }
    //
    //         if (__instance.m_nview.IsOwner() && __instance.ShouldUpdate())
    //         {
    //             if (ZNetScene.instance.OutsideActiveArea(__instance.transform.position))
    //             {
    //                 __instance.m_support = __instance.GetMaxSupport();
    //                 __instance.m_nview.GetZDO().Set("support", __instance.m_support);
    //                 return false;
    //             }
    //             else
    //             {
    //                 return true;
    //             }
    //         }
    //
    //         return true;
    //     }
    // }
}
