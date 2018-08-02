using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManagerTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
        Libs.EM.I.Add("EventName",OnEvent);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    bool OnEvent(string name,object data){
        Debug.LogFormat(" On Event Name = {0},data = {1}",name,data);
        return false;
    }

    void OnGUI(){
        if(GUI.Button(new Rect(0,0,78,24),"Send")){
            Libs.EM.I.Send("EventName",this);
        }
    }
       
    void OnDestroy(){
        if(Libs.EventManager.I)
        Libs.EventManager.I.RemByTargetAll(this);
    }
}
