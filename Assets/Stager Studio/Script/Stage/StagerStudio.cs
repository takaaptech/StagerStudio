﻿namespace StagerStudio {
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.UI;
	using UI;
	using Stage;
	using Object;
	using Rendering;
	using Data;
	using Saving;
	using UndoRedo;
	using DebugLog;


	public partial class StagerStudio : MonoBehaviour {




		#region --- SUB ---


		[System.Serializable]
		public struct CursorData {
			public Texture2D Cursor;
			public Vector2 Offset;
		}


		[System.Serializable]
		private class UndoData {



			[System.Serializable]
			public struct UndoColor {
				public byte R;
				public byte G;
				public byte B;
				public byte A;
				public UndoColor (Color32 color) {
					R = color.r;
					G = color.g;
					B = color.b;
					A = color.a;
				}
			}



			[System.Serializable]
			public struct UndoCurve {
				[System.Serializable]
				public struct UndoKeyframe {
					public float time;
					public float value;
					public float inTangent;
					public float outTangent;
					public float inWeight;
					public float outWeight;
				}
				public UndoKeyframe[] Keys;
				public UndoCurve (AnimationCurve curve) {
					int len = curve.length;
					Keys = new UndoKeyframe[len];
					for (int i = 0; i < len; i++) {
						var sourceKey = curve[i];
						Keys[i] = new UndoKeyframe() {
							time = sourceKey.time,
							value = sourceKey.value,
							inTangent = sourceKey.inTangent,
							outTangent = sourceKey.outTangent,
							inWeight = sourceKey.inWeight,
							outWeight = sourceKey.outWeight,
						};
					}
				}
			}


			public Beatmap Map;
			public UndoColor[] Palette;
			public UndoCurve[] Tweens;
			public int SelectingItemType;
			public int SelectingItemIndex;
			public bool ContainerActive_0;
			public bool ContainerActive_1;
			public bool ContainerActive_2;
			public bool ContainerActive_3;


			public UndoData (
				Beatmap map, List<Color32> palette, List<AnimationCurve> curves, int selectingItemType, int selectingItemIndex,
				bool containerActive_0, bool containerActive_1, bool containerActive_2, bool containerActive_3
			) {
				Map = map;
				SelectingItemType = selectingItemType;
				SelectingItemIndex = selectingItemIndex;
				ContainerActive_0 = containerActive_0;
				ContainerActive_1 = containerActive_1;
				ContainerActive_2 = containerActive_2;
				ContainerActive_3 = containerActive_3;
				// Pal
				int pCount = palette.Count;
				Palette = new UndoColor[pCount];
				for (int i = 0; i < pCount; i++) {
					Palette[i] = new UndoColor(palette[i]);
				}
				// Tween
				int tCount = curves.Count;
				Tweens = new UndoCurve[tCount];
				for (int i = 0; i < tCount; i++) {
					Tweens[i] = new UndoCurve(curves[i]);
				}
			}


			public List<Color32> GetPalette () {
				var result = new List<Color32>();
				foreach (var c in Palette) {
					result.Add(new Color32(c.R, c.G, c.B, c.A));
				}
				return result;
			}


			public List<AnimationCurve> GetTweens () {
				var result = new List<AnimationCurve>();
				foreach (var t in Tweens) {
					int keyCount = t.Keys.Length;
					var keyArray = new Keyframe[keyCount];
					for (int i = 0; i < keyCount; i++) {
						var sourceKey = t.Keys[i];
						keyArray[i] = new Keyframe() {
							time = sourceKey.time,
							value = sourceKey.value,
							inTangent = sourceKey.inTangent,
							outTangent = sourceKey.outTangent,
							inWeight = sourceKey.inWeight,
							outWeight = sourceKey.outWeight,
							weightedMode = WeightedMode.Both,
						};
					}
					result.Add(new AnimationCurve() {
						keys = keyArray,
						postWrapMode = WrapMode.Clamp,
						preWrapMode = WrapMode.Clamp,
					});
				}
				return result;
			}


		}


		#endregion




		#region --- VAR ---


		// Const
		private const string UI_QuitConfirm = "Menu.UI.QuitConfirm";
		private const string UI_SelectorStageMenu = "Menu.UI.SelectorStage";
		private const string UI_SelectorTrackMenu = "Menu.UI.SelectorTrack";
		private const string UI_OpenWebMSG = "Dialog.OpenWebMSG";
		private const string UI_OpenSkinEditorConfirm = "Dialog.OpenSkinEditorConfirm";
		private const string Confirm_DeleteProjectSound = "ProjectInfo.Dialog.DeleteSound";
		private const string Confirm_SwipeProjectSound = "ProjectInfo.Dialog.SwipeSound";
		private const string Hint_CommandDone = "Command.Hint.CommandDone";
		private const string Hint_Volume = "Music.Hint.Volume";
		private const string Hint_Undo = "Undo.Hint.Undo";

		// Ser
		[Header("Stage")]
		[SerializeField] private StageMusic m_Music = null;
		[SerializeField] private StageProject m_Project = null;
		[SerializeField] private StageGame m_Game = null;
		[SerializeField] private StageSoundFX m_SoundFX = null;
		[SerializeField] private StageEditor m_Editor = null;
		[SerializeField] private StageLanguage m_Language = null;
		[SerializeField] private StageSkin m_Skin = null;
		[SerializeField] private StageShortcut m_Shortcut = null;
		[SerializeField] private StageMenu m_Menu = null;
		[SerializeField] private StageState m_State = null;
		[SerializeField] private StageEffect m_Effect = null;
		[SerializeField] private StageEasterEgg m_EasterEgg = null;
		[SerializeField] private StageGene m_Gene = null;
		[SerializeField] private StageInspector m_Inspector = null;
		[Header("Misc")]
		[SerializeField] private Transform m_CanvasRoot = null;
		[SerializeField] private RectTransform m_DirtyMark = null;
		[SerializeField] private Text m_BeatmapSwiperLabel = null;
		[SerializeField] private Text m_SkinSwiperLabel = null;
		[SerializeField] private Image m_AbreastTGMark = null;
		[SerializeField] private Toggle m_GridTG = null;
		[SerializeField] private Toggle m_SnapTG = null;
		[SerializeField] private Text m_VersionLabel = null;
		[SerializeField] private GridRenderer m_GridRenderer = null;
		[SerializeField] private RectTransform m_PitchWarningBlock = null;
		[SerializeField] private RectTransform m_PitchTrebleClef = null;
		[SerializeField] private RectTransform m_PitchBassClef = null;
		[SerializeField] private Transform m_CameraTF = null;
		[SerializeField] private Text m_TipLabelA = null;
		[SerializeField] private Text m_TipLabelB = null;
		[SerializeField] private RectTransform m_MotionInspector = null;
		[SerializeField] private RectTransform m_SelectBrushMark = null;
		[SerializeField] private RectTransform m_EraseBrushMark = null;
		[SerializeField] private Slider m_DropSpeedSlider = null;
		[Header("UI")]
		[SerializeField] private BackgroundUI m_Background = null;
		[SerializeField] private ProgressUI m_Progress = null;
		[SerializeField] private HintBarUI m_Hint = null;
		[SerializeField] private ZoneUI m_Zone = null;
		[SerializeField] private PreviewUI m_Preview = null;
		[SerializeField] private WaveUI m_Wave = null;
		[SerializeField] private TimingPreviewUI m_TimingPreview = null;
		[SerializeField] private AxisHandleUI m_Axis = null;
		[SerializeField] private MotionPainterUI m_MotionPainter = null;
		[SerializeField] private KeypressUI m_Keypress = null;
		[SerializeField] private LinkerUI m_Linker = null;
		[Header("Data")]
		[SerializeField] private TextSpriteSheet m_TextSheet = null;
		[SerializeField] private Text[] m_LanguageTexts = null;
		[SerializeField] private CursorData[] m_Cursors = null;

		// Saving
		private SavingBool SoloOnEditMotion = new SavingBool("StagerStudio.SoloOnEditMotion", true);

		// Data
		private bool WillUndo = false;
		private bool WillRedo = false;


		#endregion




		#region --- MSG ---


		private void Awake () {

			Awake_Message();
			Awake_Quit();
			Awake_Setting();
			Awake_Setting_UI_Input();
			Awake_Setting_UI_Toggle();
			Awake_Setting_UI_Slider();
			Awake_Menu();
			Awake_Object();
			Awake_Project();
			Awake_Game();
			Awake_Music();
			Awake_Sfx();
			Awake_Editor();
			Awake_Skin();
			Awake_Undo();
			Awake_Gene();
			Awake_Command();
			Awake_ProjectInfo();
			Awake_InspectorUI();
			Awake_Inspector();
			Awake_Home();
			Awake_Progress();
			Awake_SkinEditor();
			Awake_Selector();
			Awake_Linker();
			Awake_Misc();

		}


		private void Start () {
			LoadAllSettings();
			UI_RemoveUI();
			DebugLog_Start();
			if (Screen.fullScreen) { Screen.fullScreen = false; }
			QualitySettings.vSyncCount = 0;
			m_EasterEgg.Workspace = m_Project.Workspace;
			m_Background.SetBackground(null, false);
			m_SelectBrushMark.gameObject.TrySetActive(m_Editor.SelectingBrushIndex == -1);
			m_EraseBrushMark.gameObject.TrySetActive(m_Editor.SelectingBrushIndex == -2);
			m_GridRenderer.SetSortingLayer(SortingLayer.NameToID("Gizmos"), 0);
			m_VersionLabel.text = $"v{Application.version}";
			m_TextSheet.Init();
		}


		private void Update () {

			var dropSpeed = m_Game.GameDropSpeed;
			var map = m_Project.Beatmap;
			float musicTime = m_Music.Time;
			CursorUI.GlobalUpdate();
			StageObject.ZoneMinMax = ObjectTimer.ZoneMinMax = m_Zone.GetZoneMinMax();
			StageObject.Abreast = (m_Game.AbreastIndex, m_Game.AbreastValue, m_Game.AbreastWidth);
			StageObject.ScreenZoneMinMax = m_Zone.GetScreenZoneMinMax();
			StageObject.MusicTime = musicTime;
			StageObject.ShowGridOnPlay = m_Game.ShowGridTimerOnPlay.Value;
			StageObject.Solo = (
				SoloOnEditMotion.Value && m_MotionPainter.ItemType >= 0,
				m_MotionPainter.ItemType == 0 ? m_MotionPainter.ItemIndex :
				map != null ? map.GetParentIndex(1, m_MotionPainter.ItemIndex) : -1,
				m_MotionPainter.ItemType == 1 ? m_MotionPainter.ItemIndex : -1
			);
			Object.Stage.StageCount = m_Game.GetItemCount(0);
			Note.CameraWorldPos = m_CameraTF.position;
			TimingNote.ZoneMinMax = m_Zone.GetZoneMinMax(true);
			TimingNote.GameSpeedMuti = dropSpeed;
			TimingNote.MusicTime = musicTime;
			ObjectTimer.MusicTime = musicTime;
			ObjectTimer.SpeedMuti = dropSpeed;
			// Undo
			if (!Input.anyKey) {
				UndoRedo.RegisterUndoIfDirty();
			}
			if (WillUndo) {
				UndoRedo.Undo();
				WillUndo = false;
			}
			if (WillRedo) {
				UndoRedo.Redo();
				WillRedo = false;
			}
		}


		private void OnApplicationQuit () {
			DebugLog.CloseLogStream();
		}


		private void Awake_Message () {
			// Language
			StageProject.GetLanguage = m_Language.Get;
			StageGame.GetLanguage = m_Language.Get;
			StageMenu.GetLanguage = m_Language.Get;
			StageState.GetLanguage = m_Language.Get;
			StageSkin.GetLanguage = m_Language.Get;
			DialogUtil.GetLanguage = m_Language.Get;
			HomeUI.GetLanguage = m_Language.Get;
			ProjectInfoUI.GetLanguage = m_Language.Get;
			TooltipUI.GetLanguage = m_Language.Get;
			ColorPickerUI.GetLanguage = m_Language.Get;
			DialogUI.GetLanguage = m_Language.Get;
			TweenEditorUI.GetLanguage = m_Language.Get;
			ProjectCreatorUI.GetLanguage = m_Language.Get;
			SkinEditorUI.GetLanguage = m_Language.Get;
			SettingUI.GetLanguage = m_Language.Get;
			StageEditor.GetLanguage = m_Language.Get;
			StageInspector.GetLanguage = m_Language.Get;
			CommandUI.GetLanguage = m_Language.Get;
			TimingInspectorUI.GetLanguage = m_Language.Get;
			NoteInspectorUI.GetLanguage = m_Language.Get;
			// Misc
			TooltipUI.SetTip = (tip) => {
				m_TipLabelA.text = tip;
				m_TipLabelB.text = tip;
			};
			StageProject.LogHint = LogHint;
			StageGame.LogHint = LogHint;
			StageEditor.LogHint = LogHint;
			StageLanguage.OnLanguageLoaded = () => {
				TryRefreshSetting();
				foreach (var text in m_LanguageTexts) {
					if (text != null) {
						text.text = m_Language.Get(text.name);
					}
				}
			};
			DialogUtil.GetRoot = () => m_DialogRoot;
			DialogUtil.GetPrefab = () => m_DialogPrefab;
			TooltipUI.GetHotKey = m_Shortcut.GetHotkeyLabel;
			DebugLog.Init(Util.CombinePaths(Util.GetParentPath(Application.dataPath), "Log"));

		}


		private void Awake_Quit () {
			bool willQuit = false;
			Application.wantsToQuit += () => {
#if UNITY_EDITOR
				if (UnityEditor.EditorApplication.isPlaying) { return true; }
#endif
				if (willQuit) {
					return true;
				} else {
					DialogUtil.Dialog_OK_Cancel(UI_QuitConfirm, DialogUtil.MarkType.Info, () => {
						willQuit = true;
						Application.Quit();
					});
					return false;
				}
			};
			// Reload Language Texts
			foreach (var text in m_LanguageTexts) {
				if (text != null) {
					text.text = m_Language.Get(text.name);
				}
			}
		}


		private void Awake_Menu () {
			// Grid
			m_Menu.AddCheckerFunc("Menu.Grid.x00", () => m_Game.GridCountIndex_X0 == 0);
			m_Menu.AddCheckerFunc("Menu.Grid.x01", () => m_Game.GridCountIndex_X0 == 1);
			m_Menu.AddCheckerFunc("Menu.Grid.x02", () => m_Game.GridCountIndex_X0 == 2);

			m_Menu.AddCheckerFunc("Menu.Grid.x10", () => m_Game.GridCountIndex_X1 == 0);
			m_Menu.AddCheckerFunc("Menu.Grid.x11", () => m_Game.GridCountIndex_X1 == 1);
			m_Menu.AddCheckerFunc("Menu.Grid.x12", () => m_Game.GridCountIndex_X1 == 2);

			m_Menu.AddCheckerFunc("Menu.Grid.x20", () => m_Game.GridCountIndex_X2 == 0);
			m_Menu.AddCheckerFunc("Menu.Grid.x21", () => m_Game.GridCountIndex_X2 == 1);
			m_Menu.AddCheckerFunc("Menu.Grid.x22", () => m_Game.GridCountIndex_X2 == 2);

			m_Menu.AddCheckerFunc("Menu.Grid.y0", () => m_Game.GridCountIndex_Y == 0);
			m_Menu.AddCheckerFunc("Menu.Grid.y1", () => m_Game.GridCountIndex_Y == 1);
			m_Menu.AddCheckerFunc("Menu.Grid.y2", () => m_Game.GridCountIndex_Y == 2);
			// Auto Save
			m_Menu.AddCheckerFunc("Menu.AutoSave.0", () => Mathf.Abs(m_Project.UI_AutoSaveTime - 30f) < 1f);
			m_Menu.AddCheckerFunc("Menu.AutoSave.1", () => Mathf.Abs(m_Project.UI_AutoSaveTime - 120f) < 1f);
			m_Menu.AddCheckerFunc("Menu.AutoSave.2", () => Mathf.Abs(m_Project.UI_AutoSaveTime - 300f) < 1f);
			m_Menu.AddCheckerFunc("Menu.AutoSave.3", () => Mathf.Abs(m_Project.UI_AutoSaveTime - 600f) < 1f);
			m_Menu.AddCheckerFunc("Menu.AutoSave.Off", () => m_Project.UI_AutoSaveTime < 0f);
			// Beat per Section
			m_Menu.AddCheckerFunc("Menu.Grid.bps0", () => m_Game.BeatPerSection == 2);
			m_Menu.AddCheckerFunc("Menu.Grid.bps1", () => m_Game.BeatPerSection == 3);
			m_Menu.AddCheckerFunc("Menu.Grid.bps2", () => m_Game.BeatPerSection == 4);
			// Abreast Width
			m_Menu.AddCheckerFunc("Menu.Abreast.Width0", () => m_Game.AbreastWidthIndex == 0);
			m_Menu.AddCheckerFunc("Menu.Abreast.Width1", () => m_Game.AbreastWidthIndex == 1);
			m_Menu.AddCheckerFunc("Menu.Abreast.Width2", () => m_Game.AbreastWidthIndex == 2);
			m_Menu.AddCheckerFunc("Menu.Abreast.Width3", () => m_Game.AbreastWidthIndex == 3);
			// Command
			m_Menu.AddCheckerFunc("Menu.Command.Target.None", () => CommandUI.TargetIndex == 0);
			m_Menu.AddCheckerFunc("Menu.Command.Target.Stage", () => CommandUI.TargetIndex == 1);
			m_Menu.AddCheckerFunc("Menu.Command.Target.Track", () => CommandUI.TargetIndex == 2);
			m_Menu.AddCheckerFunc("Menu.Command.Target.TrackInside", () => CommandUI.TargetIndex == 3);
			m_Menu.AddCheckerFunc("Menu.Command.Target.Note", () => CommandUI.TargetIndex == 4);
			m_Menu.AddCheckerFunc("Menu.Command.Target.NoteInside", () => CommandUI.TargetIndex == 5);
			m_Menu.AddCheckerFunc("Menu.Command.Target.Timing", () => CommandUI.TargetIndex == 6);
			m_Menu.AddCheckerFunc("Menu.Command.Command.None", () => CommandUI.CommandIndex == 0);
			m_Menu.AddCheckerFunc("Menu.Command.Command.Time", () => CommandUI.CommandIndex == 1);
			m_Menu.AddCheckerFunc("Menu.Command.Command.TimeAdd", () => CommandUI.CommandIndex == 2);
			m_Menu.AddCheckerFunc("Menu.Command.Command.X", () => CommandUI.CommandIndex == 3);
			m_Menu.AddCheckerFunc("Menu.Command.Command.XAdd", () => CommandUI.CommandIndex == 4);
			m_Menu.AddCheckerFunc("Menu.Command.Command.Width", () => CommandUI.CommandIndex == 5);
			m_Menu.AddCheckerFunc("Menu.Command.Command.WidthAdd", () => CommandUI.CommandIndex == 6);
			m_Menu.AddCheckerFunc("Menu.Command.Command.Delete", () => CommandUI.CommandIndex == 7);
			// Default BG
			m_Menu.AddCheckerFunc("Menu.Background.Default.0", () => m_Background.DefaultIndex == 0);
			m_Menu.AddCheckerFunc("Menu.Background.Default.1", () => m_Background.DefaultIndex == 1);
			m_Menu.AddCheckerFunc("Menu.Background.Default.2", () => m_Background.DefaultIndex == 2);
			m_Menu.AddCheckerFunc("Menu.Background.Default.3", () => m_Background.DefaultIndex == 3);
			m_Menu.AddCheckerFunc("Menu.Background.Default.Random", () => m_Background.DefaultIndex < 0);
		}


		private void Awake_Object () {
			StageObject.TweenEvaluate = (x, index) => index < 0 ? 0f : m_Project.Tweens[Mathf.Min(index, Mathf.Max(0, m_Project.Tweens.Count - 1))].Evaluate(x);
			StageObject.PaletteColor = (index) => index < 0 ? new Color32(0, 0, 0, 0) : m_Project.Palette[Mathf.Min(index, Mathf.Max(0, m_Project.Palette.Count - 1))];
			StageObject.Shader_MaterialZoneID = Shader.PropertyToID("_ZoneMinMax");
			StageObject.Shader_ClampAlphaID = Shader.PropertyToID("_ClampAlpha");
			Note.GetFilledTime = m_Game.FillTime;
			Note.GetDropSpeedAt = m_Game.GetDropSpeedAt;
			Note.GetGameDropOffset = (id, muti) => m_Game.AreaBetween(id, 0f, m_Music.Time, muti);
			Note.GetDropOffset = (id, time, muti) => m_Game.AreaBetween(id, 0f, time, muti);
			Note.PlayClickSound = m_Music.PlayClickSound;
			Note.PlaySfx = m_SoundFX.PlayFX;
			TimingNote.PlaySfx = m_SoundFX.PlayFX;
			MotionItem.GetZoneMinMax = () => m_Zone.GetZoneMinMax();
			MotionItem.GetBeatmap = () => m_Project.Beatmap;
			MotionItem.GetMusicTime = () => m_Music.Time;
			MotionItem.OnMotionChanged = m_MotionPainter.RefreshFieldUI;
			MotionItem.GetSpeedMuti = () => m_Game.GameDropSpeed;
			MotionItem.GetGridEnabled = () => m_Game.ShowGrid;
			// Sorting Layer ID
			StageObject.SortingLayerID_Gizmos = SortingLayer.NameToID("Gizmos");
			Object.Stage.SortingLayerID_Stage = SortingLayer.NameToID("Stage");
			Object.Stage.SortingLayerID_Judge = SortingLayer.NameToID("Judge");
			Track.SortingLayerID_TrackTint = SortingLayer.NameToID("TrackTint");
			Track.SortingLayerID_Track = SortingLayer.NameToID("Track");
			Track.SortingLayerID_Tray = SortingLayer.NameToID("Tray");
			Note.SortingLayerID_Pole_Front = SortingLayer.NameToID("Pole Front");
			Note.SortingLayerID_Pole_Back = SortingLayer.NameToID("Pole Back");
			Note.SortingLayerID_Note_Hold = SortingLayer.NameToID("HoldNote");
			Note.SortingLayerID_Note = SortingLayer.NameToID("Note");
			TimingNote.SortingLayerID_UI = SortingLayer.NameToID("UI");
			Luminous.SortingLayerID_Lum = SortingLayer.NameToID("Luminous");
			Note.LayerID_Note = LayerMask.NameToLayer("Note");
			Note.LayerID_Note_Hold = LayerMask.NameToLayer("HoldNote");
		}


		private void Awake_Project () {

			// Project
			StageProject.OnProjectLoadingStart = () => {
				m_Music.SetClip(null);
				m_SoundFX.SetClip(null);
				m_Game.SetSpeedCurveDirty();
				m_Preview.SetDirty();
				UI_RemoveUI();
				UndoRedo.ClearUndo();
				StageObject.Beatmap = TimingNote.Beatmap = ObjectTimer.Beatmap = null;
				m_Game.SetAbreastIndex(0);
				m_Game.SetUseAbreastView(false);
				m_Game.SetGameDropSpeed(1f);
				m_Inspector.RefreshUI();
				m_EasterEgg.CheckEasterEggs();
			};
			StageProject.OnProjectLoaded = () => {
				m_Game.SetSpeedCurveDirty();
				m_Music.Pitch = 1f;
				m_Music.Seek(0f);
				m_Gene.RefreshGene();
				UI_RemoveUI();
				RefreshLoading(-1f);
				DebugLog_Project("Loaded");
			};
			StageProject.OnProjectSavingStart = () => {
				StartCoroutine(SaveProgressing());
			};
			StageProject.OnProjectClosed = () => {
				m_Game.SetSpeedCurveDirty();
				m_Game.SetUseAbreastView(false);
				m_Game.SetGameDropSpeed(1f);
				m_Music.Pitch = 1f;
				m_Music.SetClip(null);
				m_SoundFX.SetClip(null);
				UndoRedo.ClearUndo();
				StageObject.Beatmap = TimingNote.Beatmap = ObjectTimer.Beatmap = null;
				m_Preview.SetDirty();
				m_Editor.ClearSelection();
				m_Inspector.RefreshUI();
				UI_RemoveUI();
			};

			// Beatmap
			StageProject.OnBeatmapOpened = (map, key) => {
				if (map != null) {
					m_Game.BPM = map.BPM;
					m_Game.Shift = map.Shift;
					m_Game.Ratio = map.Ratio;
					m_BeatmapSwiperLabel.text = map.Tag;
					StageObject.Beatmap = TimingNote.Beatmap = ObjectTimer.Beatmap = map;
				}
				TryRefreshProjectInfo();
				RefreshLoading(-1f);
				m_Editor.ClearSelection();
				m_Game.SetSpeedCurveDirty();
				m_Game.ClearAllContainers();
				m_Music.Pitch = 1f;
				m_Music.Seek(0f);
				m_Gene.FixMapInfoFromGene();
				m_Gene.FixMapDataFromGene();
				UndoRedo.ClearUndo();
				UndoRedo.SetDirty();
				m_Preview.SetDirty();
				RefreshGridRenderer();

				Note.SetCacheDirty();
				TimingNote.SetCacheDirty();
				ItemRenderer.SetGlobalDirty();
				m_Project.SetDirty();
				m_TimingPreview.SetDirty();
				m_Game.ForceUpdateZone();
				m_Linker.StopLinker();

				m_Inspector.RefreshUI();
				DebugLog_Beatmap("Open");
				Resources.UnloadUnusedAssets();
			};
			StageProject.OnBeatmapRemoved = () => {
				TryRefreshProjectInfo();
				m_Inspector.RefreshUI();
			};
			StageProject.OnBeatmapCreated = () => {
				TryRefreshProjectInfo();
				m_Inspector.RefreshUI();
			};

			// Assets
			StageProject.OnMusicLoaded = (clip) => {
				m_Music.SetClip(clip);
				m_SoundFX.SetClip(clip);
				TryRefreshProjectInfo();
				m_Wave.LoadWave(clip);
				m_Music.Pitch = 1f;
				m_Music.Seek(0f);
			};
			StageProject.OnBackgroundLoaded = (sprite) => {
				try {
					m_Background.SetBackground(sprite);
				} catch { }
				TryRefreshProjectInfo();

			};
			StageProject.OnCoverLoaded = (sprite) => {
				TryRefreshProjectInfo();

			};
			StageProject.OnClickSoundsLoaded = (clips) => {
				m_Music.SetClickSounds(clips);
				TryRefreshProjectInfo();

			};

			// Misc
			StageProject.OnDirtyChanged = m_DirtyMark.gameObject.SetActive;
			StageProject.OnLoadProgress = RefreshLoading;
			StageProject.OnException = (ex) => DebugLog_Exception("Project", ex);

			// Func
			IEnumerator SaveProgressing () {
				float pg = 0f;
				m_Hint.SetProgress(0f);
				while (m_Project.SavingProject) {
					yield return new WaitUntil(() => m_Project.SavingProgress != pg);
					pg = m_Project.SavingProgress;
					m_Hint.SetProgress(pg);
				}
				m_Hint.SetProgress(-1f);
			}

		}


		private void Awake_Game () {
			StageGame.OnItemCountChanged = () => {
				m_Preview.SetDirty();
				m_TimingPreview.SetDirty();
				m_Editor.ClearSelection();
			};
			StageGame.OnAbreastChanged = () => {
				m_AbreastTGMark.enabled = m_Game.UseAbreast;
				m_Wave.SetAlpha(m_Game.AbreastValue);
				m_TimingPreview.SetDirty();
				if (m_Editor.SelectingBrushIndex != -1) {
					m_Editor.SetBrush(-1);
				}
				if (m_Editor.SelectingItemIndex != -1) {
					m_Editor.ClearSelection();
				}
				Note.SetCacheDirty();
				TimingNote.SetCacheDirty();
			};
			StageGame.OnDropSpeedChanged = () => {
				m_Wave.Length01 = 1f / m_Game.GameDropSpeed / m_Music.Duration;
				m_TimingPreview.SetDirty();
				Note.SetCacheDirty();
				TimingNote.SetCacheDirty();
				RefreshGridRenderer();
				m_DropSpeedSlider.value = Mathf.Clamp(m_Game.GameDropSpeed * 10f, m_DropSpeedSlider.minValue, m_DropSpeedSlider.maxValue);
			};
			StageGame.OnSpeedCurveChanged = () => {
				m_TimingPreview.SetDirty();
				Note.SetCacheDirty();
				TimingNote.SetCacheDirty();
				RefreshGridRenderer();
			};
			StageGame.OnGridChanged = () => {
				m_GridTG.isOn = m_Game.ShowGrid;
				m_GridRenderer.SetShow(m_Game.ShowGrid);
				m_TimingPreview.SetDirty();
				Track.BeatPerSection = m_Game.BeatPerSection;
				StageObject.ShowGrid = m_Game.ShowGrid;
				RefreshGridRenderer();
			};
			StageGame.OnRatioChanged = (ratio) => {
				m_Zone.SetFitterRatio(ratio);
				m_TimingPreview.SetDirty();
				RefreshGridRenderer();
				m_Wave.Length01 = 1f / m_Game.GameDropSpeed / m_Music.Duration;
			};
			StageGame.GetBeatmap = () => m_Project.Beatmap;
			StageGame.GetMusicTime = () => m_Music.Time;
			StageGame.MusicSeek = m_Music.Seek;
			StageGame.MusicIsPlaying = () => m_Music.IsPlaying;
			StageGame.GetPitch = () => m_Music.Pitch;
			StageGame.SetPitch = (p) => m_Music.Pitch = p;
			StageGame.MusicPlay = m_Music.Play;
			StageGame.MusicPause = m_Music.Pause;
			StageGame.GetItemLock = m_Editor.GetItemLock;
			StageGame.OnException = (ex) => DebugLog_Exception("Game", ex);
		}


		private void Awake_Music () {
			StageMusic.OnMusicPlayPause = (playing) => {
				m_Progress.RefreshControlUI();
				m_TimingPreview.SetDirty();
				m_GridRenderer.MusicTime = m_Music.Time;
				StageObject.MusicPlaying = playing;
				TimingNote.MusicPlaying = playing;
				m_SoundFX.StopAllFx();
			};
			StageMusic.OnMusicTimeChanged = (time, duration) => {
				m_Progress.SetProgress(time);
				m_Wave.Time01 = time / duration;
				m_GridRenderer.MusicTime = time;
				m_TimingPreview.SetDirty();
				m_MotionPainter.TrySetDirty();
				StageObject.MusicDuration = duration;
			};
			StageMusic.OnMusicClipLoaded = () => {
				m_GridRenderer.MusicTime = m_Music.Time;
				m_Progress.RefreshControlUI();
				m_TimingPreview.SetDirty();
			};
			StageMusic.OnPitchChanged = () => {
				m_PitchWarningBlock.gameObject.SetActive(m_Music.Pitch < 0.05f);
				m_PitchTrebleClef.gameObject.SetActive(Mathf.Abs(m_Music.Pitch) >= 0.999f);
				m_PitchBassClef.gameObject.SetActive(!m_PitchTrebleClef.gameObject.activeSelf);
				m_SoundFX.StopAllFx();
			};
		}


		private void Awake_Sfx () {
			StageSoundFX.GetMusicPlaying = () => m_Music.IsPlaying;
			StageSoundFX.GetMusicTime = () => m_Music.Time;
			StageSoundFX.GetMusicVolume = () => SliderItemMap[SliderType.MusicVolume].saving.Value / 12f;
			StageSoundFX.SetMusicVolume = (volume) => m_Music.Volume = SliderItemMap[SliderType.MusicVolume].saving.Value / 12f * volume;
			StageSoundFX.GetMusicPitch = () => m_Music.Pitch;
			StageSoundFX.GetMusicMute = () => m_Music.Mute;
			StageSoundFX.SetMusicMute = (mute) => m_Music.Mute = mute;
			StageSoundFX.GetSecondPerBeat = () => m_Game.SPB;
			StageSoundFX.OnUseFxChanged = () => m_Music.UseMixer(m_SoundFX.UseFX);

		}


		private void Awake_Editor () {
			StageEditor.GetZoneMinMax = () => m_Zone.GetZoneMinMax();
			StageEditor.GetRealZoneMinMax = () => m_Zone.GetZoneMinMax(true);
			StageEditor.OnSelectionChanged = () => {
				m_Preview.SetDirty();
				m_Gene.RefreshInspector();
				m_Inspector.RefreshUI();
				UI_RemoveColorSelector();
				if (m_MotionPainter.ItemType >= 0) {
					if (m_Editor.SelectingItemType >= 0 && m_MotionPainter.ItemType == m_Editor.SelectingItemType) {
						m_MotionPainter.ItemIndex = m_Editor.SelectingItemIndex;
						m_MotionPainter.SetVerticesDirty();
					} else {
						m_Inspector.StopEditMotion(false);
					}
				}
			};
			StageEditor.OnBrushChanged = () => {
				m_SelectBrushMark.gameObject.TrySetActive(m_Editor.SelectingBrushIndex == -1);
				m_EraseBrushMark.gameObject.TrySetActive(m_Editor.SelectingBrushIndex == -2);
				m_Inspector.RefreshUI();
				m_Linker.StopLinker();
				m_Gene.RefreshBrushInspector(m_Editor.SelectingBrushIndex);
			};
			StageEditor.OnLockEyeChanged = () => {
				m_Editor.SetSelection(m_Editor.SelectingItemType, m_Editor.SelectingItemIndex, m_Editor.SelectingItemSubIndex);
				m_Editor.SetBrush(m_Editor.SelectingBrushIndex);
				m_TimingPreview.SetDirty();
				m_Linker.StopLinker();
			};
			StageEditor.OnSettingChanged = () => {
				m_SnapTG.isOn = m_Editor.UseMagnetSnap.Value;
			};
			StageEditor.GetBeatmap = () => m_Project.Beatmap;
			StageEditor.GetEditorActive = () =>
				m_Project.Beatmap != null &&
				!m_Music.IsPlaying &&
				!m_MotionInspector.gameObject.activeSelf;
			StageEditor.GetUseDynamicSpeed = () => m_Game.UseDynamicSpeed;
			StageEditor.GetUseAbreast = () => m_Game.UseAbreast;
			StageEditor.GetMoveAxisHovering = m_Axis.GetEntering;
			StageEditor.BeforeObjectEdited = (editType, itemType, itemIndex) => {
				if (editType == StageEditor.EditType.Delete) {
					m_Effect.SpawnDeleteEffect(itemType, itemIndex);
				}
				m_Linker.StopLinker();
			};
			StageEditor.OnObjectEdited = (editType, itemType, itemIndex) => {
				Note.SetCacheDirty();
				TimingNote.SetCacheDirty();
				ItemRenderer.SetGlobalDirty();
				m_Game.SetSpeedCurveDirty();
				m_Project.SetDirty();
				m_Preview.SetDirty();
				m_TimingPreview.SetDirty();
				m_Game.ForceUpdateZone();
				m_Linker.StopLinker();

				UndoRedo.SetDirty();
				m_Inspector.RefreshAllInspectors();
				m_MotionPainter.TrySetDirty();
				switch (editType) {
					case StageEditor.EditType.Create:
						m_Gene.FixItemFromGene(itemType, itemIndex);
						m_Effect.SpawnCreateEffect(itemType, itemIndex);
						break;
					case StageEditor.EditType.Modify:
						m_Gene.FixItemFromGene(itemType, itemIndex);
						break;
					case StageEditor.EditType.Delete:
						if (itemType == 1) {
							m_Gene.FixAllStagesFromGene();
						}
						break;
				}
			};
			StageEditor.GetFilledTime = m_Game.FillTime;
			StageEditor.SetAbreastIndex = m_Game.SetAbreastIndex;
			StageEditor.LogAxisMessage = m_Axis.LogAxisMessage;
			StageEditor.GetMusicTime = () => m_Music.Time;
			StageEditor.GetMusicDuration = () => m_Music.Duration;
			StageEditor.GetSnapedTime = m_Game.SnapTime;
			StageEditor.OnException = (ex) => DebugLog_Exception("Editor", ex);
			StageEditor.FixBrushIndexFromGene = m_Gene.FixBrushIndexFromGene;
			StageEditor.FixContainerFromGene = m_Gene.FixContainerFromGene;
			StageEditor.FixLockFromGene = m_Gene.FixLockFromGene;
			StageEditor.FixGhostSizeFromGene = m_Gene.FixGhostSizeFromGene;
			StageEditor.FixEditorAxis = m_Gene.FixEditorAxis;
			StageEditor.CheckTileTrack = m_Gene.CheckTileTrack;
		}


		private void Awake_Skin () {
			StageSkin.OnSkinLoaded = (data) => {
				TryRefreshSetting();
				StageObject.LoadSkin(data);
				Luminous.SetLuminousSkin(data);
				Resources.UnloadUnusedAssets();
				TypeSelectorUI.CalculateSprites(data);
				m_Game.ClearAllContainers();
				m_SkinSwiperLabel.text = !string.IsNullOrEmpty(StageSkin.Data.Name) ? StageSkin.Data.Name : "--";
				StageInspector.TypeCount = (
					Mathf.Max(data.TryGetItemCount(SkinType.Stage), data.TryGetItemCount(SkinType.JudgeLine)),
					Mathf.Max(data.TryGetItemCount(SkinType.Track), data.TryGetItemCount(SkinType.TrackTint)),
					Mathf.Max(data.TryGetItemCount(SkinType.Note), data.TryGetItemCount(SkinType.Pole))
				);
			};
			StageSkin.OnSkinDeleted = () => {
				TryRefreshSetting();
				m_Game.ClearAllContainers();
			};
		}


		private void Awake_Undo () {
			UndoRedo.GetStepData = () => new UndoData(
				m_Project.Beatmap,
				m_Project.Palette,
				m_Project.Tweens,
				m_Editor.SelectingItemType,
				m_Editor.SelectingItemIndex,
				m_Editor.GetContainerActive(0),
				m_Editor.GetContainerActive(1),
				m_Editor.GetContainerActive(2),
				m_Editor.GetContainerActive(3)
			);
			UndoRedo.OnUndoRedo = (stepObj) => {
				var step = stepObj as UndoData;
				if (step == null || step.Map == null) { return; }
				// Map
				m_Project.Beatmap.LoadFromOtherMap(step.Map);
				// Project
				m_Project.Tweens.Clear();
				m_Project.Palette.Clear();
				m_Project.Tweens.AddRange(step.GetTweens());
				m_Project.Palette.AddRange(step.GetPalette());
				m_Project.SetDirty();
				// Selection
				m_Editor.SetSelection(
					step.SelectingItemType,
					step.SelectingItemIndex
				);
				// Container Active
				m_Editor.SetContainerActive(0, step.ContainerActive_0);
				m_Editor.SetContainerActive(1, step.ContainerActive_1);
				m_Editor.SetContainerActive(2, step.ContainerActive_2);
				m_Editor.SetContainerActive(3, step.ContainerActive_3);
				// Final
				Note.SetCacheDirty();
				TimingNote.SetCacheDirty();
				ItemRenderer.SetGlobalDirty();
				m_Game.SetSpeedCurveDirty();
				m_Preview.SetDirty();
				m_TimingPreview.SetDirty();
				m_Game.ForceUpdateZone();
				m_Linker.StopLinker();
				TryRefreshProjectInfo();
				LogHint_Key(Hint_Undo);
			};
		}


		private void Awake_Gene () {
			StageGene.GetGene = () => m_Project.Gene;
			StageGene.GetBeatmap = () => m_Project.Beatmap;
			StageGene.SetContainerActive = m_Editor.SetContainerActive;
			StageGene.SetUseLock = m_Editor.SetLock;
			StageGene.GetMusicDuration = () => m_Music.Duration;
			StageGene.GetSelectingItemType = () => m_Editor.SelectingItemType;
			StageGene.GetSelectingItemIndex = () => m_Editor.SelectingItemIndex;
		}


		private void Awake_Command () {
			StageCommand.OnCommandDone = () => {
				Note.SetCacheDirty();
				TimingNote.SetCacheDirty();
				ItemRenderer.SetGlobalDirty();
				m_Game.SetSpeedCurveDirty();
				m_Project.SetDirty();
				m_Preview.SetDirty();
				m_TimingPreview.SetDirty();
				m_Game.ForceUpdateZone();
				m_Linker.StopLinker();
				m_Gene.FixMapDataFromGene();
				UndoRedo.SetDirty();
			};

			// UI
			CommandUI.DoCommand = (type, command, index, value) => {
				bool success = StageCommand.DoCommand(
					m_Project.Beatmap,
					(StageCommand.TargetType)type,
					(StageCommand.CommandType)command,
					index, value
				);
				if (success) {
					LogHint_Key(Hint_CommandDone);
				}
			};
			CommandUI.OpenMenu = m_Menu.OpenMenu;

		}


		private void Awake_Inspector () {

			// Inspector
			StageInspector.GetSelectingBrush = () => m_Editor.SelectingBrushIndex;
			StageInspector.GetSelectingType = () => {
				if (m_Editor.SelectingItemType == 4) {
					return 0;
				}
				if (m_Editor.SelectingItemType == 5) {
					return 1;
				}
				return m_Editor.SelectingItemType;
			};
			StageInspector.GetSelectingIndex = () => m_Editor.SelectingItemIndex;
			StageInspector.GetBeatmap = () => m_Project.Beatmap;
			StageInspector.GetBPM = () => m_Game.BPM;
			StageInspector.GetShift = () => m_Game.Shift;
			StageInspector.OnItemEdited = () => {
				Note.SetCacheDirty();
				TimingNote.SetCacheDirty();
				ItemRenderer.SetGlobalDirty();
				m_Game.SetSpeedCurveDirty();
				m_Project.SetDirty();
				m_Preview.SetDirty();
				m_TimingPreview.SetDirty();
				m_Game.ForceUpdateZone();
				m_Linker.StopLinker();
				UI_RemoveColorSelector();
				UndoRedo.SetDirty();
			};
			StageInspector.OnBeatmapEdited = () => {
				if (m_Project.Beatmap != null) {
					m_Gene.FixMapInfoFromGene();
					m_Game.BPM = m_Project.Beatmap.BPM;
					m_Game.Shift = m_Project.Beatmap.Shift;
					m_Game.Ratio = m_Project.Beatmap.Ratio;
					m_BeatmapSwiperLabel.text = m_Project.Beatmap.Tag;
				}
				m_Inspector.RefreshUI();
				RefreshGridRenderer();
			};
			StageInspector.GetBrushInfo = (brushType) => {
				switch (brushType) {
					case 0:
						return (
							m_Editor.BrushConfig.StageWidth.Value,
							m_Editor.BrushConfig.StageHeight.Value,
							m_Editor.BrushConfig.StageType.Value
						);
					case 1:
						return (
							m_Editor.BrushConfig.TrackWidth.Value,
							null,
							m_Editor.BrushConfig.TrackType.Value
						);
					case 2:
						return (
							m_Editor.BrushConfig.NoteWidth.Value,
							null,
							m_Editor.BrushConfig.NoteType.Value
						);
				}
				return (null, null, null);
			};
			StageInspector.SetBrushInfo = (brushType, info) => {
				if (info.width.HasValue) {
					info.width = Mathf.Clamp(info.width.Value, 0.01f, 1f);
				}
				if (info.height.HasValue) {
					info.height = Mathf.Clamp(info.height.Value, 0.01f, 1f);
				}
				switch (brushType) {
					case 0:
						if (info.width.HasValue) {
							m_Editor.BrushConfig.StageWidth.Value = info.width.Value;
						}
						if (info.height.HasValue) {
							m_Editor.BrushConfig.StageHeight.Value = info.height.Value;
						}
						if (info.itemType.HasValue) {
							m_Editor.BrushConfig.StageType.Value = info.itemType.Value;
						}
						break;
					case 1:
						if (info.width.HasValue) {
							m_Editor.BrushConfig.TrackWidth.Value = info.width.Value;
						}
						if (info.itemType.HasValue) {
							m_Editor.BrushConfig.TrackType.Value = info.itemType.Value;
						}
						break;
					case 2:
						if (info.width.HasValue) {
							m_Editor.BrushConfig.NoteWidth.Value = info.width.Value;
						}
						if (info.itemType.HasValue) {
							m_Editor.BrushConfig.NoteType.Value = info.itemType.Value;
						}
						break;
				}
			};

		}


		// Awake UI 
		private void Awake_ProjectInfo () {

			ProjectInfoUI.MusicStopClickSounds = m_Music.StopClickSounds;
			ProjectInfoUI.MusicPlayClickSound = m_Music.PlayClickSound;
			ProjectInfoUI.OpenMenu = m_Menu.OpenMenu;
			ProjectInfoUI.ProjectImportPalette = m_Project.UI_ImportPalette;
			ProjectInfoUI.ProjectSetDirty = m_Project.SetDirty;
			ProjectInfoUI.ProjectNewBeatmap = m_Project.NewBeatmap;
			ProjectInfoUI.ProjectImportBeatmap = m_Project.UI_ImportBeatmap;
			ProjectInfoUI.ProjectAddPaletteColor = m_Project.UI_AddPaletteColor;
			ProjectInfoUI.ProjectExportPalette = m_Project.UI_ExportPalette;
			ProjectInfoUI.ProjectImportClickSound = m_Project.ImportClickSound;
			ProjectInfoUI.ProjectAddTween = m_Project.UI_AddTween;
			ProjectInfoUI.ProjectImportTween = m_Project.UI_ImportTween;
			ProjectInfoUI.ProjectExportTween = m_Project.UI_ExportTween;
			ProjectInfoUI.GetBeatmapMap = () => m_Project.BeatmapMap;
			ProjectInfoUI.ProjectSetPaletteColor = (color, index) => {
				m_Project.SetPaletteColor(color, index);
				UndoRedo.SetDirty();
			};
			ProjectInfoUI.GetProjectPalette = () => m_Project.Palette;
			ProjectInfoUI.GetProjectTweens = () => m_Project.Tweens;
			ProjectInfoUI.SetProjectTweenCurve = (curve, index) => {
				m_Project.SetTweenCurve(curve, index);
				UndoRedo.SetDirty();
			};
			ProjectInfoUI.GetProjectClickSounds = () => m_Project.ClickSounds;
			ProjectInfoUI.GetProjectInfo = () => (
				m_Project.ProjectName, m_Project.ProjectDescription,
				m_Project.BeatmapAuthor, m_Project.MusicAuthor, m_Project.BackgroundAuthor,
				m_Project.Background.sprite, m_Project.FrontCover.sprite, m_Project.Music.data
			);
			ProjectInfoUI.GetBeatmapKey = () => m_Project.BeatmapKey;
			ProjectInfoUI.ProjectImportBackground = m_Project.ImportBackground;
			ProjectInfoUI.ProjectImportCover = m_Project.ImportCover;
			ProjectInfoUI.ProjectImportMusic = m_Project.ImportMusic;
			ProjectInfoUI.ProjectRemoveBackground = m_Project.RemoveBackground;
			ProjectInfoUI.ProjectRemoveCover = m_Project.RemoveCover;
			ProjectInfoUI.ProjectRemoveMusic = m_Project.RemoveMusic;
			ProjectInfoUI.SetProjectInfo_Name = (name) => m_Project.ProjectName = name;
			ProjectInfoUI.SetProjectInfo_Description = (des) => m_Project.ProjectDescription = des;
			ProjectInfoUI.SetProjectInfo_BgAuthor = (author) => m_Project.BackgroundAuthor = author;
			ProjectInfoUI.SetProjectInfo_MusicAuthor = (author) => m_Project.MusicAuthor = author;
			ProjectInfoUI.SetProjectInfo_MapAuthor = (author) => m_Project.BeatmapAuthor = author;
			ProjectInfoUI.SpawnColorPicker = SpawnColorPicker;
			ProjectInfoUI.SpawnTweenEditor = SpawnTweenEditor;
			ProjectInfoUI.OnBeatmapInfoChanged = () => {
				if (m_Project.Beatmap != null) {
					m_Gene.FixMapInfoFromGene();
					m_Game.BPM = m_Project.Beatmap.BPM;
					m_Game.Shift = m_Project.Beatmap.Shift;
					m_Game.Ratio = m_Project.Beatmap.Ratio;
					m_BeatmapSwiperLabel.text = m_Project.Beatmap.Tag;
				}
				m_Inspector.RefreshUI();
				RefreshGridRenderer();
				Invoke(nameof(TryRefreshProjectInfo), 0.01f);
			};
			ProjectInfoUI.OnProjectInfoChanged = () => {
				TryRefreshProjectInfo();
			};
			ProjectInfoUI.GetProjectGeneKey = () => m_Project.Gene.Key;

		}


		private void Awake_Setting () {
			SettingUI.ResetAllSettings = () => {
				// Reset All
				foreach (var pair in InputItemMap) {
					pair.Value.saving.Reset();
				}
				foreach (var pair in ToggleItemMap) {
					pair.Value.saving.Reset();
				}
				foreach (var pair in SliderItemMap) {
					pair.Value.saving.Reset();
				}
				// Load All
				LoadAllSettings();
			};
			SettingUI.SkinRefreshAllSkinNames = m_Skin.RefreshAllSkinNames;
			SettingUI.LanguageGetDisplayName = m_Language.GetDisplayName;
			SettingUI.LanguageGetDisplayName_Language = m_Language.GetDisplayName;
			SettingUI.GetAllLanguages = () => m_Language.AllLanguages;
			SettingUI.GetAllSkinNames = () => m_Skin.AllSkinNames;
			SettingUI.GetSkinName = () => StageSkin.Data.Name;
			SettingUI.SkinLoadSkin = m_Skin.LoadSkin;
			SettingUI.SkinDeleteSkin = m_Skin.UI_DeleteSkin;
			SettingUI.SkinNewSkin = m_Skin.UI_NewSkin;
			SettingUI.OpenMenu = m_Menu.OpenMenu;
			SettingUI.ShortcutCount = () => m_Shortcut.Datas.Length;
			SettingUI.GetShortcutAt = (i) => {
				var data = m_Shortcut.Datas[i];
				return (data.Name, data.Key, data.Ctrl, data.Shift, data.Alt);
			};
			SettingUI.SaveShortcut = () => {
				m_Shortcut.SaveToFile();
				m_Shortcut.ReloadMap();
			};
			SettingUI.SetShortcut = (index, key, ctrl, shift, alt) => {
				var data = m_Shortcut.Datas[index];
				data.Key = key;
				data.Ctrl = ctrl;
				data.Shift = shift;
				data.Alt = alt;
				return index;
			};
			SettingUI.SpawnSkinEditor = SpawnSkinEditor;
			SettingUI.LoadLanguage = m_Language.LoadLanguage;

		}


		private void Awake_InspectorUI () {

			// Motion 
			MotionPainterUI.GetBeatmap = () => m_Project.Beatmap;
			MotionPainterUI.GetMusicTime = () => m_Music.Time;
			MotionPainterUI.GetMusicDuration = () => m_Music.Duration;
			MotionPainterUI.GetBPM = () => m_Game.BPM;
			MotionPainterUI.GetBeatPerSection = () => m_Game.BeatPerSection.Value;
			MotionPainterUI.OnItemEdit = () => {
				Note.SetCacheDirty();
				TimingNote.SetCacheDirty();
				ItemRenderer.SetGlobalDirty();
				m_Game.SetSpeedCurveDirty();
				m_Project.SetDirty();
				m_Preview.SetDirty();
				m_TimingPreview.SetDirty();
				m_Game.ForceUpdateZone();
				m_Linker.StopLinker();
				UndoRedo.SetDirty();
			};
			MotionPainterUI.OnSelectionChanged = () => {
				UI_RemoveTweenSelector();
				UI_RemoveColorSelector();
			};
			MotionPainterUI.GetSprite = m_TextSheet.Char_to_Sprite;
			MotionPainterUI.GetPaletteCount = () => m_Project.Palette.Count;
			MotionPainterUI.SeekMusic = m_Music.Seek;

			TweenSelectorUI.GetProjectTweens = () => m_Project.Tweens;
			TweenSelectorUI.TrySetCurrentTween = m_MotionPainter.TrySetCurrentTween;

			ColorSelectorUI.GetPalette = () => m_Project.Palette;
			ColorSelectorUI.TrySetCurrentColor = m_Inspector.SetCurrentColor;

		}


		private void Awake_Home () {
			HomeUI.GotoEditor = m_State.GotoEditor;
			HomeUI.GetWorkspace = () => m_Project.Workspace;
			HomeUI.OpenMenu = m_Menu.OpenMenu;
			HomeUI.SpawnProjectCreator = UI_SpawnProjectCreator;
			HomeUI.OnException = (ex) => DebugLog_Exception("Home", ex);
		}


		private void Awake_Progress () {
			ProgressUI.GetSnapTime = (time, step) => m_Game.SnapTime(time, step);
			ProgressUI.GetDuration = () => m_Music.Duration;
			ProgressUI.GetReadyPlay = () => (m_Music.IsReady, m_Music.IsPlaying);
			ProgressUI.PlayMusic = m_Music.Play;
			ProgressUI.PauseMusic = m_Music.Pause;
			ProgressUI.SeekMusic = m_Music.Seek;
			ProgressUI.GetBPM = () => m_Game.BPM;
			ProgressUI.GetShift = () => m_Game.Shift;
		}


		private void Awake_SkinEditor () {
			SkinEditorUI.MusicSeek_Add = (add) => m_Music.Seek(m_Music.Time + add);
			SkinEditorUI.SkinReloadSkin = m_Skin.ReloadSkin;
			SkinEditorUI.OpenMenu = m_Menu.OpenMenu;
			SkinEditorUI.SkinGetPath = m_Skin.GetPath;
			SkinEditorUI.SkinSaveSkin = m_Skin.SaveSkin;
			SkinEditorUI.SpawnSetting = UI_SpawnSetting;
			SkinEditorUI.RemoveUI = UI_RemoveUI;
		}


		private void Awake_Selector () {
			SelectorUI.GetBeatmap = () => m_Project.Beatmap;
			SelectorUI.SelectStage = (index) => {
				m_Editor.SetSelection(0, index);
				if (m_Game.UseAbreast) {
					m_Game.SetAbreastIndex(index);
				}
				var map = m_Project.Beatmap;
				if (map != null && !map.GetActive(0, index)) {
					m_Music.Seek(map.GetTime(0, index));
				}
			};
			SelectorUI.SelectTrack = (index) => {
				m_Editor.SetSelection(1, index);
				var map = m_Project.Beatmap;
				if (map != null && !map.GetActive(1, index)) {
					m_Music.Seek(map.GetTime(1, index));
				}
			};
			SelectorUI.OpenItemMenu = (rt, type) => m_Menu.OpenMenu(type == 0 ? UI_SelectorStageMenu : UI_SelectorTrackMenu, rt);
			SelectorUI.GetSelectionType = () => m_Editor.SelectingItemType;
			SelectorUI.GetSelectionIndex = () => m_Editor.SelectingItemIndex;
		}


		private void Awake_Linker () {
			LinkerUI.GetBeatmap = () => m_Project.Beatmap;
			LinkerUI.GetSelectingIndex = () => m_Editor.SelectingItemIndex;
			LinkerUI.OnLink = (noteIndex) => {
				Note.SetCacheDirty();
				TimingNote.SetCacheDirty();
				ItemRenderer.SetGlobalDirty();
				m_Game.SetSpeedCurveDirty();
				m_Project.SetDirty();
				m_Preview.SetDirty();
				m_TimingPreview.SetDirty();
				m_Game.ForceUpdateZone();
				m_Linker.StopLinker();
				m_Gene.FixItemFromGene(2, noteIndex);
				UndoRedo.SetDirty();
				m_Inspector.RefreshAllInspectors();
			};
			LinkerUI.AllowLinker = () =>
				m_Project.Beatmap != null &&
				!m_Music.IsPlaying &&
				!m_MotionInspector.gameObject.activeSelf &&
				!m_Root.gameObject.activeSelf &&
				m_Editor.SelectingItemType == 2 &&
				m_Gene.FixAllowNoteLink(m_Editor.SelectingItemIndex);
		}


		// Awake Misc
		private void Awake_Misc () {

			CursorUI.GetCursorTexture = (index) => (
				index >= 0 ? m_Cursors[index].Cursor : null,
				index >= 0 ? m_Cursors[index].Offset : Vector2.zero
			);

			GridRenderer.GetAreaBetween = (id, timeA, timeB, muti, ignoreDy) => ignoreDy ? Mathf.Abs(timeA - timeB) * muti : m_Game.AreaBetween(id, timeA, timeB, muti);
			GridRenderer.GetSnapedTime = m_Game.SnapTime;

			TrackSectionRenderer.GetAreaBetween = m_Game.AreaBetween;
			TrackSectionRenderer.GetSnapedTime = m_Game.SnapTime;

			BeatmapSwiperUI.GetBeatmapMap = () => m_Project.BeatmapMap;
			BeatmapSwiperUI.TriggerSwitcher = (key) => {
				m_Project.SaveProject();
				m_Project.OpenBeatmap(key);
			};

			PreviewUI.GetMusicTime01 = (time) => time / m_Music.Duration;
			PreviewUI.GetBeatmap = () => m_Project.Beatmap;

			ProjectCreatorUI.ImportMusic = () => {
				m_Project.ImportMusic((data, _) => {
					if (data is null) { return; }
					ProjectCreatorUI.MusicData = data;
					ProjectCreatorUI.SetMusicSizeDirty();
				});
			};
			ProjectCreatorUI.GotoEditor = m_State.GotoEditor;

			SkinSwiperUI.GetAllSkinNames = () => m_Skin.AllSkinNames;
			SkinSwiperUI.SkinLoadSkin = m_Skin.LoadSkin;

			TimingPreviewUI.GetBeatmap = () => m_Project.Beatmap;
			TimingPreviewUI.GetMusicTime = () => m_Music.Time;
			TimingPreviewUI.GetSpeedMuti = () => m_Game.GameDropSpeed;
			TimingPreviewUI.ShowPreview = () => m_Game.ShowGrid && m_Editor.GetContainerActive(3);

			AxisHandleUI.GetZoneMinMax = () => m_Zone.GetZoneMinMax(true);

			TextRenderer.GetSprite = m_TextSheet.Char_to_Sprite;

			BackgroundUI.OnException = (ex) => DebugLog_Exception("Background", ex);

		}


		#endregion




		#region --- API ---


		public void Quit () => Application.Quit();


		public void About () => DialogUtil.Open(
			$"<size=38><b>Stager Studio</b> v{Application.version}</size>\n" +
			"<size=20>" +
			"\nCreated by 楠瓜Moenen\n\n" +
			//"Home     www.stager.studio\n" +
			"Email     moenen6@gmail.com\n" +
			"Twitter   _Moenen\n" +
			"QQ        754100943" +
			"</size>",
			DialogUtil.MarkType.Info, 324, () => { }
		);


		public void GotoWeb () {
			Application.OpenURL("http://www.stager.studio");
			DialogUtil.Open(
				string.Format(m_Language.Get(UI_OpenWebMSG), "www.stager.studio"),
				DialogUtil.MarkType.Info,
				() => { }, null, null, null, null
			);
		}


		public void AddVolume (float delta) {
			SetMusicVolume(Mathf.Clamp01(Util.Snap(m_Music.Volume + delta, 10f)));
			try {
				m_Hint.SetHint(string.Format(m_Language.Get(Hint_Volume), Mathf.RoundToInt(m_Music.Volume * 100f)));
			} catch { }
		}


		public void Undo () => WillUndo = true;


		public void Redo () => WillRedo = true;


		#endregion




		#region --- LGC ---


		// Try Refresh UI
		private void TryRefreshSetting () {
			var setting = m_SettingRoot.childCount > 0 ? m_SettingRoot.GetChild(0).GetComponent<SettingUI>() : null;
			if (!(setting is null)) {
				setting.Refresh();
			}
		}


		private void TryRefreshProjectInfo () {
			var pInfo = m_ProjectInfoRoot.childCount > 0 ? m_ProjectInfoRoot.GetChild(0).GetComponent<ProjectInfoUI>() : null;
			if (!(pInfo is null)) {
				pInfo.Refresh();
			}
		}


		private void RefreshLoading (float progress01, string hint = "") {
			var loading = m_LoadingRoot.childCount > 0 ? m_LoadingRoot.GetChild(0).GetComponent<LoadingUI>() : null;
			if (loading is null) {
				RemoveLoading();
				loading = Util.SpawnUI(m_LoadingPrefab, m_LoadingRoot, "Loading");
			}
			if (progress01 <= 0f || progress01 >= 1f) {
				RemoveLoading();
			} else {
				loading.SetProgress(progress01, hint);
			}
			// ==== Func ===
			void RemoveLoading () {
				m_LoadingRoot.DestroyAllChildImmediately();
				m_LoadingRoot.gameObject.SetActive(false);
				m_LoadingRoot.parent.InactiveIfNoChildActive();
			}
		}


		private void RefreshGridRenderer () {
			m_GridRenderer.SetCountX(0, m_Game.CurrentGridCountX0);
			m_GridRenderer.SetCountX(1, m_Game.CurrentGridCountX1);
			m_GridRenderer.SetCountX(2, m_Game.CurrentGridCountX2);
			m_GridRenderer.TimeGap = 60f / m_Game.BPM / m_Game.CurrentGridCountY;
			m_GridRenderer.TimeOffset = m_Game.Shift;
			m_GridRenderer.GameSpeedMuti = m_Game.GameDropSpeed;
		}


		// Hint
		private void LogHint_Key (string key, bool flash = true) {
			try {
				m_Hint.SetHint(m_Language.Get(key), flash);
			} catch { }
		}


		private void LogHint (string msg, bool flash = true) {
			try {
				m_Hint.SetHint(msg, flash);
			} catch { }
		}


		// Debug Log
		private void DebugLog_Start () {
			if (!DebugLog.UseLog) { return; }
			DebugLog.LogFormat(
				"System", "Start", true,
				("Stager Version", Application.version),
				("DotNET Framework Versions", System.Environment.Version),
				("OS Version", System.Environment.OSVersion)
			);
		}


		private void DebugLog_Project (string type) {
			if (!DebugLog.UseLog) { return; }
			DebugLog.LogFormat(
				"Project", type, true,
				("ProjectName", m_Project.ProjectName),
				("ProjectPath", m_Project.ProjectPath),
				("Workspace", m_Project.Workspace)
			);
		}


		private void DebugLog_Beatmap (string type) {
			if (!DebugLog.UseLog) { return; }
			var map = m_Project.Beatmap;
			if (map != null) {
				DebugLog.LogFormat(
					"Beatmap", type, true,
					("map", map),
					("BeatmapKey", m_Project.BeatmapKey),
					("CreatedTime", map.CreatedTime),
					("Tag", map.tag),
					("Bpm", map.bpm)
				);
			} else {
				DebugLog.LogFormat("Beatmap", type, false, ("Map is Null", null));
			}
		}


		private void DebugLog_Exception (string sub, System.Exception ex) => DebugLog.LogException("Error", sub, ex);


		#endregion




	}
}


#if UNITY_EDITOR
namespace StagerStudio.Editor {
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;
	using UndoRedo;


	[CustomEditor(typeof(StagerStudio))]
	public class StagerStudio_Inspector : Editor {



		public override void OnInspectorGUI () {

			if (EditorApplication.isPlaying) {
				LayoutH(() => {
					GUIRect(0, 18);
					if (GUI.Button(GUIRect(48, 18), "⇦")) {
						UndoRedo.Undo();
					}
					Space(2);
					if (GUI.Button(GUIRect(48, 18), "⇨")) {
						UndoRedo.Redo();
					}
					GUIRect(0, 18);
				});
				Space(4);
			}
			base.OnInspectorGUI();
		}



		// UTL
		private Rect GUIRect (float width, float height) => GUILayoutUtility.GetRect(
			width, height,
			GUILayout.ExpandWidth(width == 0),
			GUILayout.ExpandHeight(height == 0)
		);


		private void LayoutH (System.Action action, bool box = false, GUIStyle style = null) {
			if (box) {
				style = GUI.skin.box;
			}
			if (style != null) {
				GUILayout.BeginHorizontal(style);
			} else {
				GUILayout.BeginHorizontal();
			}
			action();
			GUILayout.EndHorizontal();
		}


		private void Space (float space = 4f) => GUILayout.Space(space);


	}
}
#endif