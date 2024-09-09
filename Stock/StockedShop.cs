using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace StockableShops.Stock;

/// <summary>
/// Defines a shop which has a variable stock with conditions, alongside helper classes to register, find and check for shops.
/// </summary>
public abstract class StockedShop : ModType
{
    /// <summary>
    /// Used to collect all loaded <see cref="StockedShop"/>s, in order to avoid <see cref="ModContent.GetContent{T}"/> bottleneck.
    /// </summary>
    private static readonly List<StockedShop> _allShops = [];

    private static readonly Dictionary<string, Dictionary<int, StockedShop>> _shopsPerModByNpcId = [];
    private static readonly Dictionary<int, Dictionary<string, StockedShop>> _shopsPerNpcIdByMod = [];

    /// <summary>
    /// Refers to which NPC this shop is attached to. 
    /// If the NPC you're attaching to does not have a vanilla system shop, this <see cref="StockedShop"/> does nothing.
    /// </summary>
    public abstract int NPCType { get; }

    /// <summary>
    /// By what conditions this shop's stock refreshes. 
    /// This is mandatory for mods that may want to show other mod's restock conditions without undue effort.<br/>
    /// This should be a localized object, so make sure you're using <see cref="Language.GetTextValue(string)"/> or catching a localized text to use.
    /// </summary>
    public abstract string RestockCondition { get; }

    /// <summary>
    /// The entire stock of this shop. Should only be used in <see cref="SetupStock(NPC)"/>, and never again outside of that.
    /// </summary>
    public readonly List<ShopItem> FullStock = [];

    /// <summary>
    /// The current stock of this shop. Can be modified anytime.
    /// </summary>
    private readonly List<ShopItem> stock = [];

    /// <summary>
    /// Default bool for tracking if the shop needs a restock or not.
    /// </summary>
    protected bool needsRestock = false;

    /// <summary>
    /// Used to flag if this is the first time this shop is stocked, in order to call <see cref="SetupStock(NPC)"/>.
    /// </summary>
    protected bool firstStock = true;

    /// <summary>
    /// Returns all shops registered under the given mod name.
    /// </summary>
    /// <param name="name">Name of the mod to reference.</param>
    /// <returns>The <see cref="Dictionary{TKey, TValue}"/> containing the mod's shops, by NPC ID.</returns>
    /// <exception cref="ArgumentException"/>
    public static Dictionary<int, StockedShop> ShopsPerMod(string name)
    {
        if (!_shopsPerModByNpcId.ContainsKey(name))
            throw new ArgumentException($"No mod called {name} has any registered shops!");

        return _shopsPerModByNpcId[name];
    }

    /// <summary>
    /// Returns all shops registered under the given NPC ID.
    /// </summary>
    /// <param name="id">The NPC ID to reference.</param>
    /// <returns>The <see cref="Dictionary{TKey, TValue}"/> containing each of the NPC's shops, by mod.</returns>
    /// <exception cref="ArgumentException"/>
    public static Dictionary<string, StockedShop> ShopsPerNpcId(int id)
    {
        if (!_shopsPerNpcIdByMod.ContainsKey(id))
            throw new ArgumentException($"No NPCID by the ID {id} (\"{Lang.GetNPCNameValue(id)}\") has any registered shops!");

        return _shopsPerNpcIdByMod[id];
    }

    /// <inheritdoc cref="ShopsPerNpcId(int)"/>
    public static Dictionary<string, StockedShop> ShopsPerNpcId<T>() where T : ModNPC => ShopsPerNpcId(ModContent.NPCType<T>());

    /// <summary>
    /// Gets whether the given NPC ID has a shop registered.
    /// </summary>
    /// <param name="id">The NPC ID to check.</param>
    public static bool HasShop(int id) => _shopsPerNpcIdByMod.ContainsKey(id);

    /// <inheritdoc cref="HasShop(int)"/>
    public static bool HasShop<T>() where T : ModNPC => HasShop(ModContent.NPCType<T>());

    /// <summary>
    /// Gets the shop registered to the NPC ID under the given mod name. The mod name is always simply "ModName", i.e. "Verdant".<br/>
    /// </summary>
    /// <param name="id">NPC ID to use.</param>
    /// <param name="modName">Mod name to use.</param>
    /// <returns>The <see cref="StockedShop"/> instance assinged to the given NPC under the given mod.</returns>
    public static StockedShop GetShop(int id, string modName) => _shopsPerNpcIdByMod[id][modName];

