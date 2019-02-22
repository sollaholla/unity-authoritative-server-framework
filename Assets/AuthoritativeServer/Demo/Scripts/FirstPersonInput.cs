using AuthoritativeServer.Inputs;

using UnityEngine;

namespace AuthoritativeServer.Demo
{
    public class FirstPersonInput : InputStream
    {
        private Transform m_Transform;
        private Transform m_MainCameraTransform;

        public void Initialize(Transform t)
        {
            m_Transform = t;
            m_MainCameraTransform = Camera.main?.transform;
        }

        protected override void Build(InputData data)
        {
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

            data.Add(new FloatInput(input.x));

            data.Add(new FloatInput(input.y));

            data.Add(new FloatInput(GetHeading()));
        }

        private float GetHeading()
        {
            if (m_MainCameraTransform != null)
            {
                return m_MainCameraTransform.eulerAngles.y;
            }

            return m_Transform.eulerAngles.y;
        }
    }
}