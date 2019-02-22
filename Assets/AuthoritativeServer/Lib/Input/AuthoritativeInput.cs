using System.Collections.Generic;

using UnityEngine;

namespace AuthoritativeServer.Inputs
{
    public abstract class AuthoritativeInput<TInput, TOutput> : NetworkBehaviour where TInput : InputStream, new() where TOutput : InputStream, new()
    {
        public const short ServerSendMsg = -8;
        public const short ClientSendMsg = -9;

        #region FIELDS

        protected TInput m_ClientStream;
        protected TOutput m_ServerStream;
        private List<InputData> m_Replay = new List<InputData>(100);
        private List<InputData> m_Predictions = new List<InputData>(100);

        private bool m_ExecutedInput;
        private InputData m_LastInput;

        #endregion

        #region UNITY

        protected virtual void Awake()
        {
            m_ClientStream = new TInput();
            m_ServerStream = new TOutput();
            RegisterHandlers();
        }

        protected virtual void Update()
        {
            GetInput();
        }

        protected virtual void FixedUpdate()
        {
            Send();

            Collect();
        }

        #endregion

        #region ABSTRACT

        /// <summary>
        /// Execute the given input data.
        /// </summary>
        /// <param name="input"></param>
        protected abstract void ExecuteInput(InputData input);

        /// <summary>
        /// True if the simulation updated successfully. False if we need to do a replay.
        /// </summary>
        /// <param name="serverInput"></param>
        /// <param name="prediction"></param>
        /// <param name="replay"></param>
        /// <returns></returns>
        protected abstract bool UpdateSimulation(InputData serverInput, InputData prediction);

        /// <summary>
        /// Force the client to correct its simulation to match the server input.
        /// </summary>
        /// <param name="serverInput"></param>
        protected abstract void CorrectSimulation(InputData serverInput);

        #endregion

        #region PRIVATE

        private void GetInput()
        {
            if (!IsOwner)
                return;

            if (!m_ExecutedInput)
            {
                if (IsOwner)
                {
                    m_LastInput = m_ClientStream.GetInput(Time.fixedTime);
                }

                m_ExecutedInput = true;
            }
        }

        private void Collect()
        {
            if (!IsServer)
            {
                InputData serverInput = m_ServerStream.ReceiveNext();

                while (serverInput != null)
                {
                    InputData prediction = m_Predictions.Find(x => x.Time == serverInput.Time);

                    List<InputData> replay = m_Replay.FindAll(x => x.Time >= serverInput.Time);

                    if (!UpdateSimulation(serverInput, prediction))
                    {
                        Replay(serverInput, replay);
                    }

                    m_Predictions.RemoveAll(x => x.Time <= serverInput.Time);

                    m_Replay.RemoveAll(x => x.Time <= serverInput.Time);

                    serverInput = m_ServerStream.ReceiveNext();
                }
            }

            if (IsOwner)
            {
                if (m_ExecutedInput)
                {
                    ExecuteInput(m_LastInput);

                    m_Replay.Add(m_LastInput);

                    InputData prediction = m_ServerStream.GetInput(m_LastInput.Time, false);

                    m_Predictions.Add(prediction);

                    m_ExecutedInput = false;
                }
            }

            if (IsServer)
            {
                InputData input = m_ClientStream.ReceiveNext();

                while (input != null)
                {
                    ExecuteInput(input);

                    m_ServerStream.GetInput(input.Time);

                    input = m_ClientStream.ReceiveNext();
                }
            }
        }

        private void Replay(InputData serverInput, List<InputData> replay)
        {
            CorrectSimulation(serverInput);

            for (int i = 0; i < replay.Count; i++)
            {
                InputData replayPoint = replay[i];

                ExecuteInput(replayPoint);

                m_Predictions[i] = m_ServerStream.GetInput(replayPoint.Time, false);
            }
        }

        private void Send()
        {
            if (!IsOwner && !IsServer)
                return;

            if (IsOwner)
            {
                NetworkWriter writer = GetWriter(m_ClientStream);

                if (writer != null)
                {
                    NetworkController.Instance.Send(NetworkController.Instance.ConnectionID, 0, ClientSendMsg, writer.ToArray());
                }
            }

            if (IsServer)
            {
                NetworkWriter writer = GetWriter(m_ServerStream);

                if (writer != null)
                {
                    NetworkController.Instance.SendToAll(0, ServerSendMsg, writer.ToArray());
                }
            }
        }

        private NetworkWriter GetWriter(InputStream stream)
        {
            NetworkWriter writer = new NetworkWriter();

            writer.Write((short)Identity.OwnerConnection.ConnectionID);

            byte[] data = stream.Serialize();

            if (data == null)
                return null;

            writer.WriteBytes(data);

            return writer;
        }

        private static void RegisterHandlers()
        {
            if (NetworkController.Instance.IsServer)
            {
                NetworkController.Instance.RegisterReceiveHandler(ClientSendMsg, OnReceivedClientInput);
            }
            else
            {
                NetworkController.Instance.RegisterReceiveHandler(ServerSendMsg, OnReceivedServerInput);
            }
        }

        private static void OnReceivedServerInput(NetworkWriter writer)
        {
            GetInputComponentFromMessage(writer, out byte[] data)?.m_ServerStream.Deserialize(data);
        }

        private static void OnReceivedClientInput(NetworkWriter writer)
        {
            GetInputComponentFromMessage(writer, out byte[] data)?.m_ClientStream.Deserialize(data);
        }

        private static AuthoritativeInput<TInput, TOutput> GetInputComponentFromMessage(NetworkWriter writer, out byte[] data)
        {
            short conn = writer.ReadInt16();

            NetworkPlayerObject player = NetworkController.Instance.Scene.GetPlayer(conn);

            data = null;

            if (player == null)
                return null;

            GameObject playerObj = player.GameObject;

            data = writer.ReadBytes();

            return playerObj.GetComponent<AuthoritativeInput<TInput, TOutput>>();
        }

        #endregion
    }
}
