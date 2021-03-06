﻿namespace StagerStudio.Data {
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;



	[System.Serializable]
	public enum SkinType {

		Stage = 0,
		JudgeLine = 1,

		Track = 2,
		TrackTint = 3,
		Tray = 4,

		Note = 5,
		Pole = 6,

		NoteLuminous = 7,
		HoldLuminous = 8,

	}


	[System.Serializable]
	public class SkinData {


		// API
		public Texture2D Texture { get; private set; } = null;
		public float LuminousAppendX_UI {
			get => LuminousAppendX * 100f;
			set => LuminousAppendX = Mathf.Clamp01(value / 100f);
		}
		public float LuminousAppendY_UI {
			get => LuminousAppendY * 100f;
			set => LuminousAppendY = Mathf.Clamp01(value / 100f);
		}
		public float ScaleMuti_UI {
			get => ScaleMuti;
			set => ScaleMuti = Mathf.Max(value, 1f);
		}
		public int VanishDuration_UI {
			get => (int)(VanishDuration * 1000f);
			set => VanishDuration = Mathf.Max(value / 1000f, 0f);
		}

		// Ser
		public string Author = "";
		public float ScaleMuti = 2000f;
		public float LuminousAppendX = 0f;
		public float LuminousAppendY = 0f;
		public float VanishDuration = 0.1f;
		public bool TintNote = false;
		public bool FrontPole = true;
		public bool InfiniteJudgeLine = false;
		public List<AnimatedItemData> Items = new List<AnimatedItemData>();

		// Data
		private byte[] PngBytes = null;

		// API
		public static SkinData ByteToSkin (byte[] bytes) {
			if (bytes is null || bytes.Length <= 4) { return null; }
			try {
				SkinData skin = null;
				// Json Len
				int index = 0;
				int jsonLength = System.BitConverter.ToInt32(bytes, index);
				index += 4;
				// Json
				var json = System.Text.Encoding.UTF8.GetString(bytes, index, jsonLength);
				index += jsonLength;
				skin = JsonUtility.FromJson<SkinData>(json);
				if (skin is null) {
					skin = new SkinData();
				}
				skin.Fillup();
				// PNG Len
				int pngLength = System.BitConverter.ToInt32(bytes, index);
				index += 4;
				// PNG
				skin.Texture = null;
				if (pngLength > 0) {
					skin.PngBytes = bytes.Skip(index).Take(pngLength).ToArray();
					var (pixels32, width, height) = Util.ImageToPixels(skin.PngBytes);
					if (!(pixels32 is null) && pixels32.Length > 0 && width * height == pixels32.Length) {
						skin.Texture = new Texture2D(width, height, TextureFormat.RGBA32, false) {
							filterMode = FilterMode.Point,
							wrapMode = TextureWrapMode.Clamp,
						};
						skin.Texture.SetPixels32(pixels32);
						skin.Texture.Apply();
					}
				}
				return skin;
			} catch {
				return null;
			}
		}


		public static byte[] SkinToByte (SkinData skin) {
			var list = new List<byte>();
			if (skin is null) { return null; }
			skin.Fillup();
			try {
				// Json
				var json = JsonUtility.ToJson(skin, true);
				if (string.IsNullOrEmpty(json)) { return null; }
				var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
				list.AddRange(System.BitConverter.GetBytes(jsonBytes.Length));
				list.AddRange(jsonBytes);
				// PNG
				if (skin.PngBytes != null && skin.PngBytes.Length > 0) {
					list.AddRange(System.BitConverter.GetBytes(skin.PngBytes.Length));
					list.AddRange(skin.PngBytes);
				} else {
					list.AddRange(System.BitConverter.GetBytes(0));
				}
			} catch {
				return null;
			}
			return list.ToArray();
		}


		public void Fillup () {
			if (Items is null) { Items = new List<AnimatedItemData>(); }
			int typeCount = System.Enum.GetNames(typeof(SkinType)).Length;
			// Fill
			while (Items.Count < typeCount) {
				Items.Add(new AnimatedItemData() {
					FrameDuration = 120,
					Rects = new List<AnimatedItemData.RectData>(),
				});
			}
			// Fix
			for (int i = 0; i < Items.Count; i++) {
				var item = Items[i];
				if (item is null) {
					item = new AnimatedItemData();
				}
				if (item.Rects is null) {
					item.Rects = new List<AnimatedItemData.RectData>();
				}
				Items[i] = item;
			}
		}


		public Vector3 TryGetItemSize (int itemIndex, int rectIndex) {
			if (Items is null || itemIndex < 0 || itemIndex >= Items.Count) { return default; }
			var item = Items[itemIndex];
			if (item.Rects is null || item.Rects.Count == 0) { return default; }
			var rect = item.Rects[Mathf.Clamp(rectIndex, 0, item.Rects.Count - 1)];
			return new Vector3(rect.Width, rect.Height, rect.Is3D ? rect.Thickness3D : 0f);
		}


		public Vector4 TryGetItemBorder (int index, int rectIndex) {
			if (Items is null || index < 0 || index >= Items.Count) { return default; }
			var item = Items[index];
			if (item.Rects is null || item.Rects.Count == 0) { return default; }
			var rect = item.Rects[Mathf.Clamp(rectIndex, 0, item.Rects.Count - 1)];
			return new Vector4(rect.BorderL, rect.BorderR, rect.BorderD, rect.BorderU);
		}


		public float TryGetItemMinHeight (int itemIndex, int rectIndex) {
			if (Items is null || itemIndex < 0 || itemIndex >= Items.Count) { return default; }
			var item = Items[itemIndex];
			if (item.Rects is null || item.Rects.Count == 0) { return default; }
			return item.Rects[Mathf.Clamp(rectIndex, 0, item.Rects.Count - 1)].MinHeight;
		}


		public int TryGetItemCount (SkinType type) {
			int index = (int)type;
			if (index >= 0 && index < Items.Count) {
				var ani = Items[index];
				return ani != null && ani.Rects != null ? ani.Rects.Count : 0;
			}
			return 0;
		}


		public void SetPng (Texture2D texture) {
			Texture = texture;
			PngBytes = texture != null ? texture.EncodeToPNG() : null;
		}


	}


	[System.Serializable]
	public class AnimatedItemData {


		// SUB
		[System.Serializable]
		public struct RectData {

			public int Thickness3D_UI {
				get => Mathf.Max(Thickness3D, 0);
				set => Thickness3D = Mathf.Max(value, 0);
			}

			public int R => X + Width;
			public int U => Y + Height;
			public int L => X;
			public int D => Y;

			public int X;
			public int Y;
			public int Width;
			public int Height;
			public float MinHeight;

			public int BorderU;
			public int BorderD;
			public int BorderL;
			public int BorderR;

			public bool Is3D;
			public int Thickness3D;


			public static RectData MinMax (int xMin, int xMax, int yMin, int yMax) => new RectData(xMin, yMin, xMax - xMin, yMax - yMin);

			public RectData (int x, int y, int width, int height, int borderU = 0, int borderD = 0, int borderL = 0, int borderR = 0) {
				X = x;
				Y = y;
				Width = width;
				Height = height;
				BorderU = borderU;
				BorderD = borderD;
				BorderL = borderL;
				BorderR = borderR;
				Is3D = false;
				Thickness3D = 0;
				MinHeight = height;
			}

		}


		[System.Serializable]
		public struct ColorData {
			public byte R;
			public byte G;
			public byte B;
			public byte A;
			public ColorData (byte r, byte g, byte b, byte a) {
				R = r;
				G = g;
				B = b;
				A = a;
			}
		}


		// API
		public float TotalDuration => FrameDuration / 1000f * Rects.Count;
		public Color32 HighlightTint {
			get => new Color32(Highlight.R, Highlight.G, Highlight.B, Highlight.A);
			set {
				Highlight = new ColorData(value.r, value.g, value.b, value.a);
			}
		}

		// Ser
		public List<RectData> Rects = new List<RectData>();
		public ColorData Highlight = new ColorData(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		public int FrameDuration = 200;
		public bool FixedRatio = false;

		// API
		public void SetDuration (int durationMS) => FrameDuration = Mathf.Max(durationMS, 1);


		public int GetFrame (int itemType, int loopType, float lifeTime) {
			int count = Rects.Count;
			float spf = FrameDuration / 1000f;
			if (count <= 1 || FrameDuration == 0) { return 0; }
			switch (loopType) {
				default:
				case 0: // Item Type
					return Mathf.Clamp(itemType, 0, count - 1);
				case 1: // Forward
					return Mathf.Clamp(Mathf.FloorToInt(
						Mathf.Clamp(lifeTime, 0f, TotalDuration) / spf
					), 0, count - 1);
				case 2: // Loop
					return Mathf.Clamp(Mathf.FloorToInt(
						Mathf.Repeat(lifeTime, TotalDuration) / spf
					), 0, count - 1);
			}
		}


	}



}