using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AuthoritativeServer.Demo
{
    [CreateAssetMenu(menuName = "Autho Server/Inventory/Create Blueprint")]
    public class ItemBlueprint : ScriptableObject
    {
        [SerializeField]
        private string m_BlueprintName;

        [SerializeField]
        private BlueprintInput[] m_Inputs;

        [SerializeField]
        private InventoryItem m_Output;

        public BlueprintInput[] Inputs {
            get { return m_Inputs; }
        }

        public InventoryItem Output {
            get { return m_Output; }
        }

        private Dictionary<string, int> m_InputCache;
        private static Dictionary<string, int> m_CombinedItems;

        /// <summary>
        /// True if the items given is matched by the blueprint inputs.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public bool Matches(InventoryItem[] items)
        {
            if (items.Count(x => x != null) != Inputs.Length)
                return false;

            BuildDictionary();

            m_CombinedItems = new Dictionary<string, int>();
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                    continue;

                if (m_CombinedItems.TryGetValue(items[i].ItemID, out int value))
                {
                    m_CombinedItems[items[i].ItemID] += items[i].Stack;
                }
                else
                {
                    m_CombinedItems[items[i].ItemID] = items[i].Stack;
                }
            }

            for (int i = 0; i < items.Length; i++)
            {
                InventoryItem item = items[i];

                if (item == null)
                    continue;

                try
                {
                    if (m_CombinedItems[item.ItemID] != m_InputCache[item.ItemID])
                        return false;
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }

        private void BuildDictionary()
        {
            if (m_InputCache == null)
            {
                m_InputCache = new Dictionary<string, int>();
                foreach (BlueprintInput input in m_Inputs)
                {
                    m_InputCache[input.Item.ItemID] = input.RequiredAmount;
                }
            }
        }
    }

    [System.Serializable]
    public class BlueprintInput
    {
        [SerializeField]
        private InventoryItem m_Item;

        [SerializeField]
        private int m_RequiredAmount;

        public InventoryItem Item { get { return m_Item; } }
        public int RequiredAmount { get { return m_RequiredAmount; } }
    }
}
