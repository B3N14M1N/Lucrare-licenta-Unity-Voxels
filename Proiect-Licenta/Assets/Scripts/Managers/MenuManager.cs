using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public WorldManager WorldManager;
    private bool initialized = false;
    public PlayerController playerController;
    public RectTransform mousePointer;

    [Header("Screen")]
    public KeyCode screenWindowKeyCode = KeyCode.F11;
    [Header("Stats")]
    public KeyCode statsKeyCode = KeyCode.F3;
    public GameObject Pannel;

    [Header("Settings")]
    public KeyCode menuKeyCode = KeyCode.Escape;
    public GameObject worldSettingsPannel;


    [Header("Content")]
    public TMP_Dropdown dropdown;
    public RectTransform dropdownCanvas;
    public Toggle stressMode;

    [Header("Chunk")]
    public TMP_InputField chunkWidth;
    public TMP_InputField chunkHeight;
    public TMP_InputField heightStep;
    [Header("World")]
    public TMP_InputField loadDistance;
    public TMP_InputField renderDistance;
    public TMP_InputField simulateDistance;
    [Header("Performance")]
    public TMP_InputField processedPerFrame;
    public TMP_InputField loadPerFrame;
    [Header("Player")]
    public TMP_InputField playerFlySpeed;

    public bool active = false;
    public void Start()
    {
        if(playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if(WorldSettings.Initialized)
        {
            LoadValues();
        }
        if(WorldManager == null)
            WorldManager = FindObjectOfType<WorldManager>();

        Cursor.visible = false;
    }
    public void Update()
    {
        if(Input.GetKeyUp(menuKeyCode))
        {
            worldSettingsPannel.SetActive(!worldSettingsPannel.activeSelf);
            playerController.controlling = !worldSettingsPannel.activeSelf;
            ChunkFactory.Instance.canChangeMaterial = !worldSettingsPannel.activeSelf;
            if (worldSettingsPannel.activeSelf)
            {
                dropdownCanvas.GetComponent<Canvas>().overrideSorting = false;
            }
        }
        if (!initialized)
        {
            LoadValues();
        }

        if (Input.GetKeyDown(statsKeyCode))
        {
            Pannel.SetActive(!Pannel.activeSelf);
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.visible = !Cursor.visible;
        }
        if (Input.GetKeyDown(screenWindowKeyCode))
        {
            Screen.fullScreen = !Screen.fullScreen;
        }
        mousePointer.position = Input.mousePosition + Vector3.back;
    }

    private void LoadValues()
    {
        dropdown.value = (int)WorldSettings.Mode;
        stressMode.isOn = WorldSettings.StressTest;

        chunkWidth.text = WorldSettings.ChunkWidth.ToString();
        chunkHeight.text = WorldSettings.ChunkHeight.ToString();
        heightStep.text = WorldSettings.HeightStep.ToString();

        loadDistance.text = WorldSettings.LoadDistance.ToString();
        renderDistance.text = WorldSettings.RenderDistance.ToString();
        simulateDistance.text = WorldSettings.SimulateDistance.ToString();

        processedPerFrame.text = WorldSettings.ChunksProcessedPerFrame.ToString();
        loadPerFrame.text = WorldSettings.ChunksToLoadPerFrame.ToString();

        playerFlySpeed.text = playerController.flyBaseSpeed.ToString();

        initialized = true;
    }

    public void Save()
    {
        if (!initialized)
        {
            LoadValues();
        }
        WorldSetting newSettings = ScriptableObject.CreateInstance<WorldSetting>();
        var changed = false;
        if (int.Parse(chunkWidth.text) < 8)
            chunkWidth.text = WorldSettings.ChunkWidth.ToString();
        newSettings.ChunkWidth = int.Parse(chunkWidth.text);
        if (int.Parse(chunkHeight.text) < 8)
            chunkHeight.text = WorldSettings.ChunkHeight.ToString();
        newSettings.ChunkHeight = int.Parse(chunkHeight.text);
        newSettings.HeightStep = int.Parse(heightStep.text);
        if (WorldSettings.ChunkWidth != newSettings.ChunkWidth
            || WorldSettings.ChunkHeight != newSettings.ChunkHeight
            || WorldSettings.HeightStep != newSettings.HeightStep)
            changed = true;

        newSettings.LoadDistance = int.Parse(loadDistance.text);
        newSettings.RenderDistance = int.Parse(renderDistance.text);
        newSettings.SimulateDistance = int.Parse(simulateDistance.text);

        newSettings.ChunksProcessedPerFrame = int.Parse(processedPerFrame.text);
        newSettings.ChunksToLoadPerFrame = int.Parse(loadPerFrame.text);
        newSettings.StressTest = stressMode.isOn;
        newSettings.Mode = (GenerationMode)dropdown.value;
        playerController.flyBaseSpeed = int.Parse(playerFlySpeed.text);
        WorldSettings.Initialized = false;
        WorldSettings.Initialize(newSettings, WorldSettings.Seed);
        Destroy(newSettings);

        if (changed)
        {
            ChunksManager.Instance.Dispose();
            ChunkFactory.Instance.Dispose();

            ChunksManager.Instance.Initialize();
            ChunkFactory.Instance.Initialize(WorldSettings.ChunksProcessedPerFrame);
            ChunksManager.Instance.UpdateChunks(WorldSettings.ChunkPositionFromPosition(playerController.transform.position));
        }
    }

    public void Quit()
    {
#if UNITY_EDITOR
        EditorUtility.UnloadUnusedAssetsImmediate();
        GC.Collect();
#endif
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBPLAYER
        Application.OpenURL(webplayerQuitURL);
#else
        Application.Quit();
#endif
    }
}
