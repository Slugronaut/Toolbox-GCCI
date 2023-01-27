

namespace Toolbox.GCCI
{
    /*
    /// <summary>
    /// TODO: Implement viewport checks to stop players from attacking when offscreen.
    /// 
    /// </summary>
    [DefaultExecutionOrder(CharacterControllerVelocity.ExecutionOrder - 100)]
    public class PlayerInputReader : LocalListenerBehaviour, IInputSourceComponent
    {
        /// <summary>
        /// Defines a Super attack and it's meta propeties.
        /// </summary>
        [Serializable]
        public class SuperDefinition
        {
            public Tool[] SuperTool;
            public Sprite Icon;
        }


        public MovementTypes MoveType;
        public IGroundedState Grounded;
        public IGravity Gravity;
        public IJumper Jumper;
        public IMover Mover;
        public IAimer Aimer;
        public IAttacker PrimaryAttack;
        public IAttacker MeleeAttack;
        public SuperDefinition[] Supers;
        public Tool WeaponSteal;
        public AirDash Dash;
        public SpriteRenderer Reticule;
        public WeaponInventory Inventory;
        [Tooltip("How far must the aim axis be tilted to count as attacking?")]
        public float AimAttackThreshold = 0.1f;
        [Tooltip("How long after moving the mouse before it is assumed the mouse is no longer controlling aiming?")]
        public float MouseUseTime = 2.0f;
        [Tooltip("Does the mouse directly control the aiming reticule or does it simply rotate it around the player?")]
        public bool DirectMouseCursor = false;
        [HideIf("DirectMouseCursor")]
        [Indent]
        [Tooltip("When using the mouse, is the aiming oriented around the player or around the center of the screen?")]
        public bool CenterMouseAim = true;

        int PlayerId;
        Camera MainCamera;
        Transform MainCameraTrans;
        Rewired.Player PlayerInput;
        byte SuperSelectedIndex = 0;

        static readonly string AtkButton = "Attack";
        static readonly string MeleeButton = "Melee";
        static readonly string Hor = "Horizontal";
        static readonly string Ver = "Vertical";
        static readonly string AimHor = "AimH";
        static readonly string AimVer = "AimV";
        static readonly string JumpButton = "Jump";
        static readonly string NextWepButton = "NextWep";
        static readonly string SuperButton = "Special Attack";
        static readonly string StealButton = "Steal Weapon";
        static readonly string NextSuper = "Next Super";
        static bool Quitting;


        public byte Id { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool AllInputEnabled { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool JumpEnabled { get => Jumper.JumpEnabled; set => Jumper.JumpEnabled = value; }
        public bool MotionEnabled { get => Mover.MoveEnabled; set => Mover.MoveEnabled = value; }
        public bool AimEnabled { get => Aimer.AimEnabled; set => Aimer.AimEnabled = value; }
        public bool AttackEnabled { get => PrimaryAttack.AttackEnabled; set => PrimaryAttack.AttackEnabled = value; }
        public bool InteractEnabled { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        /// <summary>
        /// Controls if the player avatar inputs will be handled. Does not affect UI-based input.
        /// </summary>
        public bool AvatarInputEnabled { get; set; } = true;

        private void Awake()
        {
            Application.quitting += HandleQuit;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private void Start()
        {
            MainCamera = Camera.main;
            MainCameraTrans = MainCamera.transform;
            var avatar = gameObject.FindComponentInEntity<PlayerAvatar>();
            PlayerId = avatar.PlayerId;

            if (PlayerInput == null)
                PlayerInput = PlayerIdFromInt(avatar.PlayerId);

            var root = gameObject.GetEntityRoot();
            GlobalMessagePump.PostMessage(EquippedSuperChanged.Shared.Modify(PlayerId, Supers[SuperSelectedIndex].Icon));
        }

        void OnDisable()
        {
            Aimer.MoveX = 0;
            Aimer.MoveY = 0;
            Mover.InputX = 0;
            Mover.InputY = 0;
            Jumper.JumpInput = false;
            PrimaryAttack.AttackInput = false;
        }

        /// <summary>
        /// Can be used to make the entity look in a specifc direction as though the player had input it.
        /// </summary>
        /// <param name="input"></param>
        public void SimulateLookDirection(Vector2 input)
        {
            Aimer.AimX = input.x;
            Aimer.AimY = input.y;
            Aimer.Aim(new Vector2(input.x, input.y));
        }

        float LastJumpTime;
        Vector3 LastMoveDirection;
        public void Update()
        {
            //All avatar-based handling should be done below this point.
            //Everything above it should be menu/UI based input.
            if (!AvatarInputEnabled)
                return;

            float x, y, ax, ay;
            bool firing;
            bool punching;

            x = PlayerInput.GetAxisRaw(Hor);
            y = PlayerInput.GetAxisRaw(Ver);
            (ax, ay, firing, punching) = GetAimAndAttackInput();
            Reticule.gameObject.SetActive(firing || IsUsingMouse);



            if(PlayerInput.GetButtonDown(NextSuper))
            {
                SuperSelectedIndex++;
                if (SuperSelectedIndex >= Supers.Length)
                    SuperSelectedIndex = 0;
                GlobalMessagePump.PostMessage(EquippedSuperChanged.Shared.Modify(PlayerId, Supers[SuperSelectedIndex].Icon));
            }

            if (WoPConfig.SuperLevel.Current > 0 && PrimaryAttack.AttackEnabled)
            {
                if (PlayerInput.GetButtonDown(StealButton))
                {
                    //resources are now consumed using a tool effect
                    WeaponSteal.Use();
                    GlobalMessagePump.PostMessage(SuperLevelChanged.Shared.Modify(1, string.Empty));
                }
                //Hacking supers right up into this bitch - yeah, we're using the primary weapon's attack flags here too. Woot woot!
                else if (PlayerInput.GetButtonDown(SuperButton))
                {
                    var variants = Supers[SuperSelectedIndex].SuperTool;
                    int index = Mathf.FloorToInt(WoPConfig.SuperLevel.Current) - 1;
                    if (index >= variants.Length)
                        index = variants.Length - 1;

                    variants[index].Use();
                    WoPConfig.SuperLevel.Current = 0;
                    WoPConfig.SuperMeter.Current = 0;
                    GlobalMessagePump.PostMessage(SuperLevelChanged.Shared.Modify(1, string.Empty));
                }
            }
           

            

            //TODO: Viewport check before attacking to avoid cheese
            // && Toolbox.Math.MathUtils.IsInViewport(MainCamera, transform.position))
            if(Grounded.IsGroundedStrict)
                MeleeAttack.AttackInput = punching;
            if(!punching) PrimaryAttack.AttackInput = firing;

            
            //if not able to move but still able to aim, aim in the direction of movement if no aim input is being used
            if(!Mover.MoveEnabled && Aimer.AimEnabled)
            {
                if (!punching) Reticule.gameObject.SetActive(firing);
                float amx = Mathf.Abs(x) > Mathf.Abs(ax) ? x : ax;
                float amy = Mathf.Abs(y) > Mathf.Abs(ay) ? y : ay;
                Aimer.AimX = amx;
                Aimer.AimY = amy;
                Aimer.Aim(new Vector2(amx, amy)); //this gives us immediate results but we still want to push to the inputs of the aimer as you see above
            }

            Jumper.JumpInput = PlayerInput.GetButton(JumpButton);

            Mover.InputX = x;
            Mover.InputY = y;

            Aimer.MoveX = x;
            Aimer.MoveY = y;
            Aimer.AimX = ax;
            Aimer.AimY = ay;
            Aimer.Aim(new Vector2(ax, ay)); //this gives us immediate results but we still want to push to the inputs of the aimer as you see above

            if (!Grounded.IsAirborn)
                GroundedDashReset = true;

            var moveDir = ControllerUtils.CalculateForwardDirection(new Vector2(x, y), MainCameraTrans, MoveType);
            if (moveDir.sqrMagnitude > Thresholds.Tenth)
                LastMoveDirection = moveDir;

            #region Jumping and Air-Dashing
            if (Jumper.JumpEnabled)
            {
                if (Jumper.JumpedThisFrame)
                    LastJumpTime = Time.time;
                else if (GroundedDashReset && Grounded.IsAirborn && Dash.enabled && PlayerInput.GetButtonDown(JumpButton))// && Time.time - LastJumpTime < AirDash.DashWindow)
                {
                    if (CanDash)
                    {
                        //air-dashing input
                        GroundedDashReset = false;
                        Aimer.Aim(LastMoveDirection);
                        Dash.Dash(LastMoveDirection); //only dash horizontally, remove all vertical motion
                        return;
                    }
                }
            }
            #endregion


            #region Inventory
            if (PlayerInput.GetButtonDown(NextWepButton))
                Inventory.NextWeapon();
            #endregion


        }

        bool GroundedDashReset = true;
        /// <summary>
        /// Helper for determing if all requirements are met for performing a dash.
        /// </summary>
        public bool CanDash
        {
            get
            {
                return  Dash.enabled &&
                        (Jumper.IsJumping ||
                        Jumper.IsFallingFromJump);// || //this section allows for dashing from a fall - we probably don't want that
                        //Grounded.IsFalling && Time.time - Grounded.LastGroundedTime > Mathf.Max(Dash.DashWindow, Jumper.JumpWindow));
            }
        }

        void HandleQuit()
        {
            Quitting = true;
        }

        /// <summary>
        /// Helper for converting an int to a PlayerId enum.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Rewired.Player PlayerIdFromInt(int id)
        {
            if (Quitting) return null;
            Assert.IsTrue(id > 0);
            return Rewired.ReInput.players.GetPlayer(id - 1);//remember that Avatar player id isn't zero-based. it starts at 1
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public (float, float, bool, bool) GetAimAndAttackInput()
        {
            float ax = 0;
            float ay = 0;
            bool firing = false;
            bool punching = false;

            if (IsUsingKeyboard())
            {

                //-- MOUSE AND KEYBOARD --
                //We've detected this player pressed a key on the keyboard recently.
                //One last check to see if we are aiming using arrow keys, otherwise w'll use the mouse
                //as our aiming source.
                if (PlayerInput.controllers.hasKeyboard &&
                   (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) ||
                   Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.UpArrow)))
                {
                    ax = PlayerInput.GetAxisRaw(AimHor);
                    ay = PlayerInput.GetAxisRaw(AimVer);
                    firing = (Mathf.Abs(ax) > AimAttackThreshold || Mathf.Abs(ay) > AimAttackThreshold) ? true : false;
                }
                else if (IsUsingMouse)
                {
                    //otherwise, using mouse
                    firing = Input.GetMouseButton(0);
                    punching = WoPConfig.AllowMelee & PlayerInput.GetButtonDown(MeleeButton);// Input.GetMouseButton(1);// this won't work for MacOS :(
                    var mouseViewPoint = MainCamera.ScreenToViewportPoint(Input.mousePosition);
                    Vector2 mouseAim = mouseViewPoint - new Vector3(0.5f, 0.5f);
                    ax = mouseAim.x;
                    ay = mouseAim.y;
                }
            }
            else
            {
                //-- GAMEPAD --
                //we aren't using mouse or keyboard, poll the controller actions like normal
                ax = PlayerInput.GetAxisRaw(AimHor);
                ay = PlayerInput.GetAxisRaw(AimVer);
                firing = (Mathf.Abs(ax) > AimAttackThreshold || Mathf.Abs(ay) > AimAttackThreshold) ? true : false;
                punching = WoPConfig.AllowMelee & PlayerInput.GetButtonDown(MeleeButton);

                //this is a weird one - in order for charged weapons to be able to be fired by pressing in a thumbstick,
                //we need to set 'firing' to false when the 'attack' button is pressed
                if (PlayerInput.GetButtonDown(AtkButton))
                    firing = false;

            }

            return (ax, ay, firing, punching);
        }

        public bool IsUsingKeyboard()
        {
            bool usingMouseKeyboard = true;
            var lastController = PlayerInput.controllers.GetLastActiveController();
            if (lastController != null)
                usingMouseKeyboard = lastController.type == Rewired.ControllerType.Mouse || lastController.type == Rewired.ControllerType.Keyboard;

            return usingMouseKeyboard;
        }


        /// <summary>
        /// Returns true if this player input system is using a mouse.
        /// </summary>
        public bool IsUsingMouse
        {
            get { return IsMouseMoving(this.PlayerInput); }
        }

        bool LastMouseResult;
        Vector3 LastMousePos;
        float LastTimeSinceMouse = float.MinValue;
        /// <summary>
        /// Helper to detect if this player has been moving the mouse recently.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        bool IsMouseMoving(Rewired.Player input)
        {
            //this is needed to ensure the mouse is detected as moving upon startup
            if (LastTimeSinceMouse < 0)
            {
                LastTimeSinceMouse = 0;
                LastMousePos = Input.mousePosition;
                return false;
            }

            if (input.controllers.hasMouse)
            {
                //code for checking startup state 'cause Unity is a prick :p
                if (Time.realtimeSinceStartup - LastTimeSinceMouse > MouseUseTime || Time.realtimeSinceStartup < MouseUseTime)
                {
                    var pos = Input.mousePosition;
                    LastMouseResult = (pos - LastMousePos).sqrMagnitude > 0.01f;
                    LastMousePos = pos;
                    if (LastMouseResult) LastTimeSinceMouse = Time.realtimeSinceStartup;
                    return LastMouseResult;
                }
                else return LastMouseResult;
            }
            return false;
        }


    }
    */
}
