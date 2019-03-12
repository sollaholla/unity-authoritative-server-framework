using System;
using System.Collections.Generic;
using UnityEngine;

namespace AuthoritativeServer.Demo
{
    [System.Serializable]
    public class InventoryItemTypeMask
    {
        [SerializeField]
        private List<string> m_Types;

        [SerializeField]
        private int m_Mask;

        /// <summary>
        /// The allowed types.
        /// </summary>
        public List<string> AllowedTypes {
            get { return m_Types; }
        }

        /// <summary>
        /// The amount of allowed types.
        /// </summary>
        public int Count { get { return m_Types.Count; } }

        /// <summary>
        /// True if the type name is contained in the definitions.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public bool Contains(string typeName)
        {
            return m_Types.Contains(typeName);
        }
    }

    [System.Serializable]
    public class ItemCategoryTypeMask
    {
        [SerializeField]
        private List<ItemCategory> m_Categories;

        [SerializeField]
        private int m_Mask;

        /// <summary>
        /// The allowed types.
        /// </summary>
        public List<ItemCategory> AllowedCategories {
            get { return m_Categories; }
        }

        /// <summary>
        /// The amount of allowed types.
        /// </summary>
        public int Count { get { return m_Categories.Count; } }

        /// <summary>
        /// True if the type name is contained in the definitions.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public bool Contains(ItemCategory category)
        {
            return m_Categories.Contains(category);
        }
    }
}
