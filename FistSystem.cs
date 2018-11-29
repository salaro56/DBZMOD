﻿using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.GameInput;
using DBZMOD.UI;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using Terraria.Graphics;
using Microsoft.Xna.Framework;
using DBZMOD.Projectiles;
using Terraria.ModLoader.IO;
using Terraria.ID;
using DBZMOD;

namespace DBZMOD
{
    class FistSystem
    {
        #region Variables
        private TriggersPack triggersPack;
        private bool IsDashLeftJustPressed;
        private bool IsDashLeftGapped;
        private bool IsDashRightJustPressed;
        private bool IsDashRightGapped;
        private bool IsDashUpJustPressed;
        private bool IsDashUpGapped;
        private bool IsDashDownJustPressed;
        private bool IsDashDownGapped;
        private bool IsDashDiagonalUpHeld;
        private bool IsDashDiagonalDownHeld;
        private int DashTimer;
        private int HoldTimer;
        private int FlurryTimer;
        private int BlockTimer;
        private bool LightPunchPressed;
        private bool LightPunchHeld;
        public bool EyeDowned;
        public bool BeeDowned;
        public bool WallDowned;
        public bool PlantDowned;
        public bool DukeDowned;
        public bool MoonlordDowned;
        private int BasicPunchDamage;
        private int HeavyPunchDamage;
        private int FlurryPunchDamage;
        private int ShootSpeed;
        #endregion

        #region Enums
        // these are to coordinate with inputs in an array
        public enum BaseDashDirections
        {
            None = 0,
            Up = 1,
            Down = 2,
            Left = 3,
            Right = 4
        };

        // enum to handle dash direction states
        [Flags]
        public enum DashDirectionStates : short
        {
            None = 0,
            Up = 1,
            Down = 1 << 1,
            Left = 1 << 2,
            Right = 1 << 3,
            UpLeft = Up & Left,
            UpRight = Up & Right,
            DownLeft = Down & Left,
            DownRight = Down & Right
        };

        [Flags]
        public enum ControlStates : short
        {
            Released = 0,
            PressedOnce = 1,
            PressedAndReleased = 1 << 1,
            PressedTwice = 1 << 2,
            PressedAndHeld = 1 << 3
        }
        #endregion

        #region ControlStateHandler
        // scrape trigger states and return new state based on previous state.
        public DashDirectionStates GetInputState(TriggersSet inputState)
        {
            return DashDirectionStates.None;
        }
        #endregion

        public void Update(TriggersSet triggersSet, Player player, Mod mod)
        {
            Vector2 projvelocity = Vector2.Normalize(Main.MouseWorld - player.position) * ShootSpeed;

            #region Mouse Clicks
            if (triggersSet.MouseLeft)
            {
                HoldTimer++;
                if (HoldTimer > 60)
                {
                    LightPunchHeld = true;
                    LightPunchPressed = false;
                    if (LightPunchHeld && MyPlayer.ModPlayer(player).CanUseFlurry)
                    {
                        FlurryTimer++;

                    }

                }
                if (!LightPunchPressed && !LightPunchHeld)
                {
                    LightPunchPressed = true;
                    ShootSpeed = 2;
                    Projectile.NewProjectile(player.position, projvelocity, BasicFistProjSelect(mod), BasicPunchDamage, 5);
                }

            }
            if (triggersSet.MouseRight) //Right click
            {
                if (!player.HasBuff(mod.BuffType("HeavyPunchCooldown")) && MyPlayer.ModPlayer(player).CanUseHeavyHit)
                {
                    Projectile.NewProjectile(player.position, projvelocity, mod.ProjectileType("KiFistProjHeavy"), HeavyPunchDamage, 50);
                }
            }
            if (triggersSet.MouseRight && triggersSet.MouseLeft)//both click, for blocking
            {
                BlockTimer++;
                if (BlockTimer < 30)
                {
                    MyPlayer.ModPlayer(player).BlockState = 1;
                    if (BlockTimer > 30 && BlockTimer < 120)
                    {
                        MyPlayer.ModPlayer(player).BlockState = 2;
                        if (BlockTimer > 120)
                        {
                            MyPlayer.ModPlayer(player).BlockState = 3;
                        }
                    }
                }
            }
            else
            {
                MyPlayer.ModPlayer(player).BlockState = 0;
                LightPunchPressed = false;
                LightPunchHeld = false;
            }
            #endregion

            #region Dash Checks
            // check initial left input for dash
            if (triggersSet.Left && !IsDashLeftJustPressed)
            {
                IsDashLeftJustPressed = true;
                DashTimer = 0;
            }

            // same for right
            if (triggersSet.Right && !IsDashRightJustPressed)
            {
                IsDashRightJustPressed = true;
                DashTimer = 0;
            }

            // same for up

            // same for down

            // check for control release, while the flags set above are true.
            if (!triggersSet.Left && IsDashLeftJustPressed)
            {
                IsDashLeftGapped = true;
            }

            // same for right
            if (!triggersSet.Right && IsDashRightJustPressed)
            {
                IsDashRightGapped = true;
            }

            if (triggersSet.Left && IsDashLeftJustPressed && IsDashLeftGapped)
            {
                IsDashLeftGapped = false;
                IsDashLeftJustPressed = false;
                MyPlayer.ModPlayer(player).IsDashing = true;
                //do dash left
            }

            if (triggersSet.Right && IsDashRightJustPressed && IsDashRightGapped)
            {
                IsDashRightGapped = false;
                IsDashRightJustPressed = false;
                MyPlayer.ModPlayer(player).IsDashing = true;
                //do dash right
            }

            if (IsDashLeftJustPressed)
            {
                DashTimer++;
                if (DashTimer > 15)
                {
                    IsDashLeftJustPressed = false;
                    DashTimer = 0;
                }
            }
            #endregion

            #region boss downed bools
            if (NPC.downedBoss1)
            {
                EyeDowned = true;
            }
            if (NPC.downedQueenBee)
            {
                BeeDowned = true;
            }
            if (Main.hardMode)
            {
                WallDowned = true;
            }
            if (NPC.downedPlantBoss)
            {
                PlantDowned = true;
            }
            if (NPC.downedFishron)
            {
                DukeDowned = true;
            }
            if (NPC.downedMoonlord)
            {
                MoonlordDowned = true;
            }
            #endregion

            #region Stat Checks
            BasicPunchDamage = 8;
            HeavyPunchDamage = BasicPunchDamage * 3;
            FlurryPunchDamage = BasicPunchDamage / 2;
            if (EyeDowned)
            {
                BasicPunchDamage += 6;
            }
            if (BeeDowned)
            {
                BasicPunchDamage += 8;
            }
            if (WallDowned)
            {
                BasicPunchDamage += 26;
            }
            if (PlantDowned)
            {
                BasicPunchDamage += 32;
            }
            if (DukeDowned)
            {
                BasicPunchDamage += 28;
            }
            if (MoonlordDowned)
            {
                BasicPunchDamage += 124;
            }

            #endregion

        }
        public int BasicFistProjSelect(Mod mod)
        {
            switch (Main.rand.Next((4)))
            {
                case 0:
                    return mod.ProjectileType("KiFistProj1");
                case 1:
                    return mod.ProjectileType("KiFistProj2");
                case 2:
                    return mod.ProjectileType("KiFistProj3");
                case 3:
                    return mod.ProjectileType("KiFistProj4");
                default:
                    return 0;

            }

        }
    }
}
