using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using static AuthoritativeServer.Demo.Utilities;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// Window allows open and closing using keys and window event callbacks.
    /// </summary>
    [AddComponentMenu("Autho Server/Demo/UI/UI Window")]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class UiWindow : MonoBehaviour
    {
        /// <summary>
        /// An event that is called when input needs to be blocked. Bool value returns true if input is allowed and false otherwise.
        /// </summary>
        public static event Action<bool> InputStateChanged;

        #region INSPECTOR

        [SerializeField]
        private bool m_ResetPositionOnStart;
        [SerializeField]
        private bool m_CloseOnStart = true;
        [SerializeField]
        private bool m_BlocksInput = true;
        [SerializeField]
        private bool m_TogglesChildren = true;
        [SerializeField]
        private KeyCode m_OpenKey = KeyCode.None;
        [SerializeField]
        private string m_OpenButton;

        public UnityEvent Opened;
        public UnityEvent Closed;

        #endregion

        #region FIELDS

        private static bool m_IsBlockingInput;
        private static List<UiWindow> m_Windows;

        private bool m_IsOpen = true;
        private Image m_Image;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// True if this window is open.
        /// </summary>
        public bool IsOpen {
            get { return m_IsOpen; }
            set {
                if (m_IsOpen && !value)
                {
                    Closed?.Invoke();
                }

                if (!m_IsOpen && value)
                {
                    Opened?.Invoke();
                }

                m_IsOpen = value;
            }
        }

        #endregion

        #region UNITY

        protected virtual void Awake()
        {
            if (m_ResetPositionOnStart)
            {
                RectTransform t = GetComponent<RectTransform>();
                t.anchoredPosition = Vector2.zero;
            }

            m_Image = GetComponent<Image>();

            if (m_Windows == null)
                m_Windows = new List<UiWindow>();

            m_Windows.Add(this);
        }

        protected virtual void Start()
        {
            if (m_CloseOnStart)
            {
                Close();
            }
        }

        protected virtual void Update()
        {
            if (IsInputInUse())
                return;

            if (m_OpenKey != KeyCode.None)
            {
                if (Input.GetKeyDown(m_OpenKey))
                {
                    Toggle();
                }
            }

            if (!string.IsNullOrEmpty(m_OpenButton))
            {
                if (Input.GetButtonDown(m_OpenButton))
                {
                    Toggle();
                }
            }
        }

        #endregion

        #region PUBLIC

        /// <summary>
        /// Opens this window.
        /// </summary>
        public void Open()
        {
            if (IsOpen)
                return;

            IsOpen = true;

            if (m_TogglesChildren)
            {
                UiWindow[] windows = GetComponentsInChildren<UiWindow>(true);

                foreach (UiWindow window in windows)
                {
                    if (window == this)
                        continue;

                    window.Open();
                }
            }

            if (m_Image != null)
            {
                m_Image.enabled = true;
            }

            foreach (Transform t in transform)
            {
                if (t == transform)
                    continue;

                t.gameObject.SetActive(true);
            }

            if (m_BlocksInput && !m_IsBlockingInput)
            {
                m_IsBlockingInput = true;
                InputStateChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Closes this window.
        /// </summary>
        public void Close()
        {
            if (!IsOpen)
                return;

            IsOpen = false;

            if (m_TogglesChildren)
            {
                UiWindow[] windows = GetComponentsInChildren<UiWindow>(true);

                foreach (UiWindow window in windows)
                {
                    if (window == this)
                        continue;

                    window.Close();
                }
            }

            if (m_Image != null)
            {
                m_Image.enabled = false;
            }

            foreach (Transform t in transform)
            {
                if (t == transform)
                    continue;

                t.gameObject.SetActive(false);
            }

            if (!ShouldBlockInput() && m_IsBlockingInput)
            {
                m_IsBlockingInput = false;
                InputStateChanged?.Invoke(true);
            }
        }

        #endregion

        #region PRIVATE

        private void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private static bool ShouldBlockInput()
        {
            foreach (UiWindow window in m_Windows)
            {
                if (window == null)
                    continue;

                if (window.IsOpen && window.m_BlocksInput)
                    return true;
            }

            return false;
        }

        #endregion
    }
}
