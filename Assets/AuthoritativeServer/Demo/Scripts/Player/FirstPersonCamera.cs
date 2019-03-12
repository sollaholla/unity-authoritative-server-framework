using System;
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
        [SerializeField, Range(0, 1f)]
        private float m_HeadBobIntensity = 0.5f;
        [SerializeField]
        private bool m_LockCursor;

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
        private bool m_UseInput = true;

        private Vector3 m_HeadBob;
        private Vector3 m_HeadTilt;

        #endregion

        #region UNITY

        private void Awake()
        {
            m_PlayerInput = GetComponentInParent<FirstPersonPlayer>();
            m_CharacterMotor = GetComponentInParent<CharacterMotor>();
            m_Camera = GetComponentInChildren<Camera>();

            m_CurrentX = m_PlayerInput.transform.eulerAngles.y;
            m_InitialHeadY = transform.localPosition.y;

            if (m_LockCursor)
                Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnEnable()
        {
            UiWindow.InputStateChanged += OnInputStateChanged;
        }

        private void OnDisable()
        {
            UiWindow.InputStateChanged -= OnInputStateChanged;
        }

        private void Update()
        {
            m_XInput = m_UseInput ? Input.GetAxis("Mouse X") : 0;
            m_YInput = m_UseInput ? Input.GetAxis("Mouse Y") : 0;

            m_CurrentX += m_XInput * (360f * m_SensitivityX) * Time.fixedDeltaTime;
            m_CurrentY -= m_YInput * (360f * m_SensitivityY) * Time.fixedDeltaTime;

            m_CurrentY = Mathf.Clamp(m_CurrentY, -90f, 90f);
        }

        private void FixedUpdate()
        {
            CalculateHeadBob();

            transform.rotation = Quaternion.Euler(m_CurrentY, m_CurrentX, 0);
            transform.localPosition = Vector3.Lerp(transform.localPosition, GetOffset(), Time.deltaTime * m_CrouchLerpSpeed) + m_HeadBob;

            if (m_Camera.transform != transform)
            {
                m_Camera.transform.localRotation = Quaternion.Lerp(m_Camera.transform.localRotation, Quaternion.Euler(m_HeadTilt), Time.deltaTime * 5f);
            }
        }

        #endregion

        #region PRIVATE

        private void OnInputStateChanged(bool useInput)
        {
            m_UseInput = useInput;

            if (m_LockCursor)
            {
                if (useInput) Cursor.lockState = CursorLockMode.Locked;
                else Cursor.lockState = CursorLockMode.None;
            }
        }

        private Vector3 GetOffset()
        {
            if (m_CharacterMotor.isCrouching)
            {
                return new Vector3(0, 1, 0);
            }

            return new Vector3(0, m_InitialHeadY, 0);
        }

        protected virtual void CalculateHeadBob()
        {
            float delta = m_CharacterMotor.isGrounded ? m_CharacterMotor.velocity.normalized.magnitude : 0;

            float yValue = Mathf.PingPong(Time.time * m_CharacterMotor.height * 3.5f * delta, 1f) * 0.025f;
            m_HeadBob.y = -yValue;
            m_HeadBob.y *= m_HeadBobIntensity;

            m_HeadTilt.x = (Mathf.PerlinNoise(0, Time.time * 3f * m_CharacterMotor.height * 0.5f) - 0.5f) * 10f;
            m_HeadTilt *= delta * m_CharacterMotor.height * 0.5f;
            m_HeadTilt *= m_HeadBobIntensity;
        }

        #endregion
    }
}
