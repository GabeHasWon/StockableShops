﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace StockableShops.Stock;

internal class StockHooks : ModSystem
{
    /// <summary>
    /// decrease the stocked stack when buying an item
    /// </summary>
    private static void OnBoughtInShopHook(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(ItemSlot), nameof(ItemSlot.RefreshStackSplitCooldown)));
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdelemRef();
        cursor.EmitLdloc(5);    // boughtItem
        cursor.EmitDelegate(OnBoughtInShopInner);
    }
    /// <summary>
    /// <br/>modify item in shop when bought
    /// <br/>called on every single buy
    /// </summary>
    private static void OnBoughtInShopInner(Item itemInShop, Item boughtItem)
    {
        var stockedItem = itemInShop.GetGlobalItem<StockedItem>();
        if (!stockedItem.Stockable)
            return;
        stockedItem.Stack -= 1;
        boughtItem.GetGlobalItem<StockedItem>().Stockable = false;
    }

    /// <summary>
    /// increase the stocked stack when selling back
    /// </summary>
    private static void SellbackItemHook(ILContext il)
    {
        ILCursor cursor = new(il);
        cursor.GotoNext(MoveType.After, i => i.MatchStloc0());
        cursor.EmitLdarg0();
        cursor.EmitLdarg1();
        cursor.EmitLdloc0();
        cursor.EmitDelegate(HandleShopSellBackNum);
    }
    private static void HandleShopSellBackNum(Chest chest, Item newItem, int removed)
    {
        if (removed <= 0)
        {
            return;
        }
        for (int i = 0; i < chest.item.Length; ++i)
        {
            if (chest.item[i] != null && chest.item[i].type == newItem.type)
            {
                var stockedItem = chest.item[i].GetGlobalItem<StockedItem>();
                if (!stockedItem.Stockable)
                {
                    continue;
                }
                stockedItem.Stack += removed;
                return;
            }
        }
    }

    /// <summary>
    /// make an item unavailable to buy when stocked stack is zero
    /// </summary>
    private class BuyLimitForPlayer : ModPlayer
    {
        public override bool CanBuyItem(NPC vendor, Item[] shopInventory, Item item)
        {
            var stockedItem = item.GetGlobalItem<StockedItem>();
            return !stockedItem.Stockable || stockedItem.Stack > 0;
        }
    }

    /// <summary>
    /// draw stocked stack when in a shop
    /// </summary>
    private static void DrawStockedStackHook(ILContext il)
    {
        static bool MatchChatManagerDrawColorString(Instruction i)
        {
            return i.MatchCall(typeof(ChatManager), nameof(ChatManager.DrawColorCodedStringWithShadow));
        }
        ILCursor cursor = new(il);
        cursor.GotoNext(
            i => i.MatchLdloc1(),
            i => i.MatchLdfld(typeof(Item), nameof(Item.stack)),
            i => i.MatchLdcI4(1),
            i => i.MatchBle(out _)
        );
        cursor.GotoNext(MatchChatManagerDrawColorString);
        cursor.GotoPrev(MoveType.AfterLabel,
            i => i.MatchLdloc1(),
            i => i.MatchLdfld(typeof(Item), nameof(Item.stack)),
            i => i.MatchLdcI4(1),
            i => i.MatchBle(out _)
        );
        // cursor now should be before "if (stack > 1)"

        // cursor2 moves after "if (stack > 1)"
        ILCursor cursor2 = new(cursor);
        cursor2.GotoNext(MoveType.After, i => i.MatchBle(out _));

        // if the item is stockable, jump the stack check to force draw the stack
        cursor.EmitLdloc1();
        cursor.EmitDelegate((Item i) => i.GetGlobalItem<StockedItem>().Stockable);
        cursor.EmitBrtrue(cursor2.Next!);

        // cursor moves after the check
        cursor = cursor2;
        cursor.MoveAfterLabels();

        var origStack = DeclareLocal<int>(il);

        // temporarily change the item stack into stocked stack
        cursor.EmitLdloc1();
        cursor.EmitDelegate((Item i) =>
        {
            var stack = i.stack;
            var stockedItem = i.GetGlobalItem<StockedItem>();
            if (stockedItem.Stockable)
                i.stack = stockedItem.Stack;
            return stack;
        });
        cursor.EmitStloc(origStack);

        // revert the stack after drawing
        cursor.GotoNext(MoveType.After, MatchChatManagerDrawColorString);
        cursor.EmitLdloc1();
        cursor.EmitLdloc(origStack);
        cursor.EmitStfld(typeof(Item).GetField(nameof(Item.stack))!);
    }

    public override void Load()
    {
        IL_ItemSlot.HandleShopSlot += OnBoughtInShopHook;
        IL_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += DrawStockedStackHook;
        IL_Chest.AddItemToShop += SellbackItemHook;
    }

    /// <inheritdoc cref="DeclareLocal(ILContext, Type)"/>
    private static VariableDefinition DeclareLocal<T>(ILContext il) => DeclareLocal(il, typeof(T));
    /// <summary>
    /// add a new local variable to a method in IL
    /// </summary>
    /// <param name="il">the method's <see cref="ILContext"/></param>
    /// <param name="type">the type of variable</param>
    /// <returns>the variable definition</returns>
    private static VariableDefinition DeclareLocal(ILContext il, Type type)
    {
        VariableDefinition result = new(il.Method.DeclaringType.Module.ImportReference(type));
        il.Body.Variables.Add(result);
        return result;
    }
}
