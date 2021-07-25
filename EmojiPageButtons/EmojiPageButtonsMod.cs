using System.Collections;
using EmojiPageButtons;
using MelonLoader;
using UIExpansionKit.API;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using System;
using System.Linq;
using System.Reflection;


[assembly:MelonInfo(typeof(EmojiPageButtonsMod), "Emoji Page Buttons", "1.0.3", "knah, PatchedPlus+", "https://github.com/knah/VRCMods")]
[assembly:MelonGame("VRChat", "VRChat")]

namespace EmojiPageButtons
{
    internal class EmojiPageButtonsMod : MelonMod
    {


        private static Func<VRCUiManager> ourGetUiManager;
        private static Func<QuickMenu> ourGetQuickMenu;

        static EmojiPageButtonsMod()
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
            ExpansionKitApi.RegisterWaitConditionBeforeDecorating(WaitAndRegisterEmojiButtons());
        }

        private IEnumerator WaitAndRegisterEmojiButtons()
        {
            while (GetQuickMenu() == null)
                yield return null;

            var emojiMenuRoot = GetQuickMenu().transform.Find("EmojiMenu");
            if (emojiMenuRoot == null)
            {
                MelonLogger.Error("Emoji menu root not found");
                yield break;
            }

            var emojiMenu = emojiMenuRoot.GetComponent<EmojiMenu>();

            var storeGo = new GameObject("ClonedPageStore");
            storeGo.transform.SetParent(emojiMenu.transform);
            storeGo.SetActive(false);

            for (var index = 0; index < emojiMenu.field_Public_List_1_GameObject_0.Count; index++)
            {
                var pageGo = emojiMenu.field_Public_List_1_GameObject_0[index];

                var clone = new GameObject($"Page{index}Button", new []{Il2CppType.Of<RectTransform>()});
                clone.transform.SetParent(storeGo.transform, false);
                var grid = clone.AddComponent<GridLayoutGroup>();
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.cellSize = new Vector2(33, 33);
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.constraintCount = 3;

                foreach (var buttonXformObject in pageGo.transform)
                {
                    var buttonTransform = buttonXformObject.Cast<Transform>();
                    if (!buttonTransform.gameObject.activeSelf) continue;

                    var buttonClone = Object.Instantiate(buttonTransform.gameObject, clone.transform, false);
                    CleanStuff(buttonClone);
                }

                var index1 = index;
                ExpansionKitApi.GetExpandedMenu(ExpandedMenu.EmojiQuickMenu).AddSimpleButton("", () =>
                {
                    emojiMenu.field_Public_List_1_GameObject_0[emojiMenu.field_Private_Int32_0].SetActive(false);
                    pageGo.SetActive(true);
                    emojiMenu.field_Private_Int32_0 = index1;
                }, buttonGo =>
                {
                    Object.Instantiate(clone, buttonGo.transform, false);
                });
            }
        }

        private void CleanStuff(GameObject obj)
        {
            var compos = obj.GetComponents<Component>();
            foreach (var component in compos)
            {
                if (component.TryCast<Image>() != null || component.TryCast<Text>() != null)
                    continue;

                if (component.TryCast<Button>() != null || component.TryCast<MonoBehaviour>() != null)
                    Object.Destroy(component);
            }

            foreach (var o in obj.transform)
                CleanStuff(o.Cast<Transform>().gameObject);
        }
    }
}