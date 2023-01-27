using UnityEngine;

namespace Toolbox.GCCI
{
    #region Interfaces
    public interface IVelocityAccumulator
    {
        void AddVelocity(Vector3 vel);
        void OverrideVelocity(Vector3 vel);
        Vector3 CurrentVelocity { get; }
    }

    public interface IMover
    {
        bool MoveEnabled { get; set; }

        float InputX { set; }
        float InputY { set; }
        Vector3 Velocity { get; }
        //bool LockDirection { get; set; }
    }

    public interface IAimer
    {
        bool AimEnabled { get; set; }

        float AimX { set; }
        float AimY { set; }
        float MoveX { set; }
        float MoveY { set; }
        void Aim(Vector3 precorrectedAimDir);
        void Aim(Vector2 aimDir);
    }

    public interface IAttacker
    {
        byte Id { get; set; }
        bool AttackEnabled { get; set; }
        bool AttackInput { set; }
        bool AttackInputUp { set; }
        bool AttackInputDown { set; }
    }
	
    public interface IGravity
    {
        bool GravityEnabled { get; set; }
        float GravityScale { get; set; }
        Vector3 GravityVelocity { get; set; }
    }

    public interface IGroundedState
    {
        bool GroundedEnabled { get; set; }

        float LastGroundedTime { get; }

        /// <summary>
        /// Is this object stricktly grounded? Does not use additional fudge-factors such as timers or raycasts.
        /// </summary>
        bool IsGrounded { get; }

        /// <summary>
        /// Is this object grounded or has been grounded within a threshold time?
        /// </summary>
        bool HasBeenGrounded { get; }

        /// <summary>
        /// Is this object both not grounded and has a negative velocity on the y-axis?
        /// </summary>
        bool IsFalling { get; }
    }

    public interface IJumper
    {
        bool JumpEnabled { get; set; }

        bool JumpInput { set; }
        bool CanJump { get; }
        bool IsJumping { get; }
        bool JumpedThisFrame { get; }
        bool IsFallingFromJump { get; }
        float JumpWindow { get; }

        void Step(float deltaTime);
    }
    #endregion

    public enum UpdateModes
    {
        Manual,
        Update,
        LateUpdate,
        FixedUpdate,
    }


}
