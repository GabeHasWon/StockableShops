using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace StockableShops.Stock.Shops;

/// <summary>
/// Example stocked shop. Sells 5 King Statues for 10 silver each, and sells 5 Queen Statues after EoW/BoC is downed for 20 gold each.
/// </summary>
internal class MerchantStock : StockedShop
{
    public override bool IsLoadingEnabled(Mod mod) => false; // This shouldn't actually be in-game so don't load this.

    public override int NPCType => NPCID.Merchant;
    public override string RestockCondition => Language.GetTextValue("Mods.StockableShops.Shops.Merchant");

    public override void SetupStock(NPC npc)
    {
        // Adds 3 King Statues to the shop with a price of 10 silver each.
        FullStock.Add(new ShopItem(new Item(ItemID.KingStatue, 3) { shopCustomPrice = Item.buyPrice(0, 0, 10, 0) }));

        // Adds 1 Queen Statue with a price of 20 gold, only after the evil boss is dead.
        FullStock.Add(new ShopItem(Condition.DownedEowOrBoc, new Item(ItemID.QueenStatue) { shopCustomPrice = Item.buyPrice(0, 20, 0, 0) }));
    }
}
