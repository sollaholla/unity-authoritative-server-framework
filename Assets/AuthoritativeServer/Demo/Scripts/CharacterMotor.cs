using System;
using UnityEngine;

namespace AuthoritativeServer.Demo
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class CharacterMotor : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField]
        private float m_DefaultRotationSpeed = 15f;
        [SerializeField]
        private float m_DefaultMoveSpeed = 5f;

        private CharacterController m_CharacterController;
        private Animator m_Animator;

        private float m_MovementSpeed;
        private float m_RotationSpeed;

        private Vector3 m_LastPosition;
        private Vector3 m_Velocity;

        private Vector3 m_GravityVector;
        private float m_FallTime;

        /// <summary>
        /// True if the <see cref="CharacterController.isGrounded"/>.
        /// </summary>
        public bool isGrounded {
            get { return m_CharacterController.isGrounded; }
        }

        private void Awake()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Animator = GetComponent<Animator>();

            m_MovementSpeed = m_DefaultMoveSpeed;
            m_RotationSpeed = m_DefaultRotationSpeed;
            m_LastPosition = transform.position;
        }

        /// <summary>
        /// Simulate movement.
        /// </summary>
        /// <param name="input">The movement input.</param>
        /// <param name="heading">The rotation.</param>
        public void Simulate(Vector2 input, float heading, bool move = true, bool overrideGrounded = false)
        {
            if (move)
            {
                UpdateGravity();
                Rotate(heading);
                Move(input);
            }

            UpdatePhysics();
            Animate(m_CharacterController.isGrounded || overrideGrounded);
        }

        private void UpdateGravity()
        {
            if (m_CharacterController.isGrounded)
            {
                m_GravityVector = Vector3.zero;
            }
            else
            {
                m_GravityVector += -Vector3.up * 9.81f * Time.fixedDeltaTime;
            }
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            m_CharacterController.enabled = false;
            transform.position = position;
            transform.rotation = rotation;
            m_CharacterController.enabled = true;
        }

        private void Rotate(float heading)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, heading, 0), Time.fixedDeltaTime * m_RotationSpeed);
        }

        private void Move(Vector2 input)
        {
            Vector3 motion = Vector3.zero;

            if (!m_CharacterController.isGrounded)
            {
                motion = m_GravityVector;
            }
            else
            {
                motion = new Vector3(input.x, 0, input.y);
                motion = transform.TransformDirection(motion) * m_MovementSpeed;
                motion -= new Vector3(0, 100, 0);
            }

            m_CharacterController.Move(motion * Time.fixedDeltaTime);
        }

        private void UpdatePhysics()
        {
            Vector3 delta = transform.position - m_LastPosition;
            delta /= Time.fixedDeltaTime;
            m_Velocity = delta;
            m_LastPosition = transform.position;
        }

        private void Animate(bool isGrounded)
        {
            Vector3 animationVelocity = transform.InverseTransformDirection(m_Velocity);
            m_Animator.SetFloat("InputX", animationVelocity.x / m_MovementSpeed, 0.1f, Time.fixedDeltaTime);
            m_Animator.SetFloat("InputY", animationVelocity.z / m_MovementSpeed, 0.1f, Time.fixedDeltaTime);
            m_Animator.SetBool("Grounded", isGrounded);
        }
    }
}
