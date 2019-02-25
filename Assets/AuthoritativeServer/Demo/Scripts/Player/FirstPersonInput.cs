using AuthoritativeServer.Inputs;

using UnityEngine;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// Defines client input for the <see cref="FirstPersonPlayer"/>.
    /// </summary>
    public class FirstPersonInput : InputStream
    {
        #region FIELDS

        private Transform m_Transform;
        private Transform m_MainCameraTransform;

        #endregion

        #region PUBLIC

        public void Initialize(Transform t)
        {
            m_Transform = t;
            m_MainCameraTransform = Camera.main?.transform;
        }

        #endregion

        #region PROTECTED

        protected override void Build(InputData data)
        {
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

            data.Add(new FloatInput(input.x));

            data.Add(new FloatInput(input.y));

            data.Add(new FloatInput(m_MainCameraTransform?.eulerAngles.y ?? 0));

            data.Add(new TriggerInput(Input.GetButtonDown("Jump")));

            data.Add(new BoolInput(Input.GetButton("Crouch")));
        }

        #endregion
    }
}