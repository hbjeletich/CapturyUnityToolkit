#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MotionTrackingConfiguration))]
public class MotionTrackingConfigurationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // draw everything normally
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Add Module Configuration", EditorStyles.boldLabel);

        // find all concrete types that extend ModuleConfiguration
        var moduleConfigTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Type.EmptyTypes; }
            })
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(ModuleConfiguration)))
            .OrderBy(t => t.Name)
            .ToArray();

        var config = (MotionTrackingConfiguration)target;

        foreach (var type in moduleConfigTypes)
        {
            // check if this type is already in the list
            bool alreadyAdded = config.modules.Any(m => m != null && m.GetType() == type);

            EditorGUI.BeginDisabledGroup(alreadyAdded);

            string label = alreadyAdded ? $"✓ {type.Name}" : $"+ Add {type.Name}";
            if (GUILayout.Button(label))
            {
                Undo.RecordObject(config, $"Add {type.Name}");
                var instance = (ModuleConfiguration)Activator.CreateInstance(type);
                config.modules.Add(instance);
                EditorUtility.SetDirty(config);
            }

            EditorGUI.EndDisabledGroup();
        }

        if (config.modules.Count > 0)
        {
            EditorGUILayout.Space(5);
            if (GUILayout.Button("Remove Last Module Config"))
            {
                Undo.RecordObject(config, "Remove Module Config");
                config.modules.RemoveAt(config.modules.Count - 1);
                EditorUtility.SetDirty(config);
            }
        }
    }
}
#endif