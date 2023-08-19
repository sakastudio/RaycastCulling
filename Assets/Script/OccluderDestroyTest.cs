using RaycastCulling.Script;
using UnityEngine;

namespace Script
{
    public class OccluderDestroyTest : MonoBehaviour
    {
        [SerializeField] private MeshRenderer targetRenderer;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                OcclusionCulling.Instance.SetIgnoreMeshRender(targetRenderer);
                targetRenderer.gameObject.SetActive(false);
            }
            
        }
    }
}