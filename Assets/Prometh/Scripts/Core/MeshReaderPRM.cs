using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Networking;

namespace prometheus
{
    public enum Version
    {
        FORMAT_OLD = 0,
        FORMAT_NEW = 1
    }
    public class MeshDataPRM
    {
        // mesh buffer
        public Vector3[] vertices = null;
        public Vector3[] normals = null;
        public Vector2[] uv = null;
        public int[] triangles = null;
        public int bufferSize = 0;

        public GCHandle gcHandlerVertices;
        public GCHandle gcHandlerNormals;
        public GCHandle gcHandlerUV;
        public GCHandle gcHandlerTriangles;

        // texture buffer
        public int textWidth = 0, textHeight = 0;
        public Color32[] colors = null;
        public byte[] color_bytes;
        public GCHandle gcHandlerColors;

        public float ptsSec;
        public bool realtime = false;

        public Version mVersion = Version.FORMAT_NEW;

        public void AllocMeshBuffer(int numTris)
        {
            if (bufferSize < numTris * 3)
            {
                ClearMeshBuffer();
                bufferSize = ((int)(numTris * 1.1) / 3 + 1) * 3;
                normals = new Vector3[bufferSize];
                vertices = new Vector3[bufferSize];
                uv = new Vector2[bufferSize];
                triangles = new int[bufferSize];
                // pin memory
                gcHandlerVertices = GCHandle.Alloc(vertices, GCHandleType.Pinned);
                gcHandlerNormals = GCHandle.Alloc(normals, GCHandleType.Pinned);
                gcHandlerUV = GCHandle.Alloc(uv, GCHandleType.Pinned);
                gcHandlerTriangles = GCHandle.Alloc(triangles, GCHandleType.Pinned);
            }
        }

        public void ClearMeshBuffer()
        {
            if (bufferSize == 0)
                return;

            if (gcHandlerVertices.IsAllocated) gcHandlerVertices.Free();
            if (gcHandlerUV.IsAllocated) gcHandlerUV.Free();
            if (gcHandlerTriangles.IsAllocated) gcHandlerTriangles.Free();
            if (gcHandlerNormals.IsAllocated) gcHandlerNormals.Free();
            bufferSize = 0;
            vertices = null;
            normals = null;
            uv = null;
            triangles = null;
        }

        public void AllocTextureBuffer(int width, int height, TextureFormat textFormat)
        {
            if (textWidth != width || textHeight != height)
            {
                ClearTextureBuffer();
                if (mVersion == Version.FORMAT_OLD)
                {
                    colors = new Color32[width * height];
                    gcHandlerColors = GCHandle.Alloc(colors, GCHandleType.Pinned);
                }
                else
                {
                    color_bytes = new byte[width * height];
                    gcHandlerColors = GCHandle.Alloc(color_bytes, GCHandleType.Pinned);
                }
                textWidth = width;
                textHeight = height;
            }
        }

        public void ClearTextureBuffer()
        {
            if (textWidth == 0 || textHeight == 0)
                return;

            if (gcHandlerColors.IsAllocated) gcHandlerColors.Free();
            colors = null;
            color_bytes = null;
            textWidth = 0;
            textHeight = 0;
        }
    }

    public class MeshReaderPRM
    {
        // public
        public int ApiKey = -1;
        public TextureFormat TextFormat;
        public int TextureWidth, TextureHeight;

        public string SourceUrl = "";
        public float SourceDurationSec = 0, SourceFPS = 0;
        public int SourceNbFrames = 0;

        //added by lhy,to record first mesh pts sec
        public float FirstPtsSecInRealTime = -1;
        public bool FirstRecordInRealTime = true;

        public MeshDataPRM MeshData = null;

        // create api instance
        static public MeshReaderPRM CreateMeshReader(ref int apiKey)
        {
            MeshReaderPRM instance = new MeshReaderPRM(apiKey);
            if (instance.ApiKey == -1)
                return null;

            return instance;
        }

        // MeshReader
        private MeshReaderPRM(int apiKey)
        {
            if (apiKey == -1)
                ApiKey = ReaderAPIPRM.CreateApiInstance();
            else
                ApiKey = apiKey;

            MeshData = new MeshDataPRM();

            Debug.Log("[MeshReader] Create API instance " + ApiKey);
        }

        ~MeshReaderPRM()
        {
            Release();
        }

