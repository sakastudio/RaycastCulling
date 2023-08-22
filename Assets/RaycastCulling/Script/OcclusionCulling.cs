using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using RaycastCulling.Script.PreProcess;
using UnityEngine;
using UnityEngine.Rendering;

namespace RaycastCulling.Script
{
    public class OcclusionCulling : MonoBehaviour
    {
        #region コンピュートシェーダーと同じ定数エリア

        private const int ResetResultArray = 0;
        private const int CheckRayMode = 1;
        private const int PackResultArray = 2;

        
        private const int GroupSize = 100;
        private const int GroupCameraNum = 5;
        private const int ThreadSize = 100;
        private const int ResultArraySize = GroupSize * GroupCameraNum * ThreadSize;
        private const int PackedResultIndex = 300; //たぶん一度に300個描画されることはないので300に設定しておく
        private static readonly Vector3Int ThreadGroupSize = new(GroupSize,GroupCameraNum,1);

        private const int IgnoreOccluderCount = 10;
        #endregion


        public static OcclusionCulling Instance { get; private set; }
        public bool Enable = false;
        
        [Header("デバッグ表示フラグ")]
        public bool ヒットした座標を描画する = false;
        public bool ヒットしたレイを描画する = false;
        public bool ヒットしなかったレイを描画する = false;
        public float ヒットしなかったレイの長さ = 100;
        
        [SerializeField] private ComputeShader raycastCullingComputeShader;
        
        [Header("カメラの周囲から出すレイの距離")]
        [SerializeField] private float AroundCheckDistance = 1.0f;


        private ComputeBuffer _occluderBoundingBox;
        private ComputeBuffer _occludeeBoundingBox;
        
        private ComputeBuffer _occluderResultBuffer;
        private ComputeBuffer _occludeeResultBuffer;
        private ComputeBuffer _packedOccluderResultBuffer;
        
        private ComputeBuffer _ignoreOccluderIndexBuffer;
        
        private ComputeBuffer _debugBuffer;
        private readonly Matrix4x4[] _debugData = new Matrix4x4[ResultArraySize];


        private OcclusionCullingSettings _settings;
        private BoundingBoxToMeshRenderer[] _occluderResult;
        public BoundingBoxToMeshRenderer[] Occluder => _occluderResult;
        private BoundingBoxToMeshRenderer[] _occludeeResult;
        public BoundingBoxToMeshRenderer[] Occludee => _occludeeResult;

        private int _currentIgnoreOccluderCount = 0;
        private int[] _ignoreOccluderIndex = new int[IgnoreOccluderCount];

        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize(BoundingBoxToMeshRenderer[] occluderResult, BoundingBoxToMeshRenderer[] occludeeResult , OcclusionCullingSettings setting)
        {
            _occluderResult = occluderResult;
            _occludeeResult = occludeeResult;
            _settings = setting;


            Instance = this;
            var ct = this.GetCancellationTokenOnDestroy();
            _occluderBoundingBox.AddTo(ct);
            _occludeeBoundingBox.AddTo(ct);
            _occluderResultBuffer.AddTo(ct);
            _packedOccluderResultBuffer.AddTo(ct);
            _occludeeResultBuffer.AddTo(ct);
            _ignoreOccluderIndexBuffer.AddTo(ct);

            //OccluderでベイクしたデータをGPUに渡せる形に修正する
            #region カリングする側 Occluderの設定
            SetOcclusionCullingData(out _occluderResult, out _occluderBoundingBox,
                _occluderResult, _settings.SpaceRange, _settings.SpaceSplitNum);
            raycastCullingComputeShader.SetBuffer(0, "OccluderBoundingBox", _occluderBoundingBox);
            raycastCullingComputeShader.SetInt("occluderCount", _occluderResult.Length);

            _occluderResultBuffer = new ComputeBuffer(_occluderResult.Length, Marshal.SizeOf<int>());
            raycastCullingComputeShader.SetBuffer(0, "OccluderHitResult", _occluderResultBuffer);
            
            _ignoreOccluderIndexBuffer = new ComputeBuffer(IgnoreOccluderCount, Marshal.SizeOf<int>());
            raycastCullingComputeShader.SetBuffer(0,"IgnoreOccluderIndex",_ignoreOccluderIndexBuffer);
            raycastCullingComputeShader.SetInt("ignoreOccluderCount",IgnoreOccluderCount);
            #endregion

            #region カリングされる側 Occludeeの設定
            SetOcclusionCullingData(out _occludeeResult, out _occludeeBoundingBox,
                _occludeeResult, _settings.SpaceRange, _settings.SpaceSplitNum);
            raycastCullingComputeShader.SetBuffer(0, "OccludeeBoundingBox", _occludeeBoundingBox);
            raycastCullingComputeShader.SetInt("occludeeCount", _occludeeResult.Length);

            //occludeeが0個のときにエラーが出るので、0個のときは1個にしておく
            var occludeCount = _occludeeResult.Length;
            _occludeeResultBuffer = new ComputeBuffer(occludeCount == 0 ? 1 : occludeCount, Marshal.SizeOf<int>());
            raycastCullingComputeShader.SetBuffer(0, "OccludeeHitResult", _occludeeResultBuffer);
            #endregion



            //compute shaderに渡す

            _packedOccluderResultBuffer = new ComputeBuffer(PackedResultIndex, Marshal.SizeOf<int>());
            raycastCullingComputeShader.SetBuffer(0, "PackedRayIndex", _packedOccluderResultBuffer);




            raycastCullingComputeShader.SetVector("spaceOriginPosition", _settings.SpaceOriginPos);
            raycastCullingComputeShader.SetVector("spaceRange", _settings.SpaceRange);
            raycastCullingComputeShader.SetInt("spaceSplitNum", _settings.SpaceSplitNum);



            _debugBuffer = new ComputeBuffer(ResultArraySize,  Marshal.SizeOf<Matrix4x4>());
            raycastCullingComputeShader.SetBuffer(0, "DebugData", _debugBuffer);
            Enable = true;
        }


