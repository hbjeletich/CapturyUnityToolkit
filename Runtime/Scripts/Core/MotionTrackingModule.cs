using UnityEngine;

public abstract class MotionTrackingModule : MonoBehaviour
{
    // each motion tracking module inherits this and is now a MonoBehaviour!
    // internal
    protected MotionTrackingManager manager;
    protected bool isCalibrated = false;

    // public get variables
    public bool IsCalibrated => isCalibrated;
    public abstract bool IsEnabled { get; }
    public abstract float Sensitivity { get; }
    public abstract bool DebugMode { get; }

    public virtual void Initialize(MotionTrackingManager manager)
    {
        this.manager = manager;
        Debug.Log($"{GetType().Name}: Initialized as GameObject");
    }

    public abstract void Calibrate(Transform[] joints);
    public abstract void UpdateTracking(ref CapturyInputState state, Transform[] joints);
    public abstract bool HasRequiredJoints(Transform[] joints);
    public abstract string[] GetRequiredJointNames();
}