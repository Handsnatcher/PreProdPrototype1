using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class UILine : MonoBehaviour
{
    public RectTransform pointA;
    public RectTransform pointB;
    public RectTransform lineRect;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Awake()
    {
        lineRect = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate()
    {
        if (pointA == null || pointB == null || lineRect == null)
        {
            return;
        }

        Vector3 a = pointA.position;
        Vector3 b = pointB.position;

        // get distance between the node
        Vector3 dir = b - a;
        float dist = dir.magnitude;

        // set the image width
        lineRect.position = a;
        lineRect.sizeDelta = new Vector2(dist, lineRect.sizeDelta.y);

        // rotate to the correct angle
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
