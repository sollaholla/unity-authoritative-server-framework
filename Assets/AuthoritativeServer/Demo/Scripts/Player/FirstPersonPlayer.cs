using AuthoritativeServer.Inputs;

using UnityEngine;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// A network server authoritative input system for a first person controller.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    [AddComponentMenu("Autho Server/Demo/Player/First Person Player")]
    public class FirstPersonPlayer : AuthoritativeInput<FirstPersonInput, FirstPersonOutput>
    {
        #region INSPECTOR

        [SerializeField]
        private FirstPersonCamera m_FirstPersonCamera;

        #endregion

        #region FIELDS

        private CharacterMotor m_Motor;

        private Quaternion m_LastRotation;
        private float m_LastAngle;
        private Vector3 m_LastPosition;
        private float m_LastDistance;
        private bool m_LastGrounded;
        private bool m_LastJumping;
        private bool m_LastCrouching;
        private bool m_Sync;

        #endregion

        #region UNITY

        protected override void Awake()
        {
            base.Awake();

            m_ServerStream.Initialize(transform);

            m_ClientStream.Initialize(transform);

            m_Motor = GetComponent<CharacterMotor>();
        }

        protected override void Update()
        {
            base.Update();

        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            SmoothObservation();
        }

        #endregion

        #region PRIVATE

        private void SmoothObservation()
        {
            if (!IsServer && !IsOwner && m_Sync)
            {
                transform.position = Vector3.MoveTowards(transform.position, m_LastPosition, m_LastDistance * (1.0f / 5));

                transform.rotation = Quaternion.RotateTowards(transform.rotation, m_LastRotation, m_LastAngle * (1.0f / 5));

                m_Motor.Animate();

                m_Motor.SetIsJumping(m_LastJumping);

                m_Motor.SetIsGrounded(m_LastGrounded);

                m_Motor.SetIsCrouching(m_LastCrouching);

                if (transform.position == m_LastPosition && transform.rotation == m_LastRotation)
                {
                    m_Sync = false;
                }
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

        #endregion

        #region PUBLIC

        public override void OnServerInitialize()
        {
            base.OnServerInitialize();

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

        #endregion

        #region PROTECTED

        protected override void CorrectSimulation(InputData serverInput)
        {
            Vector3 position = serverInput.GetInput<Vector3Input>(0);

            Quaternion rotation = Quaternion.Euler(0, serverInput.GetInput<FloatInput>(1), 0);

            m_Motor.Teleport(position, rotation);
        }

        protected override void ExecuteInput(InputData input)
        {
            float xInput = input.GetInput<FloatInput>(0);

            float yInput = input.GetInput<FloatInput>(1);

            float heading = input.GetInput<FloatInput>(2);

            bool jump = input.GetInput<TriggerInput>(3);

            bool crouch = input.GetInput<BoolInput>(4);

            Vector2 inputVector = new Vector2(xInput, yInput);

            m_Motor.Move(inputVector, heading, jump, crouch);
        }

        protected override bool UpdateSimulation(InputData serverInput, InputData prediction)
        {
            Vector3 position = serverInput.GetInput<Vector3Input>(0);

            if (IsOwner)
            {
                if (prediction != null)
                {
                    Vector3 predicted = prediction.GetInput<Vector3Input>(0);

                    float dist = Vector3.SqrMagnitude(position - predicted);

                    const float ERR = 0.00001f;

                    return dist < ERR;
                }

                throw new System.InvalidOperationException("Cannot update simulation because prediction wasn't found.");
            }
            else
            {
                Quaternion rotation = Quaternion.Euler(0, serverInput.GetInput<FloatInput>(1), 0);

                m_LastGrounded = serverInput.GetInput<BoolInput>(2);

                m_LastJumping = serverInput.GetInput<BoolInput>(3);

                m_LastCrouching = serverInput.GetInput<BoolInput>(4);

                m_LastPosition = position;

                m_LastDistance = Vector3.Distance(transform.position, m_LastPosition);

                m_LastRotation = rotation;

                m_LastAngle = Quaternion.Angle(transform.rotation, m_LastRotation);

                m_Sync = true;

                return true;
            }
        }

        #endregion
    }
}
