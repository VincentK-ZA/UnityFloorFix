using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR;
public class FloorFix : MonoBehaviour
{
    public UnityEvent FloorFixStarted;          //Event That Fires When the floor fix process starts, can be used to enable a progress indicator
    public UnityStringEvent FloorFixStatus;     //Fires an event with a string paramater, containing status of the floor fix process
    public UnityEvent FloorFixEnded;            //Event That Fires When the floor fix process ends, can be used to enable a progress indicator

    enum State { Inactive, Active }
    State CurState = State.Inactive;            //Current State of the floor fix process
    int CurMeasurementCount = 0;                //The current amount of steps the floor fix process has completed
    UInt32 ReferenceController;                 //Index of the lowest controller, used to adjust floor height
    double TempOffsetY;                         //Holder variable for offset calculation
    double TempRoll;                            //Holder variable for roll calculation

    double ControllerUpOffsetCorrection = 0.062f;   // Controller touchpad facing upwards
    double ControllerDownOffsetCorrection = 0.006f; //Controller placed touchpad down. Could add constant for oculus controller, and check for type

    //Call this method to begin the floor fix process
    public void StartFloorFix()
    {
        FloorFixStarted.Invoke();
        CurMeasurementCount = 0;
        CurState = State.Active;
    }

    private void Update()
    {
        if (CurState == State.Active)
        {
            if (OpenVR.System == null)
                return;
            TrackedDevicePose_t[] devicePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            OpenVR.System.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0.0f, devicePoses);

            UpdateTick(devicePoses);
        }
    }

    void UpdateTick(TrackedDevicePose_t[] devicePoses)
    {
        //If this is the first measurement, find and initialize the controller references
        if (CurMeasurementCount == 0)
        {
            // Get Controller ids for left/right hand
            var leftId = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.LeftHand);
            if (leftId == OpenVR.k_unTrackedDeviceIndexInvalid)
            {
                FloorFixStatus.Invoke("No left controller found.");
                FloorFixEnded.Invoke();
                CurState = State.Inactive;
                return;
            }
            var rightId = OpenVR.System.GetTrackedDeviceIndexForControllerRole(ETrackedControllerRole.RightHand);
            if (rightId == OpenVR.k_unTrackedDeviceIndexInvalid)
            {
                FloorFixStatus.Invoke("No right controller found.");
                FloorFixEnded.Invoke();
                CurState = State.Inactive;
                return;
            }
            // Get poses
            TrackedDevicePose_t leftPose = devicePoses[leftId];
            TrackedDevicePose_t rightPose = devicePoses[rightId];
            if (!leftPose.bPoseIsValid || !leftPose.bDeviceIsConnected || leftPose.eTrackingResult != ETrackingResult.Running_OK)
            {
                FloorFixStatus.Invoke("Left controller tracking problems.");
                FloorFixEnded.Invoke();
                CurState = State.Inactive;
                return;
            }
            else if (!rightPose.bPoseIsValid || !rightPose.bDeviceIsConnected || rightPose.eTrackingResult != ETrackingResult.Running_OK)
            {
                FloorFixStatus.Invoke("Right controller tracking problems.");
                FloorFixEnded.Invoke();
                CurState = State.Inactive;
                return;
            }
            else
            {
                // The controller with the lowest y-pos is the floor fix reference
                if (leftPose.mDeviceToAbsoluteTracking.m7 < rightPose.mDeviceToAbsoluteTracking.m7)
                {
                    ReferenceController = leftId;
                }
                else
                {
                    ReferenceController = rightId;
                }

                var m = devicePoses[ReferenceController].mDeviceToAbsoluteTracking;
                TempOffsetY = (double)m.m7;

                TempRoll = Math.Atan2(m.m4, m.m5);
                //Causes if to go to else statement on the next run
                CurMeasurementCount = 1;
            }

        }
        else //Not the first measurement. Start Calculating the offset
        {
            CurMeasurementCount++;
            var m = devicePoses[ReferenceController].mDeviceToAbsoluteTracking;

            double rollDiff = Math.Atan2(m.m4, m.m5) - TempRoll;
            if (rollDiff > Math.PI)
            {
                rollDiff -= 2.0 * Math.PI;
            }
            else if (rollDiff < -Math.PI)
            {
                rollDiff += 2.0 * Math.PI;
            }
            TempRoll += rollDiff / (double)CurMeasurementCount;
            if (TempRoll > Math.PI)
            {
                TempRoll -= 2.0 * Math.PI;
            }
            else if (TempRoll < -Math.PI)
            {
                TempRoll += 2.0 * Math.PI;
            }

            if (CurMeasurementCount >= 25)
            {
                float FloorOffset; //The Calculated Floor Offset
                if (Math.Abs(TempRoll) <= Math.PI / 2)
                {
                    FloorOffset = (float)(TempOffsetY - ControllerUpOffsetCorrection);
                }
                else
                {
                    FloorOffset = (float)(TempOffsetY - ControllerDownOffsetCorrection);
                }



                AddOffsetToCenter(FloorOffset);
                FloorFixStatus.Invoke("Fixed Floor: " + FloorOffset);
                FloorFixEnded.Invoke();
                CurState = State.Inactive;
            }
        }

    }

    void AddOffsetToCenter(float offset)
    {
        OpenVR.ChaperoneSetup.RevertWorkingCopy();
        HmdMatrix34_t curPos = new HmdMatrix34_t();
        OpenVR.ChaperoneSetup.GetWorkingStandingZeroPoseToRawTrackingPose(ref curPos);
        curPos.m3 += curPos.m1 * offset;
        curPos.m7 += curPos.m5 * offset;
        curPos.m11 += curPos.m9 * offset;
        OpenVR.ChaperoneSetup.SetWorkingStandingZeroPoseToRawTrackingPose(ref curPos);
        OpenVR.ChaperoneSetup.CommitWorkingCopy(EChaperoneConfigFile.Live);
    }

}
[Serializable]
public class UnityStringEvent : UnityEvent<string>
{ }