    /// <inheritdoc cref="GetShop(int, string)"/>
    /// <param name="mod">Mod to use mod.Name.</param>
    /// <param name="id">NPC ID to use.</param>
    public static StockedShop GetShop(int id, Mod mod) => GetShop(id, mod.Name);

    /// <inheritdoc cref="GetShop(int, string)"/>
    public static StockedShop GetShop<T>(string modName) where T : ModNPC => GetShop(ModContent.NPCType<T>(), modName);

    /// <inheritdoc cref="GetShop(int, Mod)"/>
    public static StockedShop GetShop<T>(Mod mod) where T : ModNPC => GetShop(ModContent.NPCType<T>(), mod.Name);

    /// <summary>
    /// This handles registering the new shop to the given dictionaries automatically, alongside the lookup.
    /// </summary>
    protected sealed override void Register()
    {
        ModTypeLookup<StockedShop>.Register(this);

        if (!_shopsPerModByNpcId.ContainsKey(Mod.Name))
            _shopsPerModByNpcId.Add(Mod.Name, new Dictionary<int, StockedShop>() { { NPCType, this } });
        else
            _shopsPerModByNpcId[Mod.Name].Add(NPCType, this);

        if (!_shopsPerNpcIdByMod.ContainsKey(NPCType))
            _shopsPerNpcIdByMod.Add(NPCType, new Dictionary<string, StockedShop>() { { Mod.Name, this } });
        else
            _shopsPerNpcIdByMod[NPCType].Add(Mod.Name, this);

        _allShops.Add(this);
    }

    /// <summary>
    /// Called when the stock needs to be set up. Modify <see cref="FullStock"/>, as <see cref="stock"/> is the stock after being checked for availability.<br/>
    /// This is ONLY RUN ONCE PER LOAD. It's similar to ModifyNPCLoot, so only use it when creating the initial stock.
    /// </summary>
    public abstract void SetupStock(NPC npc);

    /// <summary>
    /// Called when the given <see cref="NPC"/>'s shop is stocked or restocked.<br/>
    /// This is run through <see cref="GlobalNPC.ModifyActiveShop(NPC, string, Item[])"/>.<br/>
    /// <see cref="ShouldRestockShop"/> is checked every time the shop is opened, and is what determines if a new or replacement stock is given.<br/>
    /// Otherwise, the shop will contain the remaining items from <see cref="stock"/>.<br/>
    /// You should only override this if you really know what you're doing! This default implementation is good for 99% of cases.
    /// </summary>
    /// <param name="npc">The NPC which is being restocked.</param>
    /// <param name="shop">The shop (item array) being modified.</param>
    /// <param name="shopName">The name of the shop.</param>
    public virtual void StockShop(NPC npc, string shopName, Item[] shop)
    {
        bool reset = true;

        if (ShouldRestockShop())
        {
            stock.Clear();

            if (firstStock)
            {
                firstStock = false;
                FullStock.Clear();
                SetupStock(npc);
            }

            RestockShop(npc, shop);
            needsRestock = false;
            reset = false;
        }

        BasicStockShop(shop, reset);
    }

    /// <summary>
    /// Called when the shop is restocked. Use this to modify variables, update stuff or manually modify <see cref="stock"/>.
    /// </summary>
    /// <param name="npc">The NPC which is being restocked.</param>
    /// <param name="shop">The shop (item array) being modified.</param>
    protected virtual void RestockShop(NPC npc, Item[] shop) { }

    /// <summary>
    /// This is the basic implementation for stocking the shop. By default, it generates the new stock (if <paramref name="reset"/> is true), 
    /// or adds in the old stock.<br/>
    /// Old stock that is null, air, or at or below a stack of 0 is automatically removed.
    /// </summary>
    /// <param name="shop"></param>
    /// <param name="reset"></param>
    private void BasicStockShop(Item[] shop, bool reset)
    {
        // Get the first index that isn't air.
        int index = Array.IndexOf(shop, shop.First(x => x is null || x.IsAir));

        if (!reset)
        {
            // Generate and stock each item.
            foreach (var item in FullStock)
            {
                if (StockIndividualItem(item, shop, ref index, true))
                    break;
            }
        }
        else
        {
            // Remove all empty items from the stock.
            stock.RemoveAll(item => item.Item is null || item.Item.IsAir || item.Item.stack < 0);

            // And restock the shop.
            foreach (var item in stock)
            {
                if (StockIndividualItem(item, shop, ref index))
                    break;
            }
        }
    }

