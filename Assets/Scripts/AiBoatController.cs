using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiBoatController : WaveObjectController
{
    [SerializeField] Renderer kBoatRenderer = null;
    private float move_speed_ = 0f;
    private float rotation_speed_ = 0f;
    private Rigidbody rigidbody_ = null;

    // Use this for initialization
    private void Start()
    {
        float scale = Random.Range(0.75f, 1.25f);
        transform.localScale = new Vector3(scale, scale, scale);
        var material = kBoatRenderer.material;
        material.color = MyUtilities.RandomColor(Color.red, Color.green);

        float multiplier = 2f - scale;
        move_speed_ = 4f * multiplier;
        rotation_speed_ = 270f * multiplier;
        rigidbody_ = GetComponent<Rigidbody>();
        base.Initialize();
    }

    private void OnDestroy()
    {
        base.Uninitialize();
    }

    // Update is called once per frame
    private void Update()
    {
        // プレイヤーの方向に向かって移動する
        var player = WaveManager.Instance.Player();
        if (player == null) return;

        var target = player.transform;
        var direction = Vector3.Scale(target.position - transform.position, new Vector3(1f, 0f, 1f)).normalized;
        var movement = Vector3.ProjectOnPlane(transform.forward, Vector3.up) * direction.magnitude * move_speed_;
        rigidbody_.MovePosition(transform.position + movement * Time.deltaTime);

        // 物理演算の時の回転を切ったのため直接にtransformで回転する
        direction = transform.InverseTransformDirection(direction);
        var turn_amount = Mathf.Atan2(direction.x, direction.z);
        transform.Rotate(0f, turn_amount * rotation_speed_ * Time.deltaTime, 0f);
        moving_ = true;
    }
}
