using Terraria;
using Terraria.ModLoader;

namespace StockableShops;

public class StockableShops : Mod
{
}

public class AnnouncementPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        Main.NewText("[Stockable Shops] If you experience issues, report to the Verdant discord! Link in this mod's description.");
    }
}