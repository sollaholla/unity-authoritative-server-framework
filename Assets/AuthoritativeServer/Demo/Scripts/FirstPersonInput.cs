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
            data.Add(new FloatInput(Input.GetAxisRaw("Horizontal")));

            data.Add(new FloatInput(Input.GetAxisRaw("Vertical")));

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