using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace StockableShops.Stock.Shops;

/// <summary>
/// Example stocked shop. Sells 5 King Statues for 10 silver each, and sells 5 Queen Statues after EoW/BoC is downed for 20 gold each.
/// </summary>
internal class MerchantStock : StockedShop
{
    public override bool IsLoadingEnabled(Mod mod) => true; // This shouldn't actually be used so don't load this.

    public override int NPCType => NPCID.Merchant;

    public override void SetupStock(NPC npc)
    {
        FullStock.Add(new ShopItem(new Item(ItemID.KingStatue, 2) { shopCustomPrice = Item.buyPrice(0, 0, 10, 0) }));
        FullStock.Add(new ShopItem(Condition.DownedEowOrBoc, new Item(ItemID.QueenStatue, 5) { shopCustomPrice = Item.buyPrice(0, 20, 0, 0) }));
    }
}
