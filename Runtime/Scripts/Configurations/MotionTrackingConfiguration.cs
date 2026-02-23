using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "MotionConfig", menuName = "Motion Tracking/Configuration")]
public class MotionTrackingConfiguration : ScriptableObject
{
    [Header("Configuration Info")]
    public string configurationName = "Default";

    [TextArea(2, 4)]
    public string description = "Default motion tracking configuration.";

    [Header("System Settings")]
    public float calibrationDelay = 2.0f;

    [Tooltip("Number of frames to average during calibration")]
    public int calibrationFrames = 30;

    [SerializeReference]
    public List<ModuleConfiguration> modules = new List<ModuleConfiguration>();

    // find a specific module configuration by type
    public T GetModuleConfig<T>() where T : ModuleConfiguration
    {
        for (int i = 0; i < modules.Count; i++)
        {
            if (modules[i] is T typed)
                return typed;
        }
        return null;
    }

    // get all enabled modules
    public IEnumerable<ModuleConfiguration> GetEnabledModules()
    {
        return modules.Where(m => m != null && m.enabled);
    }

    // check if a specific module type is enabled
    public bool IsModuleEnabled<T>() where T : ModuleConfiguration
    {
        var config = GetModuleConfig<T>();
        return config != null && config.enabled;
    }
}