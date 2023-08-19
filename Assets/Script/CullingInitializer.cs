using System.Collections.Generic;
using RaycastCulling.Script;
using UnityEngine;

namespace Script
{
    public class CullingInitializer : MonoBehaviour
    {
        [SerializeField] private OcclusionCullingSettings settings;
        [SerializeField] private OcclusionCulling occlusionCulling;
        [SerializeField] private List<MeshRenderer> occluders;
        [SerializeField] private List<MeshRenderer> occludees;
        
        private void Start()
        {
            var occluderResult = new List<BoundingBoxToMeshRenderer>();
            foreach (var occluder in occluders)
            {
                var min = occluder.bounds.min - settings.SpaceOriginPos;
                var max = occluder.bounds.max - settings.SpaceOriginPos;
                occluderResult.Add(new BoundingBoxToMeshRenderer(min, max, occluder));
            }
            
            var occludeResult = new List<BoundingBoxToMeshRenderer>();
            foreach (var occludee in occludees)
            {
                var min = occludee.bounds.min - settings.SpaceOriginPos;
                var max = occludee.bounds.max - settings.SpaceOriginPos;
                occludeResult.Add(new BoundingBoxToMeshRenderer(min, max, occludee));
            }

            occlusionCulling.Initialize(occluderResult.ToArray(), occludeResult.ToArray(),settings);

        }
    }
}