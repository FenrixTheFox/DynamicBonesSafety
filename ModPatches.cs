using HarmonyLib;
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
using FeaturePermissionSet = ObjectPublicStBoBoStBoBoBoBoBoBoUnique;
using FeaturePermissionSet_PermissionType = EnumPublicSealedvaNoCa10CaCaCaCaCaCaUnique;

namespace DynamicBonesSafety
{
    class ModPatches
    {
        private static MethodInfo GetUserSocialRank = null;

        public static void DoPatch(HarmonyLib.Harmony instance)
        {
            MelonLogger.Msg("[DEBUG] Patches - 1");
            Type PermissionType = typeof(FeaturePermissionSet_PermissionType);
            MelonLogger.Msg("[DEBUG] Patches - 2");
            foreach (MethodInfo IsPermissionEnabled in typeof(FeaturePermissionSet).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.StartsWith($"Method_Public_Boolean_{PermissionType.Name}")))
                instance.Patch(IsPermissionEnabled, GetPatch(nameof(IsPermissionEnabled)));
            MelonLogger.Msg("[DEBUG] Patches - 3");
            instance.Patch(typeof(VRCPlayer).GetMethod(nameof(VRCPlayer.Awake)), postfix: GetPatch(nameof(VRCPlayerAwakePostfix)));
            //foreach (MethodInfo AvatarFinishedLoading in typeof(VRCPlayer).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            //    .Where(m => m.Name.StartsWith("Method_Private_Void_GameObject_VRC_AvatarDescriptor_Boolean_") && !checkXref(m, "Avatar is Ready, Initializing")))
            //    instance.Patch(AvatarFinishedLoading, null, GetPatch(nameof(AvatarFinishedLoadingPostfix)));
            MelonLogger.Msg("[DEBUG] Patches - 4");
            GetUserSocialRank = typeof(FeaturePermissionManager).GetMethods().Where(m => m.ReturnType.IsEquivalentTo(DBSMod._cachedSelectedSafetyClass.ReturnType) && !m.Name.Contains("_PDM_"))
                .Where(m => m.GetCustomAttribute<CallerCountAttribute>().Count != 0)
                .OrderBy(m => m.GetCustomAttribute<CallerCountAttribute>().Count)
                .First();
            MelonLogger.Msg("[DEBUG] Patches - 5");
        }

        private static bool IsPermissionEnabled(ref bool __result, int __0)
        {
            if (__0 != DBSMod.DynamicBonesEnumValue)
                return true;

            if (!DBSMod.CanUseBones.TryGetValue(DBSMod.UiPageSafetySelectedSocial, out __result))
                __result = false;

            return false;
        }

        private static void VRCPlayerAwakePostfix(VRCPlayer __instance)
        {
            if (__instance == null)
                return;

            __instance.Method_Public_add_Void_OnAvatarIsReady_0(new Action(() =>
             {
                 var player = __instance._player;
                 if (player == null) return;

                 var avatarManager = __instance.prop_VRCAvatarManager_0;
                 if (avatarManager == null) return;

                 if (__instance._player.field_Private_APIUser_0 != null)
                 {
                     if (__instance == VRCPlayer.field_Internal_Static_VRCPlayer_0)
                         return;

                     APIUser apiUser = __instance._player.prop_APIUser_0;

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
                                 avatarManager.prop_GameObject_0.GetComponentsInChildren<DynamicBoneCollider>(true);
                             Il2CppArrayBase<DynamicBone> dynamicBoneComponents =
                                 avatarManager.prop_GameObject_0.GetComponentsInChildren<DynamicBone>(true);

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
             }));
        }

        private static string GetUserTrustRank(APIUser apiUser)
        {
            return GetUserSocialRank.Invoke(FeaturePermissionManager.prop_FeaturePermissionManager_0, new object[] { apiUser }).ToString();
        }

        private static bool IsAvatarExplicityShown(string userId)
        {
            if (!ModerationManager.prop_ModerationManager_0.field_Private_Dictionary_2_String_List_1_ApiPlayerModeration_0.ContainsKey(userId))
                return false;

            foreach (var playerModeration in ModerationManager.prop_ModerationManager_0.field_Private_Dictionary_2_String_List_1_ApiPlayerModeration_0[userId])
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
