using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RaycastCulling.Script.PreProcess
{
    /// <summary>
    /// 計算済みモートンオーダーをGPUで扱えるように一次元配列に変換する
    /// </summary>
    public static class ConvertBoundingBoxList
    {
        public static (Vector4[] OccluderBoundingBox, BoundingBoxToMeshRenderer[] meshRenderers) Convert2DListTo1DList(List<List<BoundingBoxToMeshRenderer>>  mortonOrderList)
        {
            //要素数を計算する
            var elementNum = mortonOrderList.Count;
            var maxIndex = 0;
            foreach (var mortonSpace in mortonOrderList)
            {
                elementNum += mortonSpace.Count * 2;
                if (mortonSpace.Count != 0)
                {
                    maxIndex = Mathf.Max(mortonSpace.Max(m => m.MeshRendererIndex),maxIndex);
                }
            }
            
            //要素数分の配列を確保する
            var result = new Vector4[elementNum];

            var meshRenderers = new BoundingBoxToMeshRenderer[maxIndex+1];
            
            var dataPartCurrentIndex = mortonOrderList.Count;
            for (int i = 0; i < mortonOrderList.Count; i++)
            {
                var mortonSpace = mortonOrderList[i];
                
                //floatの計算誤差で0.999999などにならないように0.5を足しておく
                result[i] = new Vector3(-1.5f, dataPartCurrentIndex + 0.5f, mortonSpace.Count * 2 + 0.5f);
                
                foreach (var boundingBox in mortonSpace)
                {
                    result[dataPartCurrentIndex] = new Vector4(boundingBox.BoundMinPos.x, boundingBox.BoundMinPos.y, boundingBox.BoundMinPos.z,boundingBox.MeshRendererIndex + 0.5f);
                    result[dataPartCurrentIndex + 1] = new Vector4(boundingBox.BoundMaxPos.x, boundingBox.BoundMaxPos.y, boundingBox.BoundMaxPos.z,boundingBox.MeshRendererIndex + 0.5f);
                    
                    meshRenderers[boundingBox.MeshRendererIndex] = boundingBox;
                    
                    dataPartCurrentIndex+=2;
                }
            }
            
            return (result,meshRenderers);
        }
    }
}