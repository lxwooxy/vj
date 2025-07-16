using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

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
    public float sharedMaterialChangeInterval = 2f;

    [Header("Beat Sync")]
    public float bpm = 60f;
    public bool markBeat = false;

    [Header("Camera")]
    public float spinSpeed = 10f;
    public float strobeSpinMultiplier = 5f;

    [Header("Background Flash")]
    public List<Color> backgroundColors = new List<Color> {
        Color.red, new Color(1f, 0.5f, 0f), Color.yellow, Color.green, Color.cyan, Color.blue, Color.magenta
    };
    public bool strobe = false;
    public float backgroundFlashSpeed = 1f;

    private List<Material> loadedMaterials = new List<Material>();
    private List<VJEffectUnit> effectUnits = new List<VJEffectUnit>();
    private Camera mainCam;

    private float materialTimer = 0f;
    private float lastBeatTime = 0f;
    private int currentColorIndex = 0;
    private Color targetBackgroundColor;
    private Color lastBackgroundColor;
    private bool lastStrobeState = false;
    private Material whiteMaterial;

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

        if (backgroundColors.Count > 0)
        {
            lastBackgroundColor = backgroundColors[0];
            targetBackgroundColor = backgroundColors[0];
        }

        whiteMaterial = new Material(Shader.Find("Standard"));
        whiteMaterial.color = Color.white;
    }

    void Update()
    {
        ApplyCameraSpin();
        UpdateMaterialSync();
        UpdateBackgroundColor();

        float beatInterval = 60f / bpm;

        if (markBeat)
        {
            lastBeatTime = Time.time;
            markBeat = false;
            TriggerBeat();
        }

        if (Time.time - lastBeatTime >= beatInterval)
        {
            lastBeatTime += beatInterval;
            TriggerBeat();
        }

        HandleStrobeEffect();
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

            if (loadedMaterials.Count > 0)
            {
                Material chosen = new Material(loadedMaterials[Random.Range(0, loadedMaterials.Count)]);
                effect.SetMaterial(chosen);
            }

            effectUnits.Add(effect);
        }
    }

    void TriggerBeat()
    {
        foreach (var unit in effectUnits)
            unit.MorphShape();

        if (backgroundColors.Count > 0 && !strobe)
        {
            lastBackgroundColor = Camera.main.backgroundColor;
            targetBackgroundColor = GetNextBackgroundColor();
        }
    }

    void UpdateMaterialSync()
    {
        if (strobe) return;

        materialTimer += Time.deltaTime;
        if (materialTimer >= sharedMaterialChangeInterval)
        {
            materialTimer = 0f;

            if (loadedMaterials.Count == 0) return;

            foreach (var unit in effectUnits)
            {
                Material chosen = new Material(loadedMaterials[Random.Range(0, loadedMaterials.Count)]);
                unit.SetMaterial(chosen);
            }
        }
    }

    void ApplyCameraSpin()
    {
        if (!mainCam) return;

        mainCam.transform.LookAt(Vector3.zero);
        float rollSpeed = spinSpeed * (strobe ? strobeSpinMultiplier : 1f);
        float roll = Time.time * rollSpeed;
        mainCam.transform.Rotate(Vector3.forward, roll);
    }

    void UpdateBackgroundColor()
    {
        if (backgroundColors.Count == 0) return;

        if (strobe)
        {
            Camera.main.backgroundColor = GetNextBackgroundColor();
        }
        else
        {
            Camera.main.backgroundColor = targetBackgroundColor;
        }
    }

    void HandleStrobeEffect()
    {
        if (strobe != lastStrobeState)
        {
            if (strobe)
            {
                foreach (var unit in effectUnits)
                    unit.SetMaterial(whiteMaterial);
            }
            else
            {
                foreach (var unit in effectUnits)
                {
                    Material chosen = new Material(loadedMaterials[Random.Range(0, loadedMaterials.Count)]);
                    unit.SetMaterial(chosen);
                }
            }
            lastStrobeState = strobe;
        }
    }

    Color GetNextBackgroundColor()
    {
        currentColorIndex = (currentColorIndex + 1) % backgroundColors.Count;
        return backgroundColors[currentColorIndex];
    }

    void LoadAllGeneratedMaterials()
    {
#if UNITY_EDITOR
        loadedMaterials.Clear();
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
