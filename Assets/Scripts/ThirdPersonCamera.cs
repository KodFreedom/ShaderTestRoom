using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Bombtet
{
	public class ThirdPersonCamera : MonoBehaviour
	{
		public enum FollowUpdateTiming
		{
			FixedUpdate,
			LateUpdate
		}

		// 変数定義
		[SerializeField] float LookDistance = 10f;

		public Transform TargetPlayer = null;
		public FollowUpdateTiming followUpdateTiming = FollowUpdateTiming.LateUpdate;
		private Transform CameraTransform = null;

		// Private
		// 初期化処理
		private void Awake()
		{
			CameraTransform = GetComponentInChildren<Camera>().transform;

			// プレイヤーとの距離の設定
			CameraTransform.localPosition = new Vector3 (CameraTransform.localPosition.x, CameraTransform.localPosition.y, -LookDistance);
		}

		// 更新処理
		private void FixedUpdate()
		{
			if (!TargetPlayer || followUpdateTiming != FollowUpdateTiming.FixedUpdate) { return; }

			// 追従処理
			transform.position = TargetPlayer.position;
		}

		private void LateUpdate()
		{
			if (!TargetPlayer || followUpdateTiming != FollowUpdateTiming.LateUpdate) { return; }

			// 追従処理
			transform.position = TargetPlayer.position;
		}

		// 更新処理
		private void Update()
		{
			if (!TargetPlayer) { return; }
		}
	}
}