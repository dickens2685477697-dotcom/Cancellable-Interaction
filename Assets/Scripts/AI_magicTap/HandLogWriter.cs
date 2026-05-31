using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class HandLogWriter : MonoBehaviour
{
    public ExpController expController;
    public Set4AITarget set4AITarget;
    private string date, filePath, fileName, fileFullPath;
    private FileStream fs;
    private StreamWriter sw;
    private int caseTimes, roundTimes;
    public bool started = false;
    private MixedRealityPose pose;
    private List<Vector3> poses = new List<Vector3>();
    void Start()
    {
        expController = GetComponent<ExpController>();
        
        date = DateTime.Now.ToString("yyyyMMdd-HH-mm-ss");
        filePath = Application.persistentDataPath + "/Logs/" + date + "/";
        fileName = date + "-" + "Hand" + ".csv";
        fileFullPath = filePath + fileName;
        Debug.Log(fileFullPath);
        //acce = gameObject.GetComponent<AcceStimulate>();
        caseTimes = 1;
        roundTimes = 1;
        InitialPath();
        expController.caseStart.AddListener(() => {
            started = true;
            set4AITarget = GameObject.Find("targetBall").GetComponent<Set4AITarget>();
        });
        expController.caseOver.AddListener(() =>
        {
            started = false;
        });
        expController.caseOver.AddListener(increCase);
        expController.roundOver.AddListener(increRound);
    }
    private void FixedUpdate()
    {
        if (started)
        {
            for (int i = 1; i < 27; i++) {
                if(HandJointUtils.TryGetJointPose((TrackedHandJoint)i, Handedness.Any, out pose))
                {
                    print((TrackedHandJoint)i);
                    poses.Add(pose.Position);
                }
                
            }

            Write();

            
            poses.Clear();
        }
    }
    public string FormatMsg()
    {
        string result = string.Empty;
        result += roundTimes.ToString() + "," + caseTimes.ToString();
        foreach (Vector3 joint in poses)
        {
            result += "," + joint.x;
            result += "," + joint.y;
            result += "," + joint.z;
        }
        result += "," + expController.newTargetPos.x + "," + expController.newTargetPos.y + "," + expController.newTargetPos.z;
        //if (set4AITarget!=null)
        //{
        //    result += "," + set4AITarget.handInBall;
        //}
        //else
        //{
        //    result += "," + 3;
        //}
        return result;

    }
    public void InitialPath()
    {
        if (!Directory.Exists(filePath))
        {
            Directory.CreateDirectory(filePath);
        }
        if (!File.Exists(fileFullPath))
        {
            // Close the operator after creation
            fs = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write);
            sw = new StreamWriter(fs);
            sw.WriteLine("Round,Case,X1,Y1,Z1,X2,Y2,Z2,X3,Y3,Z3,X4,Y4,Z4,X5,Y5,Z5,X6,Y6,Z6,X7,Y7,Z7,X8,Y8,Z8,X9,Y9,Z9,X10,Y10,Z10,X11,Y11,Z11,X12,Y12,Z12,X13,Y13,Z13,X14,Y14,Z14,X15,Y15,Z15,X16,Y16,Z16,X17,Y17,Z17,X18,Y18,Z18,X19,Y19,Z19,X20,Y20,Z20,X21,Y21,Z21,X22,Y22,Z22,TargetX,TargetY,TargetZ");
            sw.Close();
            fs.Close();
        }
    }

    public void Write()
    {
        // New an fs and sw, close an fs and sw after writing
        // because a writer and file stream will lock the memory storage so that other gameobject cannot write
        fs = new FileStream(fileFullPath, FileMode.Append, FileAccess.Write);
        sw = new StreamWriter(fs);
        string log = FormatMsg();
        sw.WriteLine(log);
        sw.Close();
        fs.Close();
    }
    public void increCase()
    {

        caseTimes++;


    }
    public void increRound()
    {
        roundTimes++;


    }
}



