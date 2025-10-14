// (c) Copyright Evolite Studio. All rights reserved.
// Website: https://www.evolite-studio.com

using UnityEngine;
using UnityEditor;
using TMPro;

#if UNITY_EDITOR

namespace EvoliteStudio.Tools
{
    public class FixText : Editor
    {
        [MenuItem("Tools/Arabic Writer/Fix Selected Text")]
        static void FixSelectedTexts()
        {
            GameObject selectedText = Selection.activeGameObject;

            if (selectedText == null)
            {
                Debug.LogWarning("No object selected! Please select a TextMeshPro or TextMeshProUGUI object.");
                return;
            }

            // Try to get a TextMeshPro or TextMeshProUGUI component
            var textMesh = selectedText.GetComponent<TextMeshPro>();
            var textMeshUI = selectedText.GetComponent<TextMeshProUGUI>();

            if (textMesh != null)
            {
                textMesh.text = ArabicSupport.Fix(textMesh.text, true, false);
                Debug.Log($"Fixed text for {selectedText.name} (TextMeshPro)");
            }
            else if (textMeshUI != null)
            {
                textMeshUI.text = ArabicSupport.Fix(textMeshUI.text, true, false);
                Debug.Log($"Fixed text for {selectedText.name} (TextMeshProUGUI)");
            }
            else
            {
                Debug.LogWarning($"No TextMeshPro or TextMeshProUGUI component found on {selectedText.name}.");
            }
        }
    }
}

#endif