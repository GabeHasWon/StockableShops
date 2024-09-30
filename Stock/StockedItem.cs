using Terraria;
using Terraria.ModLoader;

namespace StockableShops.Stock;

/// <summary>
/// handle items in a stocked shop
/// </summary>
public sealed class StockedItem : GlobalItem
{
    /// <inheritdoc/>
    public override bool InstancePerEntity => true;

    /// <summary>
    /// set this to true to mark the item in a stocked shop
    /// </summary>
    public bool Stockable { get; set; }

    /// <summary>
    /// use this to storage stack in a stocked shop instead of <see cref="Item.stack"/>
    /// </summary>
    public int Stack { get; set; }
}
