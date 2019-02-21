using AuthoritativeServer.Inputs;
using UnityEngine;

namespace AuthoritativeServer.Demo
{
    [RequireComponent(typeof(CharacterMotor))]
    public class FirstPersonPlayer : AuthoritativeInput<FirstPersonInput, FirstPersonOutput>
    {
        [SerializeField]
        private FirstPersonCamera m_FirstPersonCamera;

        private CharacterMotor m_Motor;

        protected override void Awake()
        {
            base.Awake();

            m_ServerStream.Initialize(transform);
            m_ClientStream.Initialize(transform);

            m_Motor = GetComponent<CharacterMotor>();
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
                float yRot = serverInput.GetInput<FloatInput>(1);

                Quaternion rot = Quaternion.Euler(0, yRot, 0);

                bool isGrounded = serverInput.GetInput<BoolInput>(2);

                transform.position = position;

                transform.rotation = rot;

                m_Motor.Simulate(Vector2.zero, 0, false, isGrounded);

                return true;
            }
        }
    }
}
