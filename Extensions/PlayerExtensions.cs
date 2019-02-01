﻿using System;
using System.Collections.Generic;
using System.Linq;
using DBZMOD.Effects.Animations.Aura;
using DBZMOD.Enums;
using DBZMOD.Items.Consumables.Potions;
using DBZMOD.Items.DragonBalls;
using DBZMOD.Models;
using DBZMOD.Network;
using DBZMOD;
using DBZMOD.Projectiles;
using DBZMOD.Transformations;
using DBZMOD.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using BuffInfoExtensions = DBZMOD.Extensions.BuffInfoExtensions;
using PlayerExtensions = DBZMOD.Extensions.PlayerExtensions;

namespace DBZMOD.Extensions
{
    /// <summary>
    ///     A class housing all the player/ModPlayer/MyPlayer based extensions
    /// </summary>
    public static class PlayerExtensions
    {
        /// <summary>
        ///     checks if the player has a vanilla item equipped in a non-vanity slot.
        /// </summary>
        /// <param name="player">The player being checked.</param>
        /// <param name="itemName">The name of the item to check for.</param>
        /// <returns></returns>
        public static bool IsAccessoryEquipped(this Player player, string itemName)
        {
            // switched to using an index so it's easier to detect vanity slots.
            for (int i = 3; i < 8 + player.extraAccessorySlots; i++)
            {
                if (player.armor[i].IsItemNamed(itemName))
                    return true;
            }
            return false;
        }

        /// <summary>
        ///     Find a single ki potion (first found) and consume it.
        /// </summary>
        /// <param name="player"></param>
        public static void FindAndConsumeKiPotion(this Player player)
        {
            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item item = player.inventory[i];
                if (item == null)
                    continue;
                if (item.modItem == null)
                    continue;
                if (item.modItem is KiPotion)
                {
                    KiPotion potion = (KiPotion)item.modItem;
                    potion.ConsumeItem(player);
                }
            }
        }

        /// <summary>
        ///     Return true if the player is carrying one of each dragon ball.
        /// </summary>
        /// <param name="player">The player being checked.</param>
        /// <returns></returns>
        public static bool IsCarryingAllDragonBalls(this Player player)
        {
            bool[] dragonBallsPresent = Enumerable.Repeat(false, 7).ToArray();
            for (int i = 0; i < dragonBallsPresent.Length; i++)
            {
                dragonBallsPresent[i] = player.inventory.IsDragonBallPresent(i + 1);
            }

            return dragonBallsPresent.All(x => x);
        }

        /// <summary>
        ///     Find and destroy exactly one of each dragon ball type in a player's inventory.
        ///     Called after making a wish.
        /// </summary>
        /// <param name="player">The player being checked.</param>
        public static void DestroyOneOfEachDragonBall(this Player player)
        {
            List<int> dragonBallTypeAlreadyRemoved = new List<int>();
            foreach (var item in player.inventory)
            {
                if (item == null)
                    continue;
                if (item.modItem == null)
                    continue;
                if (item.modItem is DragonBallItem)
                {
                    // only remove one of each type of dragon ball. If the player has extras, leave them. Lucky them.
                    if (dragonBallTypeAlreadyRemoved.Contains(item.type))
                        continue;
                    dragonBallTypeAlreadyRemoved.Add(item.type);
                    item.TurnToAir();
                }
            }
        }

        /// <summary>
        ///     Return the aura effect currently active on the player.
        /// </summary>
        /// <param name="modPlayer">The player being checked</param>
        public static AuraAnimationInfo GetAuraEffectOnPlayer(this MyPlayer modPlayer)
        {
            if (modPlayer.player.dead)
                return null;
            if (modPlayer.player.IsKaioken())
                return AuraAnimations.createKaiokenAura;
            if (modPlayer.player.IsSuperKaioken())
                return AuraAnimations.createSuperKaiokenAura;
            if (modPlayer.player.IsSSJ1())
                return AuraAnimations.ssj1Aura;
            if (modPlayer.player.IsAssj())
                return AuraAnimations.assjAura;
            if (modPlayer.player.IsUssj())
                return AuraAnimations.ussjAura;
            if (modPlayer.player.IsSSJ2())
                return AuraAnimations.ssj2Aura;
            if (modPlayer.player.IsSSJ3())
                return AuraAnimations.ssj3Aura;
            if (modPlayer.player.IsSSJG())
                return AuraAnimations.ssjgAura;
            if (modPlayer.player.IsLSSJ1())
                return AuraAnimations.lssjAura;
            if (modPlayer.player.IsLSSJ2())
                return AuraAnimations.lssj2Aura;
            if (modPlayer.player.IsSpectrum())
                return AuraAnimations.spectrumAura;
            // handle charging last
            if (modPlayer.isCharging)
                return AuraAnimations.createChargeAura;
            return null;
        }

        public static void ApplyChannelingSlowdown(this Player player)
        {
            MyPlayer modPlayer = MyPlayer.ModPlayer(player);
            if (modPlayer.isFlying)
            {
                float chargeMoveSpeedBonus = modPlayer.chargeMoveSpeed / 10f;
                float yVelocity = -(player.gravity + 0.001f);
                if (modPlayer.isDownHeld || modPlayer.isUpHeld)
                {
                    yVelocity = player.velocity.Y / (1.2f - chargeMoveSpeedBonus);
                }
                else
                {
                    yVelocity = Math.Min(-0.4f, player.velocity.Y / (1.2f - chargeMoveSpeedBonus));
                }
                player.velocity = new Vector2(player.velocity.X / (1.2f - chargeMoveSpeedBonus), yVelocity);
            }
            else
            {
                float chargeMoveSpeedBonus = modPlayer.chargeMoveSpeed / 10f;
                // don't neuter falling - keep the positive Y velocity if it's greater - if the player is jumping, this reduces their height. if falling, falling is always greater.                        
                player.velocity = new Vector2(player.velocity.X / (1.2f - chargeMoveSpeedBonus), Math.Max(player.velocity.Y, player.velocity.Y / (1.2f - chargeMoveSpeedBonus)));
            }
        }

        public static Projectile FindNearestOwnedProjectileOfType(this Player player, int type)
        {
            int closestProjectile = -1;
            float distance = Single.MaxValue;
            for(var i = 0; i < Main.projectile.Length; i++)
            {
                var proj = Main.projectile[i];

                // abort if the projectile is invalid, the player isn't the owner, the projectile is inactive or the type doesn't match what we want.
                if (proj == null || proj.owner != player.whoAmI || !proj.active || proj.type != type)
                    continue;               
                
                var projDistance = proj.Distance(player.Center);
                if (projDistance < distance)
                {
                    distance = projDistance;
                    closestProjectile = i;
                }
            }
            return closestProjectile == -1 ? null : Main.projectile[closestProjectile];
        }

        public static bool IsChargeBallRecaptured(this Player player, int type)
        {   
            // assume first that the player's already holding a proj
            if (player.heldProj != -1)
            {
                var heldProj = Main.projectile[player.heldProj];
                if (heldProj.modProjectile is AbstractChargeBall)
                {
                    return true;
                }
            }

            // otherwise try to recapture the held projectile if possible.
            var proj = player.FindNearestOwnedProjectileOfType(type);
            if (proj != null)
            {
                // the part that matters
                player.heldProj = proj.whoAmI;
                return true;
            }
            return false;
        }

        public static bool IsMassiveBlastInUse(this Player player)
        {            
            foreach(int massiveBlastType in ProjectileHelper.MassiveBlastProjectileTypes)
            {
                if (player.ownedProjectileCounts[massiveBlastType] > 0)
                    return true;
            }
            return false;
        }

        public static bool IsPlayerTransformed(this Player player)
        {
            foreach (TransformationDefinition buff in FormBuffHelper.AllBuffs())
            {
                if (player.HasBuff(buff.GetBuffId()))
                    return true;
            }

            return false;
        }

        public static bool PlayerHasBuffIn(this Player player, TransformationDefinition[] buffs)
        {
            foreach (TransformationDefinition buff in buffs)
            {
                if (player.HasBuff(buff.GetBuffId()))
                    return true;
            }

            return false;
        }

        public static bool IsSSJ(this Player player)
        {
            return player.PlayerHasBuffIn(FormBuffHelper.ssjBuffs);
        }

        public static bool IsSSJ2(this Player player)
        {
            return player.HasBuff(DBZMOD.Instance.TransformationDefinitionManager.SSJ2Definition.GetBuffId());
        }

        public static bool IsSSJ3(this Player player)
        {
            return player.HasBuff(DBZMOD.Instance.TransformationDefinitionManager.SSJ3Definition.GetBuffId());
        }

        public static bool IsSpectrum(this Player player)
        {
            return player.HasBuff(DBZMOD.Instance.TransformationDefinitionManager.SpectrumDefinition.GetBuffId());
        }

        public static bool IsKaioken(this Player player)
        {
            return player.HasBuff(DBZMOD.Instance.TransformationDefinitionManager.KaiokenDefinition.GetBuffId());
        }

        public static bool IsAnyKaioken(this Player player)
        {
            return player.IsKaioken() || player.IsSuperKaioken();
        }

        public static bool IsDevBuffed(this Player player)
        {
            return player.IsSpectrum();
        }

        public static bool IsAnythingOtherThanKaioken(this Player player)
        {
            return player.IsLSSJ() || player.IsSSJ() || player.IsSSJG() || player.IsDevBuffed() || player.IsAscended() || player.IsSuperKaioken();
        }

        public static bool IsValidKaiokenForm(this Player player)
        {
            return player.IsSSJ1(); // || IsSSJB(player) || IsSSJR(player)
        }

        public static bool CanTransform(this Player player, MenuSelectionID menuId)
        {
            return CanTransform(player, FormBuffHelper.GetBuffFromMenuSelection(menuId));
        }

        public static bool IsAscended(this Player player)
        {
            return player.HasBuff(DBZMOD.Instance.TransformationDefinitionManager.ASSJDefinition.GetBuffId()) || player.HasBuff(DBZMOD.Instance.TransformationDefinitionManager.USSJDefinition.GetBuffId());
        }

        public static bool IsTransformBlocked(this Player player)
        {
            MyPlayer modPlayer = MyPlayer.ModPlayer(player);
            return modPlayer.isTransforming || modPlayer.IsPlayerImmobilized() || modPlayer.IsKiDepleted();
        }
        
        public static bool CanTransform(this Player player, TransformationDefinition buff)
        {
            if (buff == null)
                return false;

            if (player.IsTransformBlocked())
                return false;
            
            MyPlayer modPlayer = MyPlayer.ModPlayer(player);
            if (buff.Equals(DBZMOD.Instance.TransformationDefinitionManager.SSJ1Definition))
                return modPlayer.SSJ1Achieved && !player.IsExhaustedFromTransformation();
            if (buff.Equals(DBZMOD.Instance.TransformationDefinitionManager.SSJ2Definition))
                return !modPlayer.IsPlayerLegendary() && modPlayer.SSJ2Achieved && !player.IsExhaustedFromTransformation();
            if (buff.Equals(DBZMOD.Instance.TransformationDefinitionManager.SSJ3Definition))
                return !modPlayer.IsPlayerLegendary() && modPlayer.SSJ3Achieved && !player.IsExhaustedFromTransformation();
            if (buff.Equals(DBZMOD.Instance.TransformationDefinitionManager.SSJGDefinition))
                return !modPlayer.IsPlayerLegendary() && modPlayer.SSJGAchieved && !player.IsExhaustedFromTransformation();
            if (buff.Equals(DBZMOD.Instance.TransformationDefinitionManager.LSSJDefinition))
                return modPlayer.IsPlayerLegendary() && modPlayer.LSSJAchieved && !player.IsExhaustedFromTransformation();
            if (buff.Equals(DBZMOD.Instance.TransformationDefinitionManager.LSSJ2Definition))
                return modPlayer.IsPlayerLegendary() && modPlayer.LSSJ2Achieved && !player.IsExhaustedFromTransformation();
            if (buff.Equals(DBZMOD.Instance.TransformationDefinitionManager.ASSJDefinition))
                return (player.IsSSJ1() || player.IsUssj()) && modPlayer.ASSJAchieved && !player.IsExhaustedFromTransformation();
            if (buff.Equals(DBZMOD.Instance.TransformationDefinitionManager.USSJDefinition))
                return player.IsAssj() && modPlayer.USSJAchieved && !player.IsExhaustedFromTransformation();
            if (buff.Equals(DBZMOD.Instance.TransformationDefinitionManager.KaiokenDefinition))
                return modPlayer.kaioAchieved && !player.IsTiredFromKaioken();
            if (buff.Equals(DBZMOD.Instance.TransformationDefinitionManager.SuperKaiokenDefinition))
                return modPlayer.kaioAchieved && !player.IsTiredFromKaioken() && !player.IsExhaustedFromTransformation();
            if (buff.Equals(DBZMOD.Instance.TransformationDefinitionManager.SpectrumDefinition))
                return player.name == "Nuova";
            return false;
        }

        public static void AddKaiokenExhaustion(this Player player, int multiplier)
        {
            MyPlayer modPlayer = MyPlayer.ModPlayer(player);
            player.AddBuff(DBZMOD.Instance.TransformationDefinitionManager.KaiokenFatigueDefinition.GetBuffId(), (int)Math.Ceiling(modPlayer.kaiokenTimer * multiplier));
            modPlayer.kaiokenTimer = 0f;
        }

        public static void AddTransformationExhaustion(this Player player)
        {
            player.AddBuff(DBZMOD.Instance.TransformationDefinitionManager.TransformationExhaustionDefinition.GetBuffId(), 600);
        }

        public static bool IsExhaustedFromTransformation(this Player player) { return player.HasBuff(DBZMOD.Instance.TransformationDefinitionManager.TransformationExhaustionDefinition.GetBuffId()); }
        public static bool IsTiredFromKaioken(this Player player) { return player.HasBuff(DBZMOD.Instance.TransformationDefinitionManager.KaiokenFatigueDefinition.GetBuffId()); }

        public static void ClearAllTransformations(this Player player)
        {
            foreach (TransformationDefinition buff in FormBuffHelper.AllBuffs())
            {
                // don't clear buffs the player doesn't have, obviously.
                if (!player.HasBuff(buff.GetBuffId()))
                    continue;

                player.RemoveTransformation(buff.UnlocalizedName);
            }
        }

        public static void RemoveTransformation(this Player player, string buffKeyName)
        {
            TransformationDefinition buff = FormBuffHelper.GetBuffByKeyName(buffKeyName);

            player.ClearBuff(buff.GetBuffId());

            if (!Main.dedServ && Main.netMode == NetmodeID.MultiplayerClient && player.whoAmI == Main.myPlayer)
            {                
                NetworkHelper.formSync.SendFormChanges(256, player.whoAmI, player.whoAmI, buffKeyName, 0);                
            }
        }

        public static void DoTransform(this Player player, TransformationDefinition buff, Mod mod)
        {
            MyPlayer modPlayer = MyPlayer.ModPlayer(player);
            
            // don't.. try to apply the same transformation. This just stacks projectile auras and looks dumb.
            if (buff.Equals(player.GetCurrentTransformation(true, false)) || buff.Equals(player.GetCurrentTransformation(false, true)))
                return;

            // make sure to swap kaioken with super kaioken when appropriate.
            if (buff.Equals(DBZMOD.Instance.TransformationDefinitionManager.SuperKaiokenDefinition))
            {
                player.RemoveTransformation(DBZMOD.Instance.TransformationDefinitionManager.KaiokenDefinition.UnlocalizedName);
            }

            // remove all *transformation* buffs from the player.
            // this needs to know we're powering down a step or not
            player.EndTransformations();

            // add whatever buff it is for a really long time.
            player.AddTransformation(buff.UnlocalizedName, FormBuffHelper.ABSURDLY_LONG_BUFF_DURATION);
        }

        public static void EndTransformations(this Player player)
        {
            MyPlayer modPlayer = player.GetModPlayer<MyPlayer>();
            player.ClearAllTransformations();
            modPlayer.isTransformationAnimationPlaying = false;
            modPlayer.transformationFrameTimer = 0;
            
            modPlayer.isTransforming = false;
        }

        public static void AddTransformation(this Player player, string buffKeyName, int duration)
        {            
            TransformationDefinition buff = FormBuffHelper.GetBuffByKeyName(buffKeyName);
            player.AddBuff(buff.GetBuffId(), FormBuffHelper.ABSURDLY_LONG_BUFF_DURATION, false);

            if (!String.IsNullOrEmpty(buff.TransformationText))
                CombatText.NewText(player.Hitbox, buff.TransformationTextColor, buff.TransformationText, false, false);

            if (!Main.dedServ && Main.netMode == NetmodeID.MultiplayerClient && player.whoAmI == Main.myPlayer) {
                NetworkHelper.formSync.SendFormChanges(256, player.whoAmI, player.whoAmI, buffKeyName, duration);
            }

            // start the transformation animation, if one exists. This auto cancels if nothing is there to play.
            player.GetModPlayer<MyPlayer>().isTransformationAnimationPlaying = true;
        }

        public static TransformationDefinition GetCurrentTransformation(this Player player, bool isIgnoringKaioken, bool isIgnoringNonKaioken)
        {
            foreach (TransformationDefinition buff in FormBuffHelper.AllBuffs())
            {
                if (buff.IsKaioken() && isIgnoringKaioken)
                    continue;

                if (buff.IsAnythingOtherThanKaioken() && isIgnoringNonKaioken)
                    continue;

                if (player.HasBuff(buff.GetBuffId()))
                {
                    return buff;
                }
            }

            // is the player transformed? Something bad may have happened.
            return null;
        }

        public static TransformationDefinition GetNextTransformationStep(this Player player)
        {
            TransformationDefinition currentTransformation = player.GetCurrentTransformation(false, false);
            TransformationDefinition currentNonKaioTransformation = player.GetCurrentTransformation(true, false);
            if (currentTransformation.IsKaioken())
            {
                // player was in kaioken, trying to power up. Go to super kaioken but set the player's kaioken level to 1 because that's how things are now.
                if (currentNonKaioTransformation == null && player.GetModPlayer<MyPlayer>().hasSSJ1)
                {
                    player.GetModPlayer<MyPlayer>().kaiokenLevel = 1;
                    return DBZMOD.Instance.TransformationDefinitionManager.SuperKaiokenDefinition;
                }

                // insert handler for SSJBK here

                // insert handler for SSJRK here
            }

            // SSJ1 is always the starting point if there isn't a current form tree to step through.
            if (currentTransformation == null)
                return DBZMOD.Instance.TransformationDefinitionManager.SSJ1Definition;

            // the player is legendary and doing a legendary step up.
            if (currentTransformation.IsLSSJ() && MyPlayer.ModPlayer(player).IsPlayerLegendary())
            {
                for (int i = 0; i < FormBuffHelper.legendaryBuffs.Length; i++)
                {
                    if (FormBuffHelper.legendaryBuffs[i].Equals(currentTransformation) && i < FormBuffHelper.legendaryBuffs.Length - 1)
                    {
                        return FormBuffHelper.legendaryBuffs[i + 1];
                    }
                }
            }

            // the player isn't legendary and is doing a normal step up.
            if (currentTransformation.IsSSJ() && !MyPlayer.ModPlayer(player).IsPlayerLegendary())
            {
                for (int i = 0; i < FormBuffHelper.ssjBuffs.Length; i++)
                {
                    if (FormBuffHelper.ssjBuffs[i].Equals(currentTransformation) && i < FormBuffHelper.ssjBuffs.Length - 1)
                    {
                        return FormBuffHelper.ssjBuffs[i + 1];
                    }
                }
            }

            // whatever happened here, the function couldn't find a next step. Either the player is maxed in their steps, or something bad happened.
            return null;
        }

        public static TransformationDefinition GetPreviousTransformationStep(this Player player)
        {
            TransformationDefinition currentTransformation = player.GetCurrentTransformation(true, false);

            // the player is legendary and doing a legendary step down.
            if (currentTransformation.IsLSSJ() && MyPlayer.ModPlayer(player).IsPlayerLegendary())
            {
                for (int i = 0; i < FormBuffHelper.legendaryBuffs.Length; i++)
                {
                    if (FormBuffHelper.legendaryBuffs[i].Equals(currentTransformation) && i > 0)
                    {
                        return FormBuffHelper.legendaryBuffs[i - 1];
                    }
                }
            }

            // the player isn't legendary and is doing a normal step down.
            if (currentTransformation.IsSSJ() && !MyPlayer.ModPlayer(player).IsPlayerLegendary())
            {
                for (int i = 0; i < FormBuffHelper.ssjBuffs.Length; i++)
                {
                    if (FormBuffHelper.ssjBuffs[i].Equals(currentTransformation) && i > 0)
                    {
                        return FormBuffHelper.ssjBuffs[i - 1];
                    }
                }
            }

            // figure out what the step down for ascension should be, if the player is in an ascended form.
            if (currentTransformation.IsAscended())
            {
                for (int i = 0; i < FormBuffHelper.ascensionBuffs.Length; i++)
                {
                    if (FormBuffHelper.ascensionBuffs[i].Equals(currentTransformation) && i > 0)
                    {
                        return FormBuffHelper.ascensionBuffs[i - 1];
                    }
                }
            }

            // either the player is at minimum or something bad has happened.
            return null;
        }

