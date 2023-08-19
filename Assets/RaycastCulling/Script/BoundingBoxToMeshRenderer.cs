using UnityEngine;

namespace RaycastCulling.Script
{
    public class BoundingBoxToMeshRenderer
    {
        public int MeshRendererIndex { get; set; }


        public Vector3 BoundMinPos { get; }
        public Vector3 BoundMaxPos { get; }
        public MeshRenderer Target { get; }

        public Vector3 CenterPos => (BoundMinPos + BoundMaxPos) / 2;
        public Vector3 Size => BoundMaxPos - BoundMinPos;

        public BoundingBoxToMeshRenderer(Vector3 boundMinPos,Vector3 boundMaxPos, MeshRenderer target,int index = 0)
        {
            BoundMinPos = boundMinPos;
            BoundMaxPos = boundMaxPos;
            Target = target;
            
            MeshRendererIndex = index;
        }
    
    
        public void SetForceRenderingOff(bool isForceRenderingOff)
        {
            Target.forceRenderingOff = isForceRenderingOff;
        }
    }
}