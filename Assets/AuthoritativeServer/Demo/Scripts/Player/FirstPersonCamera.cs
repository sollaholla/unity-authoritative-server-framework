using UnityEngine;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// A camera controller for the <see cref="FirstPersonPlayer"/> component.
    /// </summary>
    [AddComponentMenu("Autho Server/Demo/Player/First Person Camera")]
    public class FirstPersonCamera : MonoBehaviour
    {
        #region INSPECTOR

        [Header("Camera")]
        [SerializeField]
        private float m_SensitivityX = 1f;
        [SerializeField]
        private float m_SensitivityY = 1f;
        [SerializeField]
        private float m_CrouchLerpSpeed = 10f;

        #endregion

        #region FIELDS

        private FirstPersonPlayer m_PlayerInput;
        private CharacterMotor m_CharacterMotor;
        private Camera m_Camera;

        private float m_XInput;
        private float m_YInput;

        private float m_CurrentX;
        private float m_CurrentY;

        private float m_InitialHeadY;

        #endregion

        #region UNITY

        private void Awake()
        {
            m_PlayerInput = GetComponentInParent<FirstPersonPlayer>();
            m_CharacterMotor = GetComponentInParent<CharacterMotor>();
            m_Camera = GetComponentInChildren<Camera>();

            m_CurrentX = m_PlayerInput.transform.eulerAngles.y;
            m_InitialHeadY = transform.localPosition.y;
        }

        private void Update()
        {
            m_XInput = Input.GetAxis("Mouse X");
            m_YInput = Input.GetAxis("Mouse Y");

            m_CurrentX += m_XInput * (360f * m_SensitivityX) * Time.fixedDeltaTime;
            m_CurrentY -= m_YInput * (360f * m_SensitivityY) * Time.fixedDeltaTime;

            m_CurrentY = Mathf.Clamp(m_CurrentY, -90f, 90f);
        }

        private void FixedUpdate()
        {
            transform.rotation = Quaternion.Euler(m_CurrentY, m_CurrentX, 0);
            transform.localPosition = Vector3.Lerp(transform.localPosition, GetOffset(), Time.deltaTime * m_CrouchLerpSpeed);
        }

        #endregion

        #region PRIVATE

        private Vector3 GetOffset()
        {
            if (m_CharacterMotor.isCrouching)
            {
                return new Vector3(0, 1, 0);
            }

            return new Vector3(0, m_InitialHeadY, 0);
        }

        #endregion
    }
}
