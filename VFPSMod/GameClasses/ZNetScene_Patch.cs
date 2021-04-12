using System;
using System.Collections.Generic;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace VFPSMod
{
    // TODO: Avoid wear and tear for things not rendered, or things around things not rendered.
    // TODO: Fix issue with monster cloning.
    // TODO: Fix on death issue.

    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    public static class ZNetScene_Awake_Patch
    {
        public static bool Prefix(ZNetScene __instance)
        {
            VFPSModPlugin.TryRegisterPrefabs(__instance);
            return true;
        }
    }

    public class ObjectScanThread
    {
        public static List<ZDO> CurrentObjects = new List<ZDO>();
        public static List<ZDO> CurrentDistantObjects = new List<ZDO>();
        
        private static float ScanDistance = 50.0f;
        private static float MinimumDistance = 0.0f;

        public static void CheckForInput()
        {
            if (Input.GetKeyUp(KeyCode.KeypadPlus) || Input.GetKeyUp(KeyCode.Equals))
            {
                ScanDistance += 10.0f;
                VFPSModPlugin.VFPSModLogger.LogInfo($"ObjectScanThread ScanDistance is now {ScanDistance}.");
                ChatUtils.SendLocalChatMessage("VFPSMod", $"Instance scan distance set to {ScanDistance}.", Talker.Type.Normal);
            }
            else if (Input.GetKeyUp(KeyCode.KeypadMinus) || Input.GetKeyUp(KeyCode.Minus))
            {
                if (ScanDistance <= MinimumDistance)
                {
                    VFPSModPlugin.VFPSModLogger.LogInfo($"ObjectScanThread ScanDistance is already at the minimum distance {ScanDistance}.");
                    ChatUtils.SendLocalChatMessage("VFPSMod", $"ObjectScanThread ScanDistance is already at the minimum distance {ScanDistance}.", Talker.Type.Normal);
                    return;
                }
                ScanDistance -= 10.0f;
                VFPSModPlugin.VFPSModLogger.LogInfo($"ObjectScanThread ScanDistance is now {ScanDistance}.");
                ChatUtils.SendLocalChatMessage("VFPSMod", $"Instance scan distance set to {ScanDistance}.", Talker.Type.Normal);
            }
        }

        public void ThreadEx()
        {
            while (true)
            {
                try
                {
                    this.RunScan();
                }
                catch (Exception ex)
                {
                    // Issue running scan.
                }

                VFPSModPlugin.VFPSModLogger.LogDebug($"ObjectScanThread AfterDist (SectorsCurrent = {CurrentObjects.Count}) (SectorsDistant = {CurrentDistantObjects.Count})");

                Thread.Sleep(100);
            }
        }

        public void RunScan()
        {
            VFPSModPlugin.VFPSModLogger.LogDebug($"ObjectScanThread Running.");

            Vector2i zone = ZoneSystem.instance.GetZone(ZNet.instance.GetReferencePosition());

            VFPSModPlugin.VFPSModLogger.LogDebug($"ObjectScanThread Zone = {zone}");

            List<ZDO> tempCurrentObjects = new List<ZDO>();
            List<ZDO> tempCurrentDistantObjects = new List<ZDO>();

            ZDOMan.instance.FindSectorObjects(zone, ZoneSystem.instance.m_activeArea,
                ZoneSystem.instance.m_activeDistantArea,
                tempCurrentObjects, tempCurrentDistantObjects);

            VFPSModPlugin.VFPSModLogger.LogDebug($"ObjectScanThread (SectorsCurrent = {tempCurrentObjects.Count}) (SectorsDistant = {tempCurrentDistantObjects.Count})");

            Dictionary<string, int> unique = new Dictionary<string, int>();

            if (Player.m_localPlayer?.transform?.position != null)
            {
                List<ZDO> filteredZdos = new List<ZDO>();
                foreach (ZDO zdo in tempCurrentObjects)
                {
                    // Distance based.
                    GameObject go = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
                    bool isMonster = go.GetComponent<MonsterAI>() != null;
                    bool canWearAndTear = go.GetComponent<WearNTear>() != null;
                    bool isTerraform = go.GetComponent<TerrainModifier>() != null;
                    
                    if (isMonster)
                    {
                        filteredZdos.Add(zdo);
                    }
                    else if (!canWearAndTear && !isTerraform)
                    {
                        if (!unique.ContainsKey(go.name))
                            unique[go.name] = 0;
                        unique[go.name] += 1;

                        float distance = Vector3.Distance(zdo.m_position, Player.m_localPlayer.transform.position);
                        if (distance <= ScanDistance * 2.0f)
                        {
                            filteredZdos.Add(zdo);
                        }
                    }
                    else
                    {
                        float distance = Vector3.Distance(zdo.m_position, Player.m_localPlayer.transform.position);
                        if (distance <= ScanDistance)
                        {
                            filteredZdos.Add(zdo);
                        }
                    }
                }

                // foreach (var un in unique)
                // {
                //     VFPSModPlugin.VFPSModLogger.LogDebug($"ObjectScanThread Unique = {un.Key} ; Num = {un.Value}");
                // }

                CurrentObjects = filteredZdos;
                CurrentDistantObjects = tempCurrentDistantObjects;
            }
            else
            {
                CurrentObjects = tempCurrentObjects;
                CurrentDistantObjects = tempCurrentDistantObjects;
            }
        }
    }

    [HarmonyPatch(typeof(ZNetScene), "CreateDestroyObjects")]
    public static class ZNetScene_CreateDestroyObjects_Patch
    {
        private static ObjectScanThread objectScanThread = null;

        private static bool Prefix(ref ZNetScene __instance)
        {
            if (ZNetScene.instance.InLoadingScreen())
            {
                return true;
            }

            if (objectScanThread == null)
            {
                VFPSModPlugin.VFPSModLogger.LogInfo($"Starting ObjectScanThread.");

                objectScanThread = new ObjectScanThread();
                
                // Force run scan once.
                objectScanThread.RunScan();

                // Then start thread.
                Thread scanThread = new Thread(objectScanThread.ThreadEx);
                scanThread.Start();
            }

            __instance.m_tempCurrentObjects.Clear();
            __instance.m_tempCurrentDistantObjects.Clear();

            __instance.m_tempCurrentObjects = new List<ZDO>(ObjectScanThread.CurrentObjects);
            __instance.m_tempCurrentDistantObjects = new List<ZDO>(ObjectScanThread.CurrentDistantObjects);

            __instance.CreateObjects(__instance.m_tempCurrentObjects, __instance.m_tempCurrentDistantObjects);
            __instance.RemoveObjects(__instance.m_tempCurrentObjects, __instance.m_tempCurrentDistantObjects);

            // Skip original code.
            return false;
        }
    }

    [HarmonyPatch(typeof(ZNetScene), "RemoveObjects")]
    public static class ZNetScene_RemoveObjects_Patch
    {
        private static bool Prefix(ref ZNetScene __instance, ref List<ZDO> currentNearObjects, ref List<ZDO> currentDistantObjects)
        {
            int frameCount = Time.frameCount;

            foreach (ZDO currentNearObject in currentNearObjects)
                currentNearObject.m_tempRemoveEarmark = frameCount;
            foreach (ZDO currentDistantObject in currentDistantObjects)
                currentDistantObject.m_tempRemoveEarmark = frameCount;

            __instance.m_tempRemoved.Clear();

            foreach (ZNetView znetView in __instance.m_instances.Values)
            {
                if (znetView.GetZDO().m_tempRemoveEarmark != frameCount)
                    __instance.m_tempRemoved.Add(znetView);
            }

            for (int index = 0; index < __instance.m_tempRemoved.Count; ++index)
            {
                ZNetView znetView = __instance.m_tempRemoved[index];

                ZDO zdo = znetView.GetZDO();

                znetView.ResetZDO();

                UnityEngine.Object.Destroy((UnityEngine.Object)znetView.gameObject);

                if (!zdo.m_persistent && zdo.IsOwner())
                    ZDOMan.instance.DestroyZDO(zdo);

                __instance.m_instances.Remove(zdo);
            }

            // Skip original code.
            return false;
        }
    }
}
