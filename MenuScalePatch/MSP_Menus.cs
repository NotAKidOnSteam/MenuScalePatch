﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core;
using ABI_RC.Systems.MovementSystem;
using HarmonyLib;

namespace NAK.Melons.MenuScalePatch.Helpers;

public class MSP_MenuInfo
{
    //Shared Info
    public static float ScaleFactor = 1f;
    public static float AspectRatio = 1f;
    public static Transform CameraTransform;

    //Settings...?
    public static bool WorldAnchorQM;

    //if other mods need to disable?
    public static bool DisableQMHelper;
    public static bool DisableQMHelper_VR;
    public static bool DisableMMHelper;
    public static bool DisableMMHelper_VR;

    public static void ToggleDesktopInputMethod(bool flag)
    {
        if (MetaPort.Instance.isUsingVr) return;
        PlayerSetup.Instance._movementSystem.disableCameraControl = flag;
        CVRInputManager.Instance.inputEnabled = !flag;
        RootLogic.Instance.ToggleMouse(flag);
        CVR_MenuManager.Instance.desktopControllerRay.enabled = !flag;
        Traverse.Create(CVR_MenuManager.Instance).Field("_desktopMouseMode").SetValue(flag);
    }

    static readonly FieldInfo ms_followAngleY = typeof(MovementSystem).GetField("_followAngleY", BindingFlags.NonPublic | BindingFlags.Instance);
    public static bool independentHeadTurn = false;

    public static void HandleIndependentLookInput()
    {
        //angle of independent look axis
        float angle = (float)ms_followAngleY.GetValue(MovementSystem.Instance);
        bool isPressed = CVRInputManager.Instance.independentHeadTurn;
        if (isPressed && !independentHeadTurn)
        {
            independentHeadTurn = true;
            MSP_MenuInfo.ToggleDesktopInputMethod(false);
        }else if (!isPressed && independentHeadTurn && angle == 0f)
        {
            independentHeadTurn = false;
            MSP_MenuInfo.ToggleDesktopInputMethod(true);
        }
    }
}