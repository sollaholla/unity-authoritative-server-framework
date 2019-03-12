using System;
using System.Text;
using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace AuthoritativeServer.Demo
{
    public class ItemHoverInterface : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text m_NameText;
        [SerializeField]
        private TMP_Text m_DescriptionText;
        [SerializeField]
        private Image m_IconImage;

        private bool m_IsVisible = true;
        private Vector2 m_DrawPosition;
        private bool m_IsDrawing;

        private static ItemHoverInterface m_Instance;

        private void Awake()
        {
            if (m_Instance == null)
            {
                m_Instance = this;
            }
            else if (m_Instance != this)
            {
                Destroy(gameObject);
            }

            SetVisible(false);
        }

        private void OnDisable()
        {
            m_IsDrawing = false;
            SetVisible(false);
        }

        private void Update()
        {
            if (m_IsDrawing)
            {
                transform.position = m_DrawPosition;
            }
            else
            {
                SetVisible(false);
            }

            m_IsDrawing = false;
        }

        private void SetVisible(bool visible)
        {
            if (m_IsVisible && visible)
                return;

            if (!m_IsVisible && !visible)
                return;

            foreach (Transform t in transform)
            {
                if (t == transform) continue;
                t.gameObject.SetActive(visible);
            }

            Image img = GetComponent<Image>();
            img.enabled = visible;

            m_IsVisible = visible;
        }

        private void Repaint(Vector2 position, InventoryItem item)
        {
            transform.position = position;
            m_DescriptionText.text = item.Description;
            m_IconImage.sprite = item.Icon;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine();
            builder.AppendLine("<align=left>");
            for (int i = 0; i < item.Stats.Length; i++)
            {
                InventoryItemStatInstance inst = item.Stats[i];
                switch (inst.Type)
                {
                    case StatType.Fixed:
                        builder.AppendLine(inst.Name + ": " + inst.Value + "/" + inst.MaxValue);
                        break;
                    case StatType.Add:
                        builder.AppendLine((inst.Value >= 0 ? "<color=green>+" : "<color=red>-") + inst.Value + " " + inst.Name + "</color>");
                        break;
                }
            }
            m_DescriptionText.text += builder.ToString();
            m_NameText.text = item.Name;
            m_DrawPosition = position;
            SetVisible(true);
        }

        /// <summary>
        /// Show an item hover interface at the specified position.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="item"></param>
        public static void Show(Vector2 position, InventoryItem item)
        {
            if (!m_Instance.m_IsDrawing)
                m_Instance.Repaint(position, item);
            else m_Instance.m_DrawPosition = position;
            m_Instance.m_IsDrawing = true;
        }
    }
}
