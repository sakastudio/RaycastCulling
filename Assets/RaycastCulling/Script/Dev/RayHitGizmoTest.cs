using System;
using System.Collections.Generic;
using RaycastCulling.Script.PreProcess;
using UnityEngine;

namespace RaycastCulling.Script.Dev
{
    public class RayHitGizmoTest : MonoBehaviour
    {
        [SerializeField] private OcclusionCulling culling;
        [SerializeField] private OcclusionCullingSettings settings;
        [SerializeField] private int spaceSplitNum;
        [SerializeField] private Transform rayStart;

        [SerializeField] private bool レイとヒットしたボックスを表示;
        [SerializeField] private bool レイを表示;
        [SerializeField] private bool レイがチェックしたボックスを表示;
        [SerializeField] private bool レイがヒットした位置を表示;
        [SerializeField] private int モートン配列のモデルを表示 = -1;
        [SerializeField] private bool ヒットしたOccludeを表示;

        private Vector4[] occluderList;
        private Vector4[] occludeList;
        private int[] occludeHitReslut = Array.Empty<int>();
        private BoundingBoxToMeshRenderer[] occluderListIndexToMeshRenderer;
        private BoundingBoxToMeshRenderer[] occludeListIndexToMeshRenderer;

        void OnDrawGizmos() {
            if (rayStart == null)
            {
                return;
            }

            if (occluderList == null)
            {
                var mortonOccluderList = CalculateMortonOrder.CalculateMortonOrderList(culling.Occluder, settings.SpaceRange, spaceSplitNum);
                (occluderList,occluderListIndexToMeshRenderer) = ConvertBoundingBoxList.Convert2DListTo1DList(mortonOccluderList);
                var mortonOccludeList = CalculateMortonOrder.CalculateMortonOrderList(culling.Occludee, settings.SpaceRange, spaceSplitNum);
                (occludeList,occludeListIndexToMeshRenderer) = ConvertBoundingBoxList.Convert2DListTo1DList(mortonOccludeList);
                occludeHitReslut = new int[occludeList.Length];
            }
            
            
            //rayの通り道を計算
            var rayStartPos = rayStart.position;
            var rayDir = rayStart.forward;
            for (int i = 0; i < 100; i++)
            {
                //ListUpMortonRayWay.GetHitIndex(rayStartPos,rayDir,settings.SpaceOriginPos,settings.SpaceRange,spaceSplitNum,mortonList);
            }

            for (int i = 0; i < occludeHitReslut.Length; i++)
            {
                occludeHitReslut[i] = 0;
            }
            var hitIndex = DevelopmentCullingSystem.GetHitIndex(rayStartPos,rayDir,settings.SpaceOriginPos,settings.SpaceRange,spaceSplitNum,occluderList,occludeList,occludeHitReslut);



            if (レイとヒットしたボックスを表示 && occluderListIndexToMeshRenderer != null)
            {
                if (hitIndex != -1)
                {
                    DrawBoundingBox(settings.SpaceOriginPos, DevelopmentCullingSystem.HitMinBoundingBox,DevelopmentCullingSystem.HitMaxBoundingBox);
                }
            }
            if (ヒットしたOccludeを表示)
            {
                for (int i = 0; i < occludeHitReslut.Length; i++)
                {
                    if (occludeHitReslut[i] == 0) continue;
                    var box = occludeListIndexToMeshRenderer[i]; 
                    DrawBoundingBox(settings.SpaceOriginPos, box.BoundMinPos, box.BoundMaxPos);
                }
            }

            if (レイを表示)
            {
                DrawRay(rayStartPos, rayDir);
            }
            
            if (0 <= モートン配列のモデルを表示 && モートン配列のモデルを表示 < occluderList.Length)
            {
                DrawMortonSpace(occluderList, モートン配列のモデルを表示, settings.SpaceOriginPos, settings.SpaceRange);
            }

            if (レイがチェックしたボックスを表示)
            { 
                DrawRayWay(DevelopmentCullingSystem.CheckedRayWay,spaceSplitNum,settings.SpaceRange,settings.SpaceOriginPos);
            }

            if (レイがヒットした位置を表示)
            {
                var worldSpaceHitPos = new Vector3(
                    DevelopmentCullingSystem.HitPos.x  / settings.SpaceRange.x,
                    DevelopmentCullingSystem.HitPos.y  / settings.SpaceRange.y,
                    DevelopmentCullingSystem.HitPos.z  / settings.SpaceRange.z
                );
                Gizmos.DrawSphere(DevelopmentCullingSystem.HitPos + settings.SpaceOriginPos, 0.1f);
            }
        }


        private static void DrawBoundingBox(Vector3 spaceOriginPos,Vector3 min,Vector3 max)
        {
            //ヒットしたボクセルの描画
            var center = spaceOriginPos + (min + max) / 2;
            var size = max - min;
            Gizmos.color = new Color(1,0,0,0.5f);
            Gizmos.DrawCube(center, size * 1.01f);
        }


        private static void DrawRay(Vector3 rayStartPos,Vector3 rayDir)
        {
            //rayの描画
            Gizmos.color = Color.magenta; 
            Gizmos.DrawRay(rayStartPos, rayDir * 100); 
        }
        
        private static void DrawMortonSpace(Vector4[] mortonList,int mortonIndex,Vector3 spaceOriginPos,Vector3 spaceRange)
        {
            if (mortonList == null)
            {
                return;
            }
            var morton = mortonList[mortonIndex];
            if ((int)morton.x != -1)
            {
                return;
            }

            var startIndex = (int)morton.y;
            var count = (int)morton.z;

            for (int i = startIndex; i < startIndex + count; i+=2)
            {
                var minPos = (Vector3)mortonList[i];
                var maxPos = (Vector3)mortonList[i + 1];
                
                var boundingBoxCenterPos = spaceOriginPos + (minPos + maxPos) / 2;
                var boxSize = maxPos - minPos;
                boxSize *= 1.05f;
                
                Gizmos.color = new Color(1,0,0,0.25f);
                Gizmos.DrawCube(boundingBoxCenterPos,boxSize  * 1.05f);
            }


            var (level, spaceNum) = CalculateMortonOrder.ArrayIndexToSpaceLevelAndSpaceNum(mortonIndex);
            var cellSize = spaceRange / (int)Mathf.Pow(2, level);

            var pos = CalculateMortonOrder.MortonNumberToVector3Int(spaceNum);
            var centerPos = spaceOriginPos + new Vector3(pos.x * cellSize.x, pos.y * cellSize.y, pos.z * cellSize.z) + cellSize / 2;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(centerPos, cellSize);
        }
        
        private static void DrawRayWay(List<Vector3Int> rayWay,int spaceSplitNum,Vector3 spaceRange,Vector3 spaceOriginPos)
        {
            //分割後の空間の描画
            var spaceNum =(int)Mathf.Pow(2, spaceSplitNum);
            var cellSize = spaceRange / spaceNum;
            
            var rayWayHash = new HashSet<Vector3Int>();
            rayWayHash.UnionWith(rayWay);

            for (int x = 0; x < spaceNum; x++)
            {
                for (int y = 0; y < spaceNum; y++)
                {
                    for (int z = 0; z < spaceNum; z++)
                    {
                        if (!rayWayHash.Contains(new Vector3Int(x, y, z))) continue;
                        
                        var cellPos = spaceOriginPos + new Vector3(x * cellSize.x, y * cellSize.y, z * cellSize.z) + cellSize / 2;
                        Gizmos.color = new Color(0.0f, 1f, 1f, 0.5f);
                        Gizmos.DrawCube(cellPos, cellSize);
                    }
                }
            } 
        }

        public void ClearMortonList()
        {
            occluderList = null;
        }
    }
}