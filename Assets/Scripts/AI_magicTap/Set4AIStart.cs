using Oculus.Platform.Samples.VrHoops;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Set4AIStart : MonoBehaviour
{
    public AcceStimulate Acce;
    public GameObject ball, ball2;
    public S2Player Player, Player2;
    public BallSpawner ballSpawner;
    public int modeSelected;
    public List<GameObject> balls;
    public bool inBall = false;
    public bool canceled = false;
    public UnityEvent startClicked;
    public Material material, oriMaterial;
    private bool handInBall = false;
    public void Start()
    {
        Acce = GetComponent<AcceStimulate>();
        switch (modeSelected)
        {
            case 0:
                SetFastMode();
                break;
            case 1:
                Acce.Delay = true;
                Acce.WaitTime = 0.25F;
                SetHesMode();
                break;
            case 2:
                Acce.Acce = true;
                SetHesMode();
                //print("Hes");
                break;
            case 3:
                //¿ì»÷
                //SetFastMode();
                Acce.Fast = true;
                Set010();
                break;
        }
        Acce.InEvent.AddListener(() =>
        {
            if (!handInBall)
            {
                handInBall = true;
                gameObject.GetComponent<MeshRenderer>().material = material;
            }

        });
        Acce.AfterOutEvent.AddListener(() =>
        {
            handInBall = false;
            gameObject.GetComponent<MeshRenderer>().material = oriMaterial;
        });
    }
    public void SetHesMode()
    {
        //Acce.BeforeHesEvent.AddListener(Acce.SetInvoked);
        ////Acce.HesEvent.AddListener(Acce.OutMaterial);
        //Acce.AfterOutEvent.AddListener(Acce.UnInvoked);


        ////Acce.InEvent.AddListener(Player.TurnShallow);
        //Acce.HesEvent.AddListener(() =>
        //{
        //    gameObject.GetComponent<MeshRenderer>().material = oriMaterial;
        //});
        Acce.HesEvent.AddListener(startClicked.Invoke);


        //Acce.AfterOutEvent.AddListener(() => {
        //    Player.TurnShallowBack();
        //});

    }
    public void SetFastMode()
    {
        Acce.InEvent.AddListener(() =>
        {

            if (!inBall)
            {
                startClicked.Invoke();
            }
            inBall = true;
        });

        Acce.AfterOutEvent.AddListener(() =>
        {
            inBall = false;
        });
    }
    public void Set010()
    {
        Acce.HesEvent.AddListener(() =>
        {
            canceled = true;
            Player2.TurnShallowBack();
        });
        Acce.AfterOutEvent.AddListener(() =>
        {
            if (!canceled)
            {
                Player.TurnYellow();
            }
            canceled = false;
        });
    }
    private void Update()
    {
        //if (Time.time - oldTime > 1)
        //{
        //    oldTime = Time.time;
        //    Player.TurnYellowBack();
        //}
    }
    public void Set01Mode()
    {
        Acce.AfterOutEvent.AddListener(Reset);

    }
    public void Reset()
    {
        //foreach (var ball in balls) {
        //    ball.GetComponent<MeshRenderer>().material = 
        //}
    }
    public void Set010Mode()
    {
        print(777);
        if (Player != null)
        {
            //Acce.OutEvent.AddListener(Player.ChangeColor);
        }
    }
}
