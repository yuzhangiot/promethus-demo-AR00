using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowFPS : MonoBehaviour
{
    public int fpsTarget;
    public float updateInterval = 0.5f;
    private float lastInterval;
    private int frames = 0;
    private float fps;
    // Start is called before the first frame update
    void Start()
    {
        //设置帧率
        Application.targetFrameRate = fpsTarget;
        lastInterval = Time.realtimeSinceStartup;
        frames = 0;
    }

    // Update is called once per frame
    void Update()
    {
        ++frames;
        float timeNow = Time.realtimeSinceStartup;
        if (timeNow >= lastInterval + updateInterval)
        {
            fps = frames / (timeNow - lastInterval);
            frames = 0;
            lastInterval = timeNow;
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle
        {
            fontSize = 50
        };

        GUI.Label(new Rect(30, 30, 100, 30), fps.ToString(), style);
    }
}
