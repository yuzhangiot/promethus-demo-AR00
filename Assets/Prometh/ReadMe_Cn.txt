## 简介
  HoloCatchLightPlugin是一套强大的容积视频Unity插件，可以将我们提供的或者您手中的容积视频（将您的OBJ模型序列通过我们的工具转成通用格式）导入到Unity进行开发，并包含丰富且强大的二次开发能力，功能模块开源，您可根据自身需求自定义扩展，帮助您更快的将容积视频素材融入您的项目。


  容积视频模型序列转换工具：https://holodata.s3.cn-northwest-1.amazonaws.com.cn/DownloadFile/Tools/ObjSeqConverter.zip

## 主要特性

* 提供录播和实时直播的高性能解码能力
* 可在编辑器模式预览
* 全平台
* 强大的Timeline辅助编辑功能
* 支持VFX特效
* 多种材质支持,支持替换颜色

## 支持设备

* Window
* Mac
* IOS
* Android
* Hololens（马上就绪）

## 开发环境要求
    unity2018.4以上，建议unity2019.3及以上，本工程包含Unity VFX特效,所以必须采用URP渲染管线,如何切换为URP渲染管线请参考Unity官方教程。

## 文件目录
    ├─Prometh 插件主要部分
    │  ├─Editor
    │  │      MeshPreviewPRMEditor.cs 编辑器脚本，提供编辑器模式预览等功能
    │  │
    │  ├─Plugins 各个平台的库
    │  │  ├─arm64-v8a 对应安卓64位平台
    │  │  ├─armeabi-a7v 对应安卓32位平台
    │  │  ├─ios 对应ios平台
    │  │  ├─Mac 对应Mac平台
    │  │  ├─UWP 对应Hololens平台(UWP平台需要切换至对应平台，然后从压缩包中解压出目录中的插件)
    │  │  └─x86_64 对应windows64位平台
    │  │
    │  ├─Prefabs
    │  │  │  PromethCube.prefab 用来快速使用的预制体
    │  │  │
    │  │  └─Material
    │  │          Logo.png
    │  │          MatPrometh.mat 预制体对应材质
    │  │
    │  ├─RendererSetting  URP管线配置文件，可以根据需求自行替换
    │  │
    │  ├─Scripts
    │  │     插件C#主要逻辑部分
    │  │     
    │  ├─Scenes
    │  │  ├─Basic 基本解码播放
    │  │  ├─MaterialDemo 材质切换演示
    │  │  ├─RepalceColorDemo 颜色替换演示
    │  │  ├─TimelineDemo 时间线控制演示
    │  │  └─VFXDemo VFX特效演示
    │  │
    │  └─StreamingAssets 首先要将这个文件夹移到根目录，容积视频文件要放在这里

## 快速入门
首先将Prometh/StreamingAssets文件夹放到Assets根目录
您可以直接运行Scene里面的Basic场景来快速使用，或者新建场景自行创建，首先将PromethCube拖进场景，将组件中的SourceType选择为PLAYBACK，SourcePath填入StreamingAssets文件夹下的路径，在StreamingAssets下文路径要勾选InStreamingAssets属性，编辑器下可以添加MeshPreviewPRM组件然后拖动进度条进行预览。

## API控制
MeshPlayerPRM提供出一些接口可以对视频播放进行控制
* MeshPlayerPRM.OpenSource(string url,float startTime, bool autoPlay) // 打开文件，参数为地址，开始时间，是否直接播放
* MeshPlayerPRM.Play()  // 播放
* MeshPlayerPRM.Pause()  // 暂停
* MeshPlayerPRM.GotoSecond(float sec)  // 跳到多少秒
* MeshPlayerPRM.SpeedRatio //控制速度，播放前生效

## 扩展组件
* MeshPreviewPRM  // 编辑器预览
* MeshMaterialsPRM  // 材质替换
* MeshTimelinePRM  // Timeline控制
* MeshVfxPRM  // VFX特效

## 提示 
* MeshPlayerPRM的文件路径也可以填写硬盘上的绝对路径，需要取消勾选InStreamingAssets属性
* 如果没有安装对应平台的支持（如android，ios），可能会在打包的时候导致插件重名冲突，可以将不需要的平台插件从你的工程中移除
* 打包Ios到Xcode后，需要在UnityFramework中添加VideoToolBox.framework库，才能正常build到ios设备上

## 演示视频
(https://www.bilibili.com/video/BV1Dt4y1C7Qs)

* 您有什么疑问或者需求请联系技术支持: busiyg@163.com
