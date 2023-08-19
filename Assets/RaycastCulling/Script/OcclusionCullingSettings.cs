using System;
using UnityEngine;

namespace RaycastCulling.Script
{
    /// <summary>
    /// ベイク時のセルの座標は実際のセルの左下座標を表す
    /// </summary>
    [Serializable]
    public class OcclusionCullingSettings : MonoBehaviour
    {
        [SerializeField] private Vector3 spaceRange;
        [SerializeField] private Vector3 spaceOffset;
        [SerializeField] private int spaceSplitNum = 6;

        public int SpaceSplitNum => spaceSplitNum;
        public Vector3 SpaceRange => spaceRange;
        public Vector3 SpaceOriginPos => (transform.position + spaceOffset) - SpaceRange / 2;
    }
}