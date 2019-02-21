using AuthoritativeServer.Inputs;

using UnityEngine;

namespace AuthoritativeServer.Demo
{
    public class FirstPersonOutput : InputStream
    {
        private Transform m_Transform;

        private CharacterMotor m_Motor;

        public void Initialize(Transform t)
        {
            m_Transform = t;

            m_Motor = t.GetComponent<CharacterMotor>();
        }

        protected override void Build(InputData data)
        {
            data.Add(new Vector3Input(m_Transform.position));

            data.Add(new FloatInput(m_Transform.eulerAngles.y));

            data.Add(new BoolInput(m_Motor.isGrounded));
        }
    }
}