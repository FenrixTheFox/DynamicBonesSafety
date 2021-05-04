using DynamicBonesSafety;
using MelonLoader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[assembly: MelonGame("VRChat", "VRChat")]
[assembly: MelonInfo(typeof(DBSMod), "Dynamic Bones Safety", "2.1.0", "Fenrix")]

namespace DynamicBonesSafety
{
    public class DBSMod : MelonMod
    {
        public const int DynamicBonesEnumValue = 100;

        public static Dictionary<string, bool> CanUseBones = new Dictionary<string, bool>();

        private static VRCUiPageSafety _cachedPageSafety;
        public static MethodInfo _cachedSelectedSafetyClass;
        public static string UiPageSafetySelectedSocial => _cachedSelectedSafetyClass.Invoke(_cachedPageSafety, new object[] { }).ToString();

        public override void OnApplicationStart()
        {
            // Generate Config
            if (Directory.Exists("UserData")
                && File.Exists("UserData/DynamicBonesSafety.json"))
            {
                CanUseBones = JsonConvert.DeserializeObject<Dictionary<string, bool>>(
                    File.ReadAllText("UserData/DynamicBonesSafety.json"));
            }
            else
            {
                Directory.CreateDirectory("UserData");
            }

            _cachedSelectedSafetyClass = typeof(VRCUiPageSafety).GetProperties().Where(prop => prop.PropertyType.IsEnum).First().GetGetMethod();
            ModPatches.DoPatch(Harmony);
        }

        public override void VRChat_OnUiManagerInit()
        {
            GameObject SafetyUiMenu = GameObject.Find("UserInterface/MenuContent/Screens/Settings_Safety");
            _cachedPageSafety = SafetyUiMenu.GetComponent<VRCUiPageSafety>();

            Transform SafetyMatrixTogglesTransform = GameObject.Find("UserInterface/MenuContent/Screens/Settings_Safety/_SafetyMatrix/_Toggles").transform;

            // Rescale all children to make it not be  S Q U I S H
            foreach (var t in SafetyMatrixTogglesTransform)
            {
                t.Cast<Transform>().localScale = new Vector3(0.9f, 0.9f, 0.9f);
            }

            GameObject DynamicBoneToggle = GameObject.Instantiate(GameObject.Find("UserInterface/MenuContent/Screens/Settings_Safety/_SafetyMatrix/_Toggles/Toggle_CustomAnimations"), SafetyMatrixTogglesTransform);
            DynamicBoneToggle.name = "Toggle_DynamicBones";

            DynamicBoneToggle.GetComponentInChildren<Text>().text = "Dynamic Bones";

            UiSafetyFeatureToggle UiSafetyFeature = DynamicBoneToggle.GetComponent<UiSafetyFeatureToggle>();
            UiSafetyFeature.field_Public_String_0 = "<i>Dynamic Bones</i> turns on or off dynamic bones on a user's avatar for the combined <i>Safety Mode</i> and <i>User Trust Rank</i>.";
            UiSafetyFeature.GetType().GetProperties().Where(prop => prop.PropertyType.IsEnum).First().SetValue(UiSafetyFeature, DynamicBonesEnumValue);

            Toggle BoneToggle = DynamicBoneToggle.GetComponent<Toggle>();

            BoneToggle.onValueChanged.AddListener(DelegateSupport.ConvertDelegate<UnityAction<bool>>(new Action<bool>(delegate
            {
                CanUseBones[UiPageSafetySelectedSocial] = BoneToggle.isOn;
                File.WriteAllText("UserData/DynamicBonesSafety.json", JsonConvert.SerializeObject(CanUseBones, Formatting.Indented));
            })));

            // Load our icon
            // Taken directly from UIX
            AssetBundle assetBundle;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DynamicBonesSafety.dbsmod"))
            using (var tempStream = new MemoryStream((int)stream.Length))
            {
                stream.CopyTo(tempStream);

                assetBundle = AssetBundle.LoadFromMemory_Internal(tempStream.ToArray(), 0);
                assetBundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }

            DynamicBoneToggle.GetComponentInChildren<RawImage>().texture = assetBundle.LoadAsset_Internal("boneIcon", Il2CppType.Of<Texture2D>()).Cast<Texture2D>();
        }
    }
}
