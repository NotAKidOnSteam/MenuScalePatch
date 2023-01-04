﻿using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using HarmonyLib;
using NAK.Melons.MenuScalePatch.Helpers;
using UnityEngine;

namespace NAK.Melons.MenuScalePatch.HarmonyPatches;

/**
    ViewManager.SetScale runs once a second when it should only run when aspect ratio changes- CVR bug
    assuming its caused by cast from int to float getting the screen size, something floating point bleh
    
    ViewManager.UpdatePosition & CVR_MenuManager.UpdatePosition are called every second in a scheduled job.
    (its why ViewManager.SetScale is called, because MM uses aspect ratio in scale calculation)

    I nuke those methods. Fuck them. I cannot disable the jobs though...
**/

[HarmonyPatch]
internal class HarmonyPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVR_MenuManager), "SetScale")]
    private static void Prefix_CVR_MenuManager_SetScale(float avatarHeight, ref float ____scaleFactor, out bool __runOriginal)
    {
        ____scaleFactor = avatarHeight / 1.8f;
        if (MetaPort.Instance.isUsingVr) ____scaleFactor *= 0.5f;
        MSP_MenuInfo.ScaleFactor = ____scaleFactor;
        __runOriginal = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ViewManager), "SetScale")]
    private static void Prefix_ViewManager_SetScale(out bool __runOriginal)
    {
        //bitch
        __runOriginal = false;
    }

    //nuke UpdateMenuPosition methods
    //there are 2 Jobs calling this each second, which is fucking my shit
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVR_MenuManager), "UpdateMenuPosition")]
    private static void Prefix_CVR_MenuManager_UpdateMenuPosition(out bool __runOriginal)
    {
        //fuck u
        __runOriginal = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ViewManager), "UpdateMenuPosition")]
    private static void Prefix_ViewManager_UpdateMenuPosition(ref float ___cachedScreenAspectRatio, out bool __runOriginal)
    {
        //this is called once a second, so ill fix their dumb aspect ratio shit
        float ratio = (float)Screen.width / (float)Screen.height;
        float clamp = Mathf.Clamp(ratio, 0f, 1.8f);
        MSP_MenuInfo.AspectRatio = 1.7777779f / clamp;
        __runOriginal = false;
    }

    //Set QM stuff
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CVR_MenuManager), "Start")]
    private static void Postfix_CVR_MenuManager_Start(ref CVR_MenuManager __instance, ref GameObject ____leftVrAnchor)
    {
        QuickMenuHelper helper = __instance.quickMenu.gameObject.AddComponent<QuickMenuHelper>();
        helper.handAnchor = ____leftVrAnchor.transform;
    }

    //Set MM stuff
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ViewManager), "Start")]
    private static void Postfix_ViewManager_Start(ref ViewManager __instance)
    {
        __instance.gameObject.AddComponent<MainMenuHelper>();
    }

    //hook quickmenu open/close
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CVR_MenuManager), "ToggleQuickMenu", new Type[] { typeof(bool) })]
    private static void Prefix_CVR_MenuManager_ToggleQuickMenu(bool show, ref bool ____quickMenuOpen)
    {
        if (QuickMenuHelper.Instance == null) return;
        if (show != ____quickMenuOpen)
        {
            QuickMenuHelper.Instance.UpdateWorldAnchors();
            MSP_MenuInfo.ToggleDesktopInputMethod(show);
        }
        QuickMenuHelper.Instance.enabled = show;
    }

    //hook menu open/close
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ViewManager), "UiStateToggle", new Type[] { typeof(bool) })]
    private static void Postfix_ViewManager_UiStateToggle(bool show, ref bool ____gameMenuOpen)
    {
        if (MainMenuHelper.Instance == null) return;
        if (show != ____gameMenuOpen)
        {
            MainMenuHelper.Instance.UpdateWorldAnchors();
            MSP_MenuInfo.ToggleDesktopInputMethod(show);
        }
        MainMenuHelper.Instance.enabled = show;
    }

    //add independent head movement to important input
    [HarmonyPostfix]
    [HarmonyPatch(typeof(InputModuleMouseKeyboard), "UpdateImportantInput")]
    private static void Postfix_InputModuleMouseKeyboard_UpdateImportantInput(ref CVRInputManager ____inputManager)
    {
        ____inputManager.independentHeadTurn |= Input.GetKey(KeyCode.LeftAlt);
    }

    //Support for changing VRMode during runtime.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerSetup), "CalibrateAvatar")]
    private static void Postfix_PlayerSetup_CalibrateAvatar()
    {
        MSP_MenuInfo.CameraTransform = PlayerSetup.Instance.GetActiveCamera().transform;
    }
}