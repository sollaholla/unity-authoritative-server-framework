using System;
using UnityEngine;

namespace AuthoritativeServer.Demo
{
    /// <summary>
    /// A <see cref="CharacterController"/> movement handler.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class CharacterMotor : MonoBehaviour
    {
        #region INSPECTOR

        public event Action<Collision> CharacterCollision;

        [Header("Movement")]
        [SerializeField]
        private float m_DefaultRotationSpeed = 15f;
        [SerializeField]
        private float m_DefaultMoveSpeed = 5f;

        [Header("Physics")]
        [SerializeField]
        private float m_JumpForce = 500;

        #endregion

        #region FIELDS

        private CharacterController m_CharacterController;
        private Animator m_Animator;
        private StatusEffect m_Stats;

        private Vector3 m_CurrentGravity;
        private Vector3 m_HorizontalVelocity;
        private Vector3 m_Velocity;
        private Vector3 m_LastPosition;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// True if we're grounded.
        /// </summary>
        public bool isGrounded { get; private set; }

        /// <summary>
        /// True if the character is jumping.
        /// </summary>
        public bool isJumping { get; private set; }

        /// <summary>
        /// True if crouching.
        /// </summary>
        public bool isCrouching { get; private set; }

        /// <summary>
        /// The character velocity.
        /// </summary>
        public Vector3 velocity { get { return m_Velocity; } }

        /// <summary>
        /// The characters height.
        /// </summary>
        public float height { get { return m_CharacterController.height; } }

        /// <summary>
        /// The horizontal velocity of the character in air.
        /// </summary>
        public Vector3 horizontalVelocity { get { return m_HorizontalVelocity; } }

        /// <summary>
        /// The gravitational acceleration when falling.
        /// </summary>
        public float gravitationalAccel { get { return m_CurrentGravity.y; } }

        #endregion

        #region UNITY

        private void Awake()
        {
            m_CharacterController = GetComponent<CharacterController>();

            m_Animator = GetComponent<Animator>();

            m_Stats = GetComponent<StatusEffect>();

            m_LastPosition = transform.position;
        }

        private void FixedUpdate()
        {
            m_Velocity = transform.position - m_LastPosition;

            m_Velocity /= Time.fixedDeltaTime;

            m_LastPosition = transform.position;
        }

        private void OnCollisionEnter(Collision collision)
        {
            CharacterCollision?.Invoke(collision);
        }

        #endregion

        #region PUBLIC

        /// <summary>
        /// Move the character.
        /// </summary>
        public void Move(Vector2 input, float heading, bool jump, bool crouch)
        {
            Rotate(heading);

            isGrounded = m_CharacterController.isGrounded;

            UpdateCrouch(crouch);

            if (isCrouching)
            {
                input *= 0.5f;
            }

            Vector3 motion = CalculateJumpMotion(jump);

            motion += CalculateGravity();

            if (isGrounded)
            {
                Vector3 inputMotion = CalculateInputMotion(input) * m_Stats.GetValue("Speed", 1f);

                motion += inputMotion;
            }
            else
            {
                motion += m_HorizontalVelocity;
            }

            m_CharacterController.Move(motion * Time.fixedDeltaTime);

            Animate();
        }

        /// <summary>
        /// Force simulate character physics.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        public void ForceSimulate(Vector3 position, Quaternion rotation, Vector3 horiVelocity, float grav)
        {
            Vector3 diff = position - transform.position;
            m_CharacterController.Move(diff);
            transform.rotation = rotation;
            m_HorizontalVelocity = horiVelocity;
            m_CurrentGravity = new Vector3(0, grav, 0);
        }

        /// <summary>
        /// Set the jump state, it will also trigger an animation.
        /// </summary>
        /// <param name="jumping"></param>
        public void SetIsJumping(bool jumping)
        {
            if (jumping && !isJumping)
            {
                TriggerJumpAnimation();
            }

            isJumping = jumping;
        }

        /// <summary>
        /// Set the grounded state.
        /// </summary>
        /// <param name="grounded"></param>
        public void SetIsGrounded(bool grounded)
        {
            this.isGrounded = grounded;
        }

        /// <summary>
        /// Set the crouching state.
        /// </summary>
        /// <param name="crouching"></param>
        public void SetIsCrouching(bool crouching)
        {
            UpdateCrouch(crouching);
        }

        /// <summary>
        /// Animate the character. Will be called automatically in <see cref="Move(Vector2, float, bool, bool)"/>
        /// </summary>
        public void Animate()
        {
            Vector3 relativeVelocity = transform.InverseTransformDirection(m_Velocity);
            float speed = isCrouching ? m_DefaultMoveSpeed * 0.5f : m_DefaultMoveSpeed;

            m_Animator.SetFloat("InputX", relativeVelocity.x / speed, 0.1f, Time.fixedDeltaTime);
            m_Animator.SetFloat("InputY", relativeVelocity.z / speed, 0.1f, Time.fixedDeltaTime);
            m_Animator.SetBool("Grounded", isGrounded);
            m_Animator.SetBool("Crouching", isCrouching);
        }

        #endregion

        #region PRIVATE

        private void UpdateCrouch(bool crouchInput)
        {
            if (crouchInput && isGrounded)
            {
                m_CharacterController.height = 1f;

                m_CharacterController.center = new Vector3(0, 0.5f, 0);

                isCrouching = true;
            }
            else if (CanUnCrouch())
            {
                m_CharacterController.height = 2f;

                m_CharacterController.center = Vector3.up;

                isCrouching = false;
            }
        }

        private bool CanUnCrouch()
        {
            Ray ray = new Ray(transform.position + (Vector3.up * 2f) - (Vector3.up * 0.01f), Vector3.up);

            return !Physics.Raycast(ray, 0.02f);
        }

        private void TriggerJumpAnimation()
        {
            m_Animator.SetTrigger("Jump");
        }

        private Vector3 CalculateGravity()
        {
            if (isGrounded)
            {
                m_CurrentGravity = Vector3.zero;

                m_HorizontalVelocity = Vector3.Scale(new Vector3(1, 0, 1), m_CharacterController.velocity);

                if (isJumping)
                {
                    return Vector3.zero;
                }

                return Physics.gravity;
            }

            m_CurrentGravity += Physics.gravity * Time.fixedDeltaTime;

            return m_CurrentGravity;
        }

        private Vector3 CalculateJumpMotion(bool jumpInput)
        {
            if (isGrounded && isJumping)
            {
                isJumping = false;
            }

            if (isGrounded && jumpInput)
            {
                isJumping = true;
            }

            if (isJumping)
            {
                return Vector3.up * m_JumpForce * Time.fixedDeltaTime;
            }

            return Vector3.zero;
        }

        private void Rotate(float heading)
        {
            transform.rotation = Quaternion.Euler(0, heading, 0);
        }

        private Vector3 CalculateInputMotion(Vector2 input)
        {
            Vector3 motion = new Vector3(input.x, 0, input.y);

            motion = transform.TransformDirection(motion) * m_DefaultMoveSpeed;

            return motion;
        }

        #endregion
    }
}
