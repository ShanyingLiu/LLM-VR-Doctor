using UnityEngine;
using System.Reflection;

public class SALSADebugger : MonoBehaviour
{
    public CrazyMinnow.SALSA.Salsa salsa; // Assign your SALSA component here in the Inspector.
    public SkinnedMeshRenderer skinnedMeshRenderer; // Assign your Skinned Mesh Renderer here in the Inspector.

    void Update()
    {
        if (salsa != null)
        {
            for (int i = 0; i < salsa.visemes.Count; i++)
            {
                var viseme = salsa.visemes[i];
                if (viseme != null)
                {
                    Debug.Log($"Viseme {i}: {viseme}");
                    var properties = viseme.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in properties)
                    {
                        Debug.Log($"{prop.Name}: {prop.GetValue(viseme)}");
                    }

                    var fields = viseme.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        Debug.Log($"{field.Name}: {field.GetValue(viseme)}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Viseme {i} is null.");
                }
            }
        }
        else
        {
            Debug.LogWarning("SALSA component is not assigned.");
        }

        if (skinnedMeshRenderer != null)
        {
            for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
            {
                Debug.Log($"Blendshape {i}: {skinnedMeshRenderer.GetBlendShapeWeight(i)}");
            }
        }
        else
        {
            Debug.LogWarning("Skinned Mesh Renderer is not assigned.");
        }
    }
}



