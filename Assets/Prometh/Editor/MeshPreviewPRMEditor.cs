using UnityEngine;
using UnityEditor;

namespace prometheus
{
    [CustomEditor(typeof(MeshPreviewPRM))]
    public class MeshPreviewPRMEditor : Editor
    {
        float selectSec = 0.0f;
        public override void OnInspectorGUI()
        {
            MeshPreviewPRM mTarget = (MeshPreviewPRM)target;    
            BuildFilesInspector(mTarget);
        }

        private void BuildFilesInspector(MeshPreviewPRM mTarget)
        {
            GUILayout.Label("SourceDurationSec:" + mTarget.SourceDurationSec);
            GUIContent previewframe = new GUIContent("Preview Frame (Seconds)");
            selectSec = EditorGUILayout.Slider(previewframe, selectSec, 0, mTarget.SourceDurationSec);
            if (!EditorApplication.isPlaying && mTarget.PreviewSec != selectSec)
            {
                mTarget.Preview(selectSec);
            }
            GUILayout.Space(10);
        }
    }
}

