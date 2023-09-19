using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class Item : MonoBehaviour
{
    public ItemData data;
    public abstract void Init();
    public abstract void Equip(ItemSlot slot);

    public abstract void Unequip();

    public virtual void Drop()
    {
        Unequip();
    }
}
