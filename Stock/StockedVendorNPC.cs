using Terraria;
using Terraria.ModLoader;

namespace StockableShops.Stock;

/// <summary>
/// Handles stocked vendor NPCs.
/// </summary>
public sealed class StockedVendorNPC : GlobalNPC
{
    /// <inheritdoc/>
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.townNPC && StockedShop.HasShop(entity.type);

    /// <inheritdoc/>
    public override void ModifyActiveShop(NPC npc, string shopName, Item[] items)
    {
        var shops = StockedShop.ShopsPerNpcId(npc.type);
        
        foreach (var item in shops.Values)
            item.StockShop(npc, shopName, items);
    }

    /// <inheritdoc/>
    public override void PostAI(NPC npc)
    {
        var talkNPC = Main.LocalPlayer.TalkNPC;

        if (talkNPC is not null && talkNPC.whoAmI == npc.whoAmI && Main.npcShop > 0)
            TrackItems(npc);
    }

    private static void TrackItems(NPC npc)
    {
        var shops = StockedShop.ShopsPerNpcId(npc.type);

        foreach (var item in shops.Values)
            item.WhileShopOpen(npc, Main.instance.shop[Main.npcShop]);
    }
}
