using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shovel : WeaponBase, IEquipable, IItemDataBase
{
    ItemDB shovelData;

    uint damage = 0;
    public uint Damage => damage;

    float weight;
    public float Weight => weight;

    MeshCollider mesh;

    private void Awake()
    {
        mesh = GetComponent<MeshCollider>();
    }
    private void Start()
    {
        shovelData = GameManager.Instance.ItemData.GetItemDB(ItemCode.Shovel);
        weight = shovelData.weight;
        damage = shovelData.damage;
    }

    private void OnEnable()
    {

    }

    public void Equip()
    {

    }

    public void Use()
    {
        
    }

    /// <summary>
    /// 애니메이션 트리거용, 플레이어에게 델리게이트 받아서 사용
    /// </summary>
    void OnColliderEnable(bool isEnabled)
    {
        mesh.enabled = isEnabled;
    }

    public ItemDB GetItemDB()
    {
        return shovelData;
    }
}
