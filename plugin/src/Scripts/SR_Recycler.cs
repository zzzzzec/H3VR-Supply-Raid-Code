using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using static FistVR.ItemSpawnerCategoryDefinitions;

namespace SupplyRaid
{
    public class SR_Recycler : MonoBehaviour
    {
        private List<FVRFireArm> weapons = new List<FVRFireArm>();

        public Transform ScanningVolume;
        private float m_scanTick = 0.8f;
        private Collider[] colbuffer;
        public LayerMask ScanningLM;
        private List<GameObject> cashList = new List<GameObject>();
        public Transform selectedBox;

        private void Start()
        {
            colbuffer = new Collider[50];
        }

        /// <summary>
        /// 尝试根据武器ItemID获取其回收价值
        /// </summary>
        /// <param name="weaponItemID">武器的ItemID</param>
        /// <param name="recycleValue">计算出的回收点数（输出参数）</param>
        /// <returns>如果成功找到价格并计算了价值，则返回true</returns>
        private bool TryGetRecycleValue(string weaponItemID, out int recycleValue)
        {
            recycleValue = 0;
            string weaponGroupName = null;
            // --- 步骤 1: 根据 ItemID 找到它在商店分类中的组名 (Group Name) ---
            foreach (var category in SR_Manager.instance.itemCategories)
            {
                if (category.name.Contains("shop") && !category.name.Contains("shop_weapons_all"))
                {
                    foreach (var group in category.objectGroups)
                    {
                        if (group.objectID.Contains(weaponItemID))
                        {
                            weaponGroupName = category.name;
                            break;
                        }
                    }
                }
                if (weaponGroupName != null)
                {
                    break; 
                }
            }
            // 如果遍历完所有分类都没找到这个武器，说明它不可回收，查找失败
            if (weaponGroupName == null)
            {
                Debug.LogError($"[SR_Recycler] Found weapon group '{weaponGroupName}' but it has not in itemCategories!");
                return false;
            }
            // --- 步骤 2: 根据组名去角色的购买列表中查找价格 ---
            var purchaseCategories = SR_Manager.instance.character.purchaseCategories;
            foreach (var purchase in purchaseCategories)
            {
                if (purchase.itemCategory.Contains(weaponGroupName))
                {
                    // 找到了对应的购买项，计算回收价格（30%）
                    recycleValue = Mathf.RoundToInt(purchase.cost * 0.3f) + 1;
                    Debug.Log($"[SR_Recycler] Found weapon group '{weaponGroupName}', '{purchase.cost}' -> '{recycleValue}' ");
                    return true; // 成功找到价格并计算完毕
                }
            }
            // 程序走到这里，说明在商店里找到了这个武器的组，但在价格列表里没有对应的项
            // 这通常是数据配置问题，我们打印一个日志方便调试，并返回失败
            Debug.LogError($"[SR_Recycler] Found weapon group '{weaponGroupName}' but it has no price entry!");
            return false;
        }

        public void Button_Recycler()
        {
            bool ignoreFail = false;
            if(cashList.Count > 0)
                SR_Manager.PlayPointsGainSFX();
            for (int i = 0; i < cashList.Count; i++)
            {
                if (cashList[i] == null)
                    continue;

                int cash = GetCashValue(cashList[i].name);
                SR_Manager.instance.Points += cash;
                Destroy(cashList[i]);
                ignoreFail = true;
            }

            if (weapons.Count <= 0)
            {
                if(!ignoreFail)
                    SR_Manager.PlayFailSFX();
                return;
            }
            //  ObjectID 可以直接反向查找到价格，3折卖出即可
            // "M4A1Block2CQBR(Clone) (FistVR.ClosedBoltWeapon)"
            // weapons[0].ObjectWrapper
            // "PP19Vityaz(Clone) (FistVR.ClosedBoltWeapon)"
            // weapons[0].ObjectWrapper.ItemID 
            // 先找到是哪个 item cat，然后找 buy 里面的cat 的价格
            if (weapons[0] != null && weapons[0].gameObject != null)
            {
                // 调用我们封装好的函数来获取回收点数
                if (TryGetRecycleValue(weapons[0].ObjectWrapper.ItemID, out int pointsToAdd))
                {
                    // 如果成功获取，就增加对应点数
                    SR_Manager.instance.Points += pointsToAdd;
                    Debug.Log($"Recycled {weapons[0].ObjectWrapper.ItemID} for {pointsToAdd} points.");
                }
                else
                {
                    // 如果失败（例如该武器不在商店列表），则增加默认的保底点数
                    SR_Manager.instance.Points += SR_Manager.instance.character.recyclerPoints;
                    Debug.LogWarning($"Could not find price for {weapons[0].ObjectWrapper.ItemID}. Awarding default points.");
                }
                Destroy(weapons[0].gameObject);
                ignoreFail = true;
            }
            weapons.Clear();
            if (ignoreFail)
                SR_Manager.PlayPointsGainSFX();
        }

