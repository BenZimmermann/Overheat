// Original Code Author: Aedan Graves
// Modified: New Input System (Action Maps), Cinemachine, stripped to walk/jump/slide/headbob/footsteps

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SUPERCharacter
{

    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    [AddComponentMenu("SUPER Character/SUPER Character Controller")]
    public class SUPERCharacterAIO : MonoBehaviour
    {

        #region Variables

        public bool controllerPaused = false;

        // -----------------------------------------------------------------------
        #region Camera / Cinemachine
        [Header("Camera & Cinemachine")]
        [Tooltip("The Cinemachine Brain on the main camera.")]
        public CinemachineBrain cinemachineBrain;
        [Tooltip("Virtual camera that follows the player (e.g. CinemachineCamera).")]
        public CinemachineCamera virtualCamera;
        [Tooltip("The plain camera used for raycasts / UI anchoring. Usually Main Camera.")]
        public Camera playerCamera;

        public bool lockAndHideMouse = true;
        public bool autoGenerateCrosshair = true;
        public Sprite crosshairSprite;

        [Tooltip("Standing eye height – drives the Cinemachine follow offset Y.")]
        public float standingEyeHeight = 0.8f;

        [Tooltip("Look sensitivity – shared by mouse and gamepad. " +
                 "Internally: mouse uses sensitivity/1000, stick uses sensitivity * 0.3 deg/s.")]
        public float sensitivity = 5f;
        [Tooltip("How far up/down the camera can look (degrees total, e.g. 170 = ±85°).")]
        public float verticalRotationRange = 170f;
        [Tooltip("Invert vertical look axis.")]
        public bool invertY = false;
        public bool invertX = false;

        // Internal
        Image crosshairImg;
        bool setInitialRot = true;
        Vector3 initialRot;
        float _cameraPitch = 0f;   // current vertical angle of the virtual camera
                                   // CinemachineRotationComposer or POV driven by us; we store yaw on the rigidbody,
                                   // pitch on a separate pivot child that the virtual camera follows.
        Transform _cameraPitchPivot;  // child transform used as the "look target" for the vcam
        #endregion

        // -----------------------------------------------------------------------
        #region Input Actions
        [Header("Input Actions")]
        public InputActionAsset inputActionAsset;
        [Tooltip("Name of the Action Map inside the asset.")]
        public string actionMapName = "Player";

        public string lookActionName = "Look";
        public string moveActionName = "Move";
        public string jumpActionName = "Jump";
        public string slideActionName = "Slide";
        public string dashActionName = "Dash";

        InputAction _lookAction;
        InputAction _moveAction;
        InputAction _jumpAction;
        InputAction _slideAction;
        InputAction _dashAction;
        #endregion

        // -----------------------------------------------------------------------
        #region Movement
        [Header("Movement Settings")]
        public bool enableMovementControl = true;

        [Range(1f, 650f)] public float walkingSpeed = 140f;
        [Range(1f, 400f)] public float decelerationSpeed = 240f;

        public LayerMask whatIsGround = -1;

        // Slope
        public float hardSlopeLimit = 70f;
        public float maxStairRise = 0.25f;
        public float stepUpSpeed = 0.2f;

        // Jump
        public bool canJump = true;
        public bool holdJump = false;
        public bool jumpEnhancements = true;
        [Range(1f, 650f)] public float jumpPower = 40f;
        [Range(0f, 1f)] public float airControlFactor = 1f;
        public float decentMultiplier = 2.5f;
        public float tapJumpMultiplier = 2.1f;

        // Double Jump
        public bool canDoubleJump = true;
        [Range(1f, 650f)] public float doubleJumpPower = 35f;

        // Coyote Time
        public bool enableCoyoteTime = true;
        [Range(0.05f, 0.5f)] public float coyoteTime = 0.15f;

        // Slide
        public bool canSlide = true;
        public float slidingDeceleration = 150f;
        public float slidingTransitionSpeed = 4f;
        public float maxFlatSlideDistance = 10f;

        // Dash
        public bool canDash = true;
        [Range(1f, 2000f)] public float dashForce = 800f;
        [Range(0.05f, 1f)] public float dashDuration = 0.2f;
        [Range(0f, 5f)] public float dashCooldown = 1f;
        public bool dashUsesInputDirection = true;   // false = always dash forward

        // Internal
        public GroundInfo currentGroundInfo = new GroundInfo();
        float standingHeight;
        float currentGroundSpeed;
        Vector3 InputDir;
        Vector2 MovInput;
        Vector2 _2DVelocity;
        float _2DVelocityMag;
        PhysicsMaterial _ZeroFriction, _MaxFriction;
        CapsuleCollider capsule;
        Rigidbody p_Rigidbody;
        bool isIdle;
        bool Jumped;
        bool jumpInput_Momentary, jumpInput_FrameOf;
        bool slideInput_Momentary, slideInput_FrameOf;
        bool isSliding;
        float jumpBlankingPeriod;
        Vector3 cachedDirPreSlide, cachedPosPreSlide;

        // Double jump & coyote internals
        bool _hasDoubleJump;          // double jump available this airtime?
        bool _wasGroundedLastFrame;   // for coyote window detection
        float _coyoteTimeCounter;      // counts down after leaving ground
        bool _jumpInputUsed;          // prevents hold-to-jump from firing repeatedly per press

        // Dash internals
        bool _isDashing;
        bool _hasDash;                // one dash available per airtime
        bool dashInput_FrameOf;
        float _dashTimer;              // counts up during active dash
        float _dashCooldownTimer;      // counts down between dashes
        Vector3 _dashDirection;
        #endregion

        // -----------------------------------------------------------------------
        #region Parkour
#if SAIO_ENABLE_PARKOUR
    [Header("Parkour / Vault")]
    public bool   canVault             = true;
    public bool   autoVaultWhenMoving;
    public string vaultObjectTag       = "Vault Obj";
    public float  vaultSpeed           = 7.5f;
    public float  maxVaultDepth        = 1.5f;
    public float  maxVaultHeight       = 0.75f;
    public string vaultActionName      = "Vault";

    InputAction _vaultAction;
    bool        vaultInput;
    bool        isVaulting;
    RaycastHit  VC_Stage1, VC_Stage2, VC_Stage3, VC_Stage4;
    Vector3     vaultForwardVec;
#endif
        bool doingPosInterp, doingCamInterp;
        #endregion

        // -----------------------------------------------------------------------
        #region Footstep System
        [Header("Footstep System")]
        public bool enableFootstepSounds = true;
        public FootstepTriggeringMode footstepTriggeringMode = FootstepTriggeringMode.calculatedTiming;
        [Range(0f, 1f)] public float stepTiming = 0.15f;
        public List<GroundMaterialProfile> footstepSoundSet = new List<GroundMaterialProfile>();

        bool shouldCalculateFootstepTriggers = true;
        float StepCycle;
        AudioSource playerAudioSource;
        List<AudioClip> currentClipSet = new List<AudioClip>();
        #endregion

        // -----------------------------------------------------------------------
        #region Headbob
        [Header("Headbob")]
        public bool enableHeadbob = true;
        [Range(1f, 5f)] public float headbobSpeed = 0.5f;
        [Range(1f, 5f)] public float headbobPower = 0.25f;
        [Range(0f, 3f)] public float ZTilt = 3f;

        Vector3 headbobCameraPosition;
        float headbobCyclePosition, headbobWarmUp;
        #endregion

        // -----------------------------------------------------------------------
        #region Animation
        [Header("Animator")]
        public Animator _1stPersonCharacterAnimator;
        public string a_velocity, a_2DVelocity, a_Grounded, a_Idle, a_Jumped, a_Sliding;
        #endregion

        [Space(10)]
        public bool enableGroundingDebugging = false;
        public bool enableMovementDebugging = false;

        #endregion // Variables

        // =======================================================================


        #region Input Binding

        void BindInputActions()
        {
            if (inputActionAsset == null)
            {
                Debug.LogWarning("[SUPERCharacter] No InputActionAsset assigned!");
                return;
            }
            var map = inputActionAsset.FindActionMap(actionMapName, throwIfNotFound: false);
            if (map == null)
            {
                Debug.LogWarning($"[SUPERCharacter] Action Map '{actionMapName}' not found.");
                return;
            }
            _lookAction = map.FindAction(lookActionName, false);
            _moveAction = map.FindAction(moveActionName, false);
            _jumpAction = map.FindAction(jumpActionName, false);
            _slideAction = map.FindAction(slideActionName, false);
            _dashAction = map.FindAction(dashActionName, false);
#if SAIO_ENABLE_PARKOUR
        _vaultAction = map.FindAction(vaultActionName, false);
#endif
            map.Enable();
        }

        void OnEnable() 
        {
            SettingsManager.Instance.OnSettingsChanged += ApplySettings;
            BindInputActions(); 
        }
        void OnDisable() 
        { 
            inputActionAsset?.FindActionMap(actionMapName, false)?.Disable();
            SettingsManager.Instance.OnSettingsChanged -= ApplySettings;
        }

        #endregion

        // =======================================================================
        void Start()
        {

            // Camera / Cinemachine
            if (lockAndHideMouse)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            // Create pitch pivot child – vcam Follow/LookAt target
            _cameraPitchPivot = new GameObject("CameraPitchPivot").transform;
            _cameraPitchPivot.SetParent(transform);
            _cameraPitchPivot.localPosition = Vector3.up * standingEyeHeight;
            _cameraPitchPivot.localRotation = Quaternion.identity;
            if (virtualCamera != null)
            {
                virtualCamera.Follow = _cameraPitchPivot;
                virtualCamera.LookAt = _cameraPitchPivot;
            }
            _cameraPitch = initialRot.x;
            ApplyCinemachineOffset(standingEyeHeight);
            headbobCameraPosition = Vector3.up * standingEyeHeight;

            if (autoGenerateCrosshair && crosshairSprite && playerCamera)
            {
                Canvas canvas = playerCamera.GetComponentInChildren<Canvas>();
                if (canvas == null)
                {
                    canvas = new GameObject("AutoCrosshair").AddComponent<Canvas>();
                    canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode =
                        CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.pixelPerfect = true;
                    canvas.transform.SetParent(playerCamera.transform);
                    canvas.transform.localPosition = Vector3.zero;
                }
                crosshairImg = new GameObject("Crosshair").AddComponent<Image>();
                crosshairImg.sprite = crosshairSprite;
                crosshairImg.rectTransform.sizeDelta = new Vector2(25, 25);
                crosshairImg.rectTransform.anchoredPosition = Vector2.zero;
                crosshairImg.transform.SetParent(canvas.transform);
                crosshairImg.raycastTarget = false;
            }

            initialRot = transform.localEulerAngles;

            // Movement
            p_Rigidbody = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            standingHeight = capsule.height;
            currentGroundSpeed = walkingSpeed;

            _ZeroFriction = new PhysicsMaterial("Zero_Friction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            _MaxFriction = new PhysicsMaterial("Max_Friction")
            {
                dynamicFriction = 1f,
                staticFriction = 1f,
                frictionCombine = PhysicsMaterialCombine.Maximum,
                bounceCombine = PhysicsMaterialCombine.Average
            };

            playerAudioSource = GetComponent<AudioSource>();
            ApplySettings();
            RefreshPlayerStats();
        }
        void RefreshPlayerStats()
        {
            var data = GameManager.Instance.Data;
            jumpPower += data.JumpPowerBonus;
            walkingSpeed += data.SpeedBonus;
            dashDuration += data.DashBonus;
            slidingDeceleration += data.SlideBonus;

        }
        void ApplySettings()
        {
            var data = SaveManager.Instance.Data;

            invertY = data.InvertY;
            invertX = data.InvertX;
            sensitivity = data.MouseSensitivity;
        }
        // =======================================================================
        void Update()
        {
            if (!controllerPaused)
            {

                // Input
                MovInput = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
                jumpInput_Momentary = _jumpAction != null && _jumpAction.IsPressed();
                jumpInput_FrameOf = _jumpAction != null && _jumpAction.WasPressedThisFrame();
                slideInput_Momentary = _slideAction != null && _slideAction.IsPressed();
                slideInput_FrameOf = _slideAction != null && _slideAction.WasPressedThisFrame();
                dashInput_FrameOf = _dashAction != null && _dashAction.WasPressedThisFrame();
#if SAIO_ENABLE_PARKOUR
            vaultInput = _vaultAction != null && _vaultAction.IsPressed();
#endif

                // Mouse Look
                RotateLook();

                // Align initial rotation
                if (setInitialRot)
                {
                    setInitialRot = false;
                    p_Rigidbody.MoveRotation(Quaternion.Euler(Vector3.up * initialRot.y));
                    InputDir = transform.forward;
                }

                // Headbob (drives Cinemachine offset)
                HeadbobCycleCalculator();

                // Movement relative to current yaw (body already rotated by RotateLook)
                InputDir = Vector3.ClampMagnitude(
                    transform.forward * MovInput.y + transform.right * MovInput.x, 1f);

                // Coyote timer
                bool trulGrounded = currentGroundInfo.isInContactWithGround && !Jumped;
                if (trulGrounded)
                {
                    _coyoteTimeCounter = coyoteTime;
                    _wasGroundedLastFrame = true;
                }
                else
                {
                    _coyoteTimeCounter -= Time.deltaTime;
                }
                // Reset double jump + dash when landing
                if (trulGrounded)
                {
                    _hasDoubleJump = canDoubleJump;
                    _hasDash = canDash;
                }

                // Jump input
                // _jumpInputUsed prevents holdJump from re-triggering double jump / coyote
                // on the same button hold. It resets only when the button is released.
                if (!jumpInput_Momentary)
                    _jumpInputUsed = false;

                bool jumpFresh = jumpInput_Momentary && !_jumpInputUsed;
                if (canJump && jumpFresh)
                {
                    bool groundOk = trulGrounded;
                    bool coyoteOk = enableCoyoteTime && _coyoteTimeCounter > 0f && !Jumped;
                    if (groundOk || coyoteOk)
                    {
                        _jumpInputUsed = true;   // consume this press
                        _coyoteTimeCounter = 0f;     // consume coyote window
                        Jump(jumpPower);
                    }
                    else if (_hasDoubleJump)
                    {
                        _jumpInputUsed = true;       // consume this press
                        _hasDoubleJump = false;
                        DoubleJump(doubleJumpPower);
                    }
                }

                // Slide
                if (canSlide && slideInput_FrameOf && !isIdle &&
                    currentGroundInfo.isInContactWithGround && !isSliding)
                    Slide();

                // Dash (air only)
                _dashCooldownTimer -= Time.deltaTime;
                if (_isDashing)
                {
                    _dashTimer += Time.deltaTime;
                    if (_dashTimer >= dashDuration)
                        EndDash();
                }
                if (_hasDash && dashInput_FrameOf &&
                    !currentGroundInfo.isInContactWithGround &&
                    !_isDashing && _dashCooldownTimer <= 0f)
                    StartDash();

                // Footsteps
                CalculateFootstepTriggers();

            }
            else
            {
                jumpInput_FrameOf = false;
                jumpInput_Momentary = false;
            }

            UpdateAnimationTriggers(controllerPaused);
        }

        // =======================================================================
        void FixedUpdate()
        {
            if (!controllerPaused && enableMovementControl)
            {
                GetGroundInfo();
                MovePlayer(InputDir, currentGroundSpeed);
                if (isSliding) SlidePhysics();
                if (_isDashing) DashPhysics();
            }
        }

        void OnTriggerEnter(Collider other)
        {
            other.GetComponent<ICollectable>()?.Collect();
        }

        // =======================================================================
        #region Cinemachine Helpers

        /// <summary>
        /// Reads the Look action and rotates:
        ///   - Rigidbody yaw  (horizontal)
        ///   - _cameraPitchPivot pitch (vertical, clamped)
        /// Cinemachine simply follows _cameraPitchPivot, so the camera
        /// automatically tracks the mouse without any extra components.
        /// </summary>
        void RotateLook()
        {
            if (_lookAction == null) return;
            Vector2 raw = _lookAction.ReadValue<Vector2>();
            if (raw.sqrMagnitude < 0.0001f) return;

            // Detect whether the current input comes from a pointer device (mouse/touchpad)
            // or from a gamepad stick, and scale accordingly.
            // InputSystem marks mouse-delta bindings with the "pointer" control path.
            bool isMouse = false;
            var control = _lookAction.activeControl;
            if (control != null)
                isMouse = control.path.Contains("Mouse") || control.path.Contains("Pointer");

            // Both inputs are converted to degrees-per-frame using a single 'sensitivity' value.
            //
            // Mouse:  raw = pixels/frame. We scale by (sensitivity / 1000) so that sensitivity=5
            //         gives 0.005 deg/pixel – a comfortable default at typical DPI & resolution.
            //
            // Stick:  raw = normalised -1..1. We want the same *feel* as the mouse, so we convert
            //         to deg/s using (sensitivity * 30) and multiply by deltaTime.
            //         This keeps both inputs at a comparable rotation speed for the same slider value.
            Vector2 delta;
            if (isMouse)
            {
                delta = raw * (sensitivity / 1000f);
            }
            else
            {
                delta = raw * (sensitivity * 30f) * Time.deltaTime;
            }

            // Yaw – rotate the whole rigidbody around Y
        float newYaw = p_Rigidbody.rotation.eulerAngles.y + (invertX ? -delta.x : delta.x);
        p_Rigidbody.MoveRotation(Quaternion.Euler(0f, newYaw, 0f));


            // Pitch – rotate the pivot child (camera follows it)
            float pitchDelta = invertY ? delta.y : -delta.y;
            _cameraPitch = Mathf.Clamp(
                _cameraPitch + pitchDelta,
                -verticalRotationRange * 0.5f,
                 verticalRotationRange * 0.5f);
            float roll = invertX ? -delta.x : delta.x;
            
            if (_cameraPitchPivot != null)
                _cameraPitchPivot.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
        }

        void ApplyCinemachineOffset(float eyeHeight)
        {
            if (virtualCamera == null) return;
            var follow = virtualCamera.GetComponent<CinemachineFollow>();
            if (follow == null) return;
            var o = follow.FollowOffset;
            o.y = eyeHeight;
            follow.FollowOffset = o;
        }

        void HeadbobCycleCalculator()
        {
            if (!enableHeadbob) return;

            if (!isIdle && currentGroundInfo.isGettingGroundInfo && !isSliding)
            {
                headbobWarmUp = Mathf.MoveTowards(headbobWarmUp, 1f, Time.deltaTime * 5f);
                headbobCyclePosition += _2DVelocity.magnitude * (Time.deltaTime * (headbobSpeed / 10f));

                headbobCameraPosition.x = Mathf.Sin(Mathf.PI * (2f * headbobCyclePosition + 0.5f))
                                            * (headbobPower / 50f) * headbobWarmUp;
                headbobCameraPosition.y = (Mathf.Abs(Mathf.Sin(Mathf.PI * 2f * headbobCyclePosition) * 0.75f)
                                            * (headbobPower / 50f) * headbobWarmUp) + standingEyeHeight;
                headbobCameraPosition.z = Mathf.Sin(Mathf.PI * 2f * headbobCyclePosition)
                                            * (ZTilt / 3f) * headbobWarmUp;
            }
            else
            {
                headbobCameraPosition = Vector3.MoveTowards(headbobCameraPosition,
                    Vector3.up * standingEyeHeight, Time.deltaTime / (headbobPower * 0.3f));
                headbobWarmUp = 0.1f;
            }

            // Drive the pivot's local position for headbob (X sway + Y height)
            if (_cameraPitchPivot != null)
            {
                _cameraPitchPivot.localPosition = new Vector3(
                    headbobCameraPosition.x,
                    headbobCameraPosition.y,
                    0f);
            }

            if (StepCycle > headbobCyclePosition * 3f)
                StepCycle = headbobCyclePosition + 0.5f;
        }

        #endregion

        // =======================================================================
        #region Movement Functions

        void MovePlayer(Vector3 direction, float speed)
        {
            if (_isDashing) return;   // dash controls velocity directly
            isIdle = direction.normalized.magnitude <= 0f;
            _2DVelocity = new Vector2(p_Rigidbody.linearVelocity.x, p_Rigidbody.linearVelocity.z);
            _2DVelocityMag = Mathf.Clamp((walkingSpeed / 50f) / Mathf.Max(_2DVelocity.magnitude, 0.001f), 0f, 2f);

            if (currentGroundInfo.isGettingGroundInfo && !Jumped && !isSliding && !doingPosInterp)
            {
                if (direction.magnitude == 0f && p_Rigidbody.linearVelocity.normalized.magnitude > 0.1f)
                {
                    p_Rigidbody.AddForce(
                        -new Vector3(p_Rigidbody.linearVelocity.x,
                            currentGroundInfo.isInContactWithGround
                                ? p_Rigidbody.linearVelocity.y - Physics.gravity.y : 0f,
                            p_Rigidbody.linearVelocity.z) * (decelerationSpeed * Time.fixedDeltaTime),
                        ForceMode.Force);
                }
                else if (currentGroundInfo.groundAngle < hardSlopeLimit &&
                           currentGroundInfo.groundAngle_Raw < hardSlopeLimit)
                {
                    // Target is a m/s velocity; step is acceleration per second * fixedDeltaTime
                    Vector3 targetVel = Vector3.ClampMagnitude(direction * (speed / 50f), speed / 50f)
                                        + Vector3.down;
                    p_Rigidbody.linearVelocity = Vector3.MoveTowards(
                        p_Rigidbody.linearVelocity,
                        targetVel,
                        (speed / 50f) * 10f * Time.fixedDeltaTime);
                }
                capsule.sharedMaterial = InputDir.magnitude > 0f ? _ZeroFriction : _MaxFriction;

            }
            else if (!currentGroundInfo.isGettingGroundInfo)
            {
                p_Rigidbody.AddForce(
                    direction * walkingSpeed * Time.fixedDeltaTime * airControlFactor * 5f
                    * currentGroundInfo.groundAngleMultiplier_Inverse_persistent,
                    ForceMode.Acceleration);
                p_Rigidbody.linearVelocity =
                    Vector3.ClampMagnitude(
                        new Vector3(p_Rigidbody.linearVelocity.x, 0f, p_Rigidbody.linearVelocity.z),
                        walkingSpeed / 50f)
                    + Vector3.up * p_Rigidbody.linearVelocity.y;

                if (!currentGroundInfo.potentialStair && jumpEnhancements)
                {
                    if (p_Rigidbody.linearVelocity.y < 0f &&
                        p_Rigidbody.linearVelocity.y > Physics.gravity.y * 1.5f)
                        p_Rigidbody.linearVelocity += Vector3.up * Physics.gravity.y
                            * decentMultiplier * Time.fixedDeltaTime;
                    else if (p_Rigidbody.linearVelocity.y > 0f && !jumpInput_Momentary)
                        p_Rigidbody.linearVelocity += Vector3.up * Physics.gravity.y
                            * (tapJumpMultiplier - 1f) * Time.fixedDeltaTime;
                }
            }
        }

        void Jump(float force)
        {
            if (currentGroundInfo.groundAngle >= hardSlopeLimit) return;
            if (Time.time <= jumpBlankingPeriod + 0.1f) return;

            Jumped = true;
            p_Rigidbody.linearVelocity =
                new Vector3(p_Rigidbody.linearVelocity.x, 0f, p_Rigidbody.linearVelocity.z);
            p_Rigidbody.AddForce(Vector3.up * (force / 10f), ForceMode.Impulse);
            capsule.sharedMaterial = _ZeroFriction;
            jumpBlankingPeriod = Time.time;
        }

        void DoubleJump(float force)
        {
            if (Time.time <= jumpBlankingPeriod + 0.05f) return;

            // Reset vertical velocity so the double jump always feels consistent
            p_Rigidbody.linearVelocity =
                new Vector3(p_Rigidbody.linearVelocity.x, 0f, p_Rigidbody.linearVelocity.z);
            p_Rigidbody.AddForce(Vector3.up * (force / 10f), ForceMode.Impulse);
            capsule.sharedMaterial = _ZeroFriction;
            jumpBlankingPeriod = Time.time;
            if (enableMovementDebugging) print("Double Jump!");
        }

        public void DoJump(float force = 10f)
        {
            if (Time.time <= jumpBlankingPeriod + 0.1f) return;
            Jumped = true;
            p_Rigidbody.linearVelocity =
                new Vector3(p_Rigidbody.linearVelocity.x, 0f, p_Rigidbody.linearVelocity.z);
            p_Rigidbody.AddForce(Vector3.up * (force / 10f), ForceMode.Impulse);
            capsule.sharedMaterial = _ZeroFriction;
            jumpBlankingPeriod = Time.time;
        }

        void Slide()
        {
            if (enableMovementDebugging) print("Starting Slide.");
            // ForceMode.VelocityChange sets velocity directly (mass-independent, framerate-independent)
            p_Rigidbody.AddForce(
                transform.forward * (walkingSpeed / 50f) + Vector3.up * currentGroundInfo.groundInfluenceDirection.y,
                ForceMode.VelocityChange);
            cachedDirPreSlide = transform.forward;
            cachedPosPreSlide = transform.position;
            capsule.sharedMaterial = _ZeroFriction;
            SoundManager.Instance.PlaySound(SoundType.Slide);
            isSliding = true;
        }

        void SlidePhysics()
        {
            p_Rigidbody.AddForce(
                -(p_Rigidbody.linearVelocity - Physics.gravity) * (slidingDeceleration * Time.fixedDeltaTime),
                ForceMode.Force);

            if (slideInput_Momentary &&
                Vector3.Distance(transform.position, cachedPosPreSlide) < maxFlatSlideDistance)
                p_Rigidbody.AddForce(cachedDirPreSlide * (walkingSpeed / 50f) * Time.fixedDeltaTime,
                    ForceMode.VelocityChange);

            if (!slideInput_Momentary ||
                p_Rigidbody.linearVelocity.magnitude < walkingSpeed / 100f)
            {
                if (enableMovementDebugging) print("Ending Slide.");
                isSliding = false;
                capsule.sharedMaterial = _MaxFriction;
            }
        }

        void StartDash()
        {
            _isDashing = true;
            _hasDash = false;   // consume the one air dash
            _dashTimer = 0f;
            _dashCooldownTimer = dashCooldown;
            capsule.sharedMaterial = _ZeroFriction;
            SoundManager.Instance.PlaySound(SoundType.Dash);
            // Direction: player input or fallback to forward
            _dashDirection = (dashUsesInputDirection && InputDir.magnitude > 0.1f)
                ? InputDir.normalized
                : transform.forward;

            // Kill current velocity so dash distance is consistent
            p_Rigidbody.linearVelocity = Vector3.zero;
            if (enableMovementDebugging) print("Dash started → " + _dashDirection);
        }

        void DashPhysics()
        {
            // Set a constant target speed each FixedUpdate – framerate-independent
            // because we assign velocity directly (not AddForce).
            // dashForce / 100 is interpreted as m/s dash speed.
            p_Rigidbody.linearVelocity = _dashDirection * (dashForce / 100f);
            p_Rigidbody.useGravity = false;
        }

        void EndDash()
        {
            _isDashing = false;
            p_Rigidbody.useGravity = true;
            // Bleed off the dash speed so it doesn't carry infinitely
            p_Rigidbody.linearVelocity *= 0.35f;
            capsule.sharedMaterial = _ZeroFriction;
            if (enableMovementDebugging) print("Dash ended.");
        }

        void GetGroundInfo()
        {
            currentGroundInfo.groundFromSweep = Physics.SphereCastAll(
                transform.position, capsule.radius - 0.001f,
                Vector3.down, capsule.height / 2f - capsule.radius / 2f, whatIsGround);

            currentGroundInfo.isInContactWithGround = Physics.Raycast(
                transform.position, Vector3.down,
                out currentGroundInfo.groundFromRay,
                capsule.height / 2f + 0.25f, whatIsGround);

            if (Jumped &&
                (Physics.Raycast(transform.position, Vector3.down, capsule.height / 2f + 0.1f, whatIsGround) ||
                 Physics.CheckSphere(
                     transform.position - Vector3.up * (capsule.height / 2f - (capsule.radius - 0.05f)),
                     capsule.radius, whatIsGround)) &&
                Time.time > jumpBlankingPeriod + 0.1f)
                Jumped = false;

            currentGroundInfo.groundNormals_highgrade = currentGroundInfo.groundNormals_highgrade ?? new List<Vector3>();

            if (currentGroundInfo.groundFromSweep != null && currentGroundInfo.groundFromSweep.Length > 0)
            {
                currentGroundInfo.isGettingGroundInfo = true;
                currentGroundInfo.groundNormals_lowgrade.Clear();
                currentGroundInfo.groundNormals_highgrade.Clear();

                foreach (var hit in currentGroundInfo.groundFromSweep)
                {
                    if (hit.point.y > currentGroundInfo.groundFromRay.point.y &&
                        Vector3.Angle(hit.normal, Vector3.up) < hardSlopeLimit)
                        currentGroundInfo.groundNormals_lowgrade.Add(hit.normal);
                    else
                        currentGroundInfo.groundNormals_highgrade.Add(hit.normal);
                }

                currentGroundInfo.groundNormal_Averaged =
                    currentGroundInfo.groundNormals_lowgrade.Any()
                        ? Average(currentGroundInfo.groundNormals_lowgrade)
                        : Average(currentGroundInfo.groundNormals_highgrade);

                currentGroundInfo.groundNormal_Raw = currentGroundInfo.groundFromRay.normal;
                currentGroundInfo.groundRawYPosition = currentGroundInfo.groundFromSweep.Average(x =>
                    x.point.y > currentGroundInfo.groundFromRay.point.y &&
                    Vector3.Angle(x.normal, Vector3.up) < hardSlopeLimit
                        ? x.point.y : currentGroundInfo.groundFromRay.point.y);
            }
            else
            {
                currentGroundInfo.isGettingGroundInfo = false;
                currentGroundInfo.groundNormal_Averaged = currentGroundInfo.groundFromRay.normal;
                currentGroundInfo.groundNormal_Raw = currentGroundInfo.groundFromRay.normal;
                currentGroundInfo.groundRawYPosition = currentGroundInfo.groundFromRay.point.y;
            }

            if (currentGroundInfo.isGettingGroundInfo)
                currentGroundInfo.groundAngleMultiplier_Inverse_persistent =
                    currentGroundInfo.groundAngleMultiplier_Inverse;

            currentGroundInfo.groundInfluenceDirection = Vector3.MoveTowards(
                currentGroundInfo.groundInfluenceDirection,
                Vector3.Cross(currentGroundInfo.groundNormal_Averaged,
                    Vector3.Cross(currentGroundInfo.groundNormal_Averaged, Vector3.up)).normalized,
                2f * Time.fixedDeltaTime);
            currentGroundInfo.groundInfluenceDirection.y = 0f;

            currentGroundInfo.groundAngle = Vector3.Angle(currentGroundInfo.groundNormal_Averaged, Vector3.up);
            currentGroundInfo.groundAngle_Raw = Vector3.Angle(currentGroundInfo.groundNormal_Raw, Vector3.up);
            currentGroundInfo.groundAngleMultiplier_Inverse = (currentGroundInfo.groundAngle - 90f) * -1f / 90f;
            currentGroundInfo.groundAngleMultiplier = currentGroundInfo.groundAngle / 90f;
            currentGroundInfo.groundTag = currentGroundInfo.isInContactWithGround
                ? currentGroundInfo.groundFromRay.transform.tag : string.Empty;

            // Stair stepping
            if (Physics.Raycast(
                    transform.position + Vector3.down * (capsule.height * 0.5f - 0.1f),
                    InputDir, out currentGroundInfo.stairCheck_RiserCheck,
                    capsule.radius + 0.1f, whatIsGround) &&
                Physics.Raycast(
                    currentGroundInfo.stairCheck_RiserCheck.point +
                    currentGroundInfo.stairCheck_RiserCheck.normal * -0.05f + Vector3.up,
                    Vector3.down, out currentGroundInfo.stairCheck_HeightCheck, 1.1f) &&
                !Physics.Raycast(
                    transform.position + Vector3.down * (capsule.height * 0.5f - maxStairRise) +
                    InputDir * (capsule.radius - 0.05f), InputDir, 0.2f, whatIsGround) &&
                !isIdle &&
                currentGroundInfo.stairCheck_HeightCheck.point.y >
                    currentGroundInfo.stairCheck_RiserCheck.point.y + 0.025f &&
                Vector3.Angle(currentGroundInfo.groundNormal_Averaged,
                    currentGroundInfo.stairCheck_RiserCheck.normal) > 0.5f)
            {
                p_Rigidbody.position += Vector3.up * 0.1f;
                currentGroundInfo.potentialStair = true;
            }
            else
            {
                currentGroundInfo.potentialStair = false;
            }

            currentGroundInfo.playerGroundPosition = Mathf.MoveTowards(
                currentGroundInfo.playerGroundPosition,
                currentGroundInfo.groundRawYPosition + capsule.height / 2f + 0.01f, 0.05f);

            // Footstep material detection
            if (currentGroundInfo.isInContactWithGround && enableFootstepSounds &&
                shouldCalculateFootstepTriggers)
            {
                if (currentGroundInfo.groundFromRay.collider is TerrainCollider)
                {
                    currentGroundInfo.groundMaterial = null;
                    currentGroundInfo.groundPhysicMaterial = currentGroundInfo.groundFromRay.collider.sharedMaterial;
                    currentGroundInfo.currentTerrain =
                        currentGroundInfo.groundFromRay.transform.GetComponent<Terrain>();
                    if (currentGroundInfo.currentTerrain)
                    {
                        float tx = (transform.position.x - currentGroundInfo.currentTerrain.transform.position.x)
                                    / currentGroundInfo.currentTerrain.terrainData.size.x
                                    * currentGroundInfo.currentTerrain.terrainData.alphamapWidth;
                        float tz = (transform.position.z - currentGroundInfo.currentTerrain.transform.position.z)
                                    / currentGroundInfo.currentTerrain.terrainData.size.z
                                    * currentGroundInfo.currentTerrain.terrainData.alphamapHeight;
                        float[,,] aMap = currentGroundInfo.currentTerrain.terrainData.GetAlphamaps(
                            (int)tx, (int)tz, 1, 1);
                        for (int i = 0; i < aMap.Length; i++)
                        {
                            if (aMap[0, 0, i] == 1f)
                            {
                                currentGroundInfo.groundLayer =
                                    currentGroundInfo.currentTerrain.terrainData.terrainLayers[i];
                                break;
                            }
                        }
                    }
                    else { currentGroundInfo.groundLayer = null; }
                }
                else
                {
                    currentGroundInfo.groundLayer = null;
                    currentGroundInfo.groundPhysicMaterial = currentGroundInfo.groundFromRay.collider.sharedMaterial;
                    var mf = currentGroundInfo.groundFromRay.transform.GetComponent<MeshFilter>();
                    currentGroundInfo.currentMesh = mf ? mf.sharedMesh : null;
                    if (currentGroundInfo.currentMesh && currentGroundInfo.currentMesh.isReadable)
                    {
                        int limit = currentGroundInfo.groundFromRay.triangleIndex * 3, submesh;
                        for (submesh = 0; submesh < currentGroundInfo.currentMesh.subMeshCount; submesh++)
                        {
                            int idx = currentGroundInfo.currentMesh.GetTriangles(submesh).Length;
                            if (idx > limit) break;
                            limit -= idx;
                        }
                        currentGroundInfo.groundMaterial =
                            currentGroundInfo.groundFromRay.transform
                            .GetComponent<Renderer>().sharedMaterials[submesh];
                    }
                    else
                    {
                        var mr = currentGroundInfo.groundFromRay.collider.GetComponent<MeshRenderer>();
                        currentGroundInfo.groundMaterial = mr ? mr.sharedMaterial : null;
                    }
                }
            }
            else
            {
                currentGroundInfo.groundMaterial = null;
                currentGroundInfo.groundLayer = null;
                currentGroundInfo.groundPhysicMaterial = null;
            }

#if UNITY_EDITOR
            if (enableGroundingDebugging)
            {
                Debug.DrawRay(transform.position, Vector3.down * (capsule.height / 2f + 0.1f), Color.green);
                Debug.DrawRay(transform.position, currentGroundInfo.groundInfluenceDirection, Color.magenta);
            }
#endif
        }

        bool OverheadCheck() =>
            !Physics.Raycast(transform.position, Vector3.up,
                standingHeight - capsule.height / 2f, whatIsGround);

        Vector3 Average(List<Vector3> vectors)
        {
            Vector3 sum = Vector3.zero;
            vectors.ForEach(v => sum += v);
            return sum / vectors.Count;
        }

        #endregion

        // =======================================================================
        #region Parkour
#if SAIO_ENABLE_PARKOUR
    void VaultCheck() {
        if (isVaulting) { if (!doingPosInterp) isVaulting = false; return; }
        if (!Physics.Raycast(transform.position - Vector3.up * (capsule.height / 4f),
            transform.forward, out VC_Stage1, capsule.radius * 2f) ||
            !VC_Stage1.transform.CompareTag(vaultObjectTag)) return;

        var up2   = Quaternion.LookRotation(VC_Stage1.normal, Vector3.up) * Vector3.up;
        float ang = Mathf.Acos(Vector3.Dot(Vector3.up, up2)) * Mathf.Rad2Deg;

        if (!Physics.Raycast(VC_Stage1.normal * -0.05f + VC_Stage1.point + up2 * maxVaultHeight,
            -up2, out VC_Stage2, capsule.height) ||
            VC_Stage2.transform != VC_Stage1.transform ||
            VC_Stage2.point.y > currentGroundInfo.groundRawYPosition + maxVaultHeight + ang) return;

        vaultForwardVec = -VC_Stage1.normal;
        if (!Physics.Linecast(
            VC_Stage2.point + vaultForwardVec * maxVaultDepth - Vector3.up * 0.01f,
            VC_Stage2.point - Vector3.up * 0.01f, out VC_Stage3)) return;

        Ray vc4 = new Ray(VC_Stage3.point + vaultForwardVec * (capsule.radius + ang * 0.01f), Vector3.down);
        Physics.SphereCast(vc4, capsule.radius, out VC_Stage4, maxVaultHeight + capsule.height / 2f);
        Vector3 proposed = new Vector3(vc4.origin.x,
            VC_Stage4.point.y + capsule.height / 2f + 0.01f, vc4.origin.z) + VC_Stage3.normal * 0.02f;

        if (VC_Stage4.collider &&
            !Physics.CheckCapsule(
                proposed - Vector3.up * (capsule.height / 2f - capsule.radius),
                proposed + Vector3.up * (capsule.height / 2f - capsule.radius), capsule.radius)) {
            isVaulting = true;
            StopCoroutine("PositionInterp");
            StartCoroutine(PositionInterp(proposed, vaultSpeed));
        }
    }

    IEnumerator PositionInterp(Vector3 pos, float speed) {
        doingPosInterp             = true;
        Vector3 vel                = p_Rigidbody.linearVelocity;
        p_Rigidbody.useGravity     = false;
        p_Rigidbody.linearVelocity = Vector3.zero;
        capsule.enabled            = false;
        while (Vector3.Distance(p_Rigidbody.position, pos) > 0.01f) {
            p_Rigidbody.linearVelocity = Vector3.zero;
            p_Rigidbody.position = Vector3.MoveTowards(p_Rigidbody.position, pos, speed * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
        capsule.enabled            = true;
        p_Rigidbody.useGravity     = true;
        p_Rigidbody.linearVelocity = vel;
        doingPosInterp             = false;
        if (isVaulting) VaultCheck();
    }
#endif
        #endregion

        // =======================================================================
        #region Footstep System

        void CalculateFootstepTriggers()
        {
            if (!enableFootstepSounds || !shouldCalculateFootstepTriggers) return;
            if (footstepTriggeringMode != FootstepTriggeringMode.calculatedTiming) return;
            if (_2DVelocity.magnitude <= currentGroundSpeed / 100f || isIdle) return;

            float cycle = enableHeadbob ? headbobCyclePosition : Time.time;
            if (cycle > StepCycle && currentGroundInfo.isGettingGroundInfo && !isSliding)
            {
                CallFootstepClip();
                StepCycle = enableHeadbob
                    ? headbobCyclePosition + 0.5f
                    : Time.time + stepTiming * _2DVelocityMag * 2f;
            }
        }

        public void CallFootstepClip()
        {
            if (!playerAudioSource || !enableFootstepSounds || !footstepSoundSet.Any()) return;
            for (int i = 0; i < footstepSoundSet.Count; i++)
            {
                bool matched = false;
                switch (footstepSoundSet[i].profileTriggerType)
                {
                    case MatProfileType.Material:
                        matched = footstepSoundSet[i]._Materials?.Contains(currentGroundInfo.groundMaterial) == true; break;
                    case MatProfileType.physicMaterial:
                        matched = footstepSoundSet[i]._physicMaterials?.Contains(currentGroundInfo.groundPhysicMaterial) == true; break;
                    case MatProfileType.terrainLayer:
                        matched = footstepSoundSet[i]._Layers?.Contains(currentGroundInfo.groundLayer) == true; break;
                }
                if (matched) { currentClipSet = footstepSoundSet[i].footstepClips; break; }
                if (i == footstepSoundSet.Count - 1) currentClipSet = null;
            }
            if (currentClipSet != null && currentClipSet.Any())
                playerAudioSource.PlayOneShot(currentClipSet[Random.Range(0, currentClipSet.Count)]);
        }

        #endregion

        // =======================================================================
        #region Animator Update

        void UpdateAnimationTriggers(bool zeroOut = false)
        {
            if (!_1stPersonCharacterAnimator) return;
            if (!zeroOut)
            {
                if (a_velocity != "") _1stPersonCharacterAnimator.SetFloat(a_velocity, p_Rigidbody.linearVelocity.sqrMagnitude);
                if (a_2DVelocity != "") _1stPersonCharacterAnimator.SetFloat(a_2DVelocity, _2DVelocity.magnitude);
                if (a_Idle != "") _1stPersonCharacterAnimator.SetBool(a_Idle, isIdle);
                if (a_Sliding != "") _1stPersonCharacterAnimator.SetBool(a_Sliding, isSliding);
                if (a_Jumped != "") _1stPersonCharacterAnimator.SetBool(a_Jumped, Jumped);
                if (a_Grounded != "") _1stPersonCharacterAnimator.SetBool(a_Grounded, currentGroundInfo.isInContactWithGround);
            }
            else
            {
                if (a_velocity != "") _1stPersonCharacterAnimator.SetFloat(a_velocity, 0f);
                if (a_2DVelocity != "") _1stPersonCharacterAnimator.SetFloat(a_2DVelocity, 0f);
                if (a_Idle != "") _1stPersonCharacterAnimator.SetBool(a_Idle, true);
                if (a_Sliding != "") _1stPersonCharacterAnimator.SetBool(a_Sliding, false);
                if (a_Jumped != "") _1stPersonCharacterAnimator.SetBool(a_Jumped, false);
                if (a_Grounded != "") _1stPersonCharacterAnimator.SetBool(a_Grounded, true);
            }
        }

        #endregion

        // =======================================================================
        #region Pause / Unpause

        public void PausePlayer(PauseModes pauseMode)
        {
            controllerPaused = true;
            switch (pauseMode)
            {
                case PauseModes.MakeKinematic: p_Rigidbody.isKinematic = true; break;
                case PauseModes.FreezeInPlace: p_Rigidbody.constraints = RigidbodyConstraints.FreezeAll; break;
            }
            p_Rigidbody.linearVelocity = Vector3.zero;
            InputDir = Vector3.zero;
            MovInput = Vector2.zero;
            capsule.sharedMaterial = _MaxFriction;
            UpdateAnimationTriggers(true);
        }

        public void UnpausePlayer(float delay = 0f)
        {
            if (delay == 0f) ApplyUnpause();
            else StartCoroutine(UnpausePlayerI(delay));
        }

        void ApplyUnpause()
        {
            controllerPaused = false;
            p_Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            p_Rigidbody.isKinematic = false;
        }

        IEnumerator UnpausePlayerI(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            ApplyUnpause();
        }

        #endregion

        // =======================================================================
        #region Gizmos
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (enableGroundingDebugging && Application.isPlaying)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(
                    transform.position - Vector3.up * (capsule.height / 2f - (capsule.radius + 0.1f)),
                    capsule.radius);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(
                    new Vector3(transform.position.x, currentGroundInfo.playerGroundPosition, transform.position.z),
                    0.05f);
            }
        }
#endif
        #endregion

    } // class SUPERCharacterAIO

    // ===========================================================================
    #region Data Classes & Enums

    [System.Serializable]
    public class GroundInfo
    {
        public bool isInContactWithGround, isGettingGroundInfo, potentialStair;
        public float groundAngleMultiplier_Inverse = 1f,
                       groundAngleMultiplier_Inverse_persistent = 1f,
                       groundAngleMultiplier, groundAngle, groundAngle_Raw,
                       playerGroundPosition, groundRawYPosition;
        public Vector3 groundInfluenceDirection, groundNormal_Averaged, groundNormal_Raw;
        public List<Vector3> groundNormals_lowgrade = new List<Vector3>();
        public List<Vector3> groundNormals_highgrade = new List<Vector3>();
        public string groundTag;
        public Material groundMaterial;
        public TerrainLayer groundLayer;
        public PhysicsMaterial groundPhysicMaterial;
        internal Terrain currentTerrain;
        internal Mesh currentMesh;
        internal RaycastHit groundFromRay, stairCheck_RiserCheck, stairCheck_HeightCheck;
        internal RaycastHit[] groundFromSweep;
    }

    [System.Serializable]
    public class GroundMaterialProfile
    {
        public MatProfileType profileTriggerType = MatProfileType.Material;
        public List<Material> _Materials;
        public List<PhysicsMaterial> _physicMaterials;
        public List<TerrainLayer> _Layers;
        public List<AudioClip> footstepClips = new List<AudioClip>();
    }

    public enum MatProfileType { Material, terrainLayer, physicMaterial }
    public enum FootstepTriggeringMode { calculatedTiming, calledFromAnimations }
    public enum PauseModes { MakeKinematic, FreezeInPlace, BlockInputOnly }

    #endregion

    // ===========================================================================
    #region Interfaces
    public interface ICollectable { void Collect(); }
    #endregion

    // ===========================================================================
    #region Editor
#if UNITY_EDITOR
    [CustomEditor(typeof(SUPERCharacterAIO))]
    public class SuperFPEditor : Editor
    {

        SUPERCharacterAIO t;
        SerializedObject tSO;
        SerializedProperty groundLayerMask, groundMatProf;

        GUIStyle headerStyle, subHeaderStyle, showMoreStyle, boxPanel;
        Texture2D boxTex;
        static bool moveFoldout, footstepFoldout;

        void OnEnable()
        {
            t = (SUPERCharacterAIO)target;
            tSO = new SerializedObject(t);
            groundLayerMask = tSO.FindProperty("whatIsGround");
            groundMatProf = tSO.FindProperty("footstepSoundSet");
            boxTex = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
            boxTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.2f));
            boxTex.Apply();
        }

        void InitStyles()
        {
            headerStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 };
            subHeaderStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 10, richText = true };
            showMoreStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, margin = new RectOffset(15, 0, 0, 0), fontStyle = FontStyle.Bold, fontSize = 11, richText = true };
            boxPanel ??= new GUIStyle(GUI.skin.box) { normal = { background = boxTex } };
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            tSO.Update();

            if (Application.isPlaying)
                EditorGUILayout.HelpBox("Inspector changes during play mode may affect physics.", MessageType.Warning);

            // Header
            EditorGUILayout.Space();
            GUILayout.Label(
                "<b><i><size=18><color=#FC80A5>S</color><color=#FFFF9F>U</color><color=#99FF99>P</color>"
                + "<color=#76D7EA>E</color><color=#BF8FCC>R</color></size></i></b> <size=12><i>Character Controller</i></size>",
                new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true, fontSize = 16 },
                GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.MaxHeight(6));

            // ── Input ────────────────────────────────────────────────────────
            Section("Input (Action Maps)");
            EditorGUILayout.BeginVertical(boxPanel);
            t.inputActionAsset = (InputActionAsset)EditorGUILayout.ObjectField(
                new GUIContent("Input Action Asset"), t.inputActionAsset, typeof(InputActionAsset), false);
            t.actionMapName = EditorGUILayout.TextField("Action Map Name", t.actionMapName);
            EditorGUILayout.Space(4);
            GUILayout.Label("<color=grey>Action Names</color>", subHeaderStyle, GUILayout.ExpandWidth(true));
            t.lookActionName = EditorGUILayout.TextField("Look", t.lookActionName);
            t.moveActionName = EditorGUILayout.TextField("Move", t.moveActionName);
            t.jumpActionName = EditorGUILayout.TextField("Jump", t.jumpActionName);
            t.slideActionName = EditorGUILayout.TextField("Slide", t.slideActionName);
            t.dashActionName = EditorGUILayout.TextField("Dash", t.dashActionName);
#if SAIO_ENABLE_PARKOUR
        t.vaultActionName = EditorGUILayout.TextField("Vault", t.vaultActionName);
#endif
            EditorGUILayout.EndVertical();

            // ── Camera / Cinemachine ─────────────────────────────────────────
            Section("Camera & Cinemachine");
            EditorGUILayout.BeginVertical(boxPanel);
            t.cinemachineBrain = (CinemachineBrain)EditorGUILayout.ObjectField(
                new GUIContent("Cinemachine Brain", "CinemachineBrain on the main camera."),
                t.cinemachineBrain, typeof(CinemachineBrain), true);
            t.virtualCamera = (CinemachineCamera)EditorGUILayout.ObjectField(
                new GUIContent("Virtual Camera", "CinemachineCamera that follows the player."),
                t.virtualCamera, typeof(CinemachineCamera), true);
            t.playerCamera = (Camera)EditorGUILayout.ObjectField(
                new GUIContent("Player Camera", "Main Camera (UI / raycasts)."),
                t.playerCamera, typeof(Camera), true);
            EditorGUILayout.Space(4);
            t.lockAndHideMouse = EditorGUILayout.ToggleLeft("Lock & Hide Mouse Cursor", t.lockAndHideMouse);
            t.autoGenerateCrosshair = EditorGUILayout.ToggleLeft("Auto-Generate Crosshair", t.autoGenerateCrosshair);
            GUI.enabled = t.autoGenerateCrosshair;
            t.crosshairSprite = (Sprite)EditorGUILayout.ObjectField("Crosshair Sprite",
                t.crosshairSprite, typeof(Sprite), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            GUI.enabled = true;
            t.standingEyeHeight = EditorGUILayout.Slider(
                new GUIContent("Eye Height", "Drives CinemachineFollow offset Y."),
                t.standingEyeHeight, 0f, 2f);
            t.sensitivity = EditorGUILayout.Slider(
                new GUIContent("Sensitivity", "Gemeinsame Sensitivität für Maus und Controller (1–20)."),
                t.sensitivity, 0.1f, 50f);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField(
                new GUIContent("  → Maus (intern)", "sensitivity / 1000"),
                t.sensitivity / 10f);
            EditorGUILayout.FloatField(
                new GUIContent("  → Stick °/s (intern)", "sensitivity × 30"),
                t.sensitivity * 0.05f);
            EditorGUI.EndDisabledGroup();
            t.verticalRotationRange = EditorGUILayout.Slider(
                new GUIContent("Vertical Rotation Range", "Gesamte vertikale Gradzahl (z.B. 170 = ±85°)."),
                t.verticalRotationRange, 10f, 180f);
            t.invertY = EditorGUILayout.ToggleLeft("Invert Y", t.invertY);
            t.invertX = EditorGUILayout.ToggleLeft("Invert X", t.invertX);
            EditorGUILayout.HelpBox(
                "Setup: Add a CinemachineCamera as child of the player, assign a CinemachineFollow component, " +
                "set Follow = player transform. The script drives the Follow Offset X/Y for headbob.",
                MessageType.Info);
            EditorGUILayout.EndVertical();

            // ── Headbob ──────────────────────────────────────────────────────
            Section("Headbob");
            EditorGUILayout.BeginVertical(boxPanel);
            t.enableHeadbob = EditorGUILayout.ToggleLeft("Enable Headbob", t.enableHeadbob);
            GUI.enabled = t.enableHeadbob;
            t.headbobSpeed = EditorGUILayout.Slider("Speed", t.headbobSpeed, 1f, 5f);
            t.headbobPower = EditorGUILayout.Slider("Power", t.headbobPower, 1f, 5f);
            t.ZTilt = EditorGUILayout.Slider("Z Tilt", t.ZTilt, 0f, 5f);
            GUI.enabled = true;
            EditorGUILayout.EndVertical();

            // ── Movement ─────────────────────────────────────────────────────
            Section("Movement");
            EditorGUILayout.BeginVertical(boxPanel);
            t.enableMovementControl = EditorGUILayout.ToggleLeft("Enable Movement", t.enableMovementControl);
            t.walkingSpeed = EditorGUILayout.Slider("Walking Speed", t.walkingSpeed, 1f, 400f);
            t.decelerationSpeed = EditorGUILayout.Slider("Deceleration", t.decelerationSpeed, 1f, 300f);
            EditorGUILayout.PropertyField(groundLayerMask, new GUIContent("What Is Ground"));
            if (moveFoldout)
            {
                EditorGUILayout.Space(6);
                GUILayout.Label("<color=grey>Slope</color>", subHeaderStyle, GUILayout.ExpandWidth(true));
                t.hardSlopeLimit = EditorGUILayout.Slider("Hard Slope Limit", t.hardSlopeLimit, 45f, 89f);
                t.maxStairRise = EditorGUILayout.Slider("Max Stair Rise", t.maxStairRise, 0f, 1.5f);
                t.stepUpSpeed = EditorGUILayout.Slider("Step Up Speed", t.stepUpSpeed, 0.01f, 0.45f);
                EditorGUILayout.Space(6);
                GUILayout.Label("<color=grey>Jump</color>", subHeaderStyle, GUILayout.ExpandWidth(true));
                t.canJump = EditorGUILayout.ToggleLeft("Can Jump", t.canJump);
                t.holdJump = EditorGUILayout.ToggleLeft("Hold to Jump", t.holdJump);
                t.jumpPower = EditorGUILayout.Slider("Jump Power", t.jumpPower, 1f, 650f);
                t.airControlFactor = EditorGUILayout.Slider("Air Control", t.airControlFactor, 0f, 1f);
                t.jumpEnhancements = EditorGUILayout.ToggleLeft("Jump Enhancements", t.jumpEnhancements);
                if (t.jumpEnhancements)
                {
                    t.decentMultiplier = EditorGUILayout.Slider("Descent Multiplier", t.decentMultiplier, 0.1f, 5f);
                    t.tapJumpMultiplier = EditorGUILayout.Slider("Tap Jump Multiplier", t.tapJumpMultiplier, 0.1f, 5f);
                }
                EditorGUILayout.Space(4);
                GUILayout.Label("<color=grey>Double Jump</color>", subHeaderStyle, GUILayout.ExpandWidth(true));
                t.canDoubleJump = EditorGUILayout.ToggleLeft("Can Double Jump", t.canDoubleJump);
                GUI.enabled = t.canDoubleJump;
                t.doubleJumpPower = EditorGUILayout.Slider("Double Jump Power", t.doubleJumpPower, 1f, 650f);
                GUI.enabled = true;
                EditorGUILayout.Space(4);
                GUILayout.Label("<color=grey>Coyote Time</color>", subHeaderStyle, GUILayout.ExpandWidth(true));
                t.enableCoyoteTime = EditorGUILayout.ToggleLeft("Enable Coyote Time", t.enableCoyoteTime);
                GUI.enabled = t.enableCoyoteTime;
                t.coyoteTime = EditorGUILayout.Slider(new GUIContent("Coyote Time", "Sekunden nach dem Verlassen des Bodens, in denen noch gesprungen werden kann."), t.coyoteTime, 0.05f, 0.5f);
                GUI.enabled = true;
                EditorGUILayout.Space(6);
                GUILayout.Label("<color=grey>Slide</color>", subHeaderStyle, GUILayout.ExpandWidth(true));
                t.canSlide = EditorGUILayout.ToggleLeft("Can Slide", t.canSlide);
                t.slidingDeceleration = EditorGUILayout.Slider("Deceleration", t.slidingDeceleration, 50f, 300f);
                t.slidingTransitionSpeed = EditorGUILayout.Slider("Transition Speed", t.slidingTransitionSpeed, 0.01f, 10f);
                t.maxFlatSlideDistance = EditorGUILayout.Slider("Flat Slide Dist", t.maxFlatSlideDistance, 0.5f, 15f);
                EditorGUILayout.Space(6);
                GUILayout.Label("<color=grey>Air Dash</color>", subHeaderStyle, GUILayout.ExpandWidth(true));
                t.canDash = EditorGUILayout.ToggleLeft("Can Air Dash", t.canDash);
                GUI.enabled = t.canDash;
                t.dashForce = EditorGUILayout.Slider("Dash Force", t.dashForce, 50f, 2000f);
                t.dashDuration = EditorGUILayout.Slider("Dash Duration", t.dashDuration, 0.05f, 1f);
                t.dashCooldown = EditorGUILayout.Slider("Dash Cooldown", t.dashCooldown, 0f, 5f);
                t.dashUsesInputDirection = EditorGUILayout.ToggleLeft(new GUIContent("Use Input Direction", "Aus: immer vorwärts dashen."), t.dashUsesInputDirection);
                GUI.enabled = true;
            }
            EditorGUILayout.Space(4);
            moveFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(moveFoldout,
                moveFoldout ? "<color=#B83C82>show less</color>" : "<color=#B83C82>show more</color>", showMoreStyle);
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndVertical();

#if SAIO_ENABLE_PARKOUR
        // ── Parkour ───────────────────────────────────────────────────────
        Section("Parkour / Vault");
        EditorGUILayout.BeginVertical(boxPanel);
        t.canVault           = EditorGUILayout.ToggleLeft("Can Vault",                t.canVault);
        t.autoVaultWhenMoving = EditorGUILayout.ToggleLeft("Auto-Vault While Moving", t.autoVaultWhenMoving);
        t.vaultObjectTag     = EditorGUILayout.TagField("Vault Tag",                  t.vaultObjectTag);
        t.vaultSpeed         = EditorGUILayout.Slider("Vault Speed",  t.vaultSpeed,    0.1f, 15f);
        t.maxVaultDepth      = EditorGUILayout.Slider("Max Depth",    t.maxVaultDepth, 0.1f,  3f);
        t.maxVaultHeight     = EditorGUILayout.Slider("Max Height",   t.maxVaultHeight,0.1f,  3f);
        EditorGUILayout.EndVertical();
#endif

            // ── Footstep Audio ───────────────────────────────────────────────
            Section("Footstep Audio");
            EditorGUILayout.BeginVertical(boxPanel);
            t.enableFootstepSounds = EditorGUILayout.ToggleLeft("Enable Footstep Sounds", t.enableFootstepSounds);
            GUI.enabled = t.enableFootstepSounds;
            t.footstepTriggeringMode = (FootstepTriggeringMode)EditorGUILayout.EnumPopup("Trigger Mode", t.footstepTriggeringMode);
            if (t.footstepTriggeringMode == FootstepTriggeringMode.calculatedTiming)
                t.stepTiming = EditorGUILayout.Slider("Step Timing", t.stepTiming, 0f, 1f);
            EditorGUI.indentLevel++;
            footstepFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(footstepFoldout,
                footstepFoldout ? "<color=#B83C82>hide clip stacks</color>" : "<color=#B83C82>show clip stacks</color>",
                showMoreStyle);
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUI.indentLevel--;
            if (footstepFoldout && !Application.isPlaying)
            {
                for (int i = 0; i < groundMatProf.arraySize; i++)
                {
                    EditorGUILayout.BeginVertical(boxPanel);
                    var profile = groundMatProf.GetArrayElementAtIndex(i);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Stack {i + 1}", EditorStyles.boldLabel);
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        t.footstepSoundSet.RemoveAt(i);
                        tSO = new SerializedObject(t);
                        groundMatProf = tSO.FindProperty("footstepSoundSet");
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(profile.FindPropertyRelative("profileTriggerType"), new GUIContent("Trigger Mode"));
                    switch (t.footstepSoundSet[i].profileTriggerType)
                    {
                        case MatProfileType.Material:
                            EditorGUILayout.PropertyField(profile.FindPropertyRelative("_Materials"), new GUIContent("Materials")); break;
                        case MatProfileType.physicMaterial:
                            EditorGUILayout.PropertyField(profile.FindPropertyRelative("_physicMaterials"), new GUIContent("Physic Materials")); break;
                        case MatProfileType.terrainLayer:
                            EditorGUILayout.PropertyField(profile.FindPropertyRelative("_Layers"), new GUIContent("Terrain Layers")); break;
                    }
                    EditorGUILayout.PropertyField(profile.FindPropertyRelative("footstepClips"), new GUIContent("Clips"), true);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
                if (GUILayout.Button("Add Profile"))
                    t.footstepSoundSet.Add(new GroundMaterialProfile { footstepClips = new List<AudioClip>() });
                if (GUILayout.Button("Clear All"))
                    t.footstepSoundSet.Clear();
            }
            EditorGUILayout.HelpBox("Material mode requires Read/Write-enabled meshes.", MessageType.Info);
            GUI.enabled = true;
            EditorGUILayout.EndVertical();

            // ── Animator ─────────────────────────────────────────────────────
            Section("Animator");
            EditorGUILayout.BeginVertical(boxPanel);
            t._1stPersonCharacterAnimator = (Animator)EditorGUILayout.ObjectField(
                "Animator", t._1stPersonCharacterAnimator, typeof(Animator), true);
            if (t._1stPersonCharacterAnimator)
            {
                t.a_velocity = EditorGUILayout.TextField("Velocity (Float)", t.a_velocity);
                t.a_2DVelocity = EditorGUILayout.TextField("2D Velocity (Float)", t.a_2DVelocity);
                t.a_Idle = EditorGUILayout.TextField("Idle (Bool)", t.a_Idle);
                t.a_Sliding = EditorGUILayout.TextField("Sliding (Bool)", t.a_Sliding);
                t.a_Jumped = EditorGUILayout.TextField("Jumped (Bool)", t.a_Jumped);
                t.a_Grounded = EditorGUILayout.TextField("Grounded (Bool)", t.a_Grounded);
            }
            EditorGUILayout.EndVertical();

            // ── Debug ────────────────────────────────────────────────────────
            Section("Debug");
            EditorGUILayout.BeginVertical(boxPanel);
            float hw = EditorGUIUtility.currentViewWidth / 2f - 20f;
            EditorGUILayout.BeginHorizontal();
            t.enableGroundingDebugging = GUILayout.Toggle(t.enableGroundingDebugging, "Debug Grounding", "Button", GUILayout.Width(hw));
            t.enableMovementDebugging = GUILayout.Toggle(t.enableMovementDebugging, "Debug Movement", "Button", GUILayout.Width(hw));
            EditorGUILayout.EndHorizontal();
            if (t.enableGroundingDebugging || t.enableMovementDebugging)
                EditorGUILayout.HelpBox("Debuggers add overhead – disable in builds!", MessageType.Warning);
            EditorGUILayout.EndVertical();

            if (GUI.changed) { EditorUtility.SetDirty(t); tSO.ApplyModifiedProperties(); }
        }

        void Section(string title)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.MaxHeight(6));
            GUILayout.Label(title, headerStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.Space(4);
        }
    }
#endif
    #endregion

} // namespace SUPERCharacter
//actual version 