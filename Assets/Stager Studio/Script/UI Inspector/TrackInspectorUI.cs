﻿namespace StagerStudio.UI {
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.UI;


	public class TrackInspectorUI : MonoBehaviour {


		// Api
		public Text[] LanguageLabels => m_LanguageLabels;

		// Ser
		[SerializeField] private InputField m_TimeIF = null;
		[SerializeField] private BeatInputUI m_BeatIF = null;
		[SerializeField] private InputField m_TypeIF = null;
		[SerializeField] private InputField m_DurationIF = null;
		[SerializeField] private InputField m_SpeedIF = null;
		[SerializeField] private InputField m_PosXIF = null;
		[SerializeField] private InputField m_WidthIF = null;
		[SerializeField] private InputField m_AngleIF = null;
		[SerializeField] private InputField m_ColorIF = null;
		[SerializeField] private InputField m_IndexIF = null;
		[SerializeField] private Toggle m_TrayTG = null;
		[SerializeField] private Text[] m_LanguageLabels = null;



		// API
		public float GetTime () => m_TimeIF.text.TryParseFloatForInspector(out float result) ? Mathf.Max(result, 0f) : 0f;
		public float GetBeat () => m_BeatIF.GetBeat();
		public int GetItemType () => m_TypeIF.text.TryParseIntForInspector(out int result) ? Mathf.Max(result, 0) : 0;
		public float GetDuration () => m_DurationIF.text.TryParseFloatForInspector(out float result) ? Mathf.Max(result, 0f) : 0f;
		public float GetSpeed () => m_SpeedIF.text.TryParseFloatForInspector(out float result) ? Mathf.Max(result, 0f) : 1f;
		public float GetPosX () => m_PosXIF.text.TryParseFloatForInspector(out float result) ? result : 0f;
		public float GetWidth () => m_WidthIF.text.TryParseFloatForInspector(out float result) ? Mathf.Max(result, 0f) : 0f;
		public float GetAngle () => m_AngleIF.text.TryParseIntForInspector(out int result) ? Mathf.Max(result, 0f) : 0f;
		public int GetColor () => m_ColorIF.text.TryParseIntForInspector(out int result) ? Mathf.Max(result, 0) : 0;
		public int GetIndex () => m_IndexIF.text.TryParseIntForInspector(out int result) ? Mathf.Max(result, 0) : 0;
		public bool GetTray () => m_TrayTG.isOn;

		public void SetTime (float value) => m_TimeIF.text = value.ToString();
		public void SetBeat (float value) => m_BeatIF.SetBeatToUI(value);
		public void SetItemType (int value) => m_TypeIF.text = value.ToString();
		public void SetDuration (float value) => m_DurationIF.text = value.ToString();
		public void SetSpeed (float value) => m_SpeedIF.text = value.ToString();
		public void SetPosX (float value) => m_PosXIF.text = value.ToString();
		public void SetWidth (float value) => m_WidthIF.text = value.ToString();
		public void SetAngle (float value) => m_AngleIF.text = ((int)value).ToString();
		public void SetColor (int value) => m_ColorIF.text = value.ToString();
		public void SetIndex (int value) => m_IndexIF.text = value.ToString();
		public void SetTray (bool value) => m_TrayTG.isOn = value;


	}
}