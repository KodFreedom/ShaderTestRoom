using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveObjectController : MonoBehaviour
{
    private Vector3 previous_position_;
    private float target_y_ = 0f;
    protected bool moving_ = false;

    // Use this for initialization
    protected void Initialize ()
    {
        previous_position_ = transform.position;
        WaveManager.Instance.Register(this);
	}

    protected void Uninitialize()
    {
        WaveManager.Instance.Deregister(this);
    }

    private void Update()
    {
    }

    // Update is called once per frame
    public void CheckWave ()
    {
		if(moving_)
        {
            var direction = transform.position - previous_position_;
            direction.y = 0f;
            direction.Normalize();
            WaveManager.Instance.Water().OccurWave(transform, direction);
        }
        moving_ = false;
    }

    public void UpdateHeight()
    {
        previous_position_ = transform.position;
        Vector3 new_position = transform.position;
        target_y_ = WaveManager.Instance.Water().ReturnHeight(new_position);
        new_position.y = Mathf.Lerp(new_position.y, target_y_, Time.deltaTime * 5f);
        transform.position = new_position;
    }
}
