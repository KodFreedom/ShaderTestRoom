using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    private Text text_;
    
	// Use this for initialization
	void Start ()
    {
        text_ = GetComponent<Text>();	
	}
	
	// Update is called once per frame
	void Update ()
    {
        float current_time = GameObserver.Instance.CurrentTime();
        text_.text = current_time.ToString("00.00");
	}
}
