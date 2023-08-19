using System.Collections.Generic;
using RaycastCulling.Script.PreProcess;
using UnityEngine;

namespace RaycastCulling.Script.Dev
{
    public static class DevelopmentCullingSystem
    {
        static Vector3 IsCollision(Vector3 rayDir,Vector3 rayFromPos,Vector3 aabbLeftPos,Vector3 aabbRightPos)
        {
            float tMin = -100000000.0f;
            float tMax = 100000000.0f;

            int backCount = 0;

            for (int i = 0; i < 3; i++)
            {
                float invDir = 1.0f / rayDir[i];

                float leftPos = (aabbLeftPos[i] - rayFromPos[i]) * invDir;
                float rightPos = (aabbRightPos[i] - rayFromPos[i]) * invDir;

                //レイの開始位置が後ろにある場合
                if (leftPos < 0 || rightPos < 0 )
                {
                    backCount++;
                }

                if (invDir < 0.0f)
                {
                    float temp = leftPos;
                    leftPos = rightPos;
                    rightPos = temp;
                }

                tMin = Mathf.Max(tMin, leftPos);
                tMax = Mathf.Min(tMax, rightPos);
                
                if (tMax <= tMin)
                {
                    return new Vector3(-1,-1,-1);
                }
            }

            if (backCount == 3)
            {
                return new Vector3(-1,-1,-1);
            }

            return rayFromPos + rayDir * tMin;
        }

        public static Vector3 HitPos;
        public static Vector3 HitMinBoundingBox;
        public static Vector3 HitMaxBoundingBox;

        private static void OccludeCollisionCheck(int spaceIndex ,float occluderHitSqrMagnitude, Vector4[] occludeBoundingBox,int[] occludeResult, Vector3 startRayMortonSpacePosition, Vector3 rayDirection)
        {
            int startIndex = (int)occludeBoundingBox[spaceIndex].y;
            int spaceVoxelCount = (int)occludeBoundingBox[spaceIndex].z;

            //その空間に属するOccludeの要素数分ループする
            for (int i = startIndex; i < startIndex + spaceVoxelCount; i+=2)
            {
                //aabbのそれぞれの座標を求める
                Vector3 minBoundingBox = occludeBoundingBox[i];
                Vector3 maxBoundingBox = occludeBoundingBox[i + 1];

                //衝突判定を実施する
                Vector3 hitPos = IsCollision(rayDirection, startRayMortonSpacePosition, minBoundingBox, maxBoundingBox);
                if (hitPos.x < -0.5) continue;
                
                Vector3 boxVector = hitPos - startRayMortonSpacePosition;
                float distance = boxVector.x * boxVector.x + boxVector.y * boxVector.y + boxVector.z * boxVector.z;

                //ヒットした座標がOccluderのヒット座標よりも遠い場合はOccluderよりも遠いので無視する
                if (occluderHitSqrMagnitude < distance) continue;
                
                //手前にOccludeがあるので、ヒットしたと判定する
                int occludeIndex = (int)occludeBoundingBox[i].w;
                occludeResult[occludeIndex] = 1;
            } 
        }
        
        /// <summary>
        /// その空間にあるバウンディングボックスと衝突判定をする
        /// </summary>
        /// <returns></returns>
        static int OccluderCollisionCheck(int spaceIndex ,Vector4[] occluderBoundingBox,Vector3 startRayMortonSpacePosition,Vector3 rayDirection, Vector4[] occludeeBoundingBox,int[] occludeResult)
        {
            //衝突したAABBと原点の距離をもめて最も小さいものを採用する
            float minCenterSqrMagnitude = 100000000.0f;
            int resultIndex = -1;
            
                
            int startIndex = (int)occluderBoundingBox[spaceIndex].y;
            int spaceVoxelCount = (int)occluderBoundingBox[spaceIndex].z;

            //その空間に属するOccluderの要素数分ループする
            for (int i = startIndex; i < startIndex + spaceVoxelCount; i+=2)
            {
                //aabbのそれぞれの座標を求める
                Vector3 minBoundingBox = occluderBoundingBox[i];
                Vector3 maxBoundingBox = occluderBoundingBox[i + 1];

                //衝突判定を実施する
                Vector3 hitPos = IsCollision(rayDirection, startRayMortonSpacePosition, minBoundingBox, maxBoundingBox);
                if (hitPos.x < -0.5) continue;
                    
                Vector3 boxVector = hitPos - startRayMortonSpacePosition;
                float distance = boxVector.x * boxVector.x + boxVector.y * boxVector.y + boxVector.z * boxVector.z;
                if (distance < minCenterSqrMagnitude)
                {
                    HitPos = hitPos;
                    HitMinBoundingBox = minBoundingBox;
                    HitMaxBoundingBox = maxBoundingBox;
                    
                    minCenterSqrMagnitude = distance;
                    resultIndex = (int) occluderBoundingBox[i].w;
                }
            }
            
            //Occluderの判定結果をもとにOccludeの判定を行う
            OccludeCollisionCheck(spaceIndex, minCenterSqrMagnitude, occludeeBoundingBox, occludeResult, startRayMortonSpacePosition, rayDirection);

            return resultIndex;
        }

        /// <summary>
        /// 角度がマイナスだった時のために開始地点を補正する
        /// </summary>
        /// <returns></returns>
        static float CalcInvertMortonSpacePosition(int cellPos,float cellSize,float startPos)
        {
            return cellPos * cellSize   +   ((cellPos + 1) * cellSize - startPos);
        }
        
        
        public static List<Vector3Int> CheckedRayWay = new List<Vector3Int>();
        
        /// <summary>
        /// チェックした空間のリストと衝突したボクセルを返す
        /// </summary>
        /// <returns>モートン空間のインデックのバウンディングボックス</returns>
        public static int GetHitIndex(Vector3 startPosition,Vector3 direction,Vector3 spaceOriginPosition,Vector3 spaceRange,int spaceSplitNum,Vector4[] occluderBoundingBox,Vector4[] occludeBoundingBox,int[] occludeResult)
        {
            CheckedRayWay.Clear();
            
            
            direction = direction.normalized;
            
            //rayがプラス方向かマイナス方向かをもとに現在のポジションを設定する
            Vector3Int rayMortonForward = new Vector3Int(0 <= direction.x ? 1 : -1, 0 <= direction.y ? 1 : -1, 0 <= direction.z ? 1 : -1);
            
            //directionを一旦すべて正の値にする
            Vector3 plusDirection = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));


            //空間の分割数
            int mortonCellNum =(int)Mathf.Pow(2, spaceSplitNum);
            Vector3Int mortonSpaceRange = new Vector3Int(mortonCellNum, mortonCellNum, mortonCellNum);
            //最大レベルのモートン空間（モートンセル）の大きさ
            Vector3 mortonCellSize = spaceRange / mortonCellNum ; 
            
            //Unity座標系をボクセル空間座標系に変換
            //プラス方向の時の開始地点をモートン空間系の座標に変換する
            Vector3 startRayMortonSpacePosition  = startPosition - spaceOriginPosition;
            
            //開始地点の座標を追加する
            Vector3Int startRayMortonCellPos = new Vector3Int(Mathf.FloorToInt(startRayMortonSpacePosition.x / mortonCellSize.x), Mathf.FloorToInt(startRayMortonSpacePosition.y / mortonCellSize.y), Mathf.FloorToInt(startRayMortonSpacePosition.z / mortonCellSize.z));
            //マイナスを考慮した開始地点の座標を追加する
            Vector3 startPlusRayMortonSpacePos = new Vector3(
                rayMortonForward.x == 1 ? startRayMortonSpacePosition.x : CalcInvertMortonSpacePosition(startRayMortonCellPos.x, mortonCellSize.x, startRayMortonSpacePosition.x),
                rayMortonForward.y == 1 ? startRayMortonSpacePosition.y : CalcInvertMortonSpacePosition(startRayMortonCellPos.y, mortonCellSize.y, startRayMortonSpacePosition.y),
                rayMortonForward.z == 1 ? startRayMortonSpacePosition.z : CalcInvertMortonSpacePosition(startRayMortonCellPos.z, mortonCellSize.z, startRayMortonSpacePosition.z));

            
            //モートン空間の縦横高さは1,1,1とは限らず、スペースの大きさと分割数によって変化するため、計算しやすいように係数を調整して1,1,1として扱えるようにする
            Vector3 mortonSpacePlusDirection = new Vector3(plusDirection.x / mortonCellSize.x, plusDirection.y / mortonCellSize.y, plusDirection.z / mortonCellSize.z);
            //1ステップ進むごとに進むベクトル
            Vector3 rayForwardOneStep = new Vector3(mortonSpacePlusDirection.x * mortonCellSize.x, mortonSpacePlusDirection.y * mortonCellSize.y, mortonSpacePlusDirection.z * mortonCellSize.z);
           
            
            Vector3Int currentPlusMortonPosition = startRayMortonCellPos;
            Vector3 currentPosition = startPlusRayMortonSpacePos;

            
            int resultIndex = -1;



            int calcCount = 0;
            
            Vector3Int oneInt3 = new Vector3Int(1, 1, 1);
            
            //空間の範囲を抜けるまで続ける
            while (true)
            {
                //無限ループ防止のために計算回数を制限する
                if (2*mortonCellNum < calcCount)
                {
                    Debug.LogError("ループが正常に終了しませんでした");
                    break;
                }
                calcCount++;
                
                
                //現在のレイの位置を計算する
                Vector3Int currentPlusMortonDifference = currentPlusMortonPosition - startRayMortonCellPos;
                Vector3Int currentMortonDifference = new Vector3Int(currentPlusMortonDifference.x * rayMortonForward.x, currentPlusMortonDifference.y * rayMortonForward.y, currentPlusMortonDifference.z * rayMortonForward.z);
                Vector3Int currentSpace = startRayMortonCellPos + currentMortonDifference;  
                
                //最初から範囲外である可能性が高いので現在のセルが範囲内かチェックする
                if (currentSpace.x  < 0 || mortonSpaceRange.x <= currentSpace.x ||
                    currentSpace.y  < 0 || mortonSpaceRange.y <= currentSpace.y ||
                    currentSpace.z  < 0 || mortonSpaceRange.z <= currentSpace.z)
                {
                    break;
                }
                
                
                CheckedRayWay.Add(currentSpace);
                
                
                //空間番号を取得し、インデックスとする
                int index = CalculateMortonOrder.CalculateVector3IntPositionToMortonIndex(currentSpace);
                
                //当たり判定をする
                resultIndex = OccluderCollisionCheck(index, occluderBoundingBox, startRayMortonSpacePosition,direction,occludeBoundingBox,occludeResult);
                if (resultIndex != -1)
                {
                    break;
                }
                
                
                
                
                
                
                //次のセルがどれかを計算する
                
                //次のセル
                Vector3Int nextMortonCell = currentPlusMortonPosition + oneInt3;
                //次セルの座標
                Vector3 nextPosition = new Vector3(nextMortonCell.x * mortonCellSize.x, nextMortonCell.y * mortonCellSize.y, nextMortonCell.z * mortonCellSize.z);
                
                //現在の座標から次のセルの座標までのベクトルを求め、rayとのベクトルで割ることで、次のセルまでのベクトルととrayがどれくらい離れているかを求める
                Vector3 nextMortonCellRayRatio = new Vector3(
                    //ある方向がもう一つの方向より小さかった時にその方向に進むというアルゴリズムであるため、rayの方向が0の場合は無限大として扱う
                    mortonSpacePlusDirection.x != 0 ? Mathf.Abs((nextPosition.x - currentPosition.x) / rayForwardOneStep.x): 10000000.0f,
                    mortonSpacePlusDirection.y != 0 ? Mathf.Abs((nextPosition.y - currentPosition.y) / rayForwardOneStep.y) : 10000000.0f,
                    mortonSpacePlusDirection.z != 0 ? Mathf.Abs((nextPosition.z - currentPosition.z) / rayForwardOneStep.z) : 10000000.0f
                );
                
                //各軸の大小を比べてどっち方向に動くかを決定する
                if (nextMortonCellRayRatio.x < nextMortonCellRayRatio.y && nextMortonCellRayRatio.x < nextMortonCellRayRatio.z)
                {
                    currentPosition += rayForwardOneStep * nextMortonCellRayRatio.x;
                    currentPlusMortonPosition.x += 1;
                }else if (nextMortonCellRayRatio.y < nextMortonCellRayRatio.x && nextMortonCellRayRatio.y < nextMortonCellRayRatio.z)
                {
                    currentPosition += rayForwardOneStep * nextMortonCellRayRatio.y;
                    currentPlusMortonPosition.y += 1;
                }
                else
                {
                    currentPosition += rayForwardOneStep * nextMortonCellRayRatio.z;
                    currentPlusMortonPosition.z += 1;
                }
                
            }
            
            

            return resultIndex;
        }

    }
    
}