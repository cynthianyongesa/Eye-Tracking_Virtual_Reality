using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;

public class EyeTrackingLogger : MonoBehaviour
{
    [SerializeField] private bool recordEyeData = true;
    [SerializeField] private bool showTargetPoint = false; 
    [SerializeField] private bool showHeatmap= false;
    [SerializeField] private bool showSaccadeFocus = false;
 

    // Input actions for eye gaze and head tracking
    public InputActionProperty leftEyeGazePositionAction;
    public InputActionProperty rightEyeGazePositionAction;
    public InputActionProperty centralEyeGazePositionAction;
    public InputActionProperty centralEyeGazeRotationAction;
    public InputActionProperty eyeGazeIsTrackedAction;
    public InputActionProperty eyeGazeTrackingStateAction;
    public InputActionProperty headPositionAction;
    public InputActionProperty headRotationAction;

    public HeatMapper heatMapper;

    // Variables for logging data
    private List<string> logData = new List<string>();
    private string csvFilePath;

    // Variables for gaze tracking
    private Vector3 lastGazePosition;
    private Vector3 lastGazeDirection;
    private float saccadeThreshold = 0.01f;
    private float angleThreshold = 2f;
    private float fixationStartTime;
    private bool isFixating = false;

    public global::System.Boolean RecordEyeData { get => recordEyeData; set => recordEyeData = value; }
    public global::System.Boolean RecordEyeData1 { get => recordEyeData; set => recordEyeData = value; }

    void Start()
    {
        if (RecordEyeData)
        {
        // Initialize CSV file path with date and time
        string dateTime = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        csvFilePath = Application.dataPath + $"/Data/EyeTrackingData_{dateTime}.csv";

        // Add header to CSV file
        logData.Add("Timestamp,LeftEyeGazePositionX,LeftEyeGazePositionY,LeftEyeGazePositionZ,RightEyeGazePositionX,RightEyeGazePositionY,RightEyeGazePositionZ,CentralEyeGazePositionX,CentralEyeGazePositionY,CentralEyeRotationYaw,CentralEyeRotationPitch,CentralEyeRotationRoll,IsTracked,TrackingState,Blink,Saccade,SaccadeSpeed,FixationDuration,LookedAtObject,HeadPositionX,HeadPositionY,HeadPositionZ,HeadRotationX,HeadRotationY,HeadRotationZ,HeadRotationW");
        Debug.Log("/------------Eye Tracking Initialized------------/");
        }
        else
        {
            Debug.Log("/------------Eye Tracking Not Initialized------------/");
        }
    }

    void Update() 
    {
        // The eye tracker updates at 120hz and update is happening at roughly 90hz.
        // This is fine for tracking headset position, but could cause there to be
        // a loss of 25% of the eye tracking data. We might need to try to update 
        // at a higher frequency somehow... Maybe with SRanipal and a callback???

        if (!RecordEyeData) return; // Doesn't collect Data if eye tracking is disabled.

        // Get and log left eye gaze position
        Vector3 leftEyeGazePosition = leftEyeGazePositionAction.action.ReadValue<Vector3>();

        // Get and log right eye gaze position
        Vector3 rightEyeGazePosition = rightEyeGazePositionAction.action.ReadValue<Vector3>();

        // Get and log central eye gaze position and rotation
        Vector3 centralEyeGazePosition = centralEyeGazePositionAction.action.ReadValue<Vector3>();
        Quaternion centralEyeGazeRotation = centralEyeGazeRotationAction.action.ReadValue<Quaternion>();
        Vector3 centralEyeEulerRotation = centralEyeGazeRotation.eulerAngles;
        float centralYaw = centralEyeEulerRotation.y;
        float centralPitch = centralEyeEulerRotation.x;
        float centralRoll = centralEyeEulerRotation.z;

        // Get and log if eye gaze is tracked
        bool isTracked = eyeGazeIsTrackedAction.action.ReadValue<float>() > 0;

        // Get and log eye gaze tracking state
        int trackingState = eyeGazeTrackingStateAction.action.ReadValue<int>();

        // Blink Detection based on central eye gaze position being (0,0,0)
        bool isBlink = centralEyeGazePosition == Vector3.zero;

        // Saccade Detection based on rapid movements in a certain direction
        Vector3 gazeDirection = centralEyeGazePosition - lastGazePosition;
        float gazeMagnitude = gazeDirection.magnitude;
        float gazeAngle = Vector3.Angle(gazeDirection, lastGazeDirection);

        bool isSaccade = gazeMagnitude > saccadeThreshold && gazeAngle > angleThreshold;
        float saccadeSpeed = gazeMagnitude / Time.deltaTime; // Calculate saccade speed

        if (isSaccade)
        {
            isFixating = false;  // End fixation when saccade detected
        }

        // Fixation Detection: Fixation starts when no saccade is detected and continues until a saccade occurs
        float fixationDuration = 0;
        if (!isSaccade && !isBlink && isTracked)
        {
            if (!isFixating)
            {
                isFixating = true;
                fixationStartTime = Time.time; // Start fixation time
            }
            fixationDuration = Time.time - fixationStartTime;  // Accumulate fixation time
        }
        else
        {
            isFixating = false;  // Reset fixation
        }

        // Perform raycast to detect the object being looked at
        string lookedAtObject = PerformRaycast(centralEyeGazePosition, centralEyeGazeRotation);

        // Get and log head position and rotation
        Vector3 headPosition = headPositionAction.action.ReadValue<Vector3>();
        Quaternion headRotation = headRotationAction.action.ReadValue<Quaternion>();

        // Add data to CSV
        string timestamp = Time.time.ToString();
        string csvLine = $"{timestamp},{leftEyeGazePosition.x},{leftEyeGazePosition.y},{leftEyeGazePosition.z}," +
                         $"{rightEyeGazePosition.x},{rightEyeGazePosition.y},{rightEyeGazePosition.z}," +
                         $"{centralEyeGazePosition.x},{centralEyeGazePosition.y},{centralYaw},{centralPitch},{centralRoll}," +
                         $"{isTracked},{trackingState},{isBlink},{isSaccade},{saccadeSpeed},{fixationDuration},{lookedAtObject}," +
                         $"{headPosition.x},{headPosition.y},{headPosition.z}," +
                         $"{headRotation.x},{headRotation.y},{headRotation.z},{headRotation.w}";
        logData.Add(csvLine);

        // Update last gaze position and direction for next frame
        lastGazePosition = centralEyeGazePosition;
        lastGazeDirection = gazeDirection;
    }

    void OnApplicationQuit()
    {
        if (RecordEyeData)
        {
            // Write all collected data to CSV file
            File.WriteAllLines(csvFilePath, logData);
            // Log file path to the console
            Debug.Log("Eye tracking data saved to: " + csvFilePath);
        }
        else
        {
            Debug.Log("/------------Eye Tracking Data Not Recorded------------/");
        }
    }

    // Perform raycast to detect the object being looked at
    private string PerformRaycast(Vector3 position, Quaternion rotation)
    {
        Ray ray = new Ray(position, rotation * Vector3.forward);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.collider.gameObject.name;
        }
        return "None";
    }
}
