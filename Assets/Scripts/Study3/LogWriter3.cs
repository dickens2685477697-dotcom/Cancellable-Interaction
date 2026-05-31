using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class LogWriter3 : MonoBehaviour
{
    public BallSpawner ballSpawner;
    private string date, filePath, fileName, fileFullPath;
    private GameObject[] Targets, Obstacles;
    private FileStream fs;
    private StreamWriter sw;
    private bool started = false;
    public GameObject temp;
    private AcceStimulate acceStimulate;
    public float triggerTime = 0f;
    public float reactTime = 0f;
    public int reactFrequency = 0;
    public int obstacleReactFrequency = 0;
    public float cancelTime = 0f;
    public int cancelTimes = 0;
    public bool successfully_Canceled = false;
    public bool isReact = false;
    public bool handInBall = false;
    public bool isMineBall = false;
    
    void Start()
    {
        ballSpawner = GetComponent<BallSpawner>();
        date = DateTime.Now.ToString("yyyyMMdd-HH-mm-ss");
        filePath = Application.persistentDataPath + "/Logs/" + date + "/";
        fileName = date + "-" + "time" + ".csv";
        fileFullPath = filePath + fileName;
        Debug.Log(fileFullPath);
        //acce = gameObject.GetComponent<AcceStimulate>();
        InitialPath();
        temp = GameObject.Find("temp");
        acceStimulate = temp.GetComponent<AcceStimulate>();
        // Start writing once entering, no need to set invoked as true because we want it record every frame inside
        //acce.InEvent.AddListener(Write);
        // Increment the case time after getting the finger out
        //acce.AfterOutEvent.AddListener(increCase);
        Targets = GameObject.FindGameObjectsWithTag("Target");
        Obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (var target in Targets)
        {
            target.GetComponent<AcceStimulate>();
        }
        ballSpawner = GetComponent<BallSpawner>();
        ballSpawner.ExpStartEvent.AddListener(() =>
        {
            started = true;
        });
        ballSpawner.ExpEndEvent.AddListener(() =>
        {
            started = false;
        });
        ballSpawner.RoundOverEvent.AddListener(Write);
    }
    void FixedUpdate()
    {
        if (handInBall || isReact)
        {
            triggerTime += Time.deltaTime;
        }
        // if (isReact)
        // {
        //     reactTime += Time.deltaTime;
        // }
    }
    public string FormatMsg()
    {
        string ans = ballSpawner.currentCaseNumber + "," + ballSpawner.currentRoundID + "," + ballSpawner.intargetTimes
        + "," + ballSpawner.enable_preview + "," + ballSpawner.enableMineBall + "," + ballSpawner.enabledOb
        + "," + ballSpawner.errorTimes + "," + ballSpawner.errorInTimes + "," + ballSpawner.usedTime + "," + ballSpawner.triggerCondition + "," + ballSpawner.initialBall + "," + ballSpawner.rotationDirection + "," + ballSpawner.modeSelected + "," + ballSpawner.TargetEnd;
        ans += "," + reactTime;
        ans += "," + reactFrequency;
        ans += "," + obstacleReactFrequency;
        ans += "," + triggerTime;
        if (isMineBall)
        {
            ans += "," + (triggerTime - reactTime);
        }
        else
        {
            ans += "," + -1;
        }
        ans += "," + isMineBall;
        if (isMineBall)
        {
            ans += "," + successfully_Canceled;
        }
        else
        {
            ans += "," + "NA";
        }
        ans += "," + cancelTimes;
        ans += "," + ballSpawner.largeCirclePoint.x;
        ans += "," + ballSpawner.largeCirclePoint.y;
        ans += "," + ballSpawner.largeCirclePoint.z;
        foreach (var temp in ballSpawner.tatgetPos)
        {
            ans += "," + temp.x;
            ans += "," + temp.y;
            ans += "," + temp.z;
        }
        return ans;
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
            sw.WriteLine("Case,ID,inTargetTimes,enable_preview,enableMineBall,enabledOb,errorTimes,errorInTimes,usedTime,triggerCondition,start,direction(0 for ),modeSelected,ballIndex,reactTime,react_frequency,obstacle_react_frequency,triggerTime,cancelTime,isMineBall(need cancel),successfully_Canceled,cancelTimes,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ,targetX,targetY,targetZ");
            sw.Close();
            fs.Close();
        }
    }
    public void Write()
    {
        // New an fs and sw, close an fs and sw after writing
        // because a writer and file stream will lock the memory storage so that other gameobject cannot write
        if (started)
        {
            fs = new FileStream(fileFullPath, FileMode.Append, FileAccess.Write);
            sw = new StreamWriter(fs);
            string log = FormatMsg();
            sw.WriteLine(log);
            sw.Close();
            fs.Close();
            reactFrequency = 0;
            obstacleReactFrequency = 0;
        }

    }
    public string GetFilePath()
    {
        return filePath;
    }
    public string GetFileFullPath()
    {
        return fileFullPath;
    }
}
