using UnityEngine;
using System.Text;
using UnityEngine.Profiling;
using UnityEngine.UI;
using TMPro;


#if UNITY_EDITOR
using UnityEditor;
#endif
#if UNITY_5
using UnityEngine.Profiling;
#endif
public class StatsManager : MonoBehaviour
{
    [Header("Stats Display")]
    private StringBuilder tx;
    public TMP_Text text;

    private float updateInterval = 1.0f;
    private float lastInterval; // Last interval end time
    private float frames = 0; // Frames over current interval

    private float framesavtick = 0;
    private float framesav = 0.0f;

    [Header("FPS settings")]
    public int maxFps = 30;
    public Slider slider;
    public TMP_Text sliderText;

    // Use this for initialization
    void Start()
    {
        Application.targetFrameRate = maxFps;
        sliderText.text = "FPS CAP: " + maxFps;
        slider.value = maxFps;
        lastInterval = Time.realtimeSinceStartup;
        frames = 0;
        framesav = 0;
        tx = new StringBuilder();
        tx.Capacity = 200;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }


    // Update is called once per frame
    void Update()
    {
        ++frames;

        var timeNow = Time.realtimeSinceStartup;

        if (timeNow > lastInterval + updateInterval)
        {
            if (!text)
            {
                text.gameObject.hideFlags = HideFlags.HideAndDontSave;
                text.gameObject.transform.position = new Vector3(0, 0, 0);
                //text.color = tx_Color;
            }

            float fps = frames / (timeNow - lastInterval);
            float ms = 1000.0f / Mathf.Max(fps, 0.00001f);

            ++framesavtick;
            framesav += fps;
            float fpsav = framesav / framesavtick;

            tx.Length = 0;

            tx.AppendFormat("Time : {0} ms\nCurrent FPS: {1}\nAvgFPS: {2}\n\nGPU memory : {3}\nSys Memory : {4}\n", ms, fps, fpsav, SystemInfo.graphicsMemorySize, SystemInfo.systemMemorySize)

            .AppendFormat("TotalAllocatedMemory : {0}mb\nTotalReservedMemory : {1}mb\nTotalUnusedReservedMemory : {2}mb",
            Profiler.GetTotalAllocatedMemoryLong() / 1048576,
            Profiler.GetTotalReservedMemoryLong() / 1048576,
            Profiler.GetTotalUnusedReservedMemoryLong() / 1048576
            ).AppendFormat("\nTotalAllocatedMemoryForGraphicsDriver: {0}mb", Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576);
#if UNITY_EDITOR
            tx.AppendFormat("\n\nDrawCalls : {0}\nUsed Texture Memory : {1}\nrenderedTextureCount : {2}", UnityStats.drawCalls, UnityStats.usedTextureMemorySize / 1048576, UnityStats.usedTextureCount);
#endif

            var vertices = 0; 
            var triangles = 0;
            (vertices, triangles) = ChunksManager.Instance.GetAllMeshesVerticesAndTriangles();
            tx.AppendFormat("\n\nRendered:\nVertices: {0}\nTriangles: {1}\n", (vertices).ToString("N0"), (triangles).ToString("N0"));

            text.text = tx.ToString();
            frames = 0;
            lastInterval = timeNow;

            if (slider.value != maxFps)
            {
                maxFps = (int)slider.value;
                Application.targetFrameRate = maxFps;
                sliderText.text = "FPS: " + maxFps;
            }
        }

    }
}

