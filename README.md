# RaycastCulling

レイベースのオクルージョンカリングシステムです。

# デモ

最小構成のシステムが入っています。

Scenese > RaycastingSample.unity を開いてください。

https://github.com/sakastudio/RaycastCulling/assets/55620461/99883ee0-3f90-4beb-a4cc-0d1817bc3d82

# コード

メインとなる処理は以下の二つです。

GPU側で処理するComputeShader

[ComputeShader.compute](https://github.com/sakastudio/RaycastCulling/blob/master/Assets/RaycastCulling/Script/BoundingBoxRayChecker.compute)

CPU側でComputeShaderに命令を出すクラス

[OcclusionCulling.cs](https://github.com/sakastudio/RaycastCulling/blob/master/Assets/RaycastCulling/Script/OcclusionCulling.cs)

でもシーンでオクルージョンカリングの設定を行うクラス

[CullingInitializer](https://github.com/sakastudio/RaycastCulling/blob/master/Assets/Script/CullingInitializer.cs)
