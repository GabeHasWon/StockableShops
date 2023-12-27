using Terraria;
using Terraria.ModLoader;

namespace StockableShops.Stock;

internal class StockedVendorNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;

    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => StockedShop.HasShop(entity.type);

    public override void ModifyActiveShop(NPC npc, string shopName, Item[] items)
    {
        var shops = StockedShop.ShopsPerNpcId(npc.type);

        foreach (var item in shops.Values)
            item.StockShop(npc, shopName, items);
    }

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
