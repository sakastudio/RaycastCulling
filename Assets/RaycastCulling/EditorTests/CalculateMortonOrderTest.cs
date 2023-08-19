using NUnit.Framework;
using RaycastCulling.Script.PreProcess;
using UnityEngine;

namespace EditorTests
{
    public class CalculateMortonOrderTest
    {

        [Test]
        public void BitSeparateFor3()
        {
            int before = 0b1011;
            int after = 0b001000001001;
            int result = CalculateMortonOrder.BitSeparateFor3(before);
            Assert.AreEqual(after, result);
        }

        [Test]
        public void CalcAabbMortonNumberToMortonArrayIndex()
        {
            var result = CalculateMortonOrder.CalcAabbMortonNumberToMortonArrayIndex(16,23,2);
            Assert.AreEqual(3,result);
            result = CalculateMortonOrder.CalcAabbMortonNumberToMortonArrayIndex(16,16,2);
            Assert.AreEqual(25,result);
        }

        [Test]
        public void CalculatePositionToMortonIndex()
        {
            var spaceRange = new Vector3(8, 8, 8);
            var spaceSplitNum = 3;
            var position = new Vector3(0.5f, 0.5f, 0.5f);
            var result = CalculateMortonOrder.CalculatePositionToMortonIndex(position, spaceSplitNum, spaceRange);
            Assert.AreEqual(0, result);
            
            position = new Vector3(2.5f, 0.5f, 0.5f);
            result = CalculateMortonOrder.CalculatePositionToMortonIndex(position, spaceSplitNum, spaceRange);
            Assert.AreEqual(8, result);
        }

        [Test]
        public void CalculateMortonArrayIndex()
        {
            var voxelSize = 1.9f;
            var splitNum = 2;
            var spaceRange = new Vector3(8, 8, 8);
            
            var voxelPosition = new Vector3Int(0, 0, 0);
            //var result = CalculateMortonOrder.CalculateMortonArrayIndex(voxelPosition, voxelSize, splitNum, spaceRange);
            //Assert.AreEqual(9, result);
            
            voxelPosition = new Vector3Int(1, 0, 0);
            //result = CalculateMortonOrder.CalculateMortonArrayIndex(voxelPosition, voxelSize, splitNum, spaceRange);
            //Assert.AreEqual(1, result);
        }

        [Test]
        public void CalcMortonOrderToArrayIndex()
        {
            Assert.AreEqual(0,CalculateMortonOrder.CalcMortonOrderToArrayIndex(0,0));
            Assert.AreEqual(1,CalculateMortonOrder.CalcMortonOrderToArrayIndex(0,1));
            Assert.AreEqual(3,CalculateMortonOrder.CalcMortonOrderToArrayIndex(2,1));
            Assert.AreEqual(309,CalculateMortonOrder.CalcMortonOrderToArrayIndex(236,3));
        }

        [Test]
        public void CalculateSplitNum()
        {
            var voxelSize = 1.1f;
            var spaceRange = new Vector3(8, 8, 8);
            //var result = CalculateMortonOrder.CalculateSplitNum(voxelSize, spaceRange);
            //Assert.AreEqual(3, result);
            
            voxelSize = 0.9f;
            //result = CalculateMortonOrder.CalculateSplitNum(voxelSize, spaceRange);
            //Assert.AreEqual(4, result);
        }

        [Test]
        public void MortonIndexToVector3Int()
        {
            var pos = CalculateMortonOrder.MortonNumberToVector3Int(339);
            Assert.AreEqual(new Vector3Int(5,3,4),pos);
            var index = CalculateMortonOrder.CalculateVector3IntPositionToMortonIndex(new Vector3Int(5, 3, 4));
            Assert.AreEqual(339,index);
        }

        [Test]
        public void ArrayIndexToSpaceLevelAndSpaceNum()
        {

            var index = CalculateMortonOrder.CalcMortonOrderToArrayIndex(5, 4);
            var result = CalculateMortonOrder.ArrayIndexToSpaceLevelAndSpaceNum(index);
            Assert.AreEqual(5, result.spaceNum);
            Assert.AreEqual(4, result.level);
        }
    }
}