        public void Release()
        {
            MeshData.ClearMeshBuffer();
            MeshData.ClearTextureBuffer();
        }

        public int GetMeshApiKey()
        {
            return ApiKey;
        }

        public int getWidthInNewFormat(int TextureWidth)
        {
            int width_new = 0;
            if (TextureWidth == 2)
                width_new = 1024;
            else if (TextureWidth == 4)
                width_new = 2048;
            else if (TextureWidth == 6)
                width_new = 2880;
            else if (TextureWidth == 10)
                width_new = 512;
            else
                width_new = 3840;
            return width_new;
        }

        public TextureFormat getTextureFormat()
        {
            TextureFormat format;
            if (Application.platform == RuntimePlatform.Android)
            {
                Debug.Log("Android");
                format = TextureFormat.ASTC_4x4;
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                Debug.Log("IOS");
                format = TextureFormat.PVRTC_RGB4;
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                Debug.Log("Window");
                format = TextureFormat.BC7;
            }
            else
                format = TextureFormat.BC7;
            return format;
        }

        // Control
        public bool OpenMeshStream(string sourceUrl, bool mDataInStreamingAssets)
        {
            if (!sourceUrl.Contains(".mp4") && !sourceUrl.Contains("rtmp://"))
            {
                sourceUrl += ".mp4";
            }

            string url = sourceUrl;

            if (!url.StartsWith("http") && !url.StartsWith("rtmp") && mDataInStreamingAssets)
            {
                url = Application.streamingAssetsPath + "/" + sourceUrl;
                Debug.Log("[MeshReader] Open in StreamingAssets: " + url);

                //ANDROID STREAMING ASSETS => need to copy the data somewhere else on device to acces it, beacause it is currently in jar file
                if (url.StartsWith("jar"))
                {
                    WWW www = new WWW(url);
                    //yield return www; //can't do yield here, not really blocking beacause the data is local
                    while (!www.isDone) ;

                    if (!string.IsNullOrEmpty(www.error))
                    {
                        Debug.LogError("[MeshReader] PATH : " + url);
                        Debug.LogError("[MeshReader] Can't read data in streaming assets: " + www.error);
                    }
                    else
                    {
                        //copy data on device
                        url = Application.persistentDataPath + "/" + sourceUrl;
                        if (!System.IO.File.Exists(url))
                        {
                            Debug.Log("[MeshReader] NEW Roopath: " + url);
                            System.IO.FileStream fs = System.IO.File.Create(url);
                            fs.Write(www.bytes, 0, www.bytesDownloaded);
                            Debug.Log("[MeshReader] data copied");
                            fs.Dispose();
                        }
                    }
                }                           
            }

            TextFormat = getTextureFormat();
            //TextFormat = TextureFormat.PVRTC_RGB4;
            Debug.Log("...................this platform is ............->" + TextFormat);
            if (!ReaderAPIPRM.OpenMeshStream(ApiKey, url ,(int)TextFormat))
                return false;
            //ReaderAPIPRM.SetReaderLoop(ApiKey, false);
            ReaderAPIPRM.GetResolution(ApiKey, ref TextureWidth, ref TextureHeight);
            MeshData.mVersion = (Version)ReaderAPIPRM.GetFormatVersion(ApiKey);
            ReaderAPIPRM.GetMeshStreamInfo(ApiKey, ref SourceDurationSec, ref SourceFPS, ref SourceNbFrames);
            if (MeshData.mVersion == Version.FORMAT_OLD)
                TextFormat = TextureFormat.RGBA32;
            else
            {
                TextureWidth = getWidthInNewFormat(TextureWidth);
                TextureHeight = TextureWidth;
            }

            Debug.Log("[MeshPlayerPlugin] Open Success!");
            Debug.Log("[MeshReader] Stream Duration = " + SourceDurationSec);
            Debug.Log("[MeshReader] Stream FPS = " + SourceFPS);
            Debug.Log("[MeshReader] Stream Number of Frames = " + SourceNbFrames);
            return true;
        }

        public bool StartFromSecond(float sec)
        {
            ReaderAPIPRM.SetReaderStartSecond(ApiKey, sec);
            return false;
        }

        public bool StartFromFrameIdx(int frmIdx)
        {
            return false;
        }

        public void SetSpeedRatio(float speedRatio)
        {
            ReaderAPIPRM.SetSpeedRatio(ApiKey, speedRatio);
            return;
        }

