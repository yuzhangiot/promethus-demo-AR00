using UnityEngine;

namespace prometheus
{
    [ExecuteInEditMode]
    public class MeshPreviewPRM : MonoBehaviour
    {
        private MeshPlayerPRM meshPlayerPRM;
        public float PreviewSec;
        public float SourceDurationSec;

        private void Awake()
        {
            if (!Application.isPlaying) {
                Preview(0);
            }     
        }

        public void Preview(float previewSec)
        {
            PreviewSec = previewSec;
            GetMeshPlayComp();
            meshPlayerPRM.Preview(PreviewSec);
            SourceDurationSec = meshPlayerPRM.sourceDurationSec;
            meshPlayerPRM.Uninitialize();
        }

        public void GetMeshPlayComp()
        {
            if (meshPlayerPRM == null)
            {
                meshPlayerPRM = GetComponent<MeshPlayerPRM>();
            }
        }
    }
}
