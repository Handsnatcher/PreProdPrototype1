using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraScroll : MonoBehaviour
{
    public Image bgImage;
    private Vector3 imagePos;

    // Start is called before the first frame update
    void Start()
    {
        imagePos = Camera.main.ScreenToWorldPoint(bgImage.transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            transform.position += Vector3.up * scroll * 200.0f;
        }

        bgImage.transform.position = Camera.main.WorldToScreenPoint(imagePos);
    }
}
