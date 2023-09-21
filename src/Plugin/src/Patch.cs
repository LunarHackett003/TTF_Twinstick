using BepInEx;
using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace H3VRMod
{
    public class Patch
    {
        public static bool isTitanSwinger = false;
        public static bool canDoubleJump = true;
        public static bool isWallRunning = false;
        public static float maxWallRunTime = 3f;
        public static float currentWallRunTime = 0f;

        [HarmonyPatch(typeof(FVRMovementManager)), HarmonyPrefix]
        public static void Patch_FixedUpdate(FVRMovementManager _instance)
        {
            #region BaseFixedUpdate
            if (_instance.m_teleportCooldown > 0f)
            {
                _instance.m_teleportCooldown -= Time.deltaTime;
            }
            if (_instance.m_teleportEnergy < 1f)
            {
                _instance.m_teleportEnergy += Time.deltaTime;
            }
            else
            {
                _instance.m_teleportEnergy = 1f;
            }
            if (_instance.m_teleportEnergy < 0f)
            {
                _instance.m_teleportEnergy = 0f;
            }
            if (!GM.CurrentSceneSettings.DoesTeleportUseCooldown)
            {
                _instance.m_teleportEnergy = 1f;
            }
            if (_instance.executeFrameTick < 2)
            {
                _instance.executeFrameTick++;
                _instance.FU();
            }
            #endregion BaseFixedUpdate
            FVRViveHand leftHand = _instance.LeftHand.GetComponent<FVRViveHand>();
            if (!_instance.m_isGrounded && !leftHand.Input.IsGrabbing)
            {
                bool handsHit = false;
                Ray forwardCheck = new Ray(leftHand.transform.position, leftHand.transform.forward);
                List<RaycastHit> hits = new(Physics.RaycastAll(forwardCheck, 0.4f));
                forwardCheck.direction = forwardCheck.direction * -1;
                hits.AddRange(Physics.RaycastAll(forwardCheck, 0.4f));
            }
        }

        [HarmonyPatch(typeof(FVRMovementManager), "UpdateMovementWithHand")]
        [HarmonyPrefix]
        public static bool Patch_HandMovementUpdate(FVRMovementManager _instance, ref FVRViveHand hand)
        {
            if(_instance.Mode == FVRMovementManager.MovementMode.Dash)
            {
                //Well done! You're using Dash mode, I hope you feel good about yourself.
                _instance.HandUpdateTwinstick(hand);
                return false;
            }
            return true;
        }

        public class FUState
        {
            public bool isTitanSwinger;
            public int BaseSpeed;
        }
        [HarmonyPatch(typeof(FVRMovementManager), "FU"), HarmonyPrefix]
        public static bool Patch_MovememntMathUpdate(FVRMovementManager _instance, out FUState state)
        {
            state = new FUState();
            if (_instance.Mode == FVRMovementManager.MovementMode.Dash)
            {
                var airControl = GM.CurrentSceneSettings.DoesAllowAirControl;
                GM.CurrentSceneSettings.DoesAllowAirControl = true;
                canDoubleJump = _instance.m_isGrounded;

                //Alright, lets get this bread. You're using your JumpKit.
                var playerbody = GM.CurrentPlayerBody;
                _instance.Mode = FVRMovementManager.MovementMode.TwinStick;
                _instance.UpdateSmoothLocomotion();

                var gravmode = GM.Options.SimulationOptions.PlayerGravityMode;
                if (isWallRunning)
                {
                    GM.Options.SimulationOptions.PlayerGravityMode = SimulationOptions.GravityMode.OnTheMoon;
                }
                else
                {
                    GM.Options.SimulationOptions.PlayerGravityMode = gravmode;
                }
            }
            else
            {
                //You've been demoted to a rifleman.
                state.isTitanSwinger = false;
                isTitanSwinger = false;
            }


            return true;
        }

        [HarmonyPatch(typeof(FVRMovementManager), "Jump")]
        public static bool Patch_DoubleJump(FVRMovementManager _instance)
        {
            if ((_instance.Mode == FVRMovementManager.MovementMode.Armswinger
                || _instance.Mode == FVRMovementManager.MovementMode.SingleTwoAxis
                || _instance.Mode == FVRMovementManager.MovementMode.TwinStick) &&
                !_instance.m_isGrounded && !canDoubleJump) { 
                //You are not grounded, and have already doublejumped since leaving the ground.
                return false;
            }
                //Do this, idk what it does lmao
                _instance.DelayGround(0.1f);
            float jumpVel = 0f;
            switch (GM.Options.SimulationOptions.PlayerGravityMode)
            {
                case SimulationOptions.GravityMode.Realistic:
                    jumpVel = 9f;
                    break;
                case SimulationOptions.GravityMode.Playful:
                    jumpVel = 5.5f;    
                   break;
                case SimulationOptions.GravityMode.OnTheMoon:
                    jumpVel = 3.75f;
                    break;
                case SimulationOptions.GravityMode.None:
                    jumpVel = 0.001f;
                    break;
            }
            jumpVel *= 0.8f;

            if(_instance.Mode == FVRMovementManager.MovementMode.Armswinger
                || _instance.Mode == FVRMovementManager.MovementMode.SingleTwoAxis
                || _instance.Mode == FVRMovementManager.MovementMode.TwinStick)
            {
                _instance.DelayGround(0.25f);
                _instance.m_smoothLocoVelocity.y = Mathf.Clamp(_instance.m_smoothLocoVelocity.y, 0, _instance.m_smoothLocoVelocity.y);
                _instance.m_smoothLocoVelocity.y = jumpVel;
                _instance.m_isGrounded = false;
            }
            canDoubleJump = false;
            return false;
        }
    }
}