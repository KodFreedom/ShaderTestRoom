using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatController : WaveObjectController
{
    private Rigidbody rigidbody_ = null;

    // Use this for initialization
    private void Start ()
    {
        rigidbody_ = GetComponent<Rigidbody>();
        base.Initialize();	
	}

    private void OnDestroy()
    {
        base.Uninitialize();
    }

    // Update is called once per frame
    private void Update ()
    {
        // カメラの方向に向かって移動する
        var camera = Camera.main.transform;
        var forward = Vector3.Scale(camera.forward, new Vector3(1f, 0f, 1f)).normalized;
        var vertical = Input.GetAxis("Vertical");
        var horizontal = Input.GetAxis("Horizontal");

        if (vertical != 0f || horizontal != 0f)
        {
            var direction = vertical * forward + horizontal * camera.right;
            var movement = Vector3.ProjectOnPlane(transform.forward, Vector3.up) * direction.magnitude * 5f;
            rigidbody_.MovePosition(transform.position + movement * Time.deltaTime);

            // 物理演算の時の回転を切ったのため直接にtransformで回転する
            direction = transform.InverseTransformDirection(direction);
            var turn_amount = Mathf.Atan2(direction.x, direction.z);
            transform.Rotate(0f, turn_amount * 360.0f * Time.deltaTime, 0f);
            moving_ = true;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag.Equals("Enemy"))
        {
            GameObserver.Instance.GameOver(this);
        }
    }
}
