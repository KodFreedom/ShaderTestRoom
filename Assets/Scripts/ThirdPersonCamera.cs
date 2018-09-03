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
		[SerializeField] float MoveSpeed = 3f;
		[SerializeField] float TurnSpeed = 3f;
		[SerializeField] float PitchMax = 75f;
		[SerializeField] float PitchMin = 5f;
		[SerializeField] float LookDistance = 10f;

		public Transform TargetPlayer = null;
		public FollowUpdateTiming followUpdateTiming = FollowUpdateTiming.LateUpdate;
		private Vector3 OriginalPivotEulers = Vector3.zero;
		private Transform PivotTransform = null;
		private Transform CameraTransform = null;
		private float RigYawAngle = 0f;
		private float PivotPitchAngle = 0f;
		private float CurrentMoveSpeed = 0f;
		private const float MoveAcceleration = 0.2f;

		// Private
		// 初期化処理
		private void Awake()
		{
			CameraTransform = GetComponentInChildren<Camera>().transform;
			PivotTransform = CameraTransform.parent;
			OriginalPivotEulers = PivotTransform.localRotation.eulerAngles;

			// プレイヤーとの距離の設定
			CameraTransform.localPosition = new Vector3 (CameraTransform.localPosition.x, CameraTransform.localPosition.y, -LookDistance);
		}

		// 更新処理
		private void FixedUpdate()
		{
			if (!TargetPlayer || followUpdateTiming != FollowUpdateTiming.FixedUpdate) { return; }

			// 追従処理
			transform.position = Vector3.Lerp(transform.position, TargetPlayer.position, Time.deltaTime * CurrentMoveSpeed);
		}

		private void LateUpdate()
		{
			if (!TargetPlayer || followUpdateTiming != FollowUpdateTiming.LateUpdate) { return; }

			// 追従処理
			transform.position = Vector3.Lerp(transform.position, TargetPlayer.position, Time.deltaTime * CurrentMoveSpeed);
		}

		// 更新処理
		private void Update()
		{
			if (!TargetPlayer) { return; }

			// コントローラ回転軸取得
			var horizontal = Input.GetAxis("CameraHorizontal");

			// RigのYaw回転処理
			RigYawAngle += horizontal * TurnSpeed;
			transform.localRotation = Quaternion.Euler(0f, RigYawAngle, 0f);

			// PivotのPitch回転処理
			//PivotPitchAngle -= vertical　*　TurnSpeed;
			PivotPitchAngle = Mathf.Clamp(PivotPitchAngle, PitchMin, PitchMax);
			PivotTransform.localRotation = Quaternion.Euler(PivotPitchAngle, OriginalPivotEulers.y , OriginalPivotEulers.z);

			CurrentMoveSpeed = Mathf.Min (MoveSpeed, CurrentMoveSpeed + Time.deltaTime * MoveAcceleration);
		}
	}
}