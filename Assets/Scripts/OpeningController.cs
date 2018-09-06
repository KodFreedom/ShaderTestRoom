using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OpeningController : MonoBehaviour
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
        if(text_.color.a <= 0f)
        {
            Destroy(gameObject);
        }

        transform.localScale += new Vector3(Time.deltaTime, Time.deltaTime);
        text_.color -= new Color(0f, 0f, 0f, Time.deltaTime);
	}
}
