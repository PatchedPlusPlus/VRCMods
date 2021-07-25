using System;
using System.Collections;
using MelonLoader;
using UIExpansionKit.API;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using VRC.UI;

namespace AdvancedSafety
{
    public static class UiExpansionKitSupport
    {
        private static Text ourBigMenuHideText;
        private static Text ourQuickMenuHideText;
        private static PageUserInfo ourUserInfoPage;

        public static void OnApplicationStart()
        {
            ClassInjector.RegisterTypeInIl2Cpp<QuickMenuHideAvatarButtonHandler>();
            
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserDetailsMenu).AddSimpleButton("Hide all avatars by this author", OnHideBigClick, ConsumeHideBigInstance);
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.UserQuickMenu).AddSimpleButton("Hide this avatar (on anyone)", OnHideAvatarClick, ConsumeOnHideAvatar);
            
            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.SettingsMenu).AddSimpleButton("Reload all avatars", ReloadAllAvatars);

            MelonCoroutines.Start(InitThings());
        }

        private static void OnHideAvatarClick()
        {
            var apiAvatar = AdvancedSafetyMod.GetQuickMenu().field_Private_Player_0?.prop_VRCPlayer_0?.prop_VRCAvatarManager_0?.field_Private_ApiAvatar_1;
            if (apiAvatar == null) return;

            if (AvatarHiding.ourBlockedAvatars.ContainsKey(apiAvatar.id))
                AvatarHiding.ourBlockedAvatars.Remove(apiAvatar.id);
            else
                AvatarHiding.ourBlockedAvatars[apiAvatar.id] = apiAvatar.name;
            
            AvatarHiding.SaveBlockedAvatars();
            
            ReloadAllAvatars();
        }

        private static void ConsumeOnHideAvatar(GameObject obj)
        {
            ourQuickMenuHideText = obj.GetComponentInChildren<Text>();
            obj.AddComponent<QuickMenuHideAvatarButtonHandler>();
        }

        private static void OnHideBigClick()
        {
            var apiUser = ourUserInfoPage?.field_Public_APIUser_0;
            var userId = apiUser?.id;
            if (userId == null) return;
            
            if (AvatarHiding.ourBlockedAvatarAuthors.ContainsKey(userId))
                AvatarHiding.ourBlockedAvatarAuthors.Remove(userId);
            else
                AvatarHiding.ourBlockedAvatarAuthors[userId] = apiUser.displayName ?? "";

            AvatarHiding.SaveBlockedAuthors();

            OnPageShown(ourUserInfoPage);
            
            ReloadAllAvatars();
        }

        private static IEnumerator InitThings()
        {
            while (AdvancedSafetyMod.GetUiManager() == null || AdvancedSafetyMod.GetQuickMenu() == null)
                yield return null;

            AdvancedSafetyMod.GetUiManager().Method_Internal_add_Void_Action_1_VRCUiPage_0(new Action<VRCUiPage>(OnPageShown));
        }

        private static void OnPageShown(VRCUiPage obj)
        {
            var userPage = obj.TryCast<PageUserInfo>();
            if (userPage == null) return;
            ourUserInfoPage = userPage;

            MelonCoroutines.Start(UpdateUserFromPage(userPage));
        }

        private static IEnumerator UpdateUserFromPage(PageUserInfo pageUserInfo)
        {
            yield return new WaitForSeconds(.5f);

            ourBigMenuHideText.text = AvatarHiding.ourBlockedAvatarAuthors.ContainsKey(pageUserInfo.field_Public_APIUser_0?.id ?? "") ? "Unhide all avatars by this author" : "Hide all avatars by this author";
        }

        private static void ConsumeHideBigInstance(GameObject obj) => ourBigMenuHideText = obj.GetComponentInChildren<Text>();

        public static void QuickMenuUpdateTick(VRCPlayer player)
        {
            var currentAvatar = player.prop_VRCAvatarManager_0?.field_Private_ApiAvatar_1;
            if (currentAvatar == null) return;

            ourQuickMenuHideText.text = AvatarHiding.ourBlockedAvatars.ContainsKey(currentAvatar.id)
                ? "Unhide this avatar (on anyone)"
                : "Hide this avatar (on anyone)";
        }
        
        private static void ReloadAllAvatars() {
            var vrcPlayer = VRCPlayer.field_Internal_Static_VRCPlayer_0;
            vrcPlayer.StartCoroutine(vrcPlayer.Method_Private_IEnumerator_Boolean_PDM_0(false));
        }
    }
}