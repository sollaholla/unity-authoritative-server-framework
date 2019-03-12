using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AuthoritativeServer.Demo
{
    [CreateAssetMenu(menuName = "Autho Server/Inventory/Create Item Category")]
    public class ItemCategory : ScriptableObject
    {
        [SerializeField]
        private string m_CategoryName;

        /// <summary>
        /// The category name.
        /// </summary>
        public string Name { get { return m_CategoryName; } }
    }
}