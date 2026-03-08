using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 baseScale;
    public float scale = 2.0f;
    Node node;

    // Start is called before the first frame update
    void Start()
    {
        baseScale = transform.localScale;
        node = GetComponentInParent<Node>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (node != null && node.isUnlocked)
        {
            transform.localScale = baseScale * scale;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = baseScale;
    }
}
