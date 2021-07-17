using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using IKTweaks;
using MelonLoader;
using RootMotionNew.FinalIK;
using UIExpansionKit.API;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using Delegate = Il2CppSystem.Delegate;
using Object = UnityEngine.Object;

[assembly:MelonInfo(typeof(IKTweaksMod), "IKTweaks", "1.0.14", "knah", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]
[assembly:MelonOptionalDependencies("UIExpansionKit")]

namespace IKTweaks
{
    internal partial class IKTweaksMod : MelonMod
    {
        private static readonly Queue<Action> ourToMainThreadQueue = new Queue<Action>();
        private static readonly Queue<Action> ourToIKLateUpdateQueue = new Queue<Action>();
        private static Func<VRCAvatarManager, float> ourGetEyeHeightDelegate;
        
        internal static GameObject ourRandomPuck;

        public override void OnApplicationStart()
        {
            IkTweaksSettings.RegisterSettings();

            BundleHolder.Init();

            ClassInjector.RegisterTypeInIl2Cpp<VRIK_New>();
            ClassInjector.RegisterTypeInIl2Cpp<TwistRelaxer_New>();

            VrIkHandling.HookVrIkInit(HarmonyInstance);
            FullBodyHandling.HookFullBodyController(HarmonyInstance);
            
            Camera.onPreRender = Delegate.Combine(Camera.onPreRender, (Camera.CameraCallback) OnVeryLateUpdate).Cast<Camera.CameraCallback>();
            
            DoAfterUiManagerInit(OnUiManagerInit);

            ourGetEyeHeightDelegate = (Func<VRCAvatarManager, float>) System.Delegate.CreateDelegate(typeof(Func<VRCAvatarManager, float>), typeof(VRCAvatarManager)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single(it =>
                    it.GetParameters().Length == 0 && it.ReturnType == typeof(float) && !it.Name.Contains("_PDM") &&
                    XrefScanner.XrefScan(it).Any(jt =>
                        jt.Type == XrefType.Global &&
                        jt.ReadAsObject()?.ToString() == "Asked for eye height before measured.")));

            HarmonyInstance.Patch(typeof(VRCAvatarManager)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Single(it =>
                    it.GetParameters().Length == 0 && it.ReturnType == typeof(float) && !it.Name.Contains("_PDM") &&
                    XrefScanner.XrefScan(it).Any(jt =>
                        jt.Type == XrefType.Global &&
                        jt.ReadAsObject()?.ToString() == "Asked for arm length before measured.")),
                new HarmonyMethod(typeof(IKTweaksMod), nameof(WingspanPatch)));
            
            if (MelonHandler.Mods.Any(it => it.Info.Name == "UI Expansion Kit" && !it.Info.Version.StartsWith("0.1."))) 
                AddUixActions();
        }

        private static bool WingspanPatch(VRCAvatarManager __instance, ref float __result)
        {
            switch (IkTweaksSettings.MeasureModeParsed)
            {
                case MeasureAvatarMode.Default:
                    return true;
                case MeasureAvatarMode.Height:
                    __result = ourGetEyeHeightDelegate(__instance) * 0.4537f;
                    return false;
                case MeasureAvatarMode.ImprovedWingspan:
                {
                    var avatarRoot = __instance.transform.Find("Avatar");
                    if (avatarRoot == null) return true;
                    var animator = avatarRoot.GetComponent<Animator>();
                    if (animator == null || !animator.isHuman) return true;
                    var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                    var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                    var leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                    var rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                    var leftElbow = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                    var rightElbow = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);

                    if (leftHand == null || rightHand == null || leftUpperArm == null || rightUpperArm == null ||
                        leftElbow == null || rightElbow == null)
                        return true;

                    var leftElbowPos = leftElbow.position;
                    var rightElbowPos = rightElbow.position;
                    var leftShoulderPos = leftUpperArm.position;
                    var rightShoulderPos = rightUpperArm.position;
                    var measuredRawWingspan = Vector3.Distance(leftHand.position, leftElbowPos) +
                                              Vector3.Distance(rightHand.position, rightElbowPos) +
                                              Vector3.Distance(leftElbowPos, leftShoulderPos) +
                                              Vector3.Distance(rightElbowPos, rightShoulderPos) +
                                              Vector3.Distance(leftShoulderPos, rightShoulderPos);

                    // this measured wingspan doesn't include hand-to-fingertip length, but eye height doesn't include the rest of the height above eyes either
                    __result = measuredRawWingspan * 0.4537f * IkTweaksSettings.WingspanMeasurementAdjustFactor.Value;
                    return false;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddUixActions()
        {
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.SettingsMenu).AddSimpleButton("More IKTweaks...", ShowIKTweaksMenu);

            ExpansionKitApi.RegisterSettingAsStringEnum(IkTweaksSettings.IkTweaksCategory,
                nameof(IkTweaksSettings.IgnoreAnimationsMode),
                new[]
                {
                    (nameof(IgnoreAnimationsMode.None), "Play all animations"),
                    (nameof(IgnoreAnimationsMode.Head), "Ignore head animations"),
                    (nameof(IgnoreAnimationsMode.Hands), "Ignore hands animations"),
                    (nameof(IgnoreAnimationsMode.HandAndHead), "Ignore head and hands"),
                    (nameof(IgnoreAnimationsMode.All), "Ignore all (always slide around)")
                });

            ExpansionKitApi.RegisterSettingAsStringEnum(IkTweaksSettings.IkTweaksCategory, nameof(IkTweaksSettings.MeasureMode), new[]
            {
                (nameof(MeasureAvatarMode.Default), "VRC default"),
                (nameof(MeasureAvatarMode.ImprovedWingspan), "Wingspan (accurate)"),
                (nameof(MeasureAvatarMode.Height), "Height"),
            });
        }

        private static void ShowIKTweaksMenu()
        {
            var menu = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);

            menu.AddSimpleButton("Clear per-avatar stored calibrations", CalibrationManager.ClearNonUniversal);
            menu.AddSpacer();
            menu.AddSpacer();
            menu.AddSimpleButton("Open documentation in browser", () => Process.Start("https://github.com/knah/VRCMods#iktweaks"));
            
            menu.AddSpacer();
            menu.AddSpacer();
            menu.AddSpacer();
            menu.AddSimpleButton("Close", menu.Hide);
            
            menu.Show();
        }

        public void OnUiManagerInit()
        {
            var calibrateButton = GameObject.Find("UserInterface/QuickMenu/ShortcutMenu/CalibrateButton")
                .GetComponent<Button>();
            var oldCalibrate = calibrateButton.onClick;
            calibrateButton.onClick = new Button.ButtonClickedEvent();
            calibrateButton.onClick.AddListener(new Action(() =>
            {
                if (IkTweaksSettings.FullBodyVrIk.Value)
                {
                    if (IkTweaksSettings.CalibrateUseUniversal.Value)
                        CalibrationManager.Clear();
                    else
                        CalibrationManager.Clear(VRCPlayer.field_Internal_Static_VRCPlayer_0.prop_ApiAvatar_0.id);
                }

                oldCalibrate.Invoke();
            }));

            var steamVrControllerManager = CalibrationManager.GetControllerManager();
            var puckPrefab = steamVrControllerManager.field_Public_ArrayOf_GameObject_0.First(it =>
                it != steamVrControllerManager.field_Public_GameObject_1 && it != steamVrControllerManager.field_Public_GameObject_0);
            var newPucks = new Il2CppReferenceArray<GameObject>(5 + 6);
            var newUints = new Il2CppStructArray<uint>(5 + 6);
            for (var i = 0; i < 5; i++)
            {
                newPucks[i] = steamVrControllerManager.field_Public_ArrayOf_GameObject_0[i];
                newUints[i] = steamVrControllerManager.field_Private_ArrayOf_UInt32_0[i];
            }

            ourRandomPuck = puckPrefab;

            var trackersParent = puckPrefab.transform.parent;
            for (var i = 0; i < 6; i++)
            {
                var newPuck = Object.Instantiate(puckPrefab, trackersParent, true);
                newPuck.name = "Puck" + (i + 4);
                newPuck.GetComponent<SteamVR_TrackedObject>().field_Public_EnumNPublicSealedvaNoHmDe18DeDeDeDeDeUnique_0 = SteamVR_TrackedObject.EnumNPublicSealedvaNoHmDe18DeDeDeDeDeUnique.None;
                newPuck.SetActive(false);
                newPucks[i + 5] = newPuck;
                newUints[i + 5] = UInt32.MaxValue;
            }

            steamVrControllerManager.field_Public_ArrayOf_GameObject_0 = newPucks;
            steamVrControllerManager.field_Private_ArrayOf_UInt32_0 = newUints;

            // only one of them is the correct type, so just try all of them 
            steamVrControllerManager.field_Private_Action_0.TryCast<SteamVR_Events.Action<VREvent_t>>()?.action?.Invoke(new VREvent_t());
            steamVrControllerManager.field_Private_Action_1.TryCast<SteamVR_Events.Action<VREvent_t>>()?.action?.Invoke(new VREvent_t());
            steamVrControllerManager.field_Private_Action_2.TryCast<SteamVR_Events.Action<VREvent_t>>()?.action?.Invoke(new VREvent_t());
        }

        private static bool ourHadUpdateThisFrame = false;
        public override void OnUpdate()
        {
            VrIkHandling.Update();
            ourHadUpdateThisFrame = false;
        }

        public void OnVeryLateUpdate(Camera _)
        {
            if (ourHadUpdateThisFrame) return;
            
            ourHadUpdateThisFrame = true;

            ProcessQueue(ourToMainThreadQueue);
        }

        internal static void ProcessIKLateUpdateQueue()
        {
            ProcessQueue(ourToIKLateUpdateQueue);
        }

        private static void ProcessQueue(Queue<Action> queue)
        {
            var toRun = queue.ToList();
            queue.Clear();

            foreach (var action in toRun)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    MelonLogger.Error(ex.ToString());
                }
            }
        }

        public static YieldVeryLateUpdateAwaitable AwaitVeryLateUpdate() => new(ourToMainThreadQueue);
        public static YieldVeryLateUpdateAwaitable AwaitIKLateUpdate() => new(ourToIKLateUpdateQueue);

        public struct YieldVeryLateUpdateAwaitable : INotifyCompletion
        {
            private readonly Queue<Action> myQueue;

            public YieldVeryLateUpdateAwaitable(Queue<Action> queue)
            {
                myQueue = queue;
            }

            public bool IsCompleted => false;

            public YieldVeryLateUpdateAwaitable GetAwaiter() => this;

            public void GetResult() { }

            public void OnCompleted(Action continuation)
            {
                myQueue.Enqueue(continuation);
            }
        }
    }
}