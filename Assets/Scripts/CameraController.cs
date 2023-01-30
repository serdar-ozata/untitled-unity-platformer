using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    public float lookingLimit = 5f;

    private CinemachineVirtualCamera _virtualCamera;
    private CinemachineFramingTransposer _transposer;
    private const float LookArea = 0.45f;
    public float cameraVerticalPressDuration;
    private float cameraVerticalTimer;
    private bool cameraButtonsArePressed;

    // Start is called before the first frame update
    void Start()
    {
        cameraButtonsArePressed = false;
        cameraVerticalTimer = cameraVerticalPressDuration;
        _virtualCamera = GetComponent<CinemachineVirtualCamera>();
        if (_virtualCamera == null)
            Debug.Log(" default VC is not found");
        else
        {
            _transposer = _virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (_transposer == null)
            {
                Debug.Log("undefined transposer, check the body");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        Look();
    }

    private void Look()
    {
        //Vector3 mousePos = Input.mousePosition;
        //float xOffset = (mousePos.x / Screen.width) - 0.5f;
        //float yOffset = (mousePos.y / Screen.height) - 0.5f;
        float yOffset = Input.GetAxisRaw("VerticalAlt");
        if (Mathf.Abs(yOffset) > Mathf.Epsilon)
        {
            // better than Input.GetButton("Vertical")
            // it is pressed to either
            cameraButtonsArePressed = true;
        } // if not pressed and was being pressed before
        else if (cameraButtonsArePressed)
        {
            cameraButtonsArePressed = false;
            cameraVerticalTimer = cameraVerticalPressDuration;
        }

        // if up or down is pressed and the timer did not reach 0, decrease timer
        if (cameraButtonsArePressed && cameraVerticalTimer > 0)
        {
            cameraVerticalTimer -= Time.deltaTime;
        }
        // if the timer did reach below zero;
        else if (cameraVerticalTimer < 0 && cameraButtonsArePressed)
        {
            _transposer.m_TrackedObjectOffset = new Vector3(0f, yOffset, 0f) * lookingLimit;
        }
        else if (!cameraButtonsArePressed)
        {
            _transposer.m_TrackedObjectOffset = new Vector3(0f, yOffset, 0f) * lookingLimit;
        }
    }
}