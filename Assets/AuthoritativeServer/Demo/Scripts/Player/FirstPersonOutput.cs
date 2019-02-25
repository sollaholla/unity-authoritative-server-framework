using AuthoritativeServer.Inputs;

using UnityEngine;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// Defines server output for the <see cref="FirstPersonPlayer"/>.
    /// </summary>
    public class FirstPersonOutput : InputStream
    {
        #region FIELDS

        private Transform m_Transform;

        private CharacterMotor m_Motor;

        #endregion

        #region PUBLIC

        public void Initialize(Transform t)
        {
            m_Transform = t;

            m_Motor = t.GetComponent<CharacterMotor>();
        }

        #endregion

        #region PROTECTED

        protected override void Build(InputData data)
        {
            data.Add(new Vector3Input(m_Transform.position));

            data.Add(new FloatInput(m_Transform.eulerAngles.y));

            data.Add(new BoolInput(m_Motor.isGrounded));

            data.Add(new BoolInput(m_Motor.isJumping));

            data.Add(new BoolInput(m_Motor.isCrouching));
        }

        #endregion
    }
}