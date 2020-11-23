/* Mesh Player Plugin for Unity.
*  All rights reserved. Prometheus 2020.
*  Contributor(s): Neil Z. Shao, Hongyi Li,Siqi Yao.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace prometheus
{
    public enum SOURCE_TYPE
    {
        PLAYBACK = 0,
        RTMP = 2,
    }

    public class MeshPlayerPRM : MonoBehaviour
    {
        #region Properties
        //-----------------------------//
        //-  Variables                -//
        //-----------------------------//

        public MeshReaderPRM meshReader = null;
        public AudioReader audioReader = null;

        public SOURCE_TYPE sourceType = SOURCE_TYPE.PLAYBACK;
        public string sourceUrl; 
        public bool dataInStreamingAssets = false;
        [HideInInspector]
        public float sourceDurationSec = 0;
        [HideInInspector]
        public float sourceFPS = 0;
        //[HideInInspector]
        //public float currentPts = -1;
        public float speedRatio = 1.0f;
        [HideInInspector]
        public bool isInitialized = false;
        private bool isOpened = false;
        private int apiKey = -1;
        [HideInInspector]
        public bool isPlaying = false;
        public bool playOnAwake = false;
        //public bool isLoop = false;
        private bool debugInfo = false;
        private float startTime;

        //for renderer
        private MeshFilter meshComponent;
        [HideInInspector]
        public Renderer rendererComponent;

        // mesh buffer
        private Mesh[] meshes = null;
        private Texture2D[] textures = null;
        private int bufferSize = 2;
        private int bufferIdx = 0;

        //for stream
        private bool isFirstMeshDataArrived = false;

        // short max
        private const int MAX_SHORT = 65535;

        //audio
        AudioClip audioClip;
        AudioSource audioSource;

        //control audioSource play speed(mfPitch) and volume(mfVolume)
        private float audioPitch = 1.0f;
        private float audioVolume = 1.0f;
        private bool audioLoop = true;

        private bool isFirstAudioDataArrived = false;
        private bool isFirstAudioPtsRecord = false;

        //the source with sound or not
        private bool withAudioSource;
        public bool isPlayAudio = true;

        //audio params
        private UInt16 mAudioChannels = 0;
        private int mAudioSampleRate = 0;

        //audio data store
        private List<float[]> audioSourceDataList;
        private List<double> audioSourcePtsList;
        private List<float> audioSourcePtsTimeList;

        private int audioDataPtsIndex = 0;
        //create 60 sec space to store audio data
        private float audioDuration = 60.0f;
        private bool mLoopTag = false;
        private int mAudioLengthSamples = 0;
        private int mAudioLoopCountInRtmp = 0;
        private int mLastAudioPtsNum = 0;
        //first audio time of mListAudioPtsTime,
        private float firstAudioDataPts = 0.0f;
        //start time of timeline
        //private float mStartTimeUseTimeLine = 0.0f;

        //mAudioSourcePlayTime < 0. mean the video is going to play back
        private float mAudioSourcePlayTime = 0.0f;

        //delete first audio frame 
        private int deletedStartAudioCount = 3;

        private int mAudioStartPts = 0;

        //time gap return the gap between audio pts and mesh pts, if gap is large, stop playing audio temporary
        private float mAudioMeshLastTimeGap = -1.0f;
        private float mAudioMeshCurTimeGap = -1.0f;
        private float mAudioMeshThreshold = 0.5f;

        //[SerializeField]
        public bool fixPositionY = false;
        private BoxCollider boxCollider;

        //comps
        public Action<Mesh, Texture2D> vfxUpdateAction;

        #endregion

        #region Functions
        //-----------------------------//
        //- Functions                 -//
        //-----------------------------//

        public void Initialize()
        {
            if (isInitialized && meshReader != null)
                return;

            if (debugInfo)
            {
                DebugDelegate callback_delegate = new DebugDelegate(CallbackDebug);
                System.IntPtr intptr_delegate = Marshal.GetFunctionPointerForDelegate(callback_delegate);
                ReaderAPIPRM.SetDebugFunction(intptr_delegate);
            }

            //initial mesh reader
            if (meshReader == null)
            {
                meshReader = MeshReaderPRM.CreateMeshReader(ref apiKey);
                if (meshReader == null)
                {
                    Debug.Log("[MeshPlayerPlugin][WARNING] Create Reader Instance Failed");
                    return;
                }
            }

            //initial audio reader
            if (audioReader == null)
            {
                audioReader = AudioReader.CreateAudioReader(meshReader.GetMeshApiKey());
                if (audioReader == null)
                {
                    Debug.Log("[MeshPlayerPlugin][WARNING] Create Reader Instance Failed");
                    return;
                }
            }
            meshComponent = GetComponent<MeshFilter>();
            rendererComponent = GetComponent<Renderer>();

            isFirstMeshDataArrived = false;
            isFirstAudioDataArrived = false;
            isFirstAudioPtsRecord = false;
            isInitialized = true;
        }

        public void Uninitialize()
        {
            if (isPlaying) {
                Pause();
            }
        
            if (!isInitialized)
                return;

            ReaderAPIPRM.UnityPluginUnload();
            meshReader = null;

            audioReader = null;
            isOpened = false;
            isInitialized = false;
        }

        private void AllocMeshBuffers()
        {
            //Allocates objects buffers for double buffering
            meshes = new Mesh[bufferSize];
            textures = new Texture2D[bufferSize];

            for (int i = 0; i < bufferSize; i++)
            {
                //Mesh
                Mesh mesh = new Mesh();
                Bounds newBounds = mesh.bounds;
                newBounds.extents = new Vector3(2, 2, 2);
                mesh.bounds = newBounds;
                meshes[i] = mesh;

                //Texture
                Texture2D texture = new Texture2D(meshReader.TextureWidth, meshReader.TextureHeight, meshReader.TextFormat, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                texture.Apply(); //upload to GPU
                textures[i] = texture;
            }
            bufferIdx = 0;
        }

        public void ClearBuffer()
        {
            if (meshes != null)
            {
                for (int i = 0; i < meshes.Length; i++)
                    DestroyImmediate(meshes[i]);
                meshes = null;
            }
            if (textures != null)
            {
                for (int i = 0; i < textures.Length; i++)
                    DestroyImmediate(textures[i]);
                textures = null;
            }
            meshReader.MeshData.ClearMeshBuffer();
            meshReader.MeshData.ClearTextureBuffer();
        }

        public void OpenSource(string url,float startTime, bool autoPlay)  {
            sourceUrl = url;
            OpenCurrentSource(startTime, autoPlay);
        }

        public void TestOpen() {
            OpenCurrentSource(0, true);
        }

        public bool OpenCurrentSource(float startTime,bool autoPlay)
        {
            Initialize();

            if (sourceUrl == "")
                return false;

            Debug.Log("[MeshPlayerPlugin] Open " + sourceUrl);
            isOpened = meshReader.OpenMeshStream(sourceUrl, dataInStreamingAssets);

            mAudioSourcePlayTime = 0;
            if (audioSource != null)
                audioSource.time = 0;

            if (isOpened)
            {
                //get audio stream info
                audioReader.audioStreamInfo();
                mAudioChannels = audioReader.mChannels;
                mAudioSampleRate = audioReader.mSampleRate;
                this.startTime = startTime;
                //check have audio or not
                if (mAudioChannels > 0 && mAudioSampleRate > 0)
                    withAudioSource = true;
                else
                    withAudioSource = false;
                SetSpeedRatio(speedRatio);        
                EnablePlayAudio(isPlayAudio);
                audioReader.setAudioMainSwitch(isPlayAudio);

                if (sourceType == SOURCE_TYPE.PLAYBACK)
                {
                    audioDuration = (int)meshReader.SourceDurationSec + 5;
                }
                else if (sourceType == SOURCE_TYPE.RTMP)
                {
                    audioDuration = 600;
                }
                mAudioLengthSamples = GetAudioLengthSample(mAudioSampleRate * (int)audioDuration, 1024);
                sourceDurationSec = meshReader.SourceDurationSec;

                if (startTime> sourceDurationSec) {
                    startTime = 0;
                }
         
                sourceFPS = meshReader.SourceFPS;
                AllocMeshBuffers();

                meshReader.SetSpeedRatio(speedRatio);
                meshReader.StartFromSecond(startTime);

                //initial mAudioList
                audioSourceDataList = new List<float[]>();
                audioSourcePtsList = new List<double>();
                audioSourcePtsTimeList = new List<float>();
                if (fixPositionY) {
                    FixedPositionY();
                }
             
                if (autoPlay)
                {
                    Play();
                }

            }
            else
            {
                Debug.Log("[MeshPlayerPlugin] Open Failed!");
            }

            return isOpened;
        }

        public void Preview(float startTime)
        {
            //Debug.LogError("start:"+Time.realtimeSinceStartup);
            Initialize();

            if (sourceUrl == "")
                return;

            Debug.Log("[MeshPlayerPlugin] Open " + sourceUrl);
            isOpened = meshReader.OpenMeshStream(sourceUrl, dataInStreamingAssets);

            if (isOpened)
            {
                //get audio stream info
                audioReader.audioStreamInfo();
                mAudioChannels = audioReader.mChannels;
                mAudioSampleRate = audioReader.mSampleRate;

                //check have audio or not
                if (mAudioChannels > 0 && mAudioSampleRate > 0)
                    withAudioSource = true;
                else
                    withAudioSource = false;

                EnablePlayAudio(isPlayAudio);
                audioReader.setAudioMainSwitch(isPlayAudio);

                if (sourceType == SOURCE_TYPE.PLAYBACK)
                {
                    audioDuration = (int)meshReader.SourceDurationSec + 5;
                }
                else if (sourceType == SOURCE_TYPE.RTMP)
                {
                    audioDuration = 600;
                }
                mAudioLengthSamples = GetAudioLengthSample(mAudioSampleRate * (int)audioDuration, 1024);

                sourceDurationSec = meshReader.SourceDurationSec;

                if (startTime > sourceDurationSec)
                {
                    startTime = 0;
                }
                sourceFPS = meshReader.SourceFPS;
                AllocMeshBuffers();
                //meshReader.SetSpeedRatio(speedRatio);
                GotoSecond(startTime);
                //initial mAudioList
                audioSourceDataList = new List<float[]>();
                audioSourcePtsList = new List<double>();
                audioSourcePtsTimeList = new List<float>();

                //Pause();
       
                meshReader.ForwardOneFrame();

                isPlaying = true;
                UpdateMeshData();
                UpdateMeshDisplay();
                isPlaying = false;
                //Debug.LogError("end:" + Time.realtimeSinceStartup);
            }
            else
            {
                Debug.Log("[MeshPlayerPlugin] Open Failed!");
            }
        }

        public void Play()
        {
            if (meshReader == null)
                return;

            if (audioReader == null)
                return;

            if (!isPlaying)
            {
                meshReader.Play();
                isPlaying = true;
            }
        }

        public void Pause()
        {
            if (meshReader == null)
                return;

            if (audioReader == null)
                return;

            if (isPlaying)
            {
                if (isPlayAudio) {
                    if (audioSource != null)
                    {
                        audioSource.Pause();
                    }
                }

                meshReader.Pause();
                isPlaying = false;
            }
        }

        public void SetSpeedRatio(float speedRatio)
        {
            this.speedRatio = speedRatio;
            audioPitch = speedRatio;
        }

        public void EnablePlayAudio(bool bo)
        {
            if (withAudioSource)
            {
                isPlayAudio = bo;
            }
            else
            {
                isPlayAudio = false;
            }
        }

        public void GotoSecond(float sec)
        {
            //currentPts = sec;
            if (meshReader == null)
                return;

            Debug.Log("[MeshPlayerPlugin] GotoSecond(): " + sec);
            meshReader.StartFromSecond(sec);
        }

        void UpdateMeshData()
        {
            float ptsSec = -1;
            if (isPlayAudio)
            {
                if (meshReader.ReadNextFrame(ref ptsSec, ref mAudioSourcePlayTime, ref mAudioMeshCurTimeGap))
                {
                    if (mAudioMeshCurTimeGap < mAudioMeshThreshold)
                    {
                        mAudioMeshLastTimeGap = -1.0f;
                        mAudioMeshCurTimeGap = -1.0f;
                    }

                    //video loop mode, mAudioSourcePlayTime return -1,mean to update mAudioSource.time 
                    isFirstMeshDataArrived = true;
                }
            }
            else
            {
                if (meshReader.ReadNextFrame(ref ptsSec))
                {
                    isFirstMeshDataArrived = true;
                }
            }
        }

        void UpdateMeshDisplay()
        {
            if (meshReader.MeshData.vertices == null) {
                return;
            }

            // display
            Mesh mesh = meshes[bufferIdx];
            mesh.MarkDynamic();
            if (meshReader.MeshData.vertices.Length > MAX_SHORT)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.vertices = meshReader.MeshData.vertices;
            mesh.normals = meshReader.MeshData.normals;
            mesh.uv = meshReader.MeshData.uv;
            mesh.triangles = meshReader.MeshData.triangles;
            mesh.UploadMeshData(false);
            mesh.RecalculateNormals();
            Texture2D texture = textures[bufferIdx];
            if (meshReader.MeshData.mVersion == Version.FORMAT_NEW)
                texture.LoadRawTextureData(meshReader.MeshData.color_bytes);
            else
                texture.SetPixels32(meshReader.MeshData.colors);
            texture.Apply();
            // texture.Apply();

            meshComponent.mesh = mesh;

            //如果场景中存在多个人人物，请使用material替代sharedMaterial。 If there are multiple characters in the scene, please use material instead of sharedMaterial
            //rendererComponent.material.mainTexture = texture;
            rendererComponent.sharedMaterial.mainTexture = texture;
            // done with buffer
            bufferIdx = (bufferIdx + 1) % bufferSize;

            //update vfx comp
            vfxUpdateAction?.Invoke(mesh, texture);
        }

        void UpdateAudioData()
        {
            if (isPlayAudio) {
                float ptsSec = -1;
                while (audioReader.GetAudioClipData(ref ptsSec))
                {
                    Debug.LogWarning("222222222222222222222222 Audio ptsSec!!:" + ptsSec);
                    //important if realtime, must delete audio frames until first mesh is found
                    if (sourceType == SOURCE_TYPE.RTMP && (!isFirstMeshDataArrived || ptsSec < meshReader.FirstPtsSecInRealTime))
                    {
                        continue;
                    }

                    //must delete first several audio frames
                    if (deletedStartAudioCount > 0)
                    {
                        deletedStartAudioCount--;
                        continue;
                    }

                    //Debug.Log("~~~~~~~~~~~~pts_sec : "  + pts_sec);
                    if (ptsSec > startTime)
                    {
                        float[] data = audioReader.AudioData.audio_data;

                        if (debugInfo)
                        {
                            int pts_n = (int)Math.Round(ptsSec * mAudioSampleRate / 1024);
                            if (pts_n > mLastAudioPtsNum + 1)
                            {
                                //to detect if there is audio packet lost
                                Debug.Log("pts " + pts_n + " mLastAudioPtsNum " + mLastAudioPtsNum);
                            }
                            mLastAudioPtsNum = pts_n;
                        }

                        bool isAudioDataEnough = false;
                        if (audioSourceDataList.Count > 30)
                            isAudioDataEnough = true;
                        if (sourceType == SOURCE_TYPE.PLAYBACK && sourceDurationSec < 1.0 && audioSourceDataList.Count > 1)
                        {
                            isAudioDataEnough = true;
                        }

                        if (isAudioDataEnough)
                        {
                            if (!isFirstAudioDataArrived)
                            {
                                isFirstAudioDataArrived = true;
                            }
                        }

                        //if playback and play loop, don't record audio data to audioclip again 
                        if (sourceType == SOURCE_TYPE.PLAYBACK && audioDataPtsIndex * 1024 / (float)mAudioSampleRate > sourceDurationSec)
                            continue;
                        //if realtime and audioclip data have
                        if (sourceType == SOURCE_TYPE.RTMP && audioDataPtsIndex * 1024 >= mAudioLengthSamples)
                        {
                            audioDataPtsIndex = 0;
                            continue;
                        }

                        audioSourceDataList.Add(data);
                        audioSourcePtsList.Add(audioDataPtsIndex++ * 1024);
                        audioSourcePtsTimeList.Add(ptsSec);
                    }
                }
            }
          
        }

        public void UpdateAudioDisplay()
        {
            if (isPlayAudio)
            {
                if (audioSource != null && isFirstAudioDataArrived == true)
                {
                    //if in live ,pts time is not started at 0
                    if (!isFirstAudioPtsRecord)
                    {
                        if (audioSourcePtsTimeList.Count > 0)
                        {
                            firstAudioDataPts = audioSourcePtsTimeList[0];
                            Debug.Log("start_time: " + firstAudioDataPts);
                            isFirstAudioPtsRecord = true;
                        }
                    }

                    if (audioSource.isPlaying == false)
                    {
                        audioSource.Play();
                    }

                    if (sourceType == SOURCE_TYPE.RTMP && audioSource != null && mAudioMeshCurTimeGap > mAudioMeshThreshold)
                    {
                        if (mAudioMeshLastTimeGap > mAudioMeshThreshold && mAudioMeshCurTimeGap > mAudioMeshLastTimeGap)
                        {
                            if (audioSource.time - mAudioMeshCurTimeGap - 3 > 0)
                                audioSource.time = audioSource.time - mAudioMeshCurTimeGap - 3;
                            else
                                audioSource.time = 0;
                        }
                        mAudioMeshLastTimeGap = mAudioMeshCurTimeGap;

                    }
                    UpdateAudioSourcePlayTime();
                }

           
                // create audiosource object and audio clip
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.pitch = audioPitch;
                    audioSource.volume = audioVolume;
                    audioSource.loop = audioLoop;
                }
                if (audioClip == null && audioSource != null)
                {
                    audioClip = AudioClip.Create("videoAudio", mAudioLengthSamples, mAudioChannels, mAudioSampleRate, false);
                    audioSource.clip = audioClip;
                }

                //add sound data to mAudioClip
                for (int i = 0; i < audioSourceDataList.Count; i++)
                {

                    if (audioSource != null)
                    {
                        if (audioSourcePtsList.Count > i && audioSourcePtsList[i] >= 0)
                        {
                            audioClip.SetData(audioSourceDataList[i], (int)((audioSourcePtsList[i] - mAudioStartPts) % mAudioLengthSamples));
                        }
                    }
                }

                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSourceDataList.Clear();
                    audioSourcePtsList.Clear();
                    audioSourcePtsTimeList.Clear();
                }
            }
        }

        public Texture2D GetCurrentTexture() {
            return textures[bufferIdx];
        }

        public void CleanMesh() {
            if (meshComponent != null)
                meshComponent.mesh = null;
        }

        int GetAudioLengthSample(int num, int divider)
        {
            return (num / divider + 1) * divider;
        }

        bool CheckIsReadyToPlay()
        {
            if (isPlayAudio)
            {
                if (isFirstAudioDataArrived && isFirstMeshDataArrived)
                    return true;
            }
            else
            {
                if (isFirstMeshDataArrived)
                    return true;
            }
            return false;
        }

        void UpdateAudioSourcePlayTime()
        {
            if (sourceType == SOURCE_TYPE.PLAYBACK)
            {
                if (mAudioSourcePlayTime < 0)
                {
                    audioSource.time = 0;
                    mAudioSourcePlayTime = 0;
                }
                else
                {
                    mAudioSourcePlayTime = audioSource.time + firstAudioDataPts - startTime;
                    Debug.Log("33333333333333333333333333 mAudioSourcePlayTime is " + mAudioSourcePlayTime);
                }
                    
            }
            else
            {
                //if AudioClip have filled with audio, then set data from begining
                float sec = mAudioLengthSamples / (float)mAudioSampleRate;
                if (audioSource.time > sec / 2)
                {
                    mLoopTag = true;
                }

                if (audioSource.time < 1.0f && mLoopTag)
                {
                    mAudioLoopCountInRtmp++;
                    mLoopTag = false;
                }

                mAudioSourcePlayTime = audioSource.time + firstAudioDataPts + mAudioLoopCountInRtmp * sec;
            }
        }

        public void FixedPositionY()
        {    
            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider>();
                gameObject.transform.localPosition += new Vector3(0, -boxCollider.bounds.min.y, 0);          
            }
        }

        // callback for debug
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DebugDelegate(string str);

        [AOT.MonoPInvokeCallback(typeof(DebugDelegate))]
        static void CallbackDebug(string str)
        {
            Debug.Log(str);
        }

        #endregion

        #region Runtime

        private void Awake()
        {
            CleanMesh();
        }

        private void Start()
        {
            if (playOnAwake) {
                OpenCurrentSource(0,true);
            }
        }

        public void Update()
        {
            if (!isInitialized)
                Initialize();

            if (!isOpened)
                return;
     
            if (isPlaying) {
                UpdateAudioData();
                UpdateMeshData();

                if (!CheckIsReadyToPlay())
                    return;

                UpdateAudioDisplay();
                UpdateMeshDisplay();        
            }
        }

        private void OnDestroy()
        {
            Destroy();
        }

        public void Destroy()
        {
            Pause();
            Uninitialize();

            if (meshes != null)
            {
                for (int i = 0; i < meshes.Length; i++)
                    DestroyImmediate(meshes[i]);
                meshes = null;
            }
            if (textures != null)
            {
                for (int i = 0; i < textures.Length; i++)
                    DestroyImmediate(textures[i]);
                textures = null;
            }

            if (audioSourceDataList != null)
                audioSourceDataList.Clear();

            if (audioSourcePtsList != null)
                audioSourcePtsList.Clear();

            if (audioSourcePtsTimeList != null)
                audioSourcePtsTimeList.Clear();
        }
        #endregion
    }
}
