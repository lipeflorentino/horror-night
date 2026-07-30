using UnityEngine;

public class CombatInventoryInitializer
{
    public static ICombatInventory BuildCombatInventory(CombatSessionData sessionData, Battler player)
    {
        ItemDatabase itemDatabase = Object.FindObjectOfType<ItemDatabase>();
        PlayerInventorySnapshot snapshot = null;
        if (sessionData?.PlayerSnapshot != null)
            snapshot = sessionData.PlayerSnapshot.inventory;

        PlayerInventory inventory = Object.FindObjectOfType<PlayerInventory>();
        if (snapshot == null && inventory != null)
            snapshot = inventory.GetSnapshot();

        if (itemDatabase != null)
            return new CombatInventory(player, itemDatabase, snapshot ?? new PlayerInventorySnapshot());

        if (inventory != null && snapshot != null)
            inventory.RestoreSnapshot(snapshot);

        return inventory;
    }

    public static ITrickInventory BuildPlayerTrickInventory(Battler owner, CombatSessionData sessionData, PerkService perkService)
    {
        TrickDatabase trickDatabase = TrickDatabase.GetOrCreateRuntimeDatabase();
        TrickInventorySnapshot snapshot = sessionData?.PlayerSnapshot.trickInventory;
        return new TrickInventory(owner, trickDatabase, snapshot, TrickInventory.DefaultIdentitySlotCount, TrickInventory.DefaultActiveCastedSlotCount, TrickInventory.DefaultPassiveCastedSlotCount, perkService);
    }

    public static ITrickInventory BuildEnemyTrickInventory(Battler owner, CombatSessionData sessionData, PerkService perkService)
    {
        TrickDatabase trickDatabase = TrickDatabase.GetOrCreateRuntimeDatabase();
        TrickInventorySnapshot snapshot = null;

        if (sessionData?.EnemyInstance?.source != null)
        {
            snapshot = sessionData.EnemyInstance.source.GetTrickInventorySnapshot();
        }

        return new TrickInventory(owner, trickDatabase, snapshot, TrickInventory.DefaultIdentitySlotCount, TrickInventory.DefaultActiveCastedSlotCount, TrickInventory.DefaultPassiveCastedSlotCount, perkService);
    }

    public static void ActivatePlayerIdentityTricks(Battler player, ITrickInventory playerTrickInventory, TrickService trickService)
    {
        if (player == null || playerTrickInventory?.IdentitySlots == null)
            return;

        for (int i = 0; i < playerTrickInventory.IdentitySlots.Count; i++)
        {
            TrickRuntimeInstance instance = playerTrickInventory.IdentitySlots[i]?.RuntimeInstance;
            if (instance?.Definition != null)
            {
                trickService.ApplyTrick(player, instance, player);
            }
        }
    }

    public static void ActivateEnemyIdentityTricks(Battler enemy, ITrickInventory enemyTrickInventory, TrickService trickService)
    {
        if (enemy == null || enemyTrickInventory?.IdentitySlots == null)
            return;

        for (int i = 0; i < enemyTrickInventory.IdentitySlots.Count; i++)
        {
            TrickRuntimeInstance instance = enemyTrickInventory.IdentitySlots[i]?.RuntimeInstance;
            if (instance?.Definition != null)
            {
                trickService.ApplyTrick(enemy, instance, enemy);
            }
        }
    }
}
