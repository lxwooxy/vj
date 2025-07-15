using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class VJFibonacciTunnel : MonoBehaviour
{
    [Header("Spiral Settings")]
    public int objectCount = 800;
    public float spiralRise = 0.15f;
    public float cameraZ = 51.1f;
    public PrimitiveType initialShape = PrimitiveType.Sphere;
    public float spiralScale = 0.5f;

    [Header("Morph Settings")]
    public float pulseSpeed = 2f;
    public float pulseScale = 0.3f;
    public Vector3 rotationSpeed = new Vector3(15f, 30f, 45f);
    public float bounceSpeed = 1.5f;
    public float bounceHeight = 0.5f;

    [Header("Material Settings")]
    public float sharedMaterialChangeInterval = 2f;

    [Header("Beat Sync")]
    public float bpm = 120f;
    public bool tapBeat = false;

    [Header("Camera")]
    public float spinSpeed = 10f;
    public float backgroundFlashSpeed = 5f;

    private List<Material> loadedMaterials = new List<Material>();
    private List<VJEffectUnit> effectUnits = new List<VJEffectUnit>();
    private Camera mainCam;
    private float materialTimer = 0f;

    private double beatStartTime = 0;
    private double beatInterval => 60.0 / bpm;
    private int lastBeatIndex = -1;

    void Start()
    {
        LoadAllGeneratedMaterials();
        CreateSpiral();

        mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.Color;
            mainCam.transform.position = new Vector3(0f, 0f, cameraZ);
        }
    }

    void Update()
    {
        FlashBackground();
        ApplyCameraSpin();
        UpdateMaterialSync();
        HandleBeatSync();
    }

    void CreateSpiral()
    {
        float goldenAngle = 137.5f * Mathf.Deg2Rad;

        for (int i = 0; i < objectCount; i++)
        {
            float radius = Mathf.Sqrt(i) * spiralScale;
            float angle = i * goldenAngle;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            float z = i * spiralRise;

            Vector3 pos = new Vector3(x, y, z);
            GameObject obj = new GameObject($"SpiralObject_{i}");
            obj.transform.position = pos;
            obj.transform.SetParent(transform);

            var effect = obj.AddComponent<VJEffectUnit>();
            effect.Setup(loadedMaterials, pulseSpeed, pulseScale, rotationSpeed, bounceSpeed, bounceHeight);
            effectUnits.Add(effect);
        }
    }

    void HandleBeatSync()
    {
        if (tapBeat)
        {
            beatStartTime = Time.timeAsDouble;
            lastBeatIndex = -1;
            tapBeat = false;
        }

        double elapsed = Time.timeAsDouble - beatStartTime;
        int beatIndex = Mathf.FloorToInt((float)(elapsed / beatInterval));

        if (beatIndex > lastBeatIndex)
        {
            foreach (var unit in effectUnits)
                unit.MorphShape();

            lastBeatIndex = beatIndex;
        }
    }

    void UpdateMaterialSync()
    {
        materialTimer += Time.deltaTime;
        if (materialTimer >= sharedMaterialChangeInterval)
        {
            materialTimer = 0f;
            foreach (var unit in effectUnits)
            {
                int randomIndex = Random.Range(0, loadedMaterials.Count);
                unit.SetMaterial(new Material(loadedMaterials[randomIndex]));
            }
        }
    }

    void ApplyCameraSpin()
    {
        if (!mainCam) return;

        mainCam.transform.LookAt(Vector3.zero);
        float roll = Time.time * spinSpeed;
        mainCam.transform.Rotate(Vector3.forward, roll);
    }

    void FlashBackground()
    {
        float hue = Mathf.Repeat(Time.time * backgroundFlashSpeed, 1f);
        Camera.main.backgroundColor = Color.HSVToRGB(hue, 1f, 1f);
    }

    void LoadAllGeneratedMaterials()
    {
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/GeneratedMaterials" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) loadedMaterials.Add(mat);
        }
#endif
    }
}
