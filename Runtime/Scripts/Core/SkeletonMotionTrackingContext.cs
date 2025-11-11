using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkeletonMotionTrackingContext : IMotionTrackingManager
{
    private MultiplayerMotionTrackingManager multiplayerManager;
    private int skeletonId;

    public MotionTrackingConfiguration Config => multiplayerManager?.Config;

    public SkeletonMotionTrackingContext(MultiplayerMotionTrackingManager manager, int skeletonId)
    {
        this.multiplayerManager = manager;
        this.skeletonId = skeletonId;
    }

    public Transform GetJointByName(string jointName)
    {
        // route call to the multiplayer manager with the specific skeleton ID
        return multiplayerManager?.GetJointByName(skeletonId, jointName);
    }
}
