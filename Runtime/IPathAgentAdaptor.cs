using UnityEngine;
using UnityEngine.Events;

namespace Peg.GCCI
{

    /// <summary>
    /// Standard interface used to adapt any pathing agent for use with the Toolbox system.
    /// </summary>
    public interface IPathAgentAdaptor
    {
        bool CancelFollowWhenCloseEnough { get; set; }
        bool HasPath { get; }
        Vector3 PathDest { get; }
        Transform FollowedTarget { get; }
        bool FreezeMotion { get; set; }
        bool enabled { get; set; }
        bool HasStopped { get; }
        //bool AllowRotation { get; set; }
        //bool PauseMotion { get; set; }
        //bool AllowRepath { get; set; }
        //float RepathRate { get; set; }
        float StoppingDistance { get; }
        float RepathRate { get; set; }
        bool SetDestination(Vector3 dest, float closeEnoughToStop);
        bool FollowTarget(Transform target, float closeEnoughToStop);
        void CancelDest();
        void AddOnStoppedCallback(UnityAction<IPathAgentAdaptor> callback);
        void RemoveOnStoppedCallback(UnityAction<IPathAgentAdaptor> callback);
        void AddOnStartMovingCallback(UnityAction<IPathAgentAdaptor> callback);
        void RemoveOnStartMovingCallback(UnityAction<IPathAgentAdaptor> callback);
        Vector3 Position { get; }
        void Teleport(Vector3 dest);
        
    }


    public delegate void MoveDelegate(IPathAgentAdaptor agent);

    /// <summary>
    /// Used to inform modules that provide movement input that they should
    /// cease all motion and processing of motion inputs. This can be utilized
    /// by both direct inputs (players) or for pathing agents.
    /// </summary>
    public class StopMotionInputCmd : IMessageCommand
    {
        public static StopMotionInputCmd Shared = new StopMotionInputCmd();
    }

    /// <summary>
    /// Used to inform modules that provide movement input that they should
    /// cease all motion and processing of motion inputs. This can be utilized
    /// by both direct inputs (players) or for pathing agents.
    /// </summary>
    public class FreezeMotionInputCmd : IMessageCommand
    {
        public static FreezeMotionInputCmd Shared = new FreezeMotionInputCmd(true);

        public bool State { get; private set; }

        public FreezeMotionInputCmd(bool state)
        {
            State = state;
        }

        public FreezeMotionInputCmd ChangeState(bool state)
        {
            State = state;
            return this;
        }
    }
}
