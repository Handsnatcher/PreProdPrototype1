using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    public Color flashColor = Color.white;
    public float flashDuration = 0.15f;
    public int flashCount = 2;

    private List<Material> originalMaterials = new List<Material>();
    private List<Material> flashMaterials = new List<Material>();
    private List<Renderer> renderers = new List<Renderer>();
    private Coroutine flashCoroutine;

    void Start()
    {
        // Grab all renderers on this object and its children
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            renderers.Add(r);
            originalMaterials.Add(r.material);

            // Create a flash material copy
            Material flash = new Material(r.material);
            flash.color = flashColor;
            flashMaterials.Add(flash);
        }
    }

    public void Flash()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        float stepDuration = flashDuration / (flashCount * 2);

        for (int i = 0; i < flashCount; i++)
        {
            SetFlashMaterials(true);
            yield return new WaitForSeconds(stepDuration);
            SetFlashMaterials(false);
            yield return new WaitForSeconds(stepDuration);
        }

        flashCoroutine = null;
    }

    private void SetFlashMaterials(bool useFlash)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].material = useFlash ? flashMaterials[i] : originalMaterials[i];
        }
    }
}