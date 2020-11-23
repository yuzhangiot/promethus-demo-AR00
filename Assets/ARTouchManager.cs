using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARTouchManager : MonoBehaviour
{
    public GameObject arObj;
    public Camera arCamera;
    public ARRaycastManager aRRaycastManager;
    public bool planeInit;

    public List<ARRaycastHit> hits = new List<ARRaycastHit>();
    public RaycastHit raycastHit;

    // Update is called once per frame
    void Update()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            Ray ray = arCamera.ScreenPointToRay(touch.position);

            if (aRRaycastManager.Raycast(ray, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinBounds))
            {
                if (planeInit == false)
                {
                    planeInit = true;
                    arObj.SetActive(true);

                }

                Pose pose = hits[0].pose;
                arObj.transform.position = pose.position;
            }
        }
    }
}
