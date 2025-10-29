using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraVisionControl : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] Camera frontCam;    // Front view camera
    [SerializeField] Camera backCam;     // Back view camera

    private bool isFrontView = true;     // Track which camera is active

    void Start()
    {
        // Initialize cameras - FrontCam on, BackCam off
        if (frontCam != null && backCam != null)
        {
            frontCam.enabled = true;
            backCam.enabled = false;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SwitchCamera();
        }
    }

    void SwitchCamera()
    {
        // Toggle camera state
        isFrontView = !isFrontView;

        // Enable/disable cameras based on the new state
        frontCam.enabled = isFrontView;
        backCam.enabled = !isFrontView;
    }
}
