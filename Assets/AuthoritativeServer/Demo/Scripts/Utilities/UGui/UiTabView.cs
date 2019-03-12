using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AuthoritativeServer.Demo
{
    [AddComponentMenu("Autho Server/Demo/UI/UI Tab View")]
    public class UiTabView : UiWindow
    {
        [SerializeField]
        private TabView[] m_TabViews;

        protected override void Start()
        {
            base.Start();

            foreach (TabView view in m_TabViews)
            {
                view.AddListener(TabOpened);

                if (view == m_TabViews[0])
                    continue;

                view.Close();
            }
        }

        private void TabOpened(TabView tab)
        {
            tab.Open();

            foreach (TabView t in m_TabViews)
            {
                if (t == tab)
                    continue;

                t.Close();
            }
        }
    }

    /// <summary>
    /// A window tab.
    /// </summary>
    [System.Serializable]
    public class TabView
    {
        [SerializeField]
        private UiWindow m_Window;
        [SerializeField]
        private Button m_Button;

        public void AddListener(UnityAction<TabView> action)
        {
            m_Button.onClick.AddListener(() => action.Invoke(this));
        }

        /// <summary>
        /// Open the window associated.
        /// </summary>
        public void Open()
        {
            m_Window.Open();
        }

        /// <summary>
        /// Close the window associated.
        /// </summary>
        public void Close()
        {
            m_Window.Close();
        }
    }
}
