using System.Collections;
using Core;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

namespace Framework
{
    /// <summary>
    /// 基于YooAsset实现的资源加载内核
    /// </summary>
    public class YooAssetProvider : IAssetProvider
    {
        private readonly EPlayMode _playMode;
        private readonly string _packageName; 
        private string _packageVersion;
        
        public YooAssetProvider(string packageName, EPlayMode playMode)
        {
            YooAssets.Initialize();
            _playMode = playMode;
            _packageName = packageName;
        }

        /// <summary>
        /// 协程启动包体初始化流程
        /// </summary>
        public void Init()
        {
            GameCore.Instance.StartCoroutine(InitPackage());
        }

        /// <summary>
        /// 初始化资源包
        /// </summary>
        /// <returns></returns>
        private IEnumerator InitPackage()
        {
            // 创建资源包实例
            if (!YooAssets.TryGetPackage(_packageName, out var package))
                package = YooAssets.CreatePackage(_packageName);

            InitializePackageOperation initializationOperation = null;
            
            // 编辑器模拟模式
            if (_playMode == EPlayMode.EditorSimulateMode)
            {
                var buildResult = EditorSimulateBuildInvoker.Build(_packageName, (int)EBundleType.VirtualAssetBundle);
                var packageRoot = buildResult.PackageRootDirectory;
                var createParameters = new EditorSimulateModeOptions
                {
                    EditorFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(packageRoot)
                };
                initializationOperation = package.InitializePackageAsync(createParameters);
            }
            
            // 离线打包模式
            if (_playMode == EPlayMode.OfflinePlayMode)
            {
                var createParameters = new OfflinePlayModeOptions
                {
                    BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters()
                };
                initializationOperation = package.InitializePackageAsync(createParameters);
            }
            
            yield return initializationOperation;
            
            if(initializationOperation is { Status: EOperationStatus.Succeeded })
            {
                Debug.Log($"[{GetType()}] 资源包初始化成功！");
            }
            else if(initializationOperation != null)
            {
                Debug.LogError($"[{GetType()}] 资源包初始化失败：{initializationOperation.Error}");
            }
            else
            {
                Debug.LogError($"[{GetType()}] 错误的资源加载模式：{_playMode}");
            }

            yield return UpdatePackageVersion(package);
            yield return UpdatePackageManifest(package, _packageVersion);
        }

        /// <summary>
        /// 获取包体版本
        /// </summary>
        private IEnumerator UpdatePackageVersion(ResourcePackage package)
        {
            var operation = package.RequestPackageVersionAsync();
            yield return operation;
            
            if (operation.Status == EOperationStatus.Succeeded)
            {
                //请求成功
                
                _packageVersion = operation.PackageVersion;
                Debug.Log($"Request package Version : {_packageVersion}");
            }
            else
            {
                //请求失败
                Debug.LogError(operation.Error);
            }
        }
        
        /// <summary>
        /// 更新包体资源列表
        /// </summary>
        private IEnumerator UpdatePackageManifest(ResourcePackage package, string packageVersion)
        {
            var operation = package.LoadPackageManifestAsync(new LoadPackageManifestOptions(packageVersion, 60));
            yield return operation;

            if (operation.Status != EOperationStatus.Succeeded)
            {
                //更新失败
                Debug.LogError(operation.Error);
            }
        }

        #region 资源管理方法

        public AssetHandle<T> Load<T>(AssetLocation location) where T : Object
        {
            return null;
        }

        public AssetHandle<T> LoadAsync<T>(AssetLocation location) where T : Object
        {
            return null;
        }

        public void ClearUnused()
        {
            GameCore.Instance.StartCoroutine(ClearUnusedAssets());
        }
        
        /// <summary>
        /// 清理没有使用的资源文件
        /// </summary>
        private IEnumerator ClearUnusedAssets()
        {
            var package = YooAssets.GetPackage(_packageName);
            var operation = package.UnloadUnusedAssetsAsync();
            operation.WaitForCompletion(); //支持同步操作
            yield return operation;

            if (operation.Status == EOperationStatus.Succeeded)
            {
                //清理成功
                Debug.Log($"[{GetType()}] 已清理所有未使用的资源文件");
            }
            else
            {
                //清理失败
                Debug.LogError(operation.Error);
            }
        }

        public void ClearAll()
        {
            GameCore.Instance.StartCoroutine(ClearAllAssets());
        }
        
        /// <summary>
        /// 清理所有的缓存资源文件
        /// </summary>
        private IEnumerator ClearAllAssets()
        {
            var package = YooAssets.GetPackage(_packageName);
            var operation = package.UnloadAllAssetsAsync();
            yield return operation;
            
            if (operation.Status == EOperationStatus.Succeeded)
            {
                //清理成功
                Debug.Log($"[{GetType()}] 已清理所有缓存文件");
            }
            else
            {
                //清理失败
                Debug.LogError(operation.Error);
            }
        }

        #endregion
    }
}