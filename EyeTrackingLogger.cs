using UnityEngine;
using UnityEngine.InputSystem;
using VIVE.OpenXR;
using VIVE.OpenXR.EyeTracker;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class EyeTrackingLogger : MonoBehaviour
{
    public bool debugHitPosition = false;
    [SerializeField] private bool useExperimentalRayCast;
    [SerializeField] private GameObject playerRig;
    private GameObject currentlyHighlightedPortal;
    public InputActionProperty centralEyeGazePositionAction;
    public InputActionProperty centralEyeGazeRotationAction;
    public GameObject gazePoint;
    private string csvFilePath;
    private List<string> logData = new List<string>();

    private Vector3 previousGazeLeft;
    private Vector3 previousGazeRight;
    private float saccadeThreshold = 0.05f;  // Adjust based on speed of movement
    private float fixationThreshold = 0.01f;

    void Start()
    {
        if (gazePoint == null) 
        {
            gazePoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gazePoint.transform.localScale = new Vector3 (0.1f,0.1f,0.1f);
            gazePoint.GetComponent<MeshRenderer>().material.color = Color.green;
            gazePoint.GetComponent<SphereCollider>().enabled = false;
        }
        string dateTime = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        csvFilePath = Application.dataPath + $"/Data/EyeTrackingData_{dateTime}.csv";

        Debug.Log($"Eye-tracking data will be saved to: {csvFilePath}");

        // Create CSV headers
        logData.Add("Timestamp,HeadPosX,HeadPosY,HeadPosZ,HeadRotX,HeadRotY,HeadRotZ," +
                    "LeftGazePosX,LeftGazePosY,LeftGazePosZ,LeftGazeRotX,LeftGazeRotY,LeftGazeRotZ," +
                    "RightGazePosX,RightGazePosY,RightGazePosZ,RightGazeRotX,RightGazeRotY,RightGazeRotZ," +
                    "CentralEyeGazePositionX,CentralEyeGazePositionY,CentralEyeGazePositionZ,CentralEyeRotationYaw,CentralEyeRotationPitch,CentralEyeRotationRoll," +
                    "LeftPupilDiameter,RightPupilDiameter," +
                    "LeftEyeOpenness,LeftEyeSqueeze,LeftEyeWide," +
                    "RightEyeOpenness,RightEyeSqueeze,RightEyeWide," +
                    "LookedObject,BlinkLeft,BlinkRight,SaccadeLeft,SaccadeRight");
    }

    void Update()
    {
        // Get head tracking data
        Vector3 headPosition = Camera.main.transform.position;
        Vector3 headRotation = Camera.main.transform.eulerAngles;

        // Get gaze data for both eyes
        XR_HTC_eye_tracker.Interop.GetEyeGazeData(out XrSingleEyeGazeDataHTC[] gazeData);
        XrSingleEyeGazeDataHTC leftEyeGaze = gazeData[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
        XrSingleEyeGazeDataHTC rightEyeGaze = gazeData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];

        Vector3 leftGazePosition = leftEyeGaze.gazePose.position.ToUnityVector();
        Quaternion leftGazeRotation = leftEyeGaze.gazePose.orientation.ToUnityQuaternion();

        Vector3 rightGazePosition = rightEyeGaze.gazePose.position.ToUnityVector();
        Quaternion rightGazeRotation = rightEyeGaze.gazePose.orientation.ToUnityQuaternion();

        Vector3 centralEyeGazePosition = centralEyeGazePositionAction.action.ReadValue<Vector3>();
        Quaternion centralEyeGazeRotation = centralEyeGazeRotationAction.action.ReadValue<Quaternion>();
        Vector3 centralEyeEulerRotation = centralEyeGazeRotation.eulerAngles;
        float centralYaw = centralEyeEulerRotation.y;
        float centralPitch = centralEyeEulerRotation.x;
        float centralRoll = centralEyeEulerRotation.z;
        

        // Get pupil data
        XR_HTC_eye_tracker.Interop.GetEyePupilData(out XrSingleEyePupilDataHTC[] pupilData);
        float leftPupilDiameter = pupilData[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC].pupilDiameter;
        float rightPupilDiameter = pupilData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC].pupilDiameter;

        // Get geometric data (eye openness, squeeze, wideness)
        XR_HTC_eye_tracker.Interop.GetEyeGeometricData(out XrSingleEyeGeometricDataHTC[] geometricData);
        XrSingleEyeGeometricDataHTC leftEyeGeometric = geometricData[(int)XrEyePositionHTC.XR_EYE_POSITION_LEFT_HTC];
        XrSingleEyeGeometricDataHTC rightEyeGeometric = geometricData[(int)XrEyePositionHTC.XR_EYE_POSITION_RIGHT_HTC];

        float leftEyeOpenness = leftEyeGeometric.eyeOpenness;
        float leftEyeSqueeze = leftEyeGeometric.eyeSqueeze;
        float leftEyeWide = leftEyeGeometric.eyeWide;

        float rightEyeOpenness = rightEyeGeometric.eyeOpenness;
        float rightEyeSqueeze = rightEyeGeometric.eyeSqueeze;
        float rightEyeWide = rightEyeGeometric.eyeWide;

        // Detect blink
        bool leftBlink = IsBlinking(leftEyeOpenness);
        bool rightBlink = IsBlinking(rightEyeOpenness);
        if (leftBlink || rightBlink) Debug.Log("Blink detected!");

        // Detect saccades
        bool leftSaccade = IsSaccade(previousGazeLeft, leftGazePosition, saccadeThreshold);
        bool rightSaccade = IsSaccade(previousGazeRight, rightGazePosition, saccadeThreshold);
        if (leftSaccade) Debug.Log("Left eye saccade detected!");
        if (rightSaccade) Debug.Log("Right eye saccade detected!");

        // Update previous gaze positions
        previousGazeLeft = leftGazePosition;
        previousGazeRight = rightGazePosition;

        // Perform raycast to detect the object being looked at
        GameObject lookedObject;

        if (useExperimentalRayCast)
        {
            lookedObject = PerformRaycastExperiment(leftGazePosition, leftGazeRotation); //Is there a center? Nope... maybe make one? Slerp quaternions at 0.5 and average eye positions?
            PerformRaycastExperiment(rightGazePosition, rightGazeRotation);
        }
        else
        {
            lookedObject = PerformRaycast(leftGazePosition, leftGazeRotation); //Is there a center? Nope... maybe make one? Slerp quaternions at 0.5 and average eye positions?
            PerformRaycast(rightGazePosition, rightGazeRotation);
        }
        string objectName = lookedObject != null ? lookedObject.name : "None";

        // Save data to log list
        logData.Add($"{Time.time},{headPosition.x},{headPosition.y},{headPosition.z}," +
                    $"{headRotation.x},{headRotation.y},{headRotation.z}," +
                    $"{leftGazePosition.x},{leftGazePosition.y},{leftGazePosition.z}," +
                    $"{leftGazeRotation.eulerAngles.x},{leftGazeRotation.eulerAngles.y},{leftGazeRotation.eulerAngles.z}," +
                    $"{rightGazePosition.x},{rightGazePosition.y},{rightGazePosition.z}," +
                    $"{rightGazeRotation.eulerAngles.x},{rightGazeRotation.eulerAngles.y},{rightGazeRotation.eulerAngles.z}," +
                    $"{centralEyeGazePosition.x},{centralEyeGazePosition.y},{centralEyeGazePosition.z},{centralYaw},{centralPitch},{centralRoll}," +
                    $"{leftPupilDiameter},{rightPupilDiameter}," +
                    $"{leftEyeOpenness},{leftEyeSqueeze},{leftEyeWide}," +
                    $"{rightEyeOpenness},{rightEyeSqueeze},{rightEyeWide}," +
                    $"{objectName},{leftBlink},{rightBlink},{leftSaccade},{rightSaccade}");
    }

    // Detect if an eye blinked
    private bool IsBlinking(float eyeOpenness)
    {
        return eyeOpenness < 0.1f; // Threshold for blink detection
    }

    // Detect saccades
    private bool IsSaccade(Vector3 prevGaze, Vector3 currentGaze, float threshold)
    {
        return Vector3.Distance(prevGaze, currentGaze) > threshold;
    }

    // Refined raycasting using head direction
    private GameObject PerformRaycast(Vector3 eyePosition, Quaternion eyeRotationLocal)
    {
        Quaternion headRotation = Camera.main.transform.rotation;
        Quaternion playerRotation = playerRig.transform.rotation;
        Vector3 headDirection = Camera.main.transform.forward;

        Vector3 adjustedEyePosition = this.transform.TransformPoint(eyePosition);
        Vector3 yOffset = new Vector3 (0, 1.36144f, 0);
        adjustedEyePosition = adjustedEyePosition - yOffset;


        Vector3 adjustedGazeDirection = Camera.main.transform.InverseTransformDirection(eyeRotationLocal * headDirection);
        //Do we even need to adjust the raycast anymore with the new API, that could?

        Ray ray = new Ray(adjustedEyePosition, adjustedGazeDirection);//adjustedGazeDirection);
        //Debug.DrawRay(eyePosition, adjustedGazeDirection * 5f, Color.blue);
        Debug.DrawRay(adjustedEyePosition, adjustedGazeDirection, Color.green);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log($"Refined Gaze Target: {hit.collider.gameObject.name}");

            if (debugHitPosition) 
            {
                gazePoint.SetActive(true);
                gazePoint.transform.position= hit.point;
            }
            return hit.collider.gameObject;
        }
        return null;
    }

    private GameObject PerformRaycastExperiment(Vector3 eyePosition, Quaternion eyeRotationLocal)
    {
        Quaternion playerRotation = playerRig.transform.rotation;

        Vector3 direction = Vector3.forward;
        Vector3 correctedRotation = (eyeRotationLocal * playerRotation) * direction;
        
        Ray ray = new Ray( Camera.main.transform.position + eyePosition, correctedRotation);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
           //Debug.Log($"Refined Gaze Target: {hit.collider.gameObject.name}");

            if (debugHitPosition) 
            {
                gazePoint.SetActive(true);
                gazePoint.transform.position= hit.point;
            }

            if (currentlyHighlightedPortal !=null)
            {
                if (hit.collider.gameObject.name != currentlyHighlightedPortal.name)
                {
                    UnHighlightPortal(currentlyHighlightedPortal);
                }
            }

            if (hit.collider.gameObject.name.Contains("Portal"))
            {
                HighlightPortal(hit.collider.gameObject);
            }

            return hit.collider.gameObject;
        }
        return null;
    }

    private void HighlightPortal(GameObject PortalToHighlight)
    {
        PortalToHighlight.GetComponent<Renderer>().material.SetColor("_Color", Color.yellow); 
        currentlyHighlightedPortal = PortalToHighlight;
    }

    private void UnHighlightPortal(GameObject PortalToUnHighlight)
    {
        currentlyHighlightedPortal.GetComponent<Renderer>().material.SetColor("_Color", Color.white); 
        currentlyHighlightedPortal = null;
    }

    // Save data to CSV file on application quit
    void OnApplicationQuit()
    {
        File.WriteAllLines(csvFilePath, logData);
        Debug.Log($"Eye tracking data successfully saved to: {csvFilePath}");
    }
}
