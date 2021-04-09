using Harmony;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnhollowerBaseLib;
using UnhollowerBaseLib.Attributes;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.Management;
using VRC.SDKBase;

namespace DynamicBonesSafety
{
    class ModPatches
    {
        private static MethodInfo GetUserSocialRank = null;

        public static void DoPatch(HarmonyInstance instance)
        {
            Type PermissionType = typeof(FeaturePermissionSet).GetNestedTypes()[0];

            foreach (MethodInfo IsPermissionEnabled in typeof(FeaturePermissionSet).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.StartsWith($"Method_Public_Boolean_{PermissionType.Name}")))
                instance.Patch(IsPermissionEnabled, GetPatch(nameof(IsPermissionEnabled)));

            foreach (MethodInfo AvatarFinishedLoading in typeof(VRCPlayer).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.StartsWith("Method_Private_Void_GameObject_VRC_AvatarDescriptor_Boolean_") && !checkXref(m, "Avatar is Ready, Initializing")))
                instance.Patch(AvatarFinishedLoading, null, GetPatch(nameof(AvatarFinishedLoadingPostfix)));

            GetUserSocialRank = typeof(FeaturePermissionManager).GetMethods().Where(m => m.ReturnType.IsEquivalentTo(DBSMod._cachedSelectedSafetyClass.ReturnType) && !m.Name.Contains("_PDM_"))
                .Where(m => m.GetCustomAttribute<CallerCountAttribute>().Count != 0)
                .OrderBy(m => m.GetCustomAttribute<CallerCountAttribute>().Count)
                .First();
        }

        private static bool IsPermissionEnabled(ref bool __result, int __0)
        {
            if (__0 != DBSMod.DynamicBonesEnumValue)
                return true;

            if (!DBSMod.CanUseBones.TryGetValue(DBSMod.UiPageSafetySelectedSocial, out __result))
                __result = false;

            return false;
        }

        private static void AvatarFinishedLoadingPostfix(VRCPlayer __instance, GameObject __0, VRC_AvatarDescriptor __1, bool __2)
        {
            if (__instance == null
                || __0 == null
                || __1 == null
                || !__2)
                return;

            if (__instance == VRCPlayer.field_Internal_Static_VRCPlayer_0)
                return;

            APIUser apiUser = __instance.field_Private_Player_0.field_Private_APIUser_0;

            if (apiUser == null)
                return;

            if (IsAvatarExplicityShown(apiUser.id))
                return;

            string selectedTrust = GetUserTrustRank(apiUser);

            if (DBSMod.CanUseBones.ContainsKey(selectedTrust.ToString()))
            {
                if (!DBSMod.CanUseBones[selectedTrust.ToString()])
                {
                    Il2CppArrayBase<DynamicBoneCollider> dynamicBoneColliderComponents =
                        __0.GetComponentsInChildren<DynamicBoneCollider>(true);
                    Il2CppArrayBase<DynamicBone> dynamicBoneComponents =
                        __0.GetComponentsInChildren<DynamicBone>(true);

                    foreach (DynamicBoneCollider dynamicBoneCollider in dynamicBoneColliderComponents)
                    {
                        GameObject.DestroyImmediate(dynamicBoneCollider, true);
                    }

                    foreach (DynamicBone dynamicBone in dynamicBoneComponents)
                    {
                        GameObject.DestroyImmediate(dynamicBone, true);
                    }
                }
            }
        }

        private static string GetUserTrustRank(APIUser apiUser)
        {
            return GetUserSocialRank.Invoke(FeaturePermissionManager.prop_FeaturePermissionManager_0, new object[] { apiUser }).ToString();
        }

        private static bool IsAvatarExplicityShown(string userId)
        {
            foreach (var playerModeration in ModerationManager.prop_ModerationManager_0.field_Private_List_1_ApiPlayerModeration_0)
            {
                if (playerModeration.moderationType == ApiPlayerModeration.ModerationType.ShowAvatar && playerModeration.targetUserId == userId)
                {
                    return true;
                }
            }

            return false;
        }

        private static HarmonyMethod GetPatch(string name)
        {
            return new HarmonyMethod(typeof(ModPatches).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static));
        }

        private static bool checkXref(MethodBase m, string match)
        {
            try
            {
                return XrefScanner.XrefScan(m).Any(
                    instance => instance.Type == XrefType.Global && instance.ReadAsObject() != null && instance.ReadAsObject().ToString()
                                   .Equals(match, StringComparison.OrdinalIgnoreCase));
            }
            catch { } // ignored

            return false;
        }
    }
}
