using HarmonyLib;
using UnityEngine;

namespace VFPSMod
{
    [HarmonyPatch(typeof(InstanceRenderer), "Update")]
    public static class InstanceRenderer_Update_Patch
    {
        public static bool Prefix(ref InstanceRenderer __instance)
        {
            Camera mainCamera = Utils.GetMainCamera();
            if (__instance.m_instanceCount == 0 || mainCamera == null)
            {
                return false;
            }
            if (__instance.m_frustumCull)
            {
                if (__instance.m_dirtyBounds)
                {
                    __instance.UpdateBounds();
                }
                if (!Utils.InsideMainCamera(__instance.m_bounds))
                {
                    return false;
                }
            }
            
            Graphics.DrawMeshInstanced(__instance.m_mesh, 0, __instance.m_material, __instance.m_instances, __instance.m_instanceCount, null, __instance.m_shadowCasting);

			// Skip original code.
			return false;
        }
    }
}