    /// <summary>
    /// Used by <see cref="BasicStockShop(Item[], bool)"/> to stock individual items. You may want to adapt this to fit your specific method if needed.
    /// </summary>
    /// <param name="item">The shop item to stock.</param>
    /// <param name="shop">The shop to stock in.</param>
    /// <param name="index">The current index for the shop.</param>
    /// <param name="addToStock">Whether this method adds the current <paramref name="item"/> to <see cref="stock"/>.</param>
    /// <returns></returns>
    private bool StockIndividualItem(ShopItem item, Item[] shop, ref int index, bool addToStock = false)
    {
        if (item.Condition.IsMet())
        {
            shop[index] = item.Item;

            if (addToStock)
                stock.Add(item);

            index++;

            // Exit early if the index surpasses the shop's size.
            if (index >= shop.Length)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the shop should restock. By default, this returns <c><see cref="stock"/>.Count == 0 or (!<see cref="needsRestock"/> and <see cref="Main.dayTime"/>)</c>.
    /// </summary>
    /// <returns>Whether the shop should restock.</returns>
    public virtual bool ShouldRestockShop() => stock.Count == 0 || needsRestock && Main.dayTime;

    /// <summary>
    /// Runs while the NPC's shop is open. Does nothing by default.
    /// </summary>
    /// <param name="npc">The NPC you're shopping at.</param>
    /// <param name="shop">The...shop...you're shopping at.</param>
    public virtual void WhileShopOpen(NPC npc, Chest shop)
    {
    }

    /// <summary>
    /// This method is always running. Use this for constant checks or changes made regardless of if the shop is or isn't open.<br/>
    /// By default, this sets <see cref="needsRestock"/> to false at night.
    /// </summary>
    public virtual void Update()
    {
        if (!Main.dayTime)
            needsRestock = true;
    }

    /// <summary>
    /// Handles a single stocked item with a given condition.<br/>
    /// For example:
    /// <c>new ShopItem(new Condition.NotTheBeesWorld(), new Item(ItemID.Dirt, 20));</c>
    /// would create a shop item that is only available on a Not The Bees! world, and has a max stock of 20.<br/><br/>
    /// </summary>
    public class ShopItem
    {
        private static readonly Condition AlwaysTrue = new(string.Empty, () => true);

        /// <summary>
        /// The condition of this <see cref="ShopItem"/>.
        /// </summary>
        public virtual Condition Condition { get; protected set; }

        /// <summary>
        /// The item this <see cref="ShopItem"/> provides.
        /// </summary>
        public virtual Item Item { get; protected set; }

        /// <summary>
        /// Creates an instance of <see cref="ShopItem"/> with the given item and a condition that is always true.
        /// </summary>
        /// <param name="item">The item that this holds.</param>
        public ShopItem(Item item)
        {
            Condition = AlwaysTrue;
            Item = item;
            Item.isAShopItem = true;
        }

        /// <summary>
        /// Creates an instance of <see cref="ShopItem"/> with the given condition and item.
        /// </summary>
        /// <param name="condition">The condition to check, i.e. <c>() => Main.dayTime;</c></param>
        /// <param name="item">The item that this holds.</param>
        public ShopItem(Condition condition, Item item)
        {
            Condition = condition;
            Item = item;
            Item.buyOnce = true;
        }

        /// <summary>
        /// Creates an instance of <see cref="ShopItem"/>, using the given <paramref name="condition"/> to create a new <see cref="Terraria.Condition"/>.
        /// </summary>
        /// <param name="condition">The condition to check, i.e. <c>() => Main.dayTime;</c>.</param>
        /// <param name="item">The item that this holds.</param>
        public ShopItem(Func<bool> condition, Item item)
        {
            Condition = new(string.Empty, condition);
            Item = item;
            Item.buyOnce = true;
        }
    }

    /// <summary>
    /// Handles updating all loaded StockedShops.
    /// </summary>
    private class StockedShopUpdater : ModSystem
    {
        public override void PostUpdateEverything()
        {
            foreach (var shop in _allShops)
                shop.Update();
        }
    }
}
