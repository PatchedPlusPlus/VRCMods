using CameraMinus;
using MelonLoader;
using UIExpansionKit.API;
using UIExpansionKit.API.Controls;
using VRC.SDKBase;
using VRC.UserCamera;
using System;
using System.Linq;
using System.Reflection;


[assembly:MelonGame("VRChat", "VRChat")]
[assembly:MelonInfo(typeof(CameraMinusMod), "CameraMinus", "3.1.0", "knah, P a t c h e d   P l u s +", "https://github.com/knah/VRCMods")]

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
            
            IMenuToggle cameraGrabbableButton = null;
            IMenuToggle qmGrabbableButton = null;
            
            IMenuToggle cameraUiVisibleButton = null;
            IMenuToggle qmUiVisibleButton = null;

            void SetCameraGrabbable(bool grabbable)
            {
                var controller = UserCameraController.field_Internal_Static_UserCameraController_0;
                if (controller != null)
                    controller.transform.Find("ViewFinder").GetComponent<VRC_Pickup>().pickupable = grabbable;

                cameraGrabbableButton!.Selected = qmGrabbableButton!.Selected = grabbable;
            }
            
            void SetUiVisible(bool visible)
            {
                var controller = UserCameraController.field_Internal_Static_UserCameraController_0;
                if (controller != null)
                    controller.transform.Find("ViewFinder/PhotoControls").gameObject.SetActive(visible);

                cameraUiVisibleButton!.Selected = qmUiVisibleButton!.Selected = visible;
            }

            var cameraEnlargeButton = ExpansionKitApi.GetExpandedMenu(ExpandedMenu.Camera).AddSimpleButton("Enlarge camera", Enlarge);
            var cameraShrinkButton = ExpansionKitApi.GetExpandedMenu(ExpandedMenu.Camera).AddSimpleButton("Shrink camera", Shrink);
            cameraGrabbableButton = ExpansionKitApi.GetExpandedMenu(ExpandedMenu.Camera).AddToggleButton("Grabbable", SetCameraGrabbable, () => true);
            cameraUiVisibleButton = ExpansionKitApi.GetExpandedMenu(ExpandedMenu.Camera).AddToggleButton("UI visible", SetUiVisible, () => true);
            var qmEnlargeButton = ExpansionKitApi.GetExpandedMenu(ExpandedMenu.CameraQuickMenu).AddSimpleButton("Enlarge camera", Enlarge);
            var qmShrinkButton = ExpansionKitApi.GetExpandedMenu(ExpandedMenu.CameraQuickMenu).AddSimpleButton("Shrink camera", Shrink);
            qmGrabbableButton = ExpansionKitApi.GetExpandedMenu(ExpandedMenu.CameraQuickMenu).AddToggleButton("Grabbable", SetCameraGrabbable, () => true);
            qmUiVisibleButton = ExpansionKitApi.GetExpandedMenu(ExpandedMenu.CameraQuickMenu).AddToggleButton("UI visible", SetUiVisible, () => true);

            void UpdateButtonVisibility(bool value)
            {
                cameraEnlargeButton.SetVisible(value);
                cameraShrinkButton.SetVisible(value);
                cameraGrabbableButton.SetVisible(value);
                cameraUiVisibleButton.SetVisible(value);
                qmEnlargeButton.SetVisible(!value);
                qmShrinkButton.SetVisible(!value);
                qmGrabbableButton.SetVisible(!value);
                qmUiVisibleButton.SetVisible(!value);
            }

            myUseCameraExpando.OnValueChanged += (_, value) =>
            {
                UpdateButtonVisibility(value);
            };
            
            UpdateButtonVisibility(myUseCameraExpando.Value);

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