using CameraMinus;
using MelonLoader;
using UIExpansionKit.API;
using UnityEngine;
using VRC.SDKBase;
using VRC.UserCamera;
using System;
using System.Linq;
using System.Reflection;


[assembly:MelonGame("VRChat", "VRChat")]
[assembly:MelonInfo(typeof(CameraMinusMod), "CameraMinus", "3.0.1", "knah, PatchedPlus+", "https://github.com/knah/VRCMods")]

namespace CameraMinus
{
    internal class CameraMinusMod : MelonMod
    {
        private MelonPreferences_Entry<bool> myUseCameraExpando;
        private MelonPreferences_Entry<bool> myUnlimitCameraPickupDistance;



        private static Func<VRCUiManager> ourGetUiManager;
        private static Func<QuickMenu> ourGetQuickMenu;

        static CameraMinusMod()
        {

            ourGetUiManager = (Func<VRCUiManager>)Delegate.CreateDelegate(typeof(Func<VRCUiManager>), typeof(VRCUiManager)
                .GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .First(it => it.PropertyType == typeof(VRCUiManager)).GetMethod);
            ourGetQuickMenu = (Func<QuickMenu>)Delegate.CreateDelegate(typeof(Func<QuickMenu>), typeof(QuickMenu)
                .GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .First(it => it.PropertyType == typeof(QuickMenu)).GetMethod);

        }

        internal static VRCUiManager GetUiManager() => ourGetUiManager();
        internal static QuickMenu GetQuickMenu() => ourGetQuickMenu();



        public override void OnApplicationStart()
        {
            var category = MelonPreferences.CreateCategory("CameraMinus", "CameraMinus");
            
            myUseCameraExpando = category.CreateEntry("UseCameraExpando", true, "Use Camera expando (instead of QM expando)");
            myUnlimitCameraPickupDistance = category.CreateEntry("UnlimitCameraPickupDistance", true, "Longer camera pickup distance");

            ExpansionKitApi.GetSettingsCategory("CameraMinus")
                .AddLabel("Disable and enable camera to update camera expando visibility");
            
            GameObject cameraEnlargeButton = null;
            GameObject cameraShrinkButton = null;
            GameObject qmEnlargeButton = null;
            GameObject qmShrinkButton = null;
            
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.Camera).AddSimpleButton("Enlarge camera", Enlarge, go =>
            {
                cameraEnlargeButton = go;
                cameraEnlargeButton.SetActive(myUseCameraExpando.Value);
            });
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.Camera).AddSimpleButton("Shrink camera", Shrink, go =>
            {
                cameraShrinkButton = go;
                cameraShrinkButton.SetActive(myUseCameraExpando.Value);
            });
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.CameraQuickMenu).AddSimpleButton("Enlarge camera", Enlarge, go =>
            {
                qmEnlargeButton = go;
                qmEnlargeButton.SetActive(!myUseCameraExpando.Value);
            });
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.CameraQuickMenu).AddSimpleButton("Shrink camera", Shrink, go =>
            {
                qmShrinkButton = go;
                qmShrinkButton.SetActive(!myUseCameraExpando.Value);
            });

            myUseCameraExpando.OnValueChanged += (_, value) =>
            {
                if (cameraEnlargeButton != null) cameraEnlargeButton.SetActive(value);
                if (cameraShrinkButton != null) cameraShrinkButton.SetActive(value);
                if (qmEnlargeButton != null) qmEnlargeButton.SetActive(!value);
                if (qmShrinkButton != null) qmShrinkButton.SetActive(!value);
            };

            myUnlimitCameraPickupDistance.OnValueChanged += (_, value) =>
            {
                UpdateCameraPickupDistance(value);
            };

            ExpansionKitApi.OnUiManagerInit += () =>
            {
                UpdateCameraPickupDistance(myUnlimitCameraPickupDistance.Value);
            };
        }

        private static void UpdateCameraPickupDistance(bool value)
        {
            var controller = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (controller != null)
                controller.transform.Find("ViewFinder").GetComponent<VRC_Pickup>().proximity = value ? 20 : 1;
        }

        private void Enlarge()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;
            cameraController.transform.Find("ViewFinder").localScale *= 1.5f;
        }

        private void Shrink()
        {
            var cameraController = UserCameraController.field_Internal_Static_UserCameraController_0;
            if (cameraController == null) return;
            cameraController.transform.Find("ViewFinder").localScale /= 1.5f;
        }
    }
}