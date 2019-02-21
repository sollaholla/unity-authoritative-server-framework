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

        private float m_CurrentX;
        private float m_CurrentY;

        private void Awake()
        {
            m_PlayerInput = GetComponentInParent<FirstPersonPlayer>();
            m_CurrentX = m_PlayerInput.transform.eulerAngles.y;
        }

        private void Update()
        {
            float inputX = Input.GetAxis("Mouse X");
            float inputY = Input.GetAxis("Mouse Y");

            m_CurrentX += inputX * Time.deltaTime * (360f * m_SensitivityX);
            m_CurrentY -= inputY * Time.deltaTime * (360f * m_SensitivityY);
            m_CurrentY = Mathf.Clamp(m_CurrentY, -90f, 90f);

            transform.rotation = Quaternion.Euler(m_CurrentY, m_CurrentX, 0);
        }
    }
}
