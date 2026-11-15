using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartBallBehavior : MonoBehaviour
{
    AcceStimulate acce;
    S1Clock s1clock;
    bool startCorrectly = false;
    public Material shallowBall;
    public Material oriBall;
    // Start is called before the first frame update
    void Start()
    {
        acce = GetComponent<AcceStimulate>();
        s1clock = GameObject.Find("Clock").GetComponent<S1Clock>();
        acce.MotionTrackingInterrupted += OnTrackingInterrupted;
        acce.InEvent.AddListener(() =>
        {
            s1clock.TapBlueBall();
            startCorrectly = true;
            gameObject.SetActive(true);
            gameObject.GetComponent<MeshRenderer>().material = shallowBall;
        });
        acce.AfterOutEvent.AddListener(() =>
        {
            gameObject.SetActive(false);
            s1clock.TapBlueBall();
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool GetState()
    {
        return startCorrectly;
    }
    public void TureUnstarted()
    {
        startCorrectly = false;
        gameObject.GetComponent<MeshRenderer>().material = oriBall;
        gameObject.SetActive(true);
    }

    private void OnTrackingInterrupted()
    {
        if (!acce.UsesMrtkJointUpdates) return;
        TureUnstarted();
        if (s1clock != null)
        {
            s1clock.StopAllCoroutines();
            s1clock.CountdownTime = s1clock.CT;
            s1clock.ResetStates();
        }
    }

    private void OnDestroy()
    {
        if (acce != null) acce.MotionTrackingInterrupted -= OnTrackingInterrupted;
    }
}
