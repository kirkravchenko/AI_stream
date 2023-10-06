using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterItemManager : MonoBehaviour
{
    //Manage character items
    //Handle equipping removing and using items

    [SerializeField] Character character;
    [SerializeField] ItemSlot[] itemSlots;
    public PreEquippedItems[] preEquippedItems;
    [System.Serializable]
    public struct PreEquippedItems
    {
        public ItemData itemData;
        public ItemSlotType slotType;
    }

    public bool IsSlotEmpty(ItemSlotType slotType)
    {
        foreach (var slot in itemSlots)
        {
            if (slot.SlotType == slotType)
            {
                if (slot.EquippedItem == null)
                    return true;
                return false;
            }
        }
        return false;
    }
    
    private ItemSlot GetSlot(ItemData itemData)
    {
        foreach (var slot in itemSlots)
        {
            if (slot.SlotType == itemData.slotType)
                return slot;
        }
        return null;
    }
    
    private ItemSlot GetSlot(ItemSlotType slotType)
    {
        foreach (var slot in itemSlots)
        {
            if (slot.SlotType == slotType)
                return slot;
        }
        return null;
    }
    
    public bool IsSlotEmpty(ItemData itemData)
    {
        return IsSlotEmpty(itemData.slotType);
    }
    
    private void Awake()
    {
        if (character == null)
        {
            if (!TryGetComponent(out character))
                Debug.LogError("Missing reference to character script!");
        }
    }
    
    private void Start()
    {
        //check if we have any items pre-equipped
        //then init them

        foreach (var preEquippedItem in preEquippedItems)
        {
            ItemSlot targetSlot = GetSlot(preEquippedItem.slotType);
            targetSlot.EquipItem(preEquippedItem.itemData);
        }
    }

    public void EquipItem(ItemData itemData)
    {
        if (itemSlots.Length > 0)
        {
            foreach (var slot in itemSlots)
            {
                if (slot.CanEquipItem(itemData))
                {
                    slot.EquipItem(itemData);
                    break;
                }
                
            }
        }    
    }
    
    public void UnequipItem(ItemData itemData)
    {
        if (itemSlots.Length > 0)
        {
            ItemSlot slot = GetSlot(itemData);
            if (slot != null)
            {
                //if slot is not empty, unequip
                if (!IsSlotEmpty(slot.SlotType))
                    slot.DropUnequipItem();
            }
        }
    }

    // Display Slots as spheres in the editor
#if true
    private void OnDrawGizmos()
    {
        foreach (var slot in itemSlots)
        {
            DisplaySlotState(slot);
        }
    }
    private void DisplaySlotState(ItemSlot slot)
    {
        if (slot.SlotTransform == null) return;
        Gizmos.color = slot.EquippedItem ? Color.red : Color.blue;
        Gizmos.DrawSphere(slot.SlotTransform.position, 0.075f);
    }
#endif
}