        private void Update()
        {
            m_scanTick -= Time.deltaTime;
            if (m_scanTick <= 0f)
            {
                m_scanTick = Random.Range(0.6f, 0.8f);
                float num = Vector3.Distance(transform.position, GM.CurrentPlayerBody.transform.position);
                if (num < 12f)
                {
                    Scan();
                }
            }

            //Selection Box
            if (weapons.Count > 0 && weapons[0] && weapons[0].GameObject)
            {
                Transform target = weapons[0].PoseOverride ? weapons[0].PoseOverride : weapons[0].transform;
                selectedBox.position = target.position;
                selectedBox.rotation = target.rotation;
                selectedBox.localScale = target.localScale;
            }
        }

        int GetCashValue(string itemName)
        {
            switch (itemName)
            {
                case "CharcoalBriquette(Clone)":
                    return SR_Manager.instance.character.recyclerTokens;
                case "Ammo_69_CashMoney_D1(Clone)":
                    return 1;
                case "Ammo_69_CashMoney_D5(Clone)":
                    return 5;
                case "Ammo_69_CashMoney_D10(Clone)":
                    return 10;
                case "Ammo_69_CashMoney_D25(Clone)":
                    return 25;
                case "Ammo_69_CashMoney_D100(Clone)":
                    return 100;
                case "Ammo_69_CashMoney_D1000(Clone)":
                    return 1000;
                default:
                    return 0;
            }
        }

        private void Scan()
        {
            int num = Physics.OverlapBoxNonAlloc(
                ScanningVolume.position, 
                ScanningVolume.localScale * 0.5f, 
                colbuffer, 
                ScanningVolume.rotation, 
                ScanningLM, QueryTriggerInteraction.Collide);
            weapons.Clear();
            cashList.Clear();


            for (int i = 0; i < num; i++)
            {
                switch (colbuffer[i].name)
                {
                    default:
                        break;
                    case "CharcoalBriquette(Clone)":
                    case "Ammo_69_CashMoney_D1(Clone)":
                    case "Ammo_69_CashMoney_D5(Clone)":
                    case "Ammo_69_CashMoney_D10(Clone)":
                    case "Ammo_69_CashMoney_D25(Clone)":
                    case "Ammo_69_CashMoney_D100(Clone)":
                    case "Ammo_69_CashMoney_D1000(Clone)":
                        cashList.Add(colbuffer[i].gameObject);
                        break;
                }

                if (colbuffer[i].attachedRigidbody != null)
                {

                    FVRFireArm component = colbuffer[i].attachedRigidbody.gameObject.GetComponent<FVRFireArm>();
                    if (component != null)
                    {
                        if (!component.SpawnLockable)
                        {
                            if (!component.IsHeld && component.QuickbeltSlot == null && !weapons.Contains(component))
                            {
                                weapons.Add(component);
                            }
                        }
                    }
                }
            }

            if (weapons.Count > 0 && weapons[0] != null)
            {
                selectedBox.gameObject.SetActive(true);
                selectedBox.position = weapons[0].transform.position;
            }
            else
            {
                selectedBox.gameObject.SetActive(false);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(ScanningVolume.position, ScanningVolume.localScale * 0.5f);
        }
    }
}
