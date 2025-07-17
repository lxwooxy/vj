//
using UnityEngine;
using System.Collections.Generic;

public class VJEffectUnit : MonoBehaviour
{
    private static readonly PrimitiveType[] PrimitiveTypes = {
        PrimitiveType.Sphere, PrimitiveType.Cube, PrimitiveType.Capsule, PrimitiveType.Cylinder
    };

    private List<Material> materials;
    private Renderer rend;
    private GameObject meshHolder;
    private Vector3 originalPosition;
    private Vector3 originalScale;

    private float pulseSpeed, pulseScale, bounceSpeed, bounceHeight;
    private int currentShapeIndex = 0;
    private Material currentMaterial;

    public void Setup(List<Material> loadedMats, float pSpeed, float pScale, Vector3 rotSpeed, float bSpeed, float bHeight)
    {
        materials = loadedMats;
        pulseSpeed = pSpeed;
        pulseScale = pScale;
        bounceSpeed = bSpeed;
        bounceHeight = bHeight;

        originalPosition = transform.position;
        originalScale = Vector3.one;

        CreateShape(currentShapeIndex);
    }

    void Update()
    {
        if (meshHolder == null) return;

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
        meshHolder.transform.localScale = originalScale * pulse;

        float yOffset = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
        meshHolder.transform.position = originalPosition + new Vector3(0f, yOffset, 0f);

        meshHolder.transform.Rotate(new Vector3(15f, 30f, 45f) * Time.deltaTime);
    }

    public void SetMaterial(Material mat)
    {
        if (mat == null) return;

        currentMaterial = new Material(mat); // copy so it's not shared globally
        MakeTransparent(currentMaterial);

        if (rend != null)
        {
            rend.material = currentMaterial;
        }
    }

    public void MorphShape()
    {
        currentShapeIndex = (currentShapeIndex + 1) % PrimitiveTypes.Length;
        CreateShape(currentShapeIndex);
    }

    void CreateShape(int index)
    {
        if (meshHolder != null)
            Destroy(meshHolder);

        GameObject shape = GameObject.CreatePrimitive(PrimitiveTypes[index]);
        Destroy(shape.GetComponent<Collider>());

        shape.transform.SetParent(transform);
        shape.transform.localPosition = Vector3.zero;
        shape.transform.localRotation = Quaternion.identity;
        shape.transform.localScale = Vector3.one;

        meshHolder = shape;
        rend = shape.GetComponent<Renderer>();

        // Apply last known material after morph
        if (currentMaterial != null)
        {
            rend.material = currentMaterial;
        }
    }

    void MakeTransparent(Material mat)
    {
        if (!mat.HasProperty("_Color")) return;

        Color c = mat.color;
        c.a = 1f;
        mat.color = c;

        mat.SetFloat("_Mode", 2);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }
}
