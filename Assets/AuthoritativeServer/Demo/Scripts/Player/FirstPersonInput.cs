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

        /// <summary>
        /// Allows input if true otherwise disallows input.
        /// </summary>
        public bool UseInput { get; set; } = true;

        #region PUBLIC

        public void Initialize(Transform t)
        {
            m_Transform = t;
            m_MainCameraTransform = Camera.main?.transform.parent;
        }

        #endregion

        #region PROTECTED

        protected override void Build(InputData data)
        {
            Vector2 input = UseInput ? new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized : Vector2.zero;

            data.Add(new FloatInput(input.x));

            data.Add(new FloatInput(input.y));

            data.Add(new FloatInput(m_MainCameraTransform?.eulerAngles.y ?? 0));

            data.Add(new TriggerInput(UseInput ? Input.GetButtonDown("Jump") : false));

            data.Add(new BoolInput(UseInput ? Input.GetButton("Crouch") : false));
        }

        #endregion
    }
}