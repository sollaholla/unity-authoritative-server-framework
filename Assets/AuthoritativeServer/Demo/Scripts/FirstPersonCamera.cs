using UnityEngine;

namespace AuthoritativeServer.Demo
{
    [RequireComponent(typeof(Camera))]
    public class FirstPersonCamera : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField]
        private float m_SensitivityX = 1f;
        [SerializeField]
        private float m_SensitivityY = 1f;

        private FirstPersonPlayer m_PlayerInput;

        private float m_XInput;
        private float m_YInput;

        private float m_CurrentX;
        private float m_CurrentY;

        private void Awake()
        {
            m_PlayerInput = GetComponentInParent<FirstPersonPlayer>();
            m_CurrentX = m_PlayerInput.transform.eulerAngles.y;
        }

        private void Update()
        {
            m_XInput = Input.GetAxis("Mouse X");
            m_YInput = Input.GetAxis("Mouse Y");

            m_CurrentX += m_XInput * (360f * m_SensitivityX) * Time.fixedDeltaTime;
            m_CurrentY -= m_YInput * (360f * m_SensitivityY) * Time.fixedDeltaTime;
            m_CurrentY = Mathf.Clamp(m_CurrentY, -90f, 90f);

            transform.rotation = Quaternion.Euler(m_CurrentY, m_CurrentX, 0);
        }
    }
}