        private const string CheckMode = "checkMode";

        private void LateUpdate()
        {
            if (!Enable) return;
            if (Camera.main == null) return;
            var camera = Camera.main;
            var cameraTransform = camera.transform;
            //移動していない時はカリングを更新しない

            SetCameraRayData(camera,cameraTransform);
            SetAroundRayData(cameraTransform);
            
            raycastCullingComputeShader.SetInt(CheckMode,ResetResultArray);
            raycastCullingComputeShader.Dispatch(0, ThreadGroupSize.x, ThreadGroupSize.y, ThreadGroupSize.z);
            
            raycastCullingComputeShader.SetInt(CheckMode,CheckRayMode);
            raycastCullingComputeShader.Dispatch(0, ThreadGroupSize.x, ThreadGroupSize.y, ThreadGroupSize.z);
            
            raycastCullingComputeShader.SetInt(CheckMode,PackResultArray);
            raycastCullingComputeShader.Dispatch(0, ThreadGroupSize.x, ThreadGroupSize.y, ThreadGroupSize.z);
            
            AsyncGPUReadback.Request(_packedOccluderResultBuffer, OccluderMeshOn);
            if (_occludeeResult.Length != 0) 
            {
                AsyncGPUReadback.Request(_occludeeResultBuffer, OccludeeMeshOn);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Riderで見やすいように関数を残しておく
        /// </summary>
        private void OnDrawGizmos()
        {
            if (ヒットした座標を描画する || ヒットしたレイを描画する || ヒットしなかったレイを描画する)
            {
                _debugBuffer.GetData(_debugData);
                for (int i = 0; i < _debugData.Length; i++)
                {
                    var start = (Vector3) _debugData[i].GetRow(1);
                    var direction = (Vector3) _debugData[i].GetRow(2);
                    
                    if (_debugData[i].m00 <= -0.5f)
                    {
                        if (ヒットしなかったレイを描画する)
                        {
                            Gizmos.color = Color.white;
                            Gizmos.DrawRay(start, direction * ヒットしなかったレイの長さ);
                        }
                        continue; 
                    }

                    var hit = (Vector3) _debugData[i].GetRow(0) + _settings.SpaceOriginPos;
                    

                    if (ヒットした座標を描画する)
                    {
                        Gizmos.color = Color.white;
                        Gizmos.DrawWireCube(hit,Vector3.one * 0.1f);
                    }

                    if (ヒットしたレイを描画する)
                    {
                        Gizmos.color = Color.white;
                        Gizmos.DrawLine(hit, start); 
                    }
                }
            }
        }
#endif


        #region ComputeShaderにデータをセット
        private void SetCameraRayData(Camera targetCamera,Transform cameraTransform)
        {
            var position = cameraTransform.position;
            var rotation = cameraTransform.rotation;
            
            raycastCullingComputeShader.SetVector("cameraRadianRotation",rotation.eulerAngles * Mathf.Deg2Rad);
            raycastCullingComputeShader.SetFloat("verticalRadianFov",targetCamera.fieldOfView * Mathf.Deg2Rad + 30 * Mathf.Deg2Rad); //縦横それぞれのFOVに30度ずつ余裕を持たせることで、カメラぎりぎりのところにあるオブジェクトを描画することができる
            raycastCullingComputeShader.SetFloat("horizontalRadianFov",VFovToHFov(targetCamera.fieldOfView,targetCamera.aspect) * Mathf.Deg2Rad + 30 * Mathf.Deg2Rad);
            raycastCullingComputeShader.SetFloat("aspectRatio",targetCamera.aspect);
            raycastCullingComputeShader.SetVector("cameraPosition",position);
        }

        private void SetAroundRayData(Transform cameraTransform)
        {
            //20cm上の座標を求める
            var up = cameraTransform.up;
            raycastCullingComputeShader.SetVector("upPosOffset",up * AroundCheckDistance);

            var right = cameraTransform.right;
            raycastCullingComputeShader.SetVector("rightPosOffset",right * AroundCheckDistance);
            
            raycastCullingComputeShader.SetVector("downPosOffset",up * -AroundCheckDistance);
            
            raycastCullingComputeShader.SetVector("leftPosOffset",right * -AroundCheckDistance);
        }
        
        #endregion


        /// <summary>
        /// TODO ここの探索めっちゃ効率悪いからアルゴリズムを変える
        /// </summary>
        /// <param name="renderer"></param>
        public void SetIgnoreMeshRender(MeshRenderer renderer)
        {
            var targetIndex = -1;
            foreach (var occluder in _occluderResult)
            {
                if (occluder.Target == renderer)
                {
                    targetIndex = occluder.MeshRendererIndex;
                    break;
                }
            }
            if (targetIndex == -1)
            {
                return;
            }
            _ignoreOccluderIndex[_currentIgnoreOccluderCount] = targetIndex;
            _ignoreOccluderIndexBuffer.SetData(_ignoreOccluderIndex);
            _currentIgnoreOccluderCount++;
        }

        private void OccluderMeshOn(AsyncGPUReadbackRequest meshOnResult)
        {
            if (meshOnResult.hasError) return;

            //一旦すべてのMeshRendererを非表示にする
            foreach (var occluder in _occluderResult)
            {
                occluder.SetForceRenderingOff(true);
            }
            
            //レイがヒットしたものだけ表示する
            using var rayHits = meshOnResult.GetData<int>();
            var length = rayHits.Length;
            for (var i = 0; i < length; i++)
            {
                var rayHit = rayHits[i];
                if (rayHit == -1) break;
                _occluderResult[rayHit].SetForceRenderingOff(false);
            }
        }

        /// <summary>
        /// Occludee （レイを貫通するオブジェクト）の設定を行う
        /// Occludeeはそこまで数が多くないことが予想されるので、基本的にすべてループする
        /// もしここがボトルネックになったら要修正
        /// </summary>
        private void OccludeeMeshOn(AsyncGPUReadbackRequest meshOffResult)
        {
            if (meshOffResult.hasError) return;
            
            foreach (var occludee in _occludeeResult)
            {
                occludee.SetForceRenderingOff(true);
            }
            using var rayHits = meshOffResult.GetData<int>();
            var length = rayHits.Length;
            for (var i = 0; i < length; i++)
            {
                if (rayHits[i] == 0) continue;
                _occludeeResult[i].SetForceRenderingOff(false);
            }
        }


        private static float VFovToHFov(float verticalFov, float aspectRatio)
        {
            return 2f * Mathf.Rad2Deg * Mathf.Atan(Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad) * aspectRatio);
        }
        
        private static void SetOcclusionCullingData(out BoundingBoxToMeshRenderer[] resultTargetList,out ComputeBuffer resultComputeBuffer, 
            BoundingBoxToMeshRenderer[] boundingBoxList, Vector3 spaceRange, int spaceSplitNum)
        {
            var (occluderBoundingBox, occlusionList) = 
                ConvertBoundingBoxList.Convert2DListTo1DList(
                CalculateMortonOrder.CalculateMortonOrderList(boundingBoxList, spaceRange, spaceSplitNum));
            
            resultTargetList = occlusionList;
            resultComputeBuffer = new ComputeBuffer(occluderBoundingBox.Length, Marshal.SizeOf<Vector4>());
            resultComputeBuffer.SetData(occluderBoundingBox);
        }
    }
}