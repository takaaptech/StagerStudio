﻿namespace StagerStudio.Stage {
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.UI;
	using Data;
	using Rendering;
	using Saving;
	using global::StagerStudio.Object;

	public class StageEditor : MonoBehaviour {




		#region --- SUB ---


		public delegate (Vector3 min, Vector3 max, float size, float ratio) ZoneHandler ();
		public delegate void VoidHandler ();
		public delegate void VoidIntHandler (int i);
		public delegate Beatmap BeatmapHandler ();
		public delegate bool BoolHandler ();
		public delegate void VoidStringBoolHandler (string str, bool b);
		public delegate string StringStringHandler (string str);
		public delegate float FillHandler (float time, float fill, float muti);


		#endregion




		#region --- VAR ---


		// Const
		private readonly static string[] ITEM_LAYER_NAMES = { "Stage", "Track", "Note", "Speed", "Motion", };
		private const string HINT_GlobalBrushScale = "Editor.Hint.GlobalBrushScale";
		private const string DIALOG_CannotSelectStageBrush = "Dialog.Editor.CannotSelectStageBrush";
		private const string DIALOG_CannotSelectStage = "Dialog.Editor.CannotSelectStage";
		private const string DIALOG_SelectingLayerLocked = "Dialog.Editor.SelectingLayerLocked";
		private const string HOLD_NOTE_LAYER_NAME = "HoldNote";

		// Handle
		public static ZoneHandler GetZoneMinMax { get; set; } = null;
		public static BeatmapHandler GetBeatmap { get; set; } = null;
		public static BoolHandler GetEditorActive { get; set; } = null;
		public static VoidHandler OnSelectionChanged { get; set; } = null;
		public static VoidIntHandler OnObjectEdited { get; set; } = null;
		public static VoidHandler OnLockEyeChanged { get; set; } = null;
		public static BoolHandler GetUseDynamicSpeed { get; set; } = null;
		public static BoolHandler GetUseAbreast { get; set; } = null;
		public static VoidStringBoolHandler LogHint { get; set; } = null;
		public static StringStringHandler GetLanguage { get; set; } = null;
		public static BoolHandler GetMoveAxisHovering { get; set; } = null;
		public static FillHandler GetFilledTime { get; set; } = null;

		// Api
		public int SelectingItemIndex { get; private set; } = -1;
		public int SelectingBrushIndex { get; private set; } = -1;

		// Short
		private Camera Camera => _Camera != null ? _Camera : (_Camera = Camera.main);
		private Transform AxisMoveX => m_MoveAxis.GetChild(0);
		private Transform AxisMoveY => m_MoveAxis.GetChild(1);
		private Transform AxisMoveXY => m_MoveAxis.GetChild(2);
		private int SelectingItemType { get; set; } = -1;

		// Ser
		[SerializeField] private Toggle[] m_EyeTGs = null;
		[SerializeField] private Toggle[] m_LockTGs = null;
		[SerializeField] private Transform[] m_Containers = null;
		[SerializeField] private Transform[] m_AntiTargets = null;
		[SerializeField] private RectTransform m_FocusCancel = null;
		[SerializeField] private Animator m_FocusAni = null;
		[SerializeField] private GridRenderer m_Grid = null;
		[SerializeField] private SpriteRenderer m_Hover = null;
		[SerializeField] private LoopUvRenderer m_Ghost = null;
		[SerializeField] private SpriteRenderer m_GhostPivot = null;
		[SerializeField] private Transform m_MoveAxis = null;
		[SerializeField] private string m_FocusKey = "Focus";
		[SerializeField] private string m_UnfocusKey = "Unfocus";
		[Header("Brush")]
		[SerializeField] private Toggle[] m_DefaultBrushTGs = null;
		[SerializeField] private Beatmap.Stage m_DefaultStage = null;
		[SerializeField] private Beatmap.Track m_DefaultTrack = null;
		[SerializeField] private Beatmap.Note m_DefaultNote = null;

		// Data
		private readonly static Dictionary<int, int> LayerToTypeMap = new Dictionary<int, int>();
		private readonly static bool[] ItemLock = { false, false, false, false, false, };
		private readonly static int[] ItemLayers = { -1, -1, -1, -1, -1 };
		private readonly static LayerMask[] ItemMasks = { -1, -1, -1, -1, -1, };
		private Renderer[] AxisRenderers = null;
		private bool FocusMode = false;
		private bool UIReady = true;
		private int SortingLayerID_UI = -1;
		private Coroutine FocusAniCor = null;
		private Camera _Camera = null;
		private LayerMask UnlockedMask = default;

		// Mouse
		private readonly static RaycastHit[] CastHits = new RaycastHit[64];
		private bool ClickStartInsideSelection = false;
		private int HoldNoteLayer = -1;
		private Vector2 HoverScaleMuti = Vector2.one;
		private Vector2 GhostPivotScaleMuti = Vector2.one;
		private Vector3 DragOffsetWorld = default;

		// Saving
		public SavingBool UseGlobalBrushScale { get; set; } = new SavingBool("StageEditor.UseGlobalBrushScale", true);
		public SavingBool ShowGridOnSelect { get; set; } = new SavingBool("StageEditor.ShowGridOnSelect", false);


		#endregion




		#region --- MSG ---


		private void Awake () {

			// Init Layer
			for (int i = 0; i < ITEM_LAYER_NAMES.Length; i++) {
				ItemLayers[i] = LayerMask.NameToLayer(ITEM_LAYER_NAMES[i]);
				LayerToTypeMap.Add(ItemLayers[i], i);
			}
			HoldNoteLayer = LayerMask.NameToLayer(HOLD_NOTE_LAYER_NAME);
			LayerToTypeMap.Add(HoldNoteLayer, 2);
			SortingLayerID_UI = SortingLayer.NameToID("UI");

			// Unlock Mask
			RefreshUnlockedMask();

			// ItemMasks
			for (int i = 0; i < ITEM_LAYER_NAMES.Length; i++) {
				ItemMasks[i] = LayerMask.GetMask(ITEM_LAYER_NAMES[i]);
			}

			// Eye TGs
			for (int i = 0; i < m_EyeTGs.Length; i++) {
				int index = i;
				var tg = m_EyeTGs[index];
				tg.isOn = false;
				tg.onValueChanged.AddListener((isOn) => {
					if (!UIReady) { return; }
					SetEye(index, !isOn);
				});
			}

			// Lock TGs
			for (int i = 0; i < m_LockTGs.Length; i++) {
				int index = i;
				var tg = m_LockTGs[index];
				tg.isOn = ItemLock[index];
				tg.onValueChanged.AddListener((locked) => {
					if (!UIReady) { return; }
					SetLock(index, locked);
				});
			}

			// Init Brush
			for (int i = 0; i < m_DefaultBrushTGs.Length; i++) {
				int index = i;
				m_DefaultBrushTGs[index].onValueChanged.AddListener((isOn) => {
					if (!UIReady) { return; }
					SetBrushLogic(isOn ? index : -1);
				});
			}

			// UI
			HoverScaleMuti = m_Hover.transform.localScale;
			GhostPivotScaleMuti = m_GhostPivot.transform.localScale;
			AxisRenderers = m_MoveAxis.GetComponentsInChildren<Renderer>(true);

		}


		private void LateUpdate () {
			if (GetEditorActive() && AntiTargetAllow()) {
				// Editor Active
				var map = GetBeatmap();
				LateUpdate_Selection();
				LateUpdate_Down();
				LateUpdate_Hover(map);
				LateUpdate_Axis(map);
			} else {
				// Editor InActive
				TrySetGridInactive();
				TrySetTargetActive(m_Hover.gameObject, false);
				TrySetTargetActive(m_Ghost.gameObject, false);
				TrySetTargetActive(m_MoveAxis.gameObject, false);
				return;
			}
		}


		private void LateUpdate_Selection () {
			if (SelectingItemIndex >= 0 && SelectingBrushIndex >= 0) {
				SelectingItemIndex = -1;
				SelectingItemType = -1;
				OnSelectionChanged();
			}
		}


		private void LateUpdate_Down () {
			if (GetMoveAxisHovering() || !Input.GetMouseButtonDown(0)) { return; }
			if (SelectingBrushIndex < 0) {
				// Select or Move
				var (overlapType, overlapIndex, _) = GetCastTypeIndex(GetMouseRay(), UnlockedMask, true);
				ClickStartInsideSelection = SelectingItemIndex >= 0 && overlapType == SelectingItemType && overlapIndex == SelectingItemIndex;
				if (!ClickStartInsideSelection) {
					// Select
					ClearSelection();
					if (overlapType >= 0 && overlapIndex >= 0) {
						SetSelection(overlapType, overlapIndex);
						OnSelectionChanged();
					}
				}
			} else {
				// Paint






			}
		}


		private void LateUpdate_Hover (Beatmap map) {
			if (SelectingBrushIndex >= 0) {
				// --- Painting ---
				if (GetItemLock(SelectingBrushIndex)) {
					TrySetTargetActive(m_Ghost.gameObject, false);
					TrySetGridInactive();
				} else {
					OnMouseHover_Grid(map, false);
					OnMouseHover_Ghost();
				}
				TrySetTargetActive(m_Hover.gameObject, false);
			} else {
				// --- Normal Select ---
				OnMouseHover_Normal();
				OnMouseHover_Grid(map, true);
				TrySetTargetActive(m_Ghost.gameObject, false);
			}
		}


		private void LateUpdate_Axis (Beatmap map) {
			var con = SelectingItemType >= 0 && SelectingItemType < m_Containers.Length ? m_Containers[SelectingItemType] : null;
			bool targetActive = map.GetActive(SelectingItemType, SelectingItemIndex);
			if (
				con != null &&
				SelectingItemIndex >= 0 && SelectingItemIndex < con.childCount &&
				(targetActive || Input.GetMouseButton(0))
			) {
				var target = con.GetChild(SelectingItemIndex);
				// Pos
				var pos = target.position;
				if (m_MoveAxis.position != pos) {
					m_MoveAxis.position = pos;
				}
				// Rot
				Quaternion rot;
				if (SelectingItemType == 0) {
					// Stage
					rot = Quaternion.identity;
				} else {
					// Track Note Timing
					var pIndex = map.GetParentIndex(SelectingItemType, SelectingItemIndex);
					var pTarget = pIndex >= 0 ? m_Containers[SelectingItemType - 1].GetChild(pIndex) : null;
					if (pTarget != null) {
						rot = pTarget.GetChild(0).rotation;
					} else {
						rot = target.GetChild(0).rotation;
					}
				}
				if (m_MoveAxis.rotation != rot) {
					m_MoveAxis.rotation = rot;
				}
				// Handle Active
				bool editorActive = GetEditorActive();
				bool xActive = editorActive && SelectingItemType != 3;
				bool yActive = editorActive && SelectingItemType != 1;
				bool xyActive = xActive || yActive;
				if (AxisMoveX.gameObject.activeSelf != xActive) {
					AxisMoveX.gameObject.SetActive(xActive);
				}
				if (AxisMoveY.gameObject.activeSelf != yActive) {
					AxisMoveY.gameObject.SetActive(yActive);
				}
				if (AxisMoveXY.gameObject.activeSelf != xyActive) {
					AxisMoveXY.gameObject.SetActive(xyActive);
				}
				// Axis Renderers
				foreach (var renderer in AxisRenderers) {
					if (renderer.enabled != targetActive) {
						renderer.enabled = targetActive;
					}
				}
				// Active
				TrySetTargetActive(m_MoveAxis.gameObject, true);
			} else {
				TrySetTargetActive(m_MoveAxis.gameObject, false);
			}
		}


		// Hover
		private void OnMouseHover_Grid (Beatmap map, bool selectingMode) {

			// Check
			if (selectingMode && SelectingItemIndex < 0) {
				TrySetGridInactive();
				return;
			}

			// Paint Grid
			bool gridEnable = false;
			Vector3 pos = default;
			Quaternion rot = default;
			Vector3 scl = default;
			var ray = GetMouseRay();
			var (zoneMin, zoneMax, zoneSize, zoneRatio) = GetZoneMinMax();
			int gridingItemType = selectingMode ? SelectingItemType : SelectingBrushIndex;
			switch (gridingItemType) {
				case 0: // Stage
					gridEnable = true;
					pos = Util.Vector3Lerp3(zoneMin, zoneMax, 0.5f, 0f);
					rot = Quaternion.identity;
					scl = new Vector3(zoneSize, zoneSize / zoneRatio, 1f);
					m_Grid.Mode = 0;
					m_Grid.IgnoreDynamicSpeed = false;
					m_Grid.ObjectSpeedMuti = 1f;
					break;
				case 1: // Track
				case 2: // Note
					int hoverItemType, hoverItemIndex;
					Transform hoverTarget;
					if (selectingMode) {
						hoverItemType = gridingItemType - 1;
						hoverItemIndex = map.GetParentIndex(gridingItemType, SelectingItemIndex);
						hoverTarget = hoverItemIndex >= 0 ? m_Containers[hoverItemType].GetChild(hoverItemIndex) : null;
					} else {
						(hoverItemType, hoverItemIndex, hoverTarget) = GetCastTypeIndex(ray, ItemMasks[gridingItemType - 1], true);
					}
					if (hoverTarget != null) {
						gridEnable = true;
						pos = hoverTarget.position;
						rot = hoverTarget.rotation;
						scl = hoverTarget.GetChild(0).localScale;
						m_Grid.ObjectSpeedMuti = GetUseDynamicSpeed() ? map.GetSpeedMuti(hoverItemType, hoverItemIndex) : 1f;
					} else {
						m_Grid.ObjectSpeedMuti = 1f;
					}
					m_Grid.Mode = gridingItemType;
					m_Grid.IgnoreDynamicSpeed = false;
					break;
				case 3: // Timing
					gridEnable = true;
					scl = new Vector3(zoneSize, zoneSize / zoneRatio, 1f);
					pos = new Vector3((zoneMin.x + zoneMax.x) / 2f, zoneMin.y, zoneMin.z);
					rot = Quaternion.identity;
					m_Grid.ObjectSpeedMuti = 1f;
					m_Grid.Mode = 3;
					m_Grid.IgnoreDynamicSpeed = true;
					break;
			}
			m_Grid.SetGridTransform(gridEnable, ShowGridOnSelect.Value || !selectingMode, pos, rot, scl);
		}


		private void OnMouseHover_Ghost () {

			// Ghost
			var ray = GetMouseRay();
			var (zoneMin, zoneMax, zoneSize, zoneRatio) = GetZoneMinMax();
			bool ghostEnable = false;
			float ghostPivotX = 0.5f;
			Vector2 ghostSize = default;
			Vector3 ghostPos = default;
			Quaternion ghostRot = Quaternion.identity;
			const float GHOST_NOTE_Y = 0.032f;
			Transform hoverTarget = null;

			// Movement
			if (RayInsideZone(ray)) {
				switch (SelectingBrushIndex) {
					case 0: // Stage
						var stage = m_DefaultStage;
						if (stage != null && !GetUseAbreast()) {
							var mousePos = Util.GetRayPosition(ray, zoneMin, zoneMax, null, true);
							ghostEnable = mousePos.HasValue;
							if (mousePos.HasValue) {
								ghostSize.x = zoneSize * stage.Width;
								ghostSize.y = zoneSize * stage.Height / zoneRatio;
								ghostPos = mousePos.Value;
								ghostPos = m_Grid.SnapWorld(ghostPos);
								ghostPivotX = 0.5f;
							}
						}
						break;
					case 1: // Track
						hoverTarget = GetCastTypeIndex(ray, ItemMasks[0], true).target;
						var track = m_DefaultTrack;
						if (track != null && hoverTarget != null) {
							var mousePos = Util.GetRayPosition(ray, zoneMin, zoneMax, null, true);
							ghostEnable = mousePos.HasValue;
							if (mousePos.HasValue) {
								if (UseGlobalBrushScale) {
									ghostSize.x = track.Width * zoneSize;
								} else {
									ghostSize.x = track.Width * hoverTarget.GetChild(0).localScale.x;
								}
								ghostSize.y = hoverTarget.GetChild(0).localScale.y;
								ghostPos = mousePos.Value;
								ghostPos = m_Grid.SnapWorld(ghostPos, true);
								ghostRot = hoverTarget.transform.rotation;
								ghostPivotX = 0.5f;
							}
						}
						break;
					case 2: // Note
						var note = m_DefaultNote;
						hoverTarget = GetCastTypeIndex(ray, ItemMasks[1], true).target;
						if (note != null && hoverTarget != null) {
							var mousePos = Util.GetRayPosition(ray, zoneMin, zoneMax, hoverTarget, false);
							ghostEnable = mousePos.HasValue;
							if (mousePos.HasValue) {
								if (UseGlobalBrushScale) {
									ghostSize.x = note.Width * zoneSize;
								} else {
									ghostSize.x = note.Width * hoverTarget.GetChild(0).localScale.x;
								}
								ghostSize.y = Mathf.Max(note.Duration * m_Grid.SpeedMuti * m_Grid.ObjectSpeedMuti * hoverTarget.GetChild(0).localScale.y, GHOST_NOTE_Y / zoneSize);
								ghostPos = mousePos.Value;
								ghostPos = m_Grid.SnapWorld(ghostPos);
								ghostRot = hoverTarget.transform.rotation;
								ghostPivotX = 0.5f;
							}
						}
						break;
					case 3: { // Speed
							var mousePos = Util.GetRayPosition(ray, zoneMin, zoneMax, null, false);
							ghostEnable = mousePos.HasValue;
							if (mousePos.HasValue) {
								ghostSize.x = 0.12f / zoneSize;
								ghostSize.y = GHOST_NOTE_Y / zoneSize;
								ghostPos.x = zoneMin.x;
								ghostPos.y = m_Grid.SnapWorld(mousePos.Value).y;
								ghostPos.z = zoneMin.z;
								ghostRot = Quaternion.identity;
								ghostPivotX = 0f;
							}
						}
						break;
				}
			}

			// Final
			if (ghostEnable) {
				m_Ghost.Size = ghostSize;
				m_Ghost.transform.localScale = ghostSize;
				m_Ghost.transform.position = ghostPos;
				m_Ghost.transform.rotation = ghostRot;
				m_Ghost.Pivot = new Vector3(ghostPivotX, 0f, 0f);
				m_Ghost.SetSortingLayer(SortingLayerID_UI, short.MaxValue - 1);
				m_GhostPivot.transform.localScale = new Vector3(GhostPivotScaleMuti.x / ghostSize.x, GhostPivotScaleMuti.y / ghostSize.y, 1f);
				TrySetTargetActive(m_Ghost.gameObject, true);
			} else {
				TrySetTargetActive(m_Ghost.gameObject, false);
			}
		}


		private void OnMouseHover_Normal () {
			if (GetMoveAxisHovering() || Input.GetMouseButton(0) || Input.GetMouseButton(1)) {
				TrySetTargetActive(m_Hover.gameObject, false);
				return;
			}
			// Hover
			var ray = GetMouseRay();
			var (_, _, target) = GetCastTypeIndex(ray, UnlockedMask, true);
			if (target != null) {
				TrySetTargetActive(m_Hover.gameObject, true);
				m_Hover.transform.position = target.GetChild(0).position;
				m_Hover.transform.rotation = target.GetChild(0).rotation;
				m_Hover.size = target.GetChild(0).localScale / HoverScaleMuti;
			} else {
				TrySetTargetActive(m_Hover.gameObject, false);
			}
		}


		// Axis
		public void OnMoveAxisDrag (Vector3 pos, Vector3? downPos, int axis) {
			var map = GetBeatmap();
			if (map != null && SelectingItemIndex >= 0) {
				var (zoneMin, zoneMax_real, zoneSize, _) = GetZoneMinMax();
				var zoneMax = zoneMax_real;
				zoneMax.y = zoneMin.y + zoneSize;
				zoneMax_real.z = zoneMin.z + zoneSize;
				if (downPos.HasValue) {
					// Down
					DragOffsetWorld = downPos.Value - m_MoveAxis.position;
				} else {
					// Drag
					pos = m_Grid.SnapWorld(
						pos - DragOffsetWorld,
						SelectingItemType == 1
					);
					var zonePos = Util.Vector3InverseLerp3(zoneMin, zoneMax, pos.x, pos.y, pos.z);
					var zonePos_real = Util.Vector3InverseLerp3(zoneMin, zoneMax_real, pos.x, pos.y, pos.z);
					int index = SelectingItemIndex;
					switch (SelectingItemType) {
						case 0:  // Stage
							if (axis == 0 || axis == 2) {
								map.SetX(0, index, zonePos.x);
							}
							if (axis == 1 || axis == 2) {
								map.SetStageY(index, zonePos.y);
							}
							break;
						case 1: // Track
							if (axis == 0 || axis == 2) {
								var track = index >= 0 && index < map.Tracks.Count ? map.Tracks[index] : null;
								if (track == null || track.StageIndex < 0 || track.StageIndex >= map.Stages.Count) { break; }
								var stage = map.Stages[track.StageIndex];
								var localPosX = Stage.ZoneToLocal(
									zonePos.x, zonePos.y, zonePos.z,
									Stage.GetStagePosition(stage, index),
									Stage.GetStageWidth(stage),
									Stage.GetStageHeight(stage),
									Stage.GetStageWorldRotationZ(stage)
								).x;
								map.SetX(1, index, localPosX);
							}
							break;
						case 2: { // Note
								var note = index >= 0 && index < map.Notes.Count ? map.Notes[index] : null;
								if (note == null || note.TrackIndex < 0 || note.TrackIndex >= map.Tracks.Count) { break; }
								var track = map.Tracks[note.TrackIndex];
								if (track == null || track.StageIndex < 0 || track.StageIndex >= map.Stages.Count) { break; }
								var stage = map.Stages[track.StageIndex];
								var localPos = Track.ZoneToLocal(
									zonePos.x, zonePos.y, zonePos.z,
									Stage.GetStagePosition(stage, track.StageIndex),
									Stage.GetStageWidth(stage),
									Stage.GetStageHeight(stage),
									Stage.GetStageWorldRotationZ(stage),
									Track.GetTrackX(track),
									Track.GetTrackWidth(track),
									Track.GetTrackAngle(track)
								);
								if (axis == 0 || axis == 2) {
									map.SetX(2, index, localPos.x);
								}
								if (axis == 1 || axis == 2) {
									map.SetTime(2, index, GetFilledTime(
										m_Grid.MusicTime,
										localPos.y,
										m_Grid.SpeedMuti * m_Grid.ObjectSpeedMuti
									));
								}
							}
							break;
						case 3:  // Timing
							if (axis == 1 || axis == 2) {
								map.SetTime(
									3, index,
									Mathf.Max(m_Grid.MusicTime + zonePos_real.y / m_Grid.SpeedMuti, 0f)
								);
							}
							break;
					}
					OnObjectEdited(SelectingItemType);
				}
			}
		}


		#endregion




		#region --- API ---


		public void SwitchUseGlobalBrushScale () {
			UseGlobalBrushScale.Value = !UseGlobalBrushScale;
			try {
				LogHint(
					string.Format(GetLanguage(HINT_GlobalBrushScale), UseGlobalBrushScale.Value ? "ON" : "OFF"),
					true
				);
			} catch { }
		}


		// Selection
		public void SetSelection (int type, int index) {
			// Select
			if (SelectingItemType >= 0 && SelectingItemIndex >= 0 && SelectingItemType != type) {
				ClearSelection();
			}
			SelectingItemType = type;
			SelectingItemIndex = index;
			// No Stage in Abreast View
			if (GetUseAbreast() && SelectingItemType == 0) {
				SelectingItemType = -1;
				SelectingItemIndex = -1;
				LogHint(GetLanguage(DIALOG_CannotSelectStage), true);
			}
		}


		public void ClearSelection () {
			var map = GetBeatmap();
			if (!(map is null) && SelectingItemIndex >= 0) {
				// Clear
				SelectingItemIndex = -1;
				SelectingItemType = -1;
				// Final
				OnSelectionChanged();
			}
		}


		// Container
		public void UI_SwitchContainerActive (int index) => SetEye(index, !GetContainerActive(index));


		public bool GetContainerActive (int index) => m_Containers[index].gameObject.activeSelf;


		public void SetContainerActive (int index, bool active) => SetEye(index, active);


		// Item Lock
		public bool GetItemLock (int item) => item >= 0 ? ItemLock[item] : false;


		public void UI_SwitchLock (int index) => SetLock(index, !GetItemLock(index));


		// Focus
		public void UI_SetFocus (bool focus) {
			if (!(FocusAniCor is null)) { return; }
			if (focus != FocusMode) {
				FocusMode = focus;
				m_FocusAni.enabled = true;
				m_FocusAni.SetTrigger(focus ? m_FocusKey : m_UnfocusKey);
				if (!(FocusAniCor is null)) {
					StopCoroutine(FocusAniCor);
					FocusAniCor = null;
				}
				FocusAniCor = StartCoroutine(AniCheck());
			}
			// Func
			IEnumerator AniCheck () {
				if (focus) {
					m_FocusCancel.gameObject.SetActive(true);
				}
				yield return new WaitForSeconds(0.5f);
				yield return new WaitUntil(() =>
					m_FocusAni.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f && !m_FocusAni.IsInTransition(0)
				);
				if (!focus) {
					m_FocusCancel.gameObject.SetActive(false);
				}
				m_FocusAni.enabled = false;
				FocusAniCor = null;
			}
		}


		public void UI_SwitchFocus () => UI_SetFocus(!FocusMode);


		// Brush
		public void SetBrush (int index) {
			if (UIReady) {
				SetBrushLogic(index);
			}
		}


		#endregion



		#region --- LGC ---


		private bool AntiTargetAllow () {
			foreach (var t in m_AntiTargets) {
				if (t.gameObject.activeSelf) {
					return false;
				}
			}
			return true;
		}


		private Ray GetMouseRay () => Camera.ScreenPointToRay(Input.mousePosition);


		private void SetLock (int index, bool locked) {
			// Set Logic
			ItemLock[index] = locked;
			// Refresh Unlock Mask
			RefreshUnlockedMask();
			// Refresh UI
			UIReady = false;
			try {
				m_LockTGs[index].isOn = locked;
			} catch { }
			UIReady = true;
			OnLockEyeChanged();
		}


		private void SetEye (int index, bool see) {
			m_Containers[index].gameObject.SetActive(see);
			// UI
			UIReady = false;
			try {
				m_EyeTGs[index].isOn = !see;
			} catch { }
			UIReady = true;
			// MSG
			OnLockEyeChanged();
		}


		private (int type, int index, Transform target) GetCastTypeIndex (Ray ray, LayerMask mask, bool insideZone) {
			int count = Physics.RaycastNonAlloc(ray, CastHits, float.MaxValue, mask);
			int overlapType = -1;
			int overlapIndex = -1;
			Transform tf = null;
			if (!insideZone || RayInsideZone(ray)) {
				for (int i = 0; i < count; i++) {
					var hit = CastHits[i];
					int layer = hit.transform.gameObject.layer;
					int type = LayerToTypeMap.ContainsKey(layer) ? LayerToTypeMap[layer] : -1;
					int itemIndex = hit.transform.parent.GetSiblingIndex();
					if (type >= overlapType) {
						if (type != overlapType) {
							overlapIndex = -1;
						}
						if (itemIndex >= overlapIndex) {
							tf = CastHits[i].transform.parent;
						}
						overlapIndex = Mathf.Max(itemIndex, overlapIndex);
						overlapType = type;
					}
				}
			}
			return (overlapType, overlapIndex, tf);
		}


		private bool RayInsideZone (Ray ray) {
			var (zoneMin, zoneMax, _, _) = GetZoneMinMax();
			if (new Plane(Vector3.back, zoneMin).Raycast(ray, out float enter)) {
				var point = ray.GetPoint(enter);
				return point.x > zoneMin.x && point.x < zoneMax.x && point.y > zoneMin.y && point.y < zoneMax.y;
			}
			return false;
		}


		private void RefreshUnlockedMask () {
			var list = new List<string>();
			for (int i = 0; i < ItemLock.Length; i++) {
				if (!ItemLock[i]) {
					list.Add(ITEM_LAYER_NAMES[i]);
				}
			}
			// Hold Note Layer
			if (!ItemLock[2]) {
				list.Add(HOLD_NOTE_LAYER_NAME);
			}
			UnlockedMask = LayerMask.GetMask(list.ToArray());
		}


		private void TrySetTargetActive (GameObject target, bool active) {
			if (target.activeSelf != active) {
				target.SetActive(active);
			}
		}


		private void TrySetGridInactive () {
			if (m_Grid.GridEnabled) {
				m_Grid.SetGridTransform(false, false);
			}
		}


		private void SetBrushLogic (int index) {
			if (!enabled) { index = -1; }
			UIReady = false;
			try {
				// No Stage in Abreast
				if (index == 0 && GetUseAbreast()) {
					index = -1;
					LogHint(GetLanguage(DIALOG_CannotSelectStageBrush), true);
				}
				// Layer Lock
				if (GetItemLock(index)) {
					index = -1;
					LogHint(GetLanguage(DIALOG_SelectingLayerLocked), true);
				}
				// Brushs TG
				for (int i = 0; i < m_DefaultBrushTGs.Length; i++) {
					m_DefaultBrushTGs[i].isOn = i == index;
				}
				// Logic
				SelectingBrushIndex = index;
			} catch { }
			UIReady = true;
		}


		#endregion



	}
}