## Introduction
  HoloCatchLightPlugin is a powerful set of volume video Unity plug-ins, which can import the volume video provided by us or in your hands (to convert your OBJ model sequence into a common format through our tool) for development, and contains rich and powerful Secondary development capabilities, open source functional modules, you can customize extensions according to your own needs, helping you to integrate volumetric video materials into your project faster.

  Volume video model sequence conversion tool:https://holodata.s3.cn-northwest-1.amazonaws.com.cn/DownloadFile/Tools/ObjSeqConverter.zip

## Main features

* Provide high-performance decoding capabilities for recording, broadcasting and real-time live broadcasting
* Can preview in editor mode
* All platforms
* Powerful Timeline auxiliary editing function
* Support VFX special effects
* A variety of material support, support for replacing colors

## supporting device

* Window
* Mac
* IOS
* Android
* Hololens(come soon)

## Development environment requirements
    Unity2018.4 and above, unity2019.3 and above are recommended. This project contains Unity VFX special effects, so URP rendering pipeline must be adopted. Please refer to the official Unity tutorial for how to switch to URP rendering pipeline.

## File Directory
    ├─Prometh plug-in main part
    │ ├─Editor
    │ │ MeshPreviewPRMEditor.cs editor script, providing editor mode preview and other functions
    │ │
    │ ├─Plugins libraries for each platform
    │ │ ├─arm64-v8a corresponds to Android 64-bit platform
    │ │ ├─armeabi-a7v corresponding to Android 32-bit platform
    │ │ ├─ios corresponding to ios platform
    │ │ ├─Mac corresponds to Mac platform
    │ │ ├─UWP corresponds to Hololens platform (UWP platform needs to switch to the corresponding platform, and then extract the plug-in in the directory from the compressed package)
    │ │ └─x86_64 corresponds to windows64-bit platform
    │ │
    │ ├─Prefabs
    │ │ │ PromethCube.prefab Prefab for quick use
    │ │ │
    │ │ └─Material
    │ │     Logo.png
    │ │     MatPrometh.mat corresponding material of prefab
    │ │
    │ │
    │ ├─RendererSetting  URP pipeline configuration file, you can replace it according to your needs
    │ ├─Scripts
    │ │    Plug-in C# main logic part
    │ │
    │ ├─Scenes
    │ │ ├─Basic basic decoding and playback
    │ │ ├─MaterialDemo material switching demo
    │ │ ├─RepalceColorDemo color replacement demo
    │ │ ├─TimelineDemo Timeline control demo
    │ │ └─VFXDemo VFX special effects demonstration
    │ │
    │ └─StreamingAssets First move this folder to the root directory, and the volume video files should be placed here

## Quick start
First put the Prometh/StreamingAssets folder in the Assets root directory
You can run the Basic scene in Scene directly to use it quickly, or create a new scene by yourself. First drag PromethCube into the scene, select the SourceType in the component as PLAYBACK, and fill in the SourcePath in the path under the StreamingAssets folder, and tick the path below StreamingAssets Select the InStreamingAssets property, you can add MeshPreviewPRM components under the editor and then drag the progress bar to preview.

## API Control
MeshPlayerPRM provides some interfaces to control video playback
* MeshPlayerPRM.OpenSource(string url,float startTime, bool autoPlay) // Open the file, the parameters are the address, start time, whether to play directly
* MeshPlayerPRM.Play() // play
* MeshPlayerPRM.Pause() // pause
* MeshPlayerPRM.GotoSecond(float sec) // how many seconds to jump to
* MeshPlayerPRM.SpeedRatio //Control speed, take effect before playing

## Extension components
* MeshPreviewPRM // Editor preview
* MeshMaterialsPRM // Material replacement
* MeshTimelinePRM // Timeline control
* MeshVfxPRM // VFX special effects

## Tips
* The file path of MeshPlayerPRM can also fill in the absolute path on the hard disk, you need to uncheck the InStreamingAssets property
* If you do not install the support of the corresponding platform (such as android, ios), it may cause the plug-in name conflict when packaging, you can remove the unnecessary platform plug-in from your project
* After packaging Ios to Xcode, you need to add the VideoToolBox.framework library to UnityFramework to build it to ios devices normally

## Demo video
(https://www.bilibili.com/video/BV1Dt4y1C7Qs)

* If you have any questions or needs, please contact technical support: busiyg@163.com