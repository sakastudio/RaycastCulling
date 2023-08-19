using System.Collections.Generic;
using UnityEngine;

namespace RaycastCulling.Script.PreProcess
{
    public static class CalculateMortonOrder
    {
        /// <summary>
        /// どのボクセルがどの空間に属するかを計算する
        /// </summary>
        /// <returns>一つ目のList どの空間かを線形八分木のインデックスで表す　二つ目のList　その空間に属するボクセルの座標のリストが入る</returns>
        public static List<List<BoundingBoxToMeshRenderer>> CalculateMortonOrderList(BoundingBoxToMeshRenderer[] boundingBoxList, Vector3 spaceRange,int spaceSplitNum = 6)
        {
            var spaceCount = CalcLastMortonNumber(spaceSplitNum) + 1;

            //各ボクセルが所属する空間を求める
            var result = new List<List<BoundingBoxToMeshRenderer>>();
            for (int i = 0; i < spaceCount; i++)
            {
                result.Add(new List<BoundingBoxToMeshRenderer>());
            }

            for (var i = 0; i < boundingBoxList.Length; i++)
            {
                var box = boundingBoxList[i];
                box.MeshRendererIndex = i;
                
                var mortonNums = CalcBoundingBoxMortonNums(box, spaceSplitNum, spaceRange);
                foreach (var mortonNum in mortonNums)
                {
                    //そのバウンディングボックスが分割された空間からはみ出しているかをチェックし、はみ出している場合は補正する
                    var cellSize = CalcCellSize(spaceRange, spaceSplitNum);

                    var cellMinIntPos = MortonNumberToVector3Int(mortonNum);
                    
                    var cellMinPos = new Vector3(cellMinIntPos.x * cellSize.x, cellMinIntPos.y * cellSize.y, cellMinIntPos.z * cellSize.z);
                    var cellMaxPos = cellMinPos + cellSize;

                    //セルの外にバウンディングボックスの境界があると、レイを進行させながらチェックするときに、大きなバウンディングボックスがあるとそちらに判定が座れ、正しく判定されなくなる
                    //そのため、セルの外にバウンディングボックスの境界がある場合は、セルの外にあるバウンディングボックスの境界をセルの境界に合わせる
                    var boxMinPos = box.BoundMinPos;
                    for (int j = 0; j < 3; j++)
                    {
                        if (boxMinPos[j] < cellMinPos[j])
                        {
                            boxMinPos[j] = cellMinPos[j];
                        }
                    }

                    var boxMaxPos = box.BoundMaxPos;
                    for (int j = 0; j < 3; j++)
                    {
                        if (cellMaxPos[j] < boxMaxPos[j])
                        {
                            boxMaxPos[j] = cellMaxPos[j];
                        }
                    }
                    

                    result[mortonNum].Add(new BoundingBoxToMeshRenderer(boxMinPos,boxMaxPos,box.Target,i));
                }
            }

            return result;
        }
        
        
        /// <summary>
        /// ボクセルの座標からモートン空間の配列番号に変換する
        /// </summary>
        /// <returns></returns>
        public static List<int> CalculateMortonArrayIndex(BoundingBoxToMeshRenderer boundingBox, int spaceSplitNum,Vector3 spaceRange)
        {
            var mortonNums = CalcBoundingBoxMortonNums(boundingBox,spaceSplitNum,spaceRange);
            var mortonArrays = new List<int>();
            foreach (var num in mortonNums)
            {
                mortonArrays.Add(CalcMortonOrderToArrayIndex(num,spaceSplitNum));
            }

            return mortonArrays;
        }


        /// <summary>
        /// そのボクセルのAABBのそれぞれのモートン番号を計算する
        /// </summary>
        /// <returns></returns>
        public static List<int> CalcBoundingBoxMortonNums(BoundingBoxToMeshRenderer boundingBox, int spaceSplitNum,Vector3 spaceRange)
        {
            //バウンディングボックスのモートン番号を求める
            var leftMortonNumber = CalculatePositionToMortonIndex(boundingBox.BoundMinPos, spaceSplitNum, spaceRange);
            var leftPos = MortonNumberToVector3Int(leftMortonNumber);
            var rightMortonNumber = CalculatePositionToMortonIndex(boundingBox.BoundMaxPos, spaceSplitNum, spaceRange);
            var rightPos = MortonNumberToVector3Int(rightMortonNumber);
            
            var mortonNums = new List<int>();
            for (int x = leftPos.x; x <= rightPos.x; x++)
            {
                for (int y = leftPos.y; y <= rightPos.y; y++)
                {
                    for (int z = leftPos.z; z <= rightPos.z; z++)
                    {
                        mortonNums.Add(CalculateVector3IntPositionToMortonIndex(new Vector3Int(x, y, z)));
                    }
                }
            }
            
            return mortonNums;
        }
        

        /// <summary>
        /// その座標値のモートン空間の番号を計算する
        /// </summary>
        public static int CalculatePositionToMortonIndex(Vector3 position, int spaceSplitNum,Vector3 spaceRange)
        {
            var cellSize = CalcCellSize(spaceRange, spaceSplitNum);
            //その座標値がモートン空間のどの座標値にいるかを計算する
            var mortonSpacePosition = new Vector3Int(
                Mathf.FloorToInt(position.x / cellSize.x),
                Mathf.FloorToInt(position.y / cellSize.y),
                Mathf.FloorToInt(position.z / cellSize.z));
            
            //その座標値のモートン空間の番号を計算する
            return CalculateVector3IntPositionToMortonIndex(mortonSpacePosition);
        }


        public static Vector3 CalcCellSize(Vector3 spaceRange, int spaceSplitNum)
        {
            return  spaceRange / Mathf.Pow(2,spaceSplitNum); 
        }


        /// <summary>
        /// 空間座標からモートン番号を計算する
        /// </summary>
        public static int CalculateVector3IntPositionToMortonIndex(Vector3Int pos)
        {
            return BitSeparateFor3(pos.x) | (BitSeparateFor3(pos.y) << 1) | (BitSeparateFor3(pos.z) << 2); 
        }


        /// <summary>
        /// 8ビットまでの値を3ビットおきに間隔を開ける
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static int BitSeparateFor3(int n)
        {
            int s = n;
            s = ( s | s<<8 ) & 0x0000f00f;
            s = ( s | s<<4 ) & 0x000c30c3;
            s = ( s | s<<2 ) & 0x00249249;
            return s;
        }


        /// <summary>
        /// XORを使ってモートン空間配列のインデックスを計算する
        /// </summary>
        /// <returns></returns>
        public static int CalcAabbMortonNumberToMortonArrayIndex(int leftMortonNumber, int rightMortonNumber,int spaceSplitNum)
        {
            if (leftMortonNumber == rightMortonNumber)
            {
                //同じ場合は最大の空間レベルに属しているので、そのインデックスを返す
                return CalcMortonOrderToArrayIndex(leftMortonNumber, spaceSplitNum);
            }
            
            //所属空間の空間番号を計算する
            var mortonSpaceLevel = CalcSpaceLevel(leftMortonNumber, rightMortonNumber,spaceSplitNum);
            var spaceNum = leftMortonNumber >> ((spaceSplitNum - mortonSpaceLevel) * 3);
            
            //所属空間の配列番号を計算する
            return CalcMortonOrderToArrayIndex(spaceNum, mortonSpaceLevel);
        }

        /// <summary>
        /// AABBのモートン番号から所属する空間のレベルを計算する
        /// </summary>
        /// <returns></returns>
        public static int CalcSpaceLevel(int leftMortonNumber, int rightMortonNumber, int spaceSplitNum)
        {
            //xorを撮って所属空間を求める
            var xor = leftMortonNumber ^ rightMortonNumber;
            //右から3ビットづつ取り、ルート空間まで探索してどの空間に属するかを検索する
            int mortonSpaceLevel = 0;
            int mask = 0b111;
            //右側（子空間）がわからチェックしていくので降順で所属空間のレベルを調べていく
            for (int i = spaceSplitNum - 1; i >= 0; i--)
            {
                if ((xor & mask) != 0)
                {
                    mortonSpaceLevel = i;
                }
                mask <<= 3;
            }

            return mortonSpaceLevel;
        }


        /// <summary>
        /// 空間番号とその空間レベルからモートン空間配列のインデックスを計算する
        /// </summary>
        public static int CalcMortonOrderToArrayIndex(int spaceNum,int mortonSpaceLevel)
        {
            //所属空間の配列番号を計算する
            return spaceNum + ((int)Mathf.Pow(8, mortonSpaceLevel) - 1) / 7;
        }
        
        
        /// <summary>
        /// その分割数の最後の空間番号を計算する
        /// </summary>
        public static int CalcLastMortonNumber(int spaceSplitNum)
        {
            return (int)Mathf.Pow(8, spaceSplitNum) - 1;
        }


        /// <summary>
        /// モートン番号からそのXYZの位置を計算する
        /// </summary>
        /// <param name="mortonSpaceNum"></param>
        /// <returns></returns>
        public static Vector3Int MortonNumberToVector3Int(int mortonSpaceNum)
        {
            var x = 0;
            var y = 0;
            var z = 0;

            var indexMask = 1;
            
            for (int i = 0; i < 10; i++)
            {
                var tmpX = mortonSpaceNum & indexMask;
                tmpX >>= (2 * i);
                x |= tmpX;
                
                var tmpY = mortonSpaceNum & (indexMask << 1);
                tmpY >>= (2 * i + 1);
                y |= tmpY;
                
                var tmpZ = mortonSpaceNum & (indexMask << 2);
                tmpZ >>= (2 * i + 2);
                z |= tmpZ;
                
                indexMask <<= 3;
            }

            return new Vector3Int(x, y, z);
        }

        /// <summary>
        /// 配列番号から空間レベルと空間番号を取得する
        /// </summary>
        public static (int level,int spaceNum) ArrayIndexToSpaceLevelAndSpaceNum(int arrayIndex)
        {
            if (arrayIndex == 0)
            {
                return (0, 0);
            }


            var spaceLevel = 0;
            while (true)
            {
                var maxSpaceNum = (int)Mathf.Pow(8, spaceLevel);
                var maxArrayIndex = CalcMortonOrderToArrayIndex(maxSpaceNum, spaceLevel);
                if (arrayIndex < maxArrayIndex)
                {
                    break;
                }

                spaceLevel++;
            }
            
            return (spaceLevel, arrayIndex - ((int)Mathf.Pow(8, spaceLevel) - 1) / 7);
        }
    }

    public static class Vector3Extension
    {
        public static Vector3 ToFloat(this Vector3Int vector3Int)
        {
            return new Vector3(vector3Int.x, vector3Int.y, vector3Int.z);
        }
        public static Vector3 Multiple(this Vector3 vector3,Vector3 multiple)
        {
            return new Vector3(vector3.x * multiple.x, vector3.y * multiple.y, vector3.z * multiple.z);
        }
    }
}