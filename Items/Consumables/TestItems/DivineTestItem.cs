﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DBZMOD.Items.Consumables.TestItems
{
    public sealed class DivineTestItem : ModItem
    {
        public override void SetDefaults()
        {
            item.width = 40;
            item.height = 40;
            item.consumable = false;
            item.maxStack = 1;
            item.UseSound = SoundID.Item3;
            item.useStyle = 2;
            item.useTurn = true;
            item.useAnimation = 17;
            item.useTime = 17;
            item.value = 0;
            item.expert = true;
            item.potion = false;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Divine Test Item");
            Tooltip.SetDefault("Gives the divine trait");
        }
		
		public override bool UseItem(Player player)
		{
		    if (!DBZMOD.allowDebugItem) return false;

            player.GetModPlayer<MyPlayer>().PlayerTrait = DBZMOD.Instance.TraitManager.Divine;
            return true;
        }
    }
}
