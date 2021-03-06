﻿namespace StagerStudio.Stage {
	using System.Collections;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using System.IO;
	using UnityEngine;
	using UnityEngine.Networking;
	using Saving;
	using Data;
	using NAudio.Wave;

	public class StageProject : MonoBehaviour {




		#region --- SUB ---


		public delegate string StringStringHandler (string key);
		public delegate void VoidFloatStringHandler (float progress01 = 1f, string msg = "");
		public delegate void VoidStringStringBoolHandler (string msg, string detail = "", bool warning = false);
		public delegate void VoidAudioClipHandler (AudioClip clip);
		public delegate void VoidAudioClipsHandler (List<AudioClip> clips);
		public delegate void VoidSpriteHandler (Sprite sprite);
		public delegate void VoidBeatmapStringHandler (Beatmap map, string key);
		public delegate void VoidBeatmapHandler (Beatmap map);
		public delegate void VoidHandler ();
		public delegate void VoidStringBoolHandler (string str, bool b);
		public delegate void VoidBoolHandler (bool value);
		public delegate void ExceptionHandler (System.Exception ex);


		public static class LanguageData {
			public const string Loading_ProjectFile = "Project.Loading.File";
			public const string Loading_Audio = "Project.Loading.Audio";
			public const string Loading_Image = "Project.Loading.Image";
			public const string Loading_Beatmap = "Project.Loading.Beatmap";
			public const string Error_ProjectFileNotExists = "Project.Error.FileNotExist";
			public const string Error_FailLoadProjectFile = "Project.Error.FailLoadProjectFile";
			public const string Error_FailLoadProjectInfo = "Project.Error.FailLoadProjectInfo";
			public const string Error_FailLoadProjectCover = "Project.Error.FailLoadProjectCover";
			public const string Error_FailLoadProjectPreview = "Project.Error.FailLoadProjectPreview";
			public const string Error_FailLoadAudio = "Project.Error.FailLoadAudio";
			public const string Error_AudioFormatNotSupport = "Project.Error.AudioFormatNotSupport";
			public const string Error_FailLoadImage = "Project.Error.FailLoadImage";
			public const string Error_FailLoadBeatmap = "Project.Error.FailLoadBeatmap";
			public const string Error_FailGetProjectDataForWrite = "Project.Error.FailGetProjectDataForWrite";
			public const string Error_ErrorOnSavingProject = "Project.Error.ErrorOnSavingProject";
			public const string Error_CantPickOnLoadingProject = "Project.Error.CantPickOnLoadingProject";
			public const string Error_CantDeleteLastBeatmap = "Project.Error.CantDeleteLastBeatmap";
			public const string UI_ImportBeatmapTitle = "Project.UI.ImportBeatmapTitle";
			public const string UI_ImportMusicTitle = "Project.UI.ImportMusicTitle";
			public const string UI_ImportClickSoundTitle = "Project.UI.ImportClickSoundTitle";
			public const string UI_ExportBeatmapTitle = "Project.UI.ExportBeatmapTitle";
			public const string UI_ExportBeatmapDone = "Project.UI.ExportBeatmapDone";
			public const string UI_ImportBGTitle = "Project.UI.ImportBGTitle";
			public const string UI_ImportCoverTitle = "Project.UI.ImportCoverTitle";
			public const string UI_RemoveBackgroundMSG = "Project.UI.RemoveBackgroundMSG";
			public const string UI_RemoveMusicMSG = "Project.UI.RemoveMusicMSG";
			public const string UI_RemoveCoverMSG = "Project.UI.RemoveCoverMSG";
			public const string UI_RemoveBeatmapMSG = "Project.UI.RemoveBeatmapMSG";
			public const string UI_BeatmapLoadedMSG = "Project.UI.BeatmapLoadedMSG";
			public const string UI_SaveProjectConfirmMSG = "Project.UI.SaveProjectConfirmMSG";
			public const string UI_AutoSaveMSG = "Project.UI.AutoSaveMSG";
			public const string UI_NoAutoSaveMSG = "Project.UI.NoAutoSaveMSG";
			public const string UI_ProjectSaveMSG = "Project.UI.ProjectSaveMSG";
			public const string UI_BeatmapDuplicateTag = "Project.UI.BeatmapDuplicateTag";

			public const string UI_NoBackgroundHint = "ProjectInfo.UI.NoBackgroundHint";
			public const string UI_NoCoverHint = "ProjectInfo.UI.NoCoverHint";
			public const string UI_NoMusicHint = "ProjectInfo.UI.NoMusicHint";
			public const string UI_ImportPalette = "ProjectInfo.Dialog.ImportPalette";
			public const string UI_ImportPaletteTitle = "ProjectInfo.Dialog.ImportPaletteTitle";
			public const string UI_ExportPaletteTitle = "ProjectInfo.Dialog.ExportPaletteTitle";
			public const string UI_PaletteFull = "ProjectInfo.Dialog.PaletteFull";
			public const string UI_PaletteExported = "ProjectInfo.Dialog.PaletteExported";

			public const string UI_ImportTween = "ProjectInfo.Dialog.ImportTween";
			public const string UI_ImportTweenTitle = "ProjectInfo.Dialog.ImportTweenTitle";
			public const string UI_ExportTweenTitle = "ProjectInfo.Dialog.ExportTweenTitle";
			public const string UI_TweenFull = "ProjectInfo.Dialog.TweenFull";
			public const string UI_TweenExported = "ProjectInfo.Dialog.TweenExported";
		}


		#endregion




		#region --- VAR ---


		// Handler
		public static VoidFloatStringHandler OnLoadProgress { get; set; } = null;
		public static VoidStringBoolHandler LogHint { get; set; } = null;
		public static StringStringHandler GetLanguage { get; set; } = null;
		public static VoidHandler OnProjectLoadingStart { get; set; } = null;
		public static VoidHandler OnProjectSavingStart { get; set; } = null;
		public static VoidHandler OnProjectLoaded { get; set; } = null;
		public static VoidHandler OnProjectClosed { get; set; } = null;
		public static VoidAudioClipHandler OnMusicLoaded { get; set; } = null;
		public static VoidAudioClipsHandler OnClickSoundsLoaded { get; set; } = null;
		public static VoidSpriteHandler OnBackgroundLoaded { get; set; } = null;
		public static VoidSpriteHandler OnCoverLoaded { get; set; } = null;
		public static VoidBeatmapStringHandler OnBeatmapOpened { get; set; } = null;
		public static VoidHandler OnBeatmapRemoved { get; set; } = null;
		public static VoidHandler OnBeatmapCreated { get; set; } = null;
		public static VoidBoolHandler OnDirtyChanged { get; set; } = null;
		public static ExceptionHandler OnException { get; set; } = null;

		// API
		public Beatmap Beatmap => !string.IsNullOrEmpty(BeatmapKey) && BeatmapMap.ContainsKey(BeatmapKey) ? BeatmapMap[BeatmapKey] : null;
		public string ProjectPath { get; private set; } = "";
		public string BeatmapKey { get; private set; } = "";
		public bool SavingProject { get; private set; } = false;
		public float SavingProgress { get; private set; } = -1f;
		public bool IsDirty {
			get => _IsDirty;
			private set {
				if (value != _IsDirty) {
					_IsDirty = value;
					OnDirtyChanged(value);
				}
			}
		}
		public string Workspace => Util.CombinePaths(Util.GetParentPath(Application.dataPath), "Projects");
		public float UI_AutoSaveTime => AutoSaveTime.Value;

		// Project Data
		public string ProjectName { get; set; } = "";
		public string ProjectDescription { get; set; } = "";
		public string BeatmapAuthor { get; set; } = "";
		public string MusicAuthor { get; set; } = "";
		public string BackgroundAuthor { get; set; } = "";
		public long LastEditTime { get; private set; } = 0;
		public Dictionary<string, Beatmap> BeatmapMap { get; } = new Dictionary<string, Beatmap>();
		public (Project.FileData data, Sprite sprite) Background { get; private set; } = (null, null);
		public (Project.ImageData data, Sprite sprite) FrontCover { get; private set; } = (null, null);
		public (Project.FileData data, AudioClip clip) Music { get; private set; } = (null, null);
		public List<Color32> Palette { get; } = new List<Color32>();
		public List<AnimationCurve> Tweens { get; } = new List<AnimationCurve>();
		public List<(Project.FileData data, AudioClip clip)> ClickSounds { get; } = new List<(Project.FileData data, AudioClip clip)>();
		public GeneData Gene { get => _Gene != null ? _Gene : (_Gene = new GeneData()); private set => _Gene = value; }

		// Short
		private string TempPath => Util.CombinePaths(Application.temporaryCachePath, "Temp");

		// Data
		private Coroutine LoadingCor = null;
		private GeneData _Gene = null;
		private bool _IsDirty = false;
		private float NextAutoSaveTime = float.MaxValue;

		// Saving
		private SavingFloat AutoSaveTime = new SavingFloat("StageProject.AutoSaveTime", -1f);


		#endregion




		#region --- MSG ---


		private void Awake () {

			// Workspace
			try {
				if (!Util.DirectoryExists(Workspace)) {
					Util.CreateFolder(Workspace);
				}
			} catch (System.Exception ex) { OnException(ex); }

			// Create Default Chapter
			try {
				if (Util.GetDirectsIn(Workspace, true).Length == 0) {
					Util.CreateFolder(Util.CombinePaths(Workspace, "Chapter I"));
				}
			} catch (System.Exception ex) { OnException(ex); }

			// Create/Clear Temp Path
			try {
				if (!Util.DirectoryExists(TempPath)) {
					Util.CreateFolder(TempPath);
				}
				Util.DeleteAllFilesIn(TempPath);
			} catch (System.Exception ex) { OnException(ex); }
		}


		private void Update () {
			if (AutoSaveTime.Value > 1f && Time.time > NextAutoSaveTime) {
				RefreshAutoSaveTime();
				SaveProject();
			}
		}


		#endregion




		#region --- API ---


		// Project
		public void OpenProject (string projectPath) {

			if (LoadingCor != null) {
				StopCoroutine(LoadingCor);
			}
			LoadingCor = StartCoroutine(LoadProject());

			// Func
			IEnumerator LoadProject () {

				const float RENDERER_WAIT = 0.4f;

				if (!Util.FileExists(projectPath)) {
					LoadingCor = null;
					LogMessageLogic(LanguageData.Error_ProjectFileNotExists, true);
					yield break;
				}

				IsDirty = false;
				ProjectPath = "";
				BeatmapKey = "";
				Palette.Clear();
				Tweens.Clear();
				ClickSounds.Clear();
				_Gene = null;
				LastEditTime = 0;
				OnProjectLoadingStart();
				RefreshAutoSaveTime();

				// File >> Project Data
				LogLoadingProgress(LanguageData.Loading_ProjectFile, 0.01f);
				yield return new WaitForSeconds(RENDERER_WAIT);

				Project project;
				try {
					project = new Project();
					using (var stream = File.OpenRead(projectPath)) {
						using (var reader = new BinaryReader(stream)) {
							project.Read(reader, (msg) => LogMessageLogic(LanguageData.Error_FailLoadProjectFile, true));
						}
					}
				} catch (System.Exception ex) {
					LogMessageLogic(LanguageData.Error_FailLoadProjectFile, true, ex.Message);
					LogLoadingProgress("", -1f);
					OnProjectLoaded.Invoke();
					LoadingCor = null;
					OnException(ex);
					yield break;
				}

				// Project Data >> Stage
				ProjectPath = projectPath;

				// Info
				try {
					if (!project) {
						LoadingCor = null;
						LogLoadingProgress("", -1f);
						OnProjectLoaded.Invoke();
						yield break;
					}
					ProjectName = project.ProjectName;
					ProjectDescription = project.Description;
					BeatmapAuthor = project.BeatmapAuthor;
					MusicAuthor = project.MusicAuthor;
					BackgroundAuthor = project.BackgroundAuthor;
					BeatmapKey = project.OpeningBeatmap;
					LastEditTime = Util.GetLongTime();
				} catch (System.Exception ex) {
					LogMessageLogic(LanguageData.Error_FailLoadProjectInfo, true, ex.Message);
					OnException(ex);
				}

				// Cover
				try {
					FrontCover = (project.FrontCover, Project.ImageData_to_Sprite(project.FrontCover));
					OnCoverLoaded.Invoke(FrontCover.sprite);
				} catch (System.Exception ex) {
					LogMessageLogic(LanguageData.Error_FailLoadProjectCover, true, ex.Message);
					OnException(ex);
				}

				// Palette
				Palette.AddRange(project.Palette);

				// Tween
				Tweens.AddRange(project.Tweens);

				// Gene
				_Gene = project.Gene;
				if (_Gene == null || string.IsNullOrEmpty(_Gene.Key)) {
					_Gene = GeneData.GetDefault();
				}

				// Music
				LogLoadingProgress(LanguageData.Loading_Audio, 0.2f);
				yield return new WaitForSeconds(RENDERER_WAIT);
				Music = (null, null);
				if (!(project.MusicData is null)) {
					yield return FileData_to_AudioClip(project.MusicData, (clip) => {
						Music = (project.MusicData, clip);
					});
					OnMusicLoaded.Invoke(Music.clip);
				}

				// Click Sounds
				LogLoadingProgress(LanguageData.Loading_Audio, 0.3f);
				yield return new WaitForSeconds(RENDERER_WAIT);
				if (!(project.ClickSounds is null)) {
					foreach (var cSoundData in project.ClickSounds) {
						yield return FileData_to_AudioClip(cSoundData, (clip) => {
							ClickSounds.Add((cSoundData, clip));
						});
					}
					LogClickSoundCallback();
				}

				// Background
				LogLoadingProgress(LanguageData.Loading_Image, 0.6f);
				yield return new WaitForSeconds(RENDERER_WAIT);
				Background = (null, null);
				if (project.BackgroundData != null) {
					yield return FileData_to_Sprite(project.BackgroundData, (sprite) => {
						Background = (project.BackgroundData, sprite);
					});
					OnBackgroundLoaded(Background.sprite);
				} else {
					OnBackgroundLoaded(null);
				}

				// Beatmap
				LogLoadingProgress(LanguageData.Loading_Beatmap, 0.8f);
				yield return new WaitForSeconds(RENDERER_WAIT);
				try {
					BeatmapMap.Clear();
					foreach (var pair in project.BeatmapMap) {
						BeatmapMap.Add(pair.Key, pair.Value);
					}
					if (BeatmapMap.Count == 0) {
						BeatmapMap.Add(System.Guid.NewGuid().ToString(), Beatmap.NewBeatmap());
					}
					// Open Map
					if (BeatmapMap.ContainsKey(BeatmapKey)) {
						OpenBeatmapLogic(BeatmapKey);
					} else {
						OpenFirstBeatmapLogic();
					}
				} catch (System.Exception ex) {
					LogMessageLogic(LanguageData.Error_FailLoadBeatmap, true, ex.Message);
					OnException(ex);
				}

				// Done
				LogLoadingProgress("", -1f);
				yield return new WaitForSeconds(RENDERER_WAIT);
				OnProjectLoaded();
				RefreshAutoSaveTime();
				Resources.UnloadUnusedAssets();
				LoadingCor = null;

			}

		}


		public void SaveProject () {
			if (string.IsNullOrEmpty(ProjectPath)) {
				string pName = "Project_" + Util.GetTimeString();
				ProjectPath = Util.CombinePaths(Workspace, pName + ".stager");
			}
			SaveProjectLogic(ProjectPath);
			LogHint(GetLanguage(LanguageData.UI_ProjectSaveMSG), false);
		}


		public void CloseProject () {
			if (LoadingCor != null) {
				StopCoroutine(LoadingCor);
			}
			ProjectPath = "";
			ProjectName = "";
			ProjectDescription = "";
			BeatmapAuthor = "";
			MusicAuthor = "";
			BackgroundAuthor = "";
			LastEditTime = 0;
			IsDirty = false;
			FrontCover = (null, null);
			Palette.Clear();
			Tweens.Clear();
			ClickSounds.Clear();
			Music = (null, null);
			Background = (null, null);
			BeatmapMap.Clear();
			OnProjectClosed();
		}


		public void SetDirty () => IsDirty = true;


		public void SetAutoSaveTime (float time) {
			if (time > 0f) {
				AutoSaveTime.Value = Mathf.Clamp(time, 30f, 600f);
				LogHint(string.Format(GetLanguage(LanguageData.UI_AutoSaveMSG), AutoSaveTime.Value.ToString("0")), true);
			} else {
				AutoSaveTime.Value = -1f;
				LogHint(GetLanguage(LanguageData.UI_NoAutoSaveMSG), true);
			}
			RefreshAutoSaveTime();
		}


		// Beatmap
		public void NewBeatmap () {
			var key = System.Guid.NewGuid().ToString();
			var map = Beatmap.NewBeatmap();
			BeatmapMap.Add(key, map);
			SetDirty();
			OnBeatmapCreated();
		}


		public void OpenBeatmap (object itemRT) {
			if (!(itemRT is RectTransform)) { return; }
			DialogUtil.Dialog_Yes_No_Cancel(LanguageData.UI_SaveProjectConfirmMSG, DialogUtil.MarkType.Warning,
				() => {
					SaveProject();
					OpenBeatmapLogic((itemRT as RectTransform).name);
					IsDirty = false;
				}, () => {
					OpenBeatmapLogic((itemRT as RectTransform).name);
					IsDirty = false;
				}
			);
		}


		public void OpenBeatmap (string key) {
			OpenBeatmapLogic(key);
			IsDirty = false;
		}


		public void UI_ImportBeatmap (System.Action done) {
			var path = DialogUtil.PickFileDialog(LanguageData.UI_ImportBeatmapTitle, "Stager Beatmap", "json");
			try {
				if (string.IsNullOrEmpty(path) || !Util.FileExists(path)) { return; }
				var map = JsonUtility.FromJson<Beatmap>(Util.FileToText(path));
				if (!(map is null)) {
					var key = System.Guid.NewGuid().ToString();
					BeatmapMap.Add(key, map);
					SetDirty();
					OnBeatmapCreated();
					done();
				}
			} catch (System.Exception ex) {
				DialogUtil.Open(ex.Message, DialogUtil.MarkType.Error, () => { });
				OnException(ex);
			}
		}


		public void ExportBeatmap (object itemRT) {
			if (!(itemRT is RectTransform) || !BeatmapMap.ContainsKey((itemRT as RectTransform).name)) { return; }
			try {
				var map = BeatmapMap[(itemRT as RectTransform).name];
				if (map is null) { return; }
				var path = DialogUtil.CreateFileDialog(LanguageData.UI_ExportBeatmapTitle, map.Tag, "json");
				if (string.IsNullOrEmpty(path)) { return; }
				Util.TextToFile(JsonUtility.ToJson(map, true), path);
				DialogUtil.Dialog_OK(LanguageData.UI_ExportBeatmapDone, DialogUtil.MarkType.Success, () => { });
			} catch (System.Exception ex) {
				LogMessageLogic(LanguageData.Error_FailLoadBeatmap, true, ex.Message);
				OnException(ex);
			}
		}


		public void DeleteBeatmap (object itemRT) {
			if (BeatmapMap.Count <= 1) {
				DialogUtil.Dialog_OK(LanguageData.Error_CantDeleteLastBeatmap, DialogUtil.MarkType.Warning);
				return;
			}
			if (!(itemRT is RectTransform) || !BeatmapMap.ContainsKey((itemRT as RectTransform).name)) { return; }
			var key = (itemRT as RectTransform).name;
			var map = BeatmapMap[key];
			DialogUtil.Open(
				string.Format(GetLanguage(LanguageData.UI_RemoveBeatmapMSG), map.Tag),
				DialogUtil.MarkType.Warning,
				() => RemoveBeatmapLogic(key), null, null, null, () => { }
			);
		}


		public void DuplicateBeatmap (object itemRT) {
			if (!(itemRT is RectTransform) || !BeatmapMap.ContainsKey((itemRT as RectTransform).name)) { return; }
			var key = (itemRT as RectTransform).name;
			if (!BeatmapMap.ContainsKey(key)) { return; }
			var map = BeatmapMap[key];
			var newMap = Beatmap.NewBeatmap();
			newMap.LoadFromOtherMap(map, false);
			newMap.Tag += GetLanguage(LanguageData.UI_BeatmapDuplicateTag);
			var newkey = System.Guid.NewGuid().ToString();
			BeatmapMap.Add(newkey, newMap);
			SetDirty();
			OnBeatmapCreated();
		}


		// Import Asset
		public void ImportMusic () => ImportMusic((data, clip) => {
			Music = (data, clip);
			OnMusicLoaded.Invoke(clip);
		});


		public void ImportMusic (System.Action<Project.FileData, AudioClip> callBack) {
			var path = DialogUtil.PickFileDialog(LanguageData.UI_ImportMusicTitle, "", "mp3", "wav", "ogg");
			if (!Util.FileExists(path)) { return; }
			ImportAudioLogic(path, callBack);
		}


		public void ImportBackground () => ImportBackground((data, sprite) => {
			Background = (data, sprite);
			OnBackgroundLoaded(sprite);
		}, 256);


		public void ImportBackground (System.Action<Project.FileData, Sprite> callBack, int maxPixelSize = int.MaxValue) {
			var path = DialogUtil.PickFileDialog((LanguageData.UI_ImportBGTitle), "", "png", "jpg");
			if (!Util.FileExists(path)) { return; }
			ImportImageFileLogic(path, callBack, maxPixelSize);
		}


		public void ImportCover () => ImportCover((data, sprite) => {
			FrontCover = (data, sprite);
			OnCoverLoaded.Invoke(sprite);
		}, 256);


		public void ImportCover (System.Action<Project.ImageData, Sprite> callBack, int maxPixelSize = int.MaxValue) {
			var path = DialogUtil.PickFileDialog(LanguageData.UI_ImportCoverTitle, "", "png", "jpg");
			if (!Util.FileExists(path)) { return; }
			ImportImageLogic(path, callBack, false, maxPixelSize);
		}


		public void ImportAudioLogic (string path, System.Action<Project.FileData, AudioClip> callBack) {
			if (LoadingCor != null) {
				LogMessageLogic(LanguageData.Error_CantPickOnLoadingProject);
				return;
			}
			// File >> Object
			if (!Util.FileExists(path)) { return; }
			LoadingCor = StartCoroutine(LoadAudio(callBack));
			IEnumerator LoadAudio (System.Action<Project.FileData, AudioClip> cBack) {
				AudioClip clip = null;
				UnityWebRequest request;
				string format, name;
				bool needDelete = false;
				string aimPath = path;
				try {
					// Mp3 >> Wav
					string aimFormat = format = Util.GetExtension(path);
					name = Util.GetNameWithoutExtension(path);
					if (format == ".mp3") {
						aimFormat = ".wav";
						aimPath = Util.ChangeExtension(TempPath, name + aimFormat);
						Mp3ToWav(path, aimPath);
						needDelete = true;
					}
					var audioType = GetAudioTyle(aimFormat);
					if (audioType == AudioType.UNKNOWN) {
						LogMessageLogic(LanguageData.Error_AudioFormatNotSupport, true);
						LoadingCor = null;
						yield break;
					}
					request = UnityWebRequestMultimedia.GetAudioClip(Util.GetUrl(aimPath), audioType);
					if (request == null) {
						LogMessageLogic(LanguageData.Error_FailLoadAudio, true);
						LoadingCor = null;
						yield break;
					}
				} catch (System.Exception ex) {
					LogMessageLogic(LanguageData.Error_FailLoadAudio, true, ex.Message);
					LoadingCor = null;
					OnException(ex);
					yield break;
				}

				yield return request.SendWebRequest();

				try {
					// Delete Temp
					if (needDelete) {
						Util.DeleteFile(aimPath);
					}
					// Object >> Stage
					if (!request.isNetworkError && !request.isHttpError) {
						var handler = (DownloadHandlerAudioClip)request.downloadHandler;
						if (handler.isDone) {
							clip = handler.audioClip;
						}
					}
					if (clip) {
						var aData = Project.AudioBytes_to_AudioData(Util.GetNameWithoutExtension(path), format, Util.FileToByte(path));
						if (aData != null) {
							clip.name = aData.Name;
							cBack.Invoke(aData, clip);
						}
					} else {
						LogMessageLogic(LanguageData.Error_FailLoadAudio, true);
					}
				} catch (System.Exception ex) {
					LogMessageLogic(LanguageData.Error_FailLoadAudio, true, ex.Message);
					OnException(ex);
				}

				SetDirty();
				LoadingCor = null;
			}
		}


		public void ImportImageLogic (string path, System.Action<Project.ImageData, Sprite> callBack, bool useAlpha, int maxPixelSize = int.MaxValue) {
			if (LoadingCor != null) {
				LogMessageLogic(LanguageData.Error_CantPickOnLoadingProject);
				return;
			}
			// File >> Object
			path = Util.GetFullPath(path);
			if (!Util.FileExists(path)) { return; }
			string key = System.Guid.NewGuid().ToString();
			LoadingCor = StartCoroutine(LoadImage(callBack, useAlpha));

			IEnumerator LoadImage (System.Action<Project.ImageData, Sprite> cBack, bool alpha) {
				yield return LoadImageLogic(path, (texture, sprite) => {
					var tData = Project.Texture_to_ImageData(texture, texture.width, texture.height, alpha);
					if (tData != null && sprite != null) {
						cBack.Invoke(tData, sprite);
					}
					SetDirty();
					LoadingCor = null;
				}, maxPixelSize);
			}
		}


		public void ImportImageFileLogic (string path, System.Action<Project.FileData, Sprite> callBack, int maxPixelSize = int.MaxValue) {
			if (LoadingCor != null) {
				LogMessageLogic(LanguageData.Error_CantPickOnLoadingProject);
				return;
			}
			// File >> Object
			path = Util.GetFullPath(path);
			if (!Util.FileExists(path)) { return; }
			string key = System.Guid.NewGuid().ToString();
			LoadingCor = StartCoroutine(LoadImage(callBack));

			IEnumerator LoadImage (System.Action<Project.FileData, Sprite> cBack) {
				yield return LoadImageLogic(path, (texture, sprite) => {
					var tData = Project.Texture_to_FileData(texture, Util.GetExtension(path));
					if (tData != null && sprite != null) {
						cBack.Invoke(tData, sprite);
					}
					SetDirty();
					LoadingCor = null;
				}, maxPixelSize);
			}
		}


		// Remove Asset
		public void RemoveMusic () => DialogUtil.Dialog_OK_Cancel(LanguageData.UI_RemoveMusicMSG, DialogUtil.MarkType.Warning, () => {
			Music = (null, null);
			OnMusicLoaded.Invoke(null);
			SetDirty();
		});


		public void RemoveBackground () => DialogUtil.Dialog_OK_Cancel(LanguageData.UI_RemoveBackgroundMSG, DialogUtil.MarkType.Warning, () => {
			Background = (null, null);
			OnBackgroundLoaded.Invoke(null);
			SetDirty();
		});


		public void RemoveCover () => DialogUtil.Dialog_OK_Cancel(LanguageData.UI_RemoveCoverMSG, DialogUtil.MarkType.Warning, () => {
			FrontCover = (null, null);
			OnCoverLoaded.Invoke(null);
			SetDirty();
		});


		// Click Sound
		public void ImportClickSound () {
			var path = DialogUtil.PickFileDialog(LanguageData.UI_ImportClickSoundTitle, "", "mp3", "wav", "ogg");
			if (!Util.FileExists(path)) { return; }
			ImportAudioLogic(path, (data, clip) => {
				ClickSounds.Add((data, clip));
				LogClickSoundCallback();
				SetDirty();
			});
		}


		public void RemoveClickSound (int index) {
			if (index < 0 || index >= ClickSounds.Count) { return; }
			ClickSounds.RemoveAt(index);
			foreach (var pair in BeatmapMap) {
				var map = pair.Value;
				if (map is null) { continue; }
				map.DeleteClickSound(index);
			}
			LogClickSoundCallback();
			SetDirty();
		}


		public void SwipeClickSound (int index, int indexAlt) {
			if (index < 0 || index >= Palette.Count || indexAlt < 0 || indexAlt >= Palette.Count || index == indexAlt) { return; }
			var temp = ClickSounds[index];
			ClickSounds[index] = ClickSounds[indexAlt];
			ClickSounds[indexAlt] = temp;
			// Fix All Beatmap Data
			foreach (var pair in BeatmapMap) {
				var map = pair.Value;
				if (map is null) { continue; }
				map.SwipeClickSound((short)index, (short)indexAlt);
			}
			SetDirty();
		}


		// Palette
		public void RemovePaletteAt (int index) {
			if (index < 0 || index >= Palette.Count) { return; }
			// Remove
			Palette.RemoveAt(index);
			if (Palette.Count == 0) {
				Palette.Add(Color.white);
			}
			SetDirty();
			// Fix All Beatmap Data
			foreach (var pair in BeatmapMap) {
				var map = pair.Value;
				if (map is null) { continue; }
				map.DeletePaletteColor(index);
			}
		}


		public void SetPaletteColor (Color color, int index) {
			if (index < 0 || index >= Palette.Count) { return; }
			Palette[index] = color;
			SetDirty();
		}


		public void AddPaletteColor (Color color) {
			if (Palette.Count >= 256) { return; }
			Palette.Add(color);
			SetDirty();
		}


		public void SwipePalette (int index, int indexAlt) {
			if (index < 0 || index >= Palette.Count || indexAlt < 0 || indexAlt >= Palette.Count || index == indexAlt) { return; }
			var temp = Palette[index];
			Palette[index] = Palette[indexAlt];
			Palette[indexAlt] = temp;
			// Fix All Beatmap Data
			foreach (var pair in BeatmapMap) {
				var map = pair.Value;
				if (map is null) { continue; }
				map.SwipePaletteColor(index, indexAlt);
			}
			SetDirty();
		}


		public void UI_AddPaletteColor () {
			if (Palette.Count >= 256) {
				DialogUtil.Dialog_OK(LanguageData.UI_PaletteFull, DialogUtil.MarkType.Warning, () => { });
				return;
			}
			AddPaletteColor(Color.white);
		}


		public void UI_ImportPalette (System.Action done) {
			DialogUtil.Dialog_OK_Cancel(LanguageData.UI_ImportPalette, DialogUtil.MarkType.Warning, () => {
				var path = DialogUtil.PickFileDialog((LanguageData.UI_ImportPaletteTitle), "images", "png", "jpg");
				if (string.IsNullOrEmpty(path)) { return; }
				try {
					var colors = Util.ImageToPixels(path, false).pixels;
					if (colors is null) { throw new System.Exception("Error on import image."); }
					Palette.Clear();
					Palette.AddRange(colors);
					SetDirty();
					if (Palette.Count > 256) {
						Palette.RemoveRange(256, Palette.Count - 256);
					}
					done();
				} catch (System.Exception ex) {
					DialogUtil.Open(ex.Message, DialogUtil.MarkType.Error, () => { });
					OnException(ex);
				}
			});
		}


		public void UI_ExportPalette () {
			var path = DialogUtil.CreateFileDialog(LanguageData.UI_ExportPaletteTitle, "Pal", "png");
			if (string.IsNullOrEmpty(path)) { return; }
			try {
				var texture = new Texture2D(Palette.Count, 1);
				var colors = new List<Color>();
				foreach (var c in Palette) {
					colors.Add(c);
				}
				texture.SetPixels(colors.ToArray());
				texture.Apply();
				Util.ByteToFile(texture.EncodeToPNG(), path);
				DialogUtil.Dialog_OK(LanguageData.UI_PaletteExported, DialogUtil.MarkType.Success, () => { });
			} catch (System.Exception ex) {
				DialogUtil.Open(ex.Message, DialogUtil.MarkType.Error, () => { });
				OnException(ex);
			}
		}


		// Tween
		public void RemoveTweenAt (int index) {
			if (index < 0 || index >= Tweens.Count) { return; }
			// Remove
			Tweens.RemoveAt(index);
			if (Tweens.Count == 0) {
				Tweens.Add(AnimationCurve.Linear(0, 0, 1, 1));
			}
			SetDirty();
			// Fix All Beatmap Data
			foreach (var pair in BeatmapMap) {
				var map = pair.Value;
				if (map is null) { continue; }
				map.DeleteTween(index);
			}
		}


		public void AddTweenCurve (AnimationCurve curve) {
			Tweens.Add(curve);
			SetDirty();
		}


		public void SetTweenCurve (AnimationCurve curve, int index) {
			if (index < 0 || index >= Tweens.Count) { return; }
			var projectTween = Tweens[index];
			if (!(curve is null)) {
				int len = projectTween.length;
				for (int i = 0; i < len; i++) {
					projectTween.RemoveKey(0);
				}
				foreach (var key in curve.keys) {
					projectTween.AddKey(key);
				}
			}
			Tweens[index] = projectTween;
			SetDirty();
		}


		public void SwipeTween (int index, int indexAlt) {
			if (index < 0 || index >= Tweens.Count || indexAlt < 0 || indexAlt >= Tweens.Count || index == indexAlt) { return; }
			var temp = Tweens[index];
			Tweens[index] = Tweens[indexAlt];
			Tweens[indexAlt] = temp;
			// Fix All Beatmap Data
			foreach (var pair in BeatmapMap) {
				var map = pair.Value;
				if (map is null) { continue; }
				map.SwipeTween(index, indexAlt);
			}
			SetDirty();
		}


		public void UI_AddTween () {
			if (Tweens.Count >= 256) {
				DialogUtil.Dialog_OK(LanguageData.UI_TweenFull, DialogUtil.MarkType.Warning, () => { });
				return;
			}
			AddTweenCurve(AnimationCurve.Linear(0, 0, 1, 1));
		}


		public void UI_ImportTween (System.Action done) {
			DialogUtil.Dialog_OK_Cancel(LanguageData.UI_ImportTween, DialogUtil.MarkType.Warning, () => {
				var path = DialogUtil.PickFileDialog((LanguageData.UI_ImportTweenTitle), "json", "json");
				if (string.IsNullOrEmpty(path) || !Util.FileExists(path)) { return; }
				try {
					var tArray = JsonUtility.FromJson<Project.TweenArray>(Util.FileToText(path));
					Tweens.Clear();
					Tweens.AddRange(tArray.GetAnimationTweens());
					SetDirty();
					if (Tweens.Count > 256) {
						Tweens.RemoveRange(256, Tweens.Count - 256);
					}
					done();
				} catch (System.Exception ex) {
					DialogUtil.Open(ex.Message, DialogUtil.MarkType.Error, () => { });
					OnException(ex);
				}
			});
		}


		public void UI_ExportTween () {
			var path = DialogUtil.CreateFileDialog((LanguageData.UI_ExportTweenTitle), "json", "json");
			if (string.IsNullOrEmpty(path)) { return; }
			try {
				var json = JsonUtility.ToJson(new Data.Project.TweenArray(Tweens), true);
				Util.TextToFile(json, path);
				DialogUtil.Dialog_OK(LanguageData.UI_TweenExported, DialogUtil.MarkType.Success, () => { });
			} catch (System.Exception ex) {
				DialogUtil.Open(ex.Message, DialogUtil.MarkType.Error, () => { });
				OnException(ex);
			}
		}


		#endregion




		#region --- LGC ---


		// Project
		private async void SaveProjectLogic (string projectPath) {

			if (SavingProject || LoadingCor != null || !Util.IsChildPath(projectPath, Workspace)) { return; }

			SavingProject = true;
			IsDirty = false;
			SavingProgress = 0f;
			OnProjectSavingStart.Invoke();

			// Stage >> Project Data
			var project = new Project();
			try {
				project.ProjectName = ProjectName;
				project.Description = ProjectDescription;
				project.BeatmapAuthor = BeatmapAuthor;
				project.MusicAuthor = MusicAuthor;
				project.BackgroundAuthor = BackgroundAuthor;
				project.FrontCover = FrontCover.data;
				project.Palette.Clear();
				project.Palette.AddRange(Palette);
				project.Tweens.Clear();
				project.Tweens.AddRange(Tweens);
				project.BackgroundData = Background.data;
				project.MusicData = Music.data;
				project.LastEditTime = LastEditTime = Util.GetLongTime();
				project.Gene = _Gene;
				// Click Sound
				project.ClickSounds.Clear();
				foreach (var (data, _) in ClickSounds) {
					project.ClickSounds.Add(data);
				}
			} catch (System.Exception ex) {
				LogMessageLogic(LanguageData.Error_FailGetProjectDataForWrite, true, ex.Message);
				OnException(ex);
			}

			// Beatmap
			try {
				if (!BeatmapMap.ContainsKey(BeatmapKey)) {
					if (string.IsNullOrEmpty(BeatmapKey)) {
						BeatmapKey = System.Guid.NewGuid().ToString();
					}
					var map = Beatmap.NewBeatmap();
					BeatmapMap.Add(BeatmapKey, map);
				}
				project.OpeningBeatmap = BeatmapKey;
				foreach (var pair in BeatmapMap) {
					project.BeatmapMap.Add(pair.Key, pair.Value);
				}
			} catch (System.Exception ex) {
				LogMessageLogic(LanguageData.Error_FailGetProjectDataForWrite, true, ex.Message);
				OnException(ex);
			}

			// Project Data >> File
			var errorMsg = "";
			await Task.Run(() => {
				try {
					using (var stream = File.Create(projectPath))
					using (var writer = new BinaryWriter(stream)) {
						project.Write(writer, (msg) => errorMsg = msg, (progress) => SavingProgress = progress);
					}
				} catch { }
			});
			SavingProgress = -1f;
			SavingProject = false;

			// Log Error
			if (!string.IsNullOrEmpty(errorMsg)) {
				LogMessageLogic(LanguageData.Error_ErrorOnSavingProject, true);
				Debug.Log(errorMsg);
			}

		}


		private void RefreshAutoSaveTime () => NextAutoSaveTime = AutoSaveTime > 0f ? Time.time + AutoSaveTime : float.MaxValue;


		// Asset
		private void RemoveBeatmapLogic (string key) {
			if (BeatmapMap.Count <= 1) {
				LogMessageLogic(LanguageData.Error_CantDeleteLastBeatmap);
				return;
			}
			if (BeatmapMap.ContainsKey(key)) {
				BeatmapMap.Remove(key);
				OnBeatmapRemoved();
				if (key == BeatmapKey) {
					OpenFirstBeatmapLogic();
				}
				SetDirty();
			}
		}


		private void OpenBeatmapLogic (string key) {
			if (!BeatmapMap.ContainsKey(key)) { return; }
			BeatmapKey = key;
			var map = BeatmapMap[key];
			if (map == null) { return; }
			LogHint(string.Format(GetLanguage(LanguageData.UI_BeatmapLoadedMSG), map.Tag), true);
			OnBeatmapOpened(map, key);
		}


		private void OpenFirstBeatmapLogic () {
			foreach (var map in BeatmapMap) {
				OpenBeatmapLogic(map.Key);
				break;
			}
		}


		// Audio
		private IEnumerator FileData_to_AudioClip (Project.FileData data, System.Action<AudioClip> callBack) {
			if (data == null || data.Data == null || data.Data.Length == 0) {
				LogMessageLogic(LanguageData.Error_FailLoadAudio, true);
				yield break;
			}

			AudioClip clip = null;
			UnityWebRequest request;
			string path, aimPath;

			try {
				path = Util.CombinePaths(TempPath, Util.GetTimeString() + data.Format);
				Util.ByteToFile(data.Data, path);
				string aimFormat = data.Format;
				aimPath = path;
				// Mp3 >> Wav
				if (data.Format == ".mp3") {
					aimFormat = ".wav";
					aimPath = Util.ChangeExtension(path, aimFormat);
					Mp3ToWav(path, aimPath);
					Util.DeleteFile(path);
				}
				var audioType = GetAudioTyle(aimFormat);
				if (audioType == AudioType.UNKNOWN) {
					LogMessageLogic(LanguageData.Error_AudioFormatNotSupport, true);
					yield break;
				}
				request = UnityWebRequestMultimedia.GetAudioClip(Util.GetUrl(aimPath), audioType);
				if (request == null) {
					LogMessageLogic(LanguageData.Error_FailLoadAudio, true);
					yield break;
				}
			} catch (System.Exception ex) {
				LogMessageLogic(LanguageData.Error_FailLoadAudio, true, ex.Message);
				OnException(ex);
				yield break;
			}

			yield return request.SendWebRequest();

			try {
				if (!request.isNetworkError && !request.isHttpError) {
					var handler = (DownloadHandlerAudioClip)request.downloadHandler;
					if (handler.isDone) {
						clip = handler.audioClip;
					}
				}
				Util.DeleteFile(aimPath);
				Util.DeleteFile(path);
				if (clip) {
					clip.name = data.Name;
					callBack(clip);
				} else {
					LogMessageLogic(LanguageData.Error_FailLoadAudio, true);
				}
			} catch (System.Exception ex) {
				LogMessageLogic(LanguageData.Error_FailLoadAudio, true, ex.Message);
				OnException(ex);
			}
		}


		// Image
		private IEnumerator FileData_to_Sprite (Project.FileData data, System.Action<Sprite> callBack) {
			if (data == null || data.Data == null || data.Data.Length == 0) {
				LogMessageLogic(LanguageData.Error_FailLoadImage, true);
				yield break;
			}


			Texture2D texture = null;
			UnityWebRequest request;
			string path;

			try {
				path = Util.CombinePaths(TempPath, Util.GetTimeString() + data.Format);
				Util.ByteToFile(data.Data, path);
				request = UnityWebRequestTexture.GetTexture(Util.GetUrl(path));
				if (request == null) {
					LogMessageLogic(LanguageData.Error_FailLoadImage, true);
					yield break;
				}
			} catch (System.Exception ex) {
				LogMessageLogic(LanguageData.Error_FailLoadImage, true, ex.Message);
				OnException(ex);
				yield break;
			}

			yield return request.SendWebRequest();

			try {
				if (!request.isNetworkError && !request.isHttpError) {
					var handler = (DownloadHandlerTexture)request.downloadHandler;
					if (handler.isDone) {
						texture = handler.texture;
					}
				}
				Util.DeleteFile(path);
				if (texture) {
					texture.filterMode = FilterMode.Bilinear;
					callBack(Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f));
				} else {
					LogMessageLogic(LanguageData.Error_FailLoadImage, true);
				}
			} catch (System.Exception ex) {
				LogMessageLogic(LanguageData.Error_FailLoadImage, true, ex.Message);
				OnException(ex);
			}
		}


		private IEnumerator LoadImageLogic (string path, System.Action<Texture2D, Sprite> cBack, int maxPixelSize) {
			UnityWebRequest request;
			Texture2D texture = null;
			string name;
			try {
				request = UnityWebRequestTexture.GetTexture(Util.GetUrl(path), false);
				name = Util.GetNameWithoutExtension(path);
				if (request == null) {
					LogMessageLogic(LanguageData.Error_FailLoadImage, true);
					LoadingCor = null;
					yield break;
				}
			} catch (System.Exception ex) {
				LogMessageLogic(LanguageData.Error_FailLoadImage, true, ex.Message);
				OnException(ex);
				LoadingCor = null;
				yield break;
			}
			yield return request.SendWebRequest();
			try {
				// Object >> Stage
				if (!request.isNetworkError && !request.isHttpError) {
					var handler = (DownloadHandlerTexture)request.downloadHandler;
					if (handler.isDone) {
						texture = handler.texture;
					}
				}
				if (texture) {
					texture.name = name;
					texture.filterMode = FilterMode.Bilinear;
					if (texture.width > maxPixelSize || texture.height > maxPixelSize) {
						texture = Util.ResizeTexture(texture, maxPixelSize);
					}
					var sp = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
					if (sp != null) {
						sp.name = name;
						cBack.Invoke(texture, sp);
					}
				} else {
					LogMessageLogic(LanguageData.Error_FailLoadImage, true);
				}
			} catch (System.Exception ex) {
				LogMessageLogic(LanguageData.Error_FailLoadImage, true, ex.Message);
				OnException(ex);
			}
		}



		// Util
		private AudioType GetAudioTyle (string extension) {
			switch (extension) {
				case ".mp3":
					return AudioType.MPEG;
				case ".wav":
					return AudioType.WAV;
				case ".ogg":
					return AudioType.OGGVORBIS;
			}
			return AudioType.UNKNOWN;
		}


		private void LogMessageLogic (string key, bool warning = false, string msg = "") {
			DialogUtil.Open(GetLanguage(key) + (string.IsNullOrEmpty(msg) ? "" : ("\n\n" + msg)), warning ? DialogUtil.MarkType.Warning : DialogUtil.MarkType.Info, () => { });
			Debug.Log(key + "\n" + msg);
		}


		private void LogLoadingProgress (string key, float progress) => OnLoadProgress?.Invoke(progress, GetLanguage?.Invoke(key));


		private void LogClickSoundCallback () {
			var list = new List<AudioClip>();
			foreach (var (_, clip) in ClickSounds) {
				list.Add(clip);
			}
			OnClickSoundsLoaded(list);
		}


		private void Mp3ToWav (string mp3, string wave) {
			using (var reader = new Mp3FileReader(mp3))
				WaveFileWriter.CreateWaveFile(wave, reader);
		}


		#endregion




	}
}