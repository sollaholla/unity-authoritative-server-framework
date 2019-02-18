using System;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace AuthoritativeServer
{
    /// <summary>
    /// A basic UI controller to connect as a server or a client.
    /// </summary>
    [AddComponentMenu("Autho Server/Interface/Network Controller UI")]
    public class NetworkControllerUI : MonoBehaviour
    {
        #region INSPECTOR

        [SerializeField]
        private Button m_ClientConnectButton;
        [SerializeField]
        private Button m_ServerConnectButton;
        [SerializeField]
        private Button m_DisconnectButton;
        [SerializeField]
        private TMP_InputField m_ServerIPInputField;
        [SerializeField]
        private TMP_Text m_Log;

        #endregion

        #region UNITY

        private void Start()
        {
            UpdateUIDisconnected();
        }

        private void OnEnable()
        {
            m_ClientConnectButton.onClick.AddListener(OnClientConnect);
            m_ServerConnectButton.onClick.AddListener(OnServerConnect);
            m_DisconnectButton.onClick.AddListener(OnDisconnect);

            NetworkController.ClientConnectionEstablished += OnConnectionEstablished;
            NetworkController.ClientDisconnected += OnClientDisconnected;
            NetworkController.ServerStarted += OnServerStarted;
            NetworkController.ServerStopped += OnServerStopped;
            NetworkController.Log += OnLog;
        }

        private void OnDisable()
        {
            m_ClientConnectButton.onClick.RemoveListener(OnClientConnect);
            m_ServerConnectButton.onClick.RemoveListener(OnServerConnect);
            m_DisconnectButton.onClick.RemoveListener(OnDisconnect);

            NetworkController.ClientConnectionEstablished -= OnConnectionEstablished;
            NetworkController.ClientDisconnected -= OnClientDisconnected;
            NetworkController.ServerStarted -= OnServerStarted;
            NetworkController.ServerStopped -= OnServerStopped;
            NetworkController.Log -= OnLog;
        }

        #endregion

        #region PRIVATE

        private void OnLog(string val, LogLevel level)
        {
            m_Log.text += val + Environment.NewLine;
        }

        private void OnServerStarted()
        {
            UpdateUIConnected();
        }

        private void OnServerStopped()
        {
            UpdateUIDisconnected();
        }

        private void OnConnectionEstablished(NetworkConnection conn)
        {
            UpdateUIConnected();
        }

        private void OnClientDisconnected()
        {
            UpdateUIDisconnected();
        }

        private void UpdateUIConnected()
        {
            m_ClientConnectButton.interactable = false;
            m_ServerConnectButton.interactable = false;
            m_DisconnectButton.interactable = true;
            m_ServerIPInputField.interactable = false;
        }

        private void UpdateUIDisconnected()
        {
            m_ClientConnectButton.interactable = true;
            m_ServerConnectButton.interactable = true;
            m_DisconnectButton.interactable = false;
            m_ServerIPInputField.interactable = true;
        }

        private void OnDisconnect()
        {
            if (!NetworkController.Instance.IsConnected)
                return;

            NetworkController.Instance.Disconnect();
        }

        private void OnServerConnect()
        {
            if (string.IsNullOrEmpty(m_ServerIPInputField.text))
                return;

            NetworkController.Instance.ConnectAsServer();
        }

        private void OnClientConnect()
        {
            if (string.IsNullOrEmpty(m_ServerIPInputField.text))
                return;

            NetworkController.Instance.ConnectAsClient(m_ServerIPInputField.text);
        }

        #endregion
    }
}
