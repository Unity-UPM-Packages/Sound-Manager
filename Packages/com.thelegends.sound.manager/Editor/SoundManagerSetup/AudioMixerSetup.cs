using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using System.IO;
using System.Collections.Generic;

namespace com.thelegends.sound.manager.Editor
{
    /// <summary>
    /// Editor utility for creating and setting up AudioMixer for Sound Manager
    /// </summary>
    public static class AudioMixerSetup
    {
        private const string MixerPath = "Assets/TripSoft/SoundManager/MainAudioMixer.mixer";
        private const string MixerTemplatePath = "Packages/com.thelegends.sound.manager/Editor/Template/MixerTemplate.mixer";
        
        // Parameter names
        private const string MasterVolumeParam = "MasterVolume";
        private const string MusicVolumeParam = "MusicVolume";
        private const string VfxVolumeParam = "VfxVolume";
        private const string UIVolumeParam = "UIVolume";
        
        // Group names
        private const string MasterGroupName = "Master";
        private const string MusicGroupName = "Music";
        private const string VfxGroupName = "Vfx";
        private const string UIGroupName = "UI";
        
        /// <summary>
        /// Creates or gets the main audio mixer for the Sound Manager
        /// </summary>
        /// <returns>The AudioMixer instance</returns>
        public static AudioMixer CreateOrGetAudioMixer()
        {
            // Check if mixer already exists
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer != null)
            {
                Debug.Log("Using existing AudioMixer at " + MixerPath);
                return mixer;
            }
            
            // Create folder if it doesn't exist
            string directory = Path.GetDirectoryName(MixerPath);
            if (!AssetDatabase.IsValidFolder(directory))
            {
                string parentFolder = Path.GetDirectoryName(directory).Replace('\\', '/');
                string folderName = Path.GetFileName(directory);
                
                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    AssetDatabase.CreateFolder("Assets", "TripSoft");
                }
                
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
            
            // Check if template mixer exists
            AudioMixer templateMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerTemplatePath);
            if (templateMixer != null)
            {
                // Copy template mixer to destination
                if (AssetDatabase.CopyAsset(MixerTemplatePath, MixerPath))
                {
                    AssetDatabase.Refresh();
                    
                    // Load the newly created mixer from template
                    mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
                    if (mixer != null)
                    {
                        Debug.Log($"Created AudioMixer from template at {MixerPath}");
                        return mixer;
                    }
                }
                else
                {
                    Debug.LogError($"Failed to copy template mixer from {MixerTemplatePath} to {MixerPath}");
                }
            }
            else 
            {
                Debug.LogError($"Template mixer not found at {MixerTemplatePath}. Make sure it exists in the package.");
            }
            
            // Fallback to original method if template approach fails
            Debug.LogWarning("Template approach failed, falling back to creating mixer through Unity menu");
            if (!CreateMixerAsset())
            {
                Debug.LogError("Failed to create AudioMixer asset. Please create one manually in Unity Editor.");
                return null;
            }
            
            // Load the newly created mixer
            mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null)
            {
                Debug.LogError("Failed to load newly created AudioMixer.");
                return null;
            }
            
            // Configure mixer with groups and parameters
            ConfigureMixer(mixer);
            
            Debug.Log("Created new AudioMixer at " + MixerPath);
            return mixer;
        }
        
        /// <summary>
        /// Create AudioMixer asset using Unity's standard method
        /// </summary>
        private static bool CreateMixerAsset()
        {
            // Method 1: Create menu item command similar to right-click -> Create -> Audio Mixer
            // This method uses reflection to call Unity's internal functions
            
            System.Type audioMixerControllerType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
            System.Type audioMixerUtilsType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerEffectUtils");
            
            if (audioMixerControllerType != null && audioMixerUtilsType != null)
            {
                // Call the Create method of AudioMixerController through reflection
                var createMethod = audioMixerControllerType.GetMethod("Create", 
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                
                if (createMethod != null)
                {
                    try 
                    {
                        // Create mixer and save directly to path
                        createMethod.Invoke(null, new object[] { MixerPath });
                        AssetDatabase.Refresh();
                        return AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath) != null;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Error creating AudioMixer through reflection: " + e.Message);
                    }
                }
            }
            
            // Method 2: Use Unity menu command to create mixer
            // This is a fallback method if reflection doesn't work
            EditorApplication.ExecuteMenuItem("Assets/Create/Audio Mixer");
            
            // Ask user to save the mixer with specific name
            EditorUtility.DisplayDialog("Create Audio Mixer", 
                "Please save the Audio Mixer as 'MainAudioMixer' in the folder 'Assets/TripSoft/SoundManager/'", "OK");
                
            // Wait for user to save
            AssetDatabase.Refresh();
            
            // Check if mixer has been created
            return AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath) != null;
        }
        
        /// <summary>
        /// Configure mixer with groups and parameters
        /// </summary>
        private static void ConfigureMixer(AudioMixer mixer)
        {
            // Get existing master group
            AudioMixerGroup masterGroup = null;
            AudioMixerGroup[] groups = mixer.FindMatchingGroups(string.Empty);
            
            if (groups.Length > 0)
            {
                masterGroup = groups[0];
                RenameAudioMixerGroup(masterGroup, MasterGroupName);
            }
            else
            {
                Debug.LogError("No master group found in mixer. Cannot configure.");
                return;
            }
            
            // Create child groups
            AudioMixerGroup musicGroup = CreateAudioMixerGroup(mixer, MusicGroupName);
            AudioMixerGroup vfxGroup = CreateAudioMixerGroup(mixer, VfxGroupName);
            AudioMixerGroup uiGroup = CreateAudioMixerGroup(mixer, UIGroupName);
            
            // Attach child groups to master (if created successfully)
            if (musicGroup != null) SetAudioMixerGroupParent(musicGroup, masterGroup);
            if (vfxGroup != null) SetAudioMixerGroupParent(vfxGroup, masterGroup);
            if (uiGroup != null) SetAudioMixerGroupParent(uiGroup, masterGroup);
            
            // Set up volume parameters
            mixer.SetFloat(MasterVolumeParam, 0f); // 0dB = unity gain
            mixer.SetFloat(MusicVolumeParam, 0f);
            mixer.SetFloat(VfxVolumeParam, 0f);
            mixer.SetFloat(UIVolumeParam, 0f);
            
            // Expose parameters to be controllable from script
            ExposeMixerParameters(mixer);
            
            // Save changes
            EditorUtility.SetDirty(mixer);
            AssetDatabase.SaveAssets();
        }
        
        /// <summary>
        /// Create a new AudioMixerGroup in the mixer
        /// </summary>
        private static AudioMixerGroup CreateAudioMixerGroup(AudioMixer mixer, string name)
        {
            // Check if group already exists
            AudioMixerGroup[] matchingGroups = mixer.FindMatchingGroups(name);
            if (matchingGroups.Length > 0)
            {
                Debug.Log($"Group {name} already exists in mixer.");
                return matchingGroups[0];
            }
            
            // AudioMixerGroup cannot be created directly - need to use Editor API
            System.Type audioMixerGroupControllerType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerGroupController");
            if (audioMixerGroupControllerType == null)
            {
                Debug.LogError("Could not find AudioMixerGroupController type through reflection.");
                return null;
            }
            
            // Get SerializedObject of mixer to add group
            
            // Depending on Unity version, AudioMixer structure may differ
            // Therefore, we need to create groups by calling functions through Editor API
            
            // This is a workaround - in practice, you should create AudioMixer through Editor
            // and fine-tune it via SerializedObject API
            
            Debug.LogWarning($"Could not create group {name} programmatically. Please add manually in Unity Editor.");
            return null;
        }
        
        /// <summary>
        /// Rename an AudioMixerGroup
        /// </summary>
        private static void RenameAudioMixerGroup(AudioMixerGroup group, string newName)
        {
            // Rename group via SerializedObject
            SerializedObject serializedGroup = new SerializedObject(group);
            SerializedProperty nameProperty = serializedGroup.FindProperty("m_Name");
            if (nameProperty != null)
            {
                nameProperty.stringValue = newName;
                serializedGroup.ApplyModifiedProperties();
            }
        }
        
        /// <summary>
        /// Set parent for an AudioMixerGroup
        /// </summary>
        private static void SetAudioMixerGroupParent(AudioMixerGroup group, AudioMixerGroup parent)
        {
            // Set parent via SerializedObject
            SerializedObject serializedGroup = new SerializedObject(group);
            SerializedProperty parentProperty = serializedGroup.FindProperty("m_ParentGroup");
            if (parentProperty != null)
            {
                parentProperty.objectReferenceValue = parent;
                serializedGroup.ApplyModifiedProperties();
            }
        }
        
        /// <summary>
        /// Exposes the volume parameters so they can be controlled via script
        /// </summary>
        private static void ExposeMixerParameters(AudioMixer mixer)
        {
            // This is a workaround since Unity doesn't provide a clean API to expose parameters
            SerializedObject serializedObject = new SerializedObject(mixer);
            SerializedProperty exposedParams = serializedObject.FindProperty("exposedParameters");
            
            if (exposedParams == null)
            {
                Debug.LogError("Could not find 'exposedParameters' property in AudioMixer.");
                return;
            }
            
            // Master volume
            AddExposedParameter(exposedParams, MasterVolumeParam);
            // Music volume
            AddExposedParameter(exposedParams, MusicVolumeParam);
            // VFX volume
            AddExposedParameter(exposedParams, VfxVolumeParam);
            // UI volume
            AddExposedParameter(exposedParams, UIVolumeParam);
            
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Adds an exposed parameter to the mixer serialized property
        /// </summary>
        private static void AddExposedParameter(SerializedProperty exposedParams, string paramName)
        {
            // First check if it already exists
            for (int i = 0; i < exposedParams.arraySize; i++)
            {
                SerializedProperty param = exposedParams.GetArrayElementAtIndex(i);
                if (param.FindPropertyRelative("name").stringValue == paramName)
                {
                    // Already exists
                    return;
                }
            }
            
            // Add new parameter
            exposedParams.arraySize++;
            SerializedProperty newParam = exposedParams.GetArrayElementAtIndex(exposedParams.arraySize - 1);
            newParam.FindPropertyRelative("name").stringValue = paramName;
            newParam.FindPropertyRelative("exposed").boolValue = true;
        }
    }
}