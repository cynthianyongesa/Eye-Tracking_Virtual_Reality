# Eye-Tracking_Virtual_Reality
Log &amp; visualize eye-tracking data in a Unity project

# EyeTrackingLogger

## Overview
The `EyeTrackingLogger` script is designed to log eye-tracking data in a Unity project. It captures various eye and head tracking metrics, including gaze positions, rotations, and tracking states. The script also includes functionality to visualize the user's gaze in real-time, with options to display the target point, heatmap, and saccade/focus points. The data is logged to a CSV file for further analysis.

## Features
- **Eye and Head Tracking Data Logging**: Records gaze positions, rotations, tracking states, and more.
- **Real-Time Visualization**: Displays the user's gaze with options for target points, heatmaps, and saccade/focus points.
- **CSV Data Export**: Saves logged data to a CSV file for easy analysis.

## Setup
1. **Add the Script**: Attach the `EyeTrackingLogger` script to a GameObject in your Unity scene.
2. **Assign Input Actions**: Assign the appropriate input actions for eye and head tracking in the Unity Inspector.
3. **HeatMapper and SaccadeAndFocalPoint**: Ensure you have the `HeatMapper` and `SaccadeAndFocalPoint` scripts attached to GameObjects in your scene and assign them to the respective fields in the `EyeTrackingLogger` script.

## Usage
- **Toggle Data Recording**: Use the `recordEyeData` boolean to enable or disable data recording.
- **Visualization Options**: Use the `showTargetPoint`, `showHeatmap`, and `showSaccadeFocus` booleans to toggle the different visualization modes.

## Example
```csharp
[SerializeField] private bool recordEyeData = true;
[SerializeField] private bool showTargetPoint = false;
[SerializeField] private bool showHeatmap = false;
[SerializeField] private bool showSaccadeFocus = false;

public InputActionProperty leftEyeGazePositionAction;
public InputActionProperty rightEyeGazePositionAction;
public InputActionProperty centralEyeGazePositionAction;
public InputActionProperty centralEyeGazeRotationAction;
public InputActionProperty eyeGazeIsTrackedAction;
public InputActionProperty eyeGazeTrackingStateAction;
public InputActionProperty headPositionAction;
public InputActionProperty headRotationAction;

public HeatMapper heatMapper;
public SaccadeAndFocalPoint saccadeAndFocalPoint;
