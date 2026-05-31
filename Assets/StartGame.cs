using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartGame : MonoBehaviour
{
    public AcceStimulate Acce;
    public GameObject Game;
    private void Start()
    {
        Acce = GetComponent<AcceStimulate>();
        Acce.InEvent.AddListener(Acce.ToggleHLMaterial);
        Acce.InEvent.AddListener(GameStart);
        Acce.InEvent.AddListener(Acce.SetInvoked);
        Acce.AfterOutEvent.AddListener(Acce.UnInvoked);
    }
    public void GameStart()
    {

    }
}