        public void Play()
        {
            ReaderAPIPRM.PlayReader(ApiKey);
        }

        public void Pause()
        {
            ReaderAPIPRM.PauseReader(ApiKey);
        }

        public void ForwardOneFrame()
        {
            ReaderAPIPRM.ForwardOneFrame(ApiKey);
        }

        // Access Data
        public bool ReadNextFrame(ref float ptsSec)
        {
            //Debug.Log("[MeshReader] ReadNextFrame()");

            if (!ReaderAPIPRM.BeginReadFrame(ApiKey, ref ptsSec))
                return false;
            //Debug.Log("~~~~~~~~~~~~~~~~~~~~~[ReadNextFrame] Video ptsSec ........" + ptsSec);
            MeshData.ptsSec = ptsSec;
            int numTris = ReaderAPIPRM.GetVerticesCount(ApiKey);
            MeshData.AllocMeshBuffer(numTris);
            ReaderAPIPRM.SetMeshVertices(ApiKey, MeshData.bufferSize, 
                MeshData.gcHandlerVertices.AddrOfPinnedObject(),
                MeshData.gcHandlerNormals.AddrOfPinnedObject(),
                MeshData.gcHandlerUV.AddrOfPinnedObject(),
                MeshData.gcHandlerTriangles.AddrOfPinnedObject());
            //Debug.Log("[MeshReader] Read Mesh Triangles = " + numTris);
            MeshData.AllocTextureBuffer(TextureWidth, TextureHeight, TextFormat);
            if (MeshData.mVersion == Version.FORMAT_NEW)
            {
                ReaderAPIPRM.SetMeshTexturesWithFormat(ApiKey, MeshData.gcHandlerColors.AddrOfPinnedObject(), (int)TextFormat);
            }
            else
            {
                ReaderAPIPRM.SetMeshTextures(ApiKey, TextureWidth, TextureHeight, 4,
                MeshData.gcHandlerColors.AddrOfPinnedObject());
            }

            //Debug.Log("[MeshReader] Read Texture = " + TextureWidth + "x" + TextureHeight);

            ReaderAPIPRM.EndReadFrame(ApiKey);
            return true;
        }

        public bool ReadNextFrame(ref float ptsSec, ref float soundSec, ref float lastTimeGap)
        {
            //Debug.Log("[MeshReader] ReadNextFrame()");

            if (!ReaderAPIPRM.BeginReadFrameWithSoundSec(ApiKey, ref ptsSec, ref soundSec, ref lastTimeGap))
                return false;
            Debug.Log("~~~~~~~~~~~~~~~~~~~~~[ReadNextFrame] Video ptsSec ........" + ptsSec);
            if (FirstRecordInRealTime && ptsSec > 0)
            {
                FirstPtsSecInRealTime = ptsSec;
                FirstRecordInRealTime = false;
            }
            MeshData.ptsSec = ptsSec;
            int numTris = ReaderAPIPRM.GetVerticesCount(ApiKey);
            Debug.Log(".............................numTris num is .........................." + numTris);
            MeshData.AllocMeshBuffer(numTris);
            ReaderAPIPRM.SetMeshVertices(ApiKey, MeshData.bufferSize,
                MeshData.gcHandlerVertices.AddrOfPinnedObject(),
                MeshData.gcHandlerNormals.AddrOfPinnedObject(),
                MeshData.gcHandlerUV.AddrOfPinnedObject(),
                MeshData.gcHandlerTriangles.AddrOfPinnedObject());
            //Debug.Log("[MeshReader] Read Mesh Triangles = " + numTris);

            MeshData.AllocTextureBuffer(TextureWidth, TextureHeight, TextFormat);
            if (MeshData.mVersion == Version.FORMAT_NEW)
            {
                ReaderAPIPRM.SetMeshTexturesWithFormat(ApiKey, MeshData.gcHandlerColors.AddrOfPinnedObject(), (int)TextFormat);
            }
            else
            {
                ReaderAPIPRM.SetMeshTextures(ApiKey, TextureWidth, TextureHeight, 4,
                MeshData.gcHandlerColors.AddrOfPinnedObject());
            }
            //Debug.Log("[MeshReader] Read Texture = " + TextureWidth + "x" + TextureHeight);

            ReaderAPIPRM.EndReadFrame(ApiKey);
            return true;
        }


    }

}
