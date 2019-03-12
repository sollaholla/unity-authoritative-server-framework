using System;
using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// A simple network chatting system.
    /// </summary>
    public class NetworkChat : NetworkBehaviour
    {
        const int ChatMessage = 9991;

        #region INSPECTOR

        [SerializeField] private TMP_InputField m_ChatInputField;
        [SerializeField] private Button m_SubmitButton;
        [SerializeField] private TMP_Text m_ChatLog;

        #endregion

        #region FIELDS

        private static NetworkChat m_Instance;

        #endregion

        #region UNITY

        private void Awake()
        {
            if (m_Instance == null)
            {
                m_Instance = this;
            }
            else if (m_Instance != this)
            {
                Destroy(this.gameObject);
            }

            RegisterHandlers();
        }

        private void OnEnable()
        {
            m_ChatInputField.onSubmit.AddListener(OnChatSubmit);

            m_SubmitButton.onClick.AddListener(OnChatSubmitButton);
        }

        private void OnDisable()
        {
            m_ChatInputField.onSubmit.RemoveListener(OnChatSubmit);

            m_SubmitButton.onClick.RemoveListener(OnChatSubmitButton);
        }

        #endregion

        #region PRIVATE

        private void OnChatSubmitButton()
        {
            OnChatSubmit(m_ChatInputField.text);
        }

        private void OnChatSubmit(string message)
        {
            if (string.IsNullOrEmpty(m_ChatInputField.text))
                return;

            NetworkWriter writer = new NetworkWriter();
            writer.Write("User " + NetworkController.Instance.LocalConnectionID);
            writer.Write(message + Environment.NewLine);

            NetworkController.Instance.Send(NetworkController.Instance.ConnectionID, NetworkController.ReliableChannel, ChatMessage, writer.ToArray());

            m_ChatInputField.text = string.Empty;
        }

        private static void RegisterHandlers()
        {
            NetworkController.Instance.RegisterReceiveHandler(ChatMessage, OnChatMessage);
        }

        private static void OnChatMessage(NetworkWriter writer)
        {
            m_Instance.Chatted(writer);
        }

        private void Chatted(NetworkWriter writer)
        {
            if (IsServer)
            {
                NetworkController.Instance.SendToAll(NetworkController.ReliableChannel, ChatMessage, writer.ToArray());
                return;
            }

            string senderName = writer.ReadString();
            string message = writer.ReadString();
            m_ChatLog.text += senderName + ": " + message;
        }

        #endregion
    }
}
