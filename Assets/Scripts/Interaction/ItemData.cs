using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item",menuName ="AiSponge/Item/New Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public ItemSlotType slotType;
    public bool forceEquipItem;
    public bool equippedByDefault;
    public string[] itemKeywords;
    [Space(5)]
    public GameObject[] prefabs;
    public Material textureMaterial;
}
