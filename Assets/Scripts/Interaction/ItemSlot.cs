using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSlot : MonoBehaviour
{
    [SerializeField] private ItemSlotType slotType;
    [SerializeField] Transform slotTransform;
    public Transform SlotTransform { get { return slotTransform; } }

    public ItemSlotType SlotType { get { return slotType; } }


    [SerializeField] Item equippedItem;
    public Item EquippedItem { get { return equippedItem; } }


    [SerializeField] private CharacterItemManager itemManager;

    public Character SlotCharacter => itemManager.GetComponent<Character>();

    public void EquipItem(ItemData itemData, bool reEquipping = false)
    {
        if (equippedItem == null)
        {
            if (itemData.prefabs.Length < 1) return;
            //Spawn a new item, equip it and initialize
            GameObject newItemGO = Instantiate(itemData.prefabs[UnityEngine.Random.Range(0, itemData.prefabs.Length)], slotTransform.position, slotTransform.rotation);
            newItemGO.transform.parent = slotTransform;

            if (newItemGO.TryGetComponent(out equippedItem))
            {
                equippedItem.Equip(this);
                InitializeEquippedItem();
                
            }
        }
        else
        {
            if (itemData.forceEquipItem && !reEquipping)
            {
                DropUnequipItem();
                EquipItem(itemData, true);
            }
            else
                Debug.LogWarning($"Slot {SlotType} is already occupied!");
        }
    }
    public void UnequipItem()
    {
        if (equippedItem != null)
        {
            if (equippedItem.transform.parent != null)
                equippedItem.transform.parent = null;
            equippedItem = null;
        }
        else
        {
            Debug.LogWarning($"No item to unequip in slot {SlotType}");
        }
    }
    public void DropUnequipItem()
    {
        if (equippedItem != null)
        {
            equippedItem.Drop();
        }
        else
        {
            Debug.LogWarning($"No item to drop and unequip in slop {SlotType}");
        }
    }
    public void InitializeEquippedItem()
    {
        if (equippedItem != null)
        {
            equippedItem.Init();
        }
        else
        {
            Debug.LogWarning($"No equipped item to initialize in slot {SlotType}");
        }
    }

    public bool CanEquipItem(ItemData itemData)
    {
        if (itemData.slotType == SlotType)
            return true;

        return false;
    }
}
