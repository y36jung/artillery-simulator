using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour {
    
    public Camera cam;
    public Transform focusObject;
    public Transform SkyView;
    private Transform TankView;
    public bool birdView;

    private float transitionTime = 1.0f;
    private float sceneWidth;
    private Vector3 pressPoint;
    private Quaternion startRotation;
    private Vector3 startPosition;
    
    private Vector3 firstMouseInput;
    private bool manual;
    private float manualAngle;
    private float snapTime;

    public void Awake() {
        sceneWidth = Screen.width * 30;
    }

    public void Update() {
        if (birdView) {
            StartCoroutine(changeView());
        } else {
            followObject();
            rotateCamera();
        }
    }

    private IEnumerator changeView() {
        float time = 0f;
        Vector3 startingPos = cam.transform.position;
        Quaternion startingRot = cam.transform.rotation;
        Vector3 endingPos = SkyView.position;
        Quaternion endingRot = SkyView.rotation;
        while (time < 1.0f) {
            time += Time.deltaTime * (Time.timeScale / transitionTime);
            cam.transform.position = Vector3.Lerp(startingPos, endingPos, time);
            cam.transform.rotation = Quaternion.Lerp(startingRot, endingRot, time);
            yield return 0;
        }
    }

    private void rotateCamera() {
        if (Input.GetMouseButtonDown(0)) {
            manual = true;
            pressPoint = Input.mousePosition;
        } else if (Input.GetMouseButton(0)) {
            float currentDistanceBetweenMousePositions = ((Input.mousePosition - pressPoint).x / sceneWidth);
            float mutateVal = 0.05f - currentDistanceBetweenMousePositions / 9;
            if (currentDistanceBetweenMousePositions < 0) { 
                manualAngle += mutateVal;
            } else if (currentDistanceBetweenMousePositions > 0) {
                manualAngle -= mutateVal;
            }
            Vector3 displacement = new Vector3(Mathf.Cos(manualAngle), 0, Mathf.Sin(manualAngle)).normalized * 9 + focusObject.position;
            cam.transform.position = Vector3.Slerp(cam.transform.position, displacement, snapTime);
        } 
    }

    private void followObject() {
        if (Input.GetKeyDown(KeyCode.Y)) {
            manual = false;
        }
        snapTime = Time.deltaTime * 5;
        if (manual) {
            snapTime *= 4;
        }
        var rotationAngle = Quaternion.LookRotation(focusObject.position - (cam.transform.position + new Vector3(0, 1, 0)));
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, rotationAngle, snapTime);
        if (!manual) {
            float angle = -(Mathf.Deg2Rad * focusObject.eulerAngles.y) - Mathf.PI/2;
            Vector3 displacement = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)).normalized * 9 + 
                                    focusObject.position + new Vector3(0, 3, 0);
            cam.transform.position = Vector3.Slerp(cam.transform.position, displacement, snapTime);
        }
    }
}
