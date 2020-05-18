﻿namespace StagerStudio.Object {
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;



	public class MotionItem_Index : MotionItem {



		// Ser
		[SerializeField] private float m_LineLength = 0.12f;
		[SerializeField] private Transform m_Handle = null;




		// MSG
		private void OnEnable () => Update();


		protected override void Update () {
			base.Update();
			var map = GetBeatmap();
			int motionIndex = transform.GetSiblingIndex();
			if (map != null) {
				if (Active) {
					if (map.GetMotionValueTween(ItemIndex, MotionType, motionIndex, out float valueA, out _, out _).hasA) {
						SetSliderValue(Util.Remap(0f, IndexCount - 1, -1f, 1f, valueA));
					}
				}
			}
		}


		protected override void InvokeAxis (Vector2 localPos) {
			var map = GetBeatmap();
			if (map != null) {
				float value = Util.Remap(-m_LineLength, m_LineLength, 0, IndexCount - 1, localPos.x);
				map.SetMotionValueTween(ItemIndex, MotionType, transform.GetSiblingIndex(), value);
				SetLabelText(Mathf.RoundToInt(value).ToString());
				OnMotionChanged();
			}
		}


		// LGC
		private void SetSliderValue (float value11) => m_Handle.localPosition = new Vector3(
			Util.Remap(-1f, 1f, -m_LineLength, m_LineLength, Mathf.Clamp(value11, -1f, 1f)),
			0f, 0f
		);



	}
}