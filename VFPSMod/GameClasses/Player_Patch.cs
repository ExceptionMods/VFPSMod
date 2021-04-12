using HarmonyLib;

namespace VFPSMod
{
    [HarmonyPatch(typeof(Player), "Update")]
    public static class Player_Update_Patch
    {
        private static void Postfix(ref Player __instance)
        {
            if (!__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner()) return;
            ObjectScanThread.CheckForInput();
        }
    }
}
