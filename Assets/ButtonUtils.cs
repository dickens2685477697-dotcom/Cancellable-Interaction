using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonUtils : MonoBehaviour
{
    public AcceStimulate Acces;
    public TMPro.TMP_Text text;
    void Start()
    {

        //Acces = GetComponent<AcceStimulate>();
        //text = GetComponentInChildren<TMPro.TMP_Text>();
    }
    
    //public void ButtonSelected()
    //{
    //    if (Acces.HighLighted)
    //        ParentSSV.ChosenIndex.Remove(int.Parse(text.text));
    //    else
    //        ParentSSV.ChosenIndex.Add(int.Parse(text.text));
    //}
    //public void ButtonReset()
    //{
    //    Acces.HighLighted = false;
    //    Acces.OutMaterial();
    //}
}
