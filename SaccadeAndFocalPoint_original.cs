using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaccadeAndFocalPoint : MonoBehaviour
{
    public Vector3 gazeOrigin;
    public Vector3 gazeDirection;
    public List<Vector3> gazeDirectionList; //holds last N gaze points
    public List<Vector3> gazeOriginList; //holds last N gazeOrigin points (for computing angular velocity)
    public List<Transform> lastTwoHits;
    public GameObject HMD;
    public bool lastFrameWasSaccade = false;
    public Transform lastPointHit;
    public LineRenderer lineRendererPrefab;
    public Vector3 lastPositionHit;


    private void Start()
    {
        gazeDirectionList = new List<Vector3>();
        lastTwoHits = new List<Transform>();
    }

    private void Update()
    {
        //Process current gaze point 
        RaycastHit gazePoint = ProcessRay(GazeController.gazeOrigin, GazeController.gazeDirection);
    }

    //process the gaze to scene ray and surrounding coloration
    RaycastHit ProcessRay(Vector3 gazeOrigin, Vector3 gazeDirection)
    {

        ProcessNewGazeDirection(gazeDirectionList, 50, gazeDirection);

        RaycastHit hit;
        if (Physics.Raycast(gazeOrigin, gazeDirection, out hit))
        {
            if (hit.collider.name != "FocalPointSimple(Clone)" && IsFocusPoint(gazeDirectionList, 10f, 1))
            {
                //check if new focal point
                if(lastPointHit != null && hit.collider.transform.name != lastPointHit.name)
                {   
                    //disable last hit point collider
                    lastPointHit.GetComponent<Collider>().enabled = false;
                }
                GameObject temp = (GameObject)Instantiate(Resources.Load("FocalPointSimple"));
                temp.transform.position = hit.point;
                Vector3 moveDirection = (gazeOrigin - temp.transform.position).normalized;

                // Calculate the distance to move this frame based on moveSpeed
                

                // Move the object towards the target position
                temp.transform.Translate(moveDirection * .1f);

                lastPointHit = temp.transform;
            }
            else if (lastPointHit != null && hit.collider.transform.name == lastPointHit.name)
            {
                float originalColliderRadius = hit.transform.GetComponent<SphereCollider>().radius;
                // Adjust the collider's size without affecting the object's transform scale
                
                float scaleFactor = .001f;
                hit.transform.localScale = new Vector3(
                    hit.transform.localScale.x + scaleFactor,
                    hit.transform.localScale.y + scaleFactor,
                    hit.transform.localScale.z + scaleFactor);

                GameObject temp = (GameObject)Instantiate(Resources.Load("FocalPointSimple"));
                float radiusScale = temp.transform.localScale.x / hit.transform.localScale.x;
                hit.transform.GetComponent<SphereCollider>().radius = 
                    temp.transform.GetComponent<SphereCollider>().radius * radiusScale;
                Destroy(temp);
                   
            }
            else if(lastPointHit != null)
            {
                CheckForLine(lastTwoHits, lastPointHit);
                lastPointHit.GetComponent<Collider>().enabled = false;
                lastPointHit = null;
            }
        }
        return hit;
    }


    public void ProcessRayPosition(Vector3 inputPosition1, Vector3 inputPosition2)
    {
            GameObject temp = (GameObject)Instantiate(Resources.Load("FocalPointSimple"));
            temp.transform.position = inputPosition1;
         
            // Instantiate a new LineRenderer from the prefab
            LineRenderer lineRenderer = (LineRenderer)Instantiate(lineRendererPrefab, Vector3.zero, Quaternion.identity);

            // Set the positions to connect obj1 and obj2
            Vector3[] linePositions = new Vector3[2];
            linePositions[0] = inputPosition1;
            linePositions[1] = inputPosition2;

            // Assign the positions to the LineRenderer
            lineRenderer.positionCount = 2;
            lineRenderer.SetPositions(linePositions);
        
    }

    //determines if the current new gaze point is part of a saccade or a focus point
    //based on all values in the list (i.e. based on gaze history)
    // @gazeInputList - the list of gaze directions 
    // @velocityThreshold - threshold velocity for focal point in degrees 
    // @frameCount - number of frames to average over for threshold
    bool IsFocusPoint(List<Vector3> gazeInputList, float velocityThreshold, int frameCount)
    {
        if (gazeInputList.Count < frameCount)
        {
            // Not enough data for calculation
            return false;
        }


        // Calculate angular velocities
        List<float> angularVelocities = new List<float>();
        for (int i = 1; i < gazeInputList.Count; i++)
        {
            Vector3 previousGazeDirection = gazeInputList[i - 1];
            Vector3 currentGazeDirection = gazeInputList[i];

            // Calculate angular velocity based on the change in gaze direction
            float angularVelocity = Vector3.Angle(currentGazeDirection, previousGazeDirection) / Time.deltaTime;
            angularVelocities.Add(angularVelocity);

            // Keep only the last 'frameCount' angular velocities
            if (angularVelocities.Count > frameCount)
            {
                angularVelocities.RemoveAt(0);
            }
        }

        float totalAngularVelocity = 0f;
        int framesAveraged = 0;

        // Calculate the total angular velocity over the last 'framesToAverage' frames
        for (int i = angularVelocities.Count - 1; i >= 0 && framesAveraged < frameCount; i--)
        {
            totalAngularVelocity += angularVelocities[i];
            framesAveraged++;
        }

        // Calculate the average angular velocity
        float averageAngularVelocity = totalAngularVelocity / framesAveraged;

        // Check if the average angular velocity exceeds the threshold
        if (averageAngularVelocity < velocityThreshold)
        {
            // Focus detected (stationary)
            return true;
        }

        // If the average angular velocity is below the threshold, it's considered a fixation (focus point)
        return false;
    }



    // adds new gaze points to the input list and ensures it doesn't exceed a maximum length
    // @inputList - the list of gaze points 
    // @listMax - an integer representing the max size of the list
    void ProcessNewGazeDirection(List<Vector3> gazeInputList, int listMax, Vector3 newGazeDirection)
    {
        // Add the new gaze point to the list
        gazeInputList.Add(newGazeDirection);

        // Check if the list size exceeds the maximum
        if (gazeInputList.Count > listMax)
        {
            // Remove the oldest gaze point(s) to maintain the maximum length
            int removeCount = gazeInputList.Count - listMax;
            gazeInputList.RemoveRange(0, removeCount);
        }
    }


    // Method to draw a line between two objects
    void DrawLineBetweenObjects(Transform obj1, Transform obj2, Object lineRendererPrefab)
    {
        // Instantiate a new LineRenderer from the prefab
        LineRenderer lineRenderer = (LineRenderer)Instantiate(lineRendererPrefab, Vector3.zero, Quaternion.identity);

        // Set the positions to connect obj1 and obj2
        Vector3[] linePositions = new Vector3[2];
        linePositions[0] = obj1.position;
        linePositions[1] = obj2.position;

        // Assign the positions to the LineRenderer
        lineRenderer.positionCount = 2;
        lineRenderer.SetPositions(linePositions);
    }

    void CheckForLine(List<Transform> objectList, Transform nextHit)
    {

        objectList.Add(nextHit);

        // Check if the list size exceeds the maximum
        if (objectList.Count > 2)
        {
            // Remove the oldest gaze point(s) to maintain the maximum length
            int removeCount = objectList.Count - 2;
            objectList.RemoveRange(0, removeCount);
        }

        if (objectList.Count > 1 &&
            objectList[0].transform != null && 
            objectList[1].transform != null && 
            objectList[0].transform != objectList[1].transform)
        {
            DrawLineBetweenObjects(objectList[0].transform, objectList[1].transform, lineRendererPrefab);
        }

    }

}