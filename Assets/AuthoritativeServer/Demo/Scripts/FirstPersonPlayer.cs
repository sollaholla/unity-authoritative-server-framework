using AuthoritativeServer.Inputs;
using UnityEngine;
using UnityEngine.Networking;

namespace AuthoritativeServer.Demo
{
    [RequireComponent(typeof(CharacterMotor))]
    public class FirstPersonPlayer : AuthoritativeInput<FirstPersonInput, FirstPersonOutput>
    {
        [SerializeField]
        private FirstPersonCamera m_FirstPersonCamera;

        private CharacterMotor m_Motor;

        private Quaternion m_LastRotation;
        private float m_LastAngle;
        private Vector3 m_LastPosition;
        private float m_LastDistance;
        private bool m_LastGrounded;
        private bool m_Sync;

        protected override void Awake()
        {
            base.Awake();

            m_ServerStream.Initialize(transform);
            m_ClientStream.Initialize(transform);

            m_Motor = GetComponent<CharacterMotor>();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            SmoothObservation();
        }

        private void SmoothObservation()
        {
            if (!IsServer && !IsOwner && m_Sync)
            {
                transform.position = Vector3.MoveTowards(transform.position, m_LastPosition, m_LastDistance * (1.0f / NetworkController.Instance.Settings.m_SendDelay) * 2f);

                transform.rotation = Quaternion.RotateTowards(transform.rotation, m_LastRotation, m_LastAngle * (1.0f / NetworkController.Instance.Settings.m_SendDelay) * 2f);

                m_Motor.Simulate(Vector2.zero, 0, false, m_LastGrounded);

                if (transform.position == m_LastPosition && transform.rotation == m_LastRotation)
                {
                    m_Sync = false;
                }
            }
        }

        public override void OnServerInitialize()
        {
            DisableCamera();
        }

        public override void OnClientInitialize()
        {
            base.OnClientInitialize();

            if (!IsOwner)
            {
                DisableCamera();
            }
            else
            {
                DisableGraphics();
            }
        }

        private void DisableCamera()
        {
            m_FirstPersonCamera?.gameObject.SetActive(false);
        }

        private void DisableGraphics()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = false;
            }
        }

        protected override void CorrectSimulation(InputData serverInput)
        {
            Vector3 pos = serverInput.GetInput<Vector3Input>(0);

            float yRot = serverInput.GetInput<FloatInput>(1);

            Quaternion rot = Quaternion.Euler(0, yRot, 0);

            m_Motor.TeleportTo(pos, rot);
        }

        protected override void ExecuteInput(InputData input)
        {
            float xInput = input.GetInput<FloatInput>(0);

            float yInput = input.GetInput<FloatInput>(1);

            float heading = input.GetInput<FloatInput>(2);

            m_Motor.Simulate(new Vector2(xInput, yInput), heading);
        }

        protected override bool UpdateSimulation(InputData serverInput, InputData prediction)
        {
            Vector3 position = serverInput.GetInput<Vector3Input>(0);

            if (IsOwner)
            {
                if (prediction != null)
                {
                    Vector3 predicted = prediction.GetInput<Vector3Input>(0);

                    float distance = Vector3.Distance(position, predicted);

                    const float ERR = 0.00001f;

                    return distance < ERR;
                }

                return false;
            }
            else
            {
                float heading = serverInput.GetInput<FloatInput>(1);

                Quaternion rotation = Quaternion.Euler(0, heading, 0);

                m_LastGrounded = serverInput.GetInput<BoolInput>(2);

                m_LastPosition = position;

                m_LastDistance = Vector3.Distance(transform.position, m_LastPosition);

                m_LastRotation = rotation;

                m_LastAngle = Quaternion.Angle(transform.rotation, m_LastRotation);

                m_Sync = true;

                return true;
            }
        }
    }
}