        public static TransformationDefinition GetNextAscensionStep(this Player player)
        {
            TransformationDefinition currentTransformation = player.GetCurrentTransformation(true, false);

            if (currentTransformation.IsAscended() || player.IsSSJ1())
            {
                for (int i = 0; i < FormBuffHelper.ascensionBuffs.Length; i++)
                {
                    if (FormBuffHelper.ascensionBuffs[i].Equals(currentTransformation) && i < FormBuffHelper.ascensionBuffs.Length - 1)
                    {
                        return FormBuffHelper.ascensionBuffs[i + 1];
                    }
                }
            }

            return currentTransformation;
        }

        public static bool IsSuperKaioken(this Player player)
        {
            return player.HasBuff(DBZMOD.Instance.TransformationDefinitionManager.SuperKaiokenDefinition.GetBuffId());
        }

        public static bool IsLSSJ(this Player player)
        {
            return player.PlayerHasBuffIn(FormBuffHelper.legendaryBuffs);
        }

        public static bool IsLSSJ1(this Player player)
        {
            return player.HasBuff(DBZMOD.Instance.TransformationDefinitionManager.LSSJDefinition.GetBuffId());
        }

        public static bool IsLSSJ2(this Player player)
        {
            return player.HasBuff(DBZMOD.Instance.TransformationDefinitionManager.LSSJ2Definition.GetBuffId());
        }

        public static bool IsSSJG(this Player player)
        {
            return player.HasBuff(DBZMOD.Instance.TransformationDefinitionManager.SSJGDefinition.GetBuffId());
        }

        public static bool IsSSJ1(this Player player)
        {
            return player.HasBuff(DBZMOD.Instance.TransformationDefinitionManager.SSJ1Definition.GetBuffId());
        }

        public static bool IsAssj(this Player player)
        {
            return player.HasBuff(DBZMOD.Instance.TransformationDefinitionManager.ASSJDefinition.GetBuffId());
        }

        public static bool IsUssj(this Player player)
        {
            return player.HasBuff(DBZMOD.Instance.TransformationDefinitionManager.USSJDefinition.GetBuffId());
        }

        public static void DrawAura(this MyPlayer modPlayer, AuraAnimationInfo aura)
        {
            Player player = modPlayer.player;
            Texture2D texture = aura.GetTexture();
            Rectangle textureRectangle = new Rectangle(0, aura.GetHeight() * modPlayer.auraCurrentFrame, texture.Width, aura.GetHeight());
            float scale = aura.GetAuraScale(modPlayer);
            Tuple<float, Vector2> rotationAndPosition = aura.GetAuraRotationAndPosition(modPlayer);
            float rotation = rotationAndPosition.Item1;
            Vector2 position = rotationAndPosition.Item2;

            SamplerState samplerState = Main.DefaultSamplerState;
            if (player.mount.Active)
            {
                samplerState = Main.MountedSamplerState;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, aura.blendState, samplerState, DepthStencilState.None, Main.instance.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // custom draw routine
            Main.spriteBatch.Draw(texture, position - Main.screenPosition, textureRectangle, Color.White, rotation, new Vector2(aura.GetWidth(), aura.GetHeight()) * 0.5f, scale, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, samplerState, DepthStencilState.None, Main.instance.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public static string GetCurrentFormForMastery(this Player player)
        {
            foreach (TransformationDefinition buff in FormBuffHelper.AllBuffs().Where(x => x.HasMastery))
            {
                if (player.HasBuff(buff.GetBuffId()))
                {
                    return buff.MasteryBuffKeyName;
                }
            }

            return string.Empty;
        }
    }
}