using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class FixShadersTool : MonoBehaviour
{
    [MenuItem("Tools/Fix Pink Particles (Add Shaders to Build)")]
    public static void AddAlwaysIncludedShaders()
    {
        SerializedObject graphicsSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        SerializedProperty it = graphicsSettings.GetIterator();
        SerializedProperty alwaysIncludedShaders = null;
        
        while (it.NextVisible(true))
        {
            if (it.name == "m_AlwaysIncludedShaders")
            {
                alwaysIncludedShaders = it;
                break;
            }
        }

        if (alwaysIncludedShaders != null)
        {
            string[] shadersToAdd = new string[]
            {
                "Universal Render Pipeline/Particles/Unlit",
                "Universal Render Pipeline/Particles/Lit",
                "Particles/Standard Unlit",
                "Legacy Shaders/Particles/Additive"
            };

            bool changesMade = false;

            foreach (string shaderName in shadersToAdd)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader == null) continue;

                bool alreadyExists = false;
                for (int i = 0; i < alwaysIncludedShaders.arraySize; i++)
                {
                    if (alwaysIncludedShaders.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (!alreadyExists)
                {
                    alwaysIncludedShaders.arraySize++;
                    alwaysIncludedShaders.GetArrayElementAtIndex(alwaysIncludedShaders.arraySize - 1).objectReferenceValue = shader;
                    changesMade = true;
                    Debug.Log("Added " + shaderName + " to Always Included Shaders.");
                }
            }

            if (changesMade)
            {
                graphicsSettings.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.Log("<color=green>Successfully fixed pink shaders! You can now build the game.</color>");
            }
            else
            {
                Debug.Log("Shaders were already in the Always Included list.");
            }
        }
    }
}
