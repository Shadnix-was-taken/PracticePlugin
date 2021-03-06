﻿using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PracticePlugin
{
	public class SongSeeker : MonoBehaviour, IDragHandler, IPointerDownHandler
	{
		public float PlaybackPosition { get; private set; }
		
		[SerializeField] private AudioSource _songAudioSource;
		private LooperUI _looperUI;
	
		private Image _seekBackg;
		private Image _seekBar;
		private Image _seekCursor;
		private TMP_Text _currentTime;
		private TMP_Text _timeLength;
	
		private Camera _mainCamera;
	
		private const float AheadTime = 1f;
		
		public static readonly Vector2 SeekBarSize = new Vector2(100, 2);
		public static readonly float HalfSeekBarSize = SeekBarSize.x / 2;
		
		private static readonly Vector2 ParentSize = new Vector2(100, 4);
		private static readonly Color BackgroundColor = new Color(0, 0, 0, 0.25f);
		private static readonly Color ForegroundColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
	
		private static readonly Vector2 SeekCursorSize = new Vector2(4, 4);
		private static readonly Color SeekCursorColor = new Color(1, 1, 1, 0.5f);
	
		private static readonly Vector2 TimeTextSize = new Vector2(16, 8);
		private const float TimeTextMargin = 4;
		
		private const float StickToLooperCursorDistance = 0.02f;
		private const float LooperUITopMargin = -5f;
	
		private int _startTimeSamples;
	
		public void Init()
		{
			_songAudioSource = Plugin.AudioTimeSync.GetPrivateField<AudioSource>("_audioSource");
			var rectTransform = transform as RectTransform;
			rectTransform.anchorMin = Vector2.right * 0.5f;
			rectTransform.anchorMax = Vector2.right * 0.5f;
			rectTransform.sizeDelta = ParentSize;
			rectTransform.anchoredPosition = new Vector2(0, 16);
		
			_seekBackg = new GameObject("Background").AddComponent<Image>();
			rectTransform = _seekBackg.rectTransform;
			rectTransform.SetParent(transform, false);
			rectTransform.sizeDelta = SeekBarSize;
			_seekBackg.color = BackgroundColor;
	
			_seekBar = new GameObject("Seek Bar").AddComponent<Image>();
			rectTransform = _seekBar.rectTransform;
			rectTransform.SetParent(transform, false);
			rectTransform.sizeDelta = SeekBarSize;
			var tex = Texture2D.whiteTexture;
			var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100, 1);
			_seekBar.sprite = sprite;
			_seekBar.type = Image.Type.Filled;
			_seekBar.fillMethod = Image.FillMethod.Horizontal;
			_seekBar.color = ForegroundColor;
	
			_seekCursor = new GameObject("Seek Cursor").AddComponent<Image>();
			rectTransform = _seekCursor.rectTransform;
			rectTransform.SetParent(_seekBar.transform, false);
			rectTransform.anchorMin = Vector2.up * 0.5f;
			rectTransform.anchorMax = Vector2.up * 0.5f;
			rectTransform.sizeDelta = SeekCursorSize;
			_seekCursor.color = SeekCursorColor;
			
			_currentTime = new GameObject("Current Time").AddComponent<TextMeshProUGUI>();
			rectTransform = _currentTime.rectTransform;
			rectTransform.SetParent(transform, false);
			rectTransform.anchorMin = Vector2.up * 0.5f;
			rectTransform.anchorMax = Vector2.up * 0.5f;
			rectTransform.sizeDelta = TimeTextSize;
			rectTransform.anchoredPosition = new Vector2(-(TimeTextSize.x / 2) - TimeTextMargin, 0);
			_currentTime.enableAutoSizing = true;
			_currentTime.fontSizeMin = 1;
			_currentTime.alignment = TextAlignmentOptions.Right;
			_currentTime.text = "0:00";
		
			_timeLength = new GameObject("Time Length").AddComponent<TextMeshProUGUI>();
			rectTransform = _timeLength.rectTransform;
			rectTransform.SetParent(transform, false);
			rectTransform.anchorMin = new Vector2(1, 0.5f);
			rectTransform.anchorMax = new Vector2(1, 0.5f);
			rectTransform.sizeDelta = TimeTextSize;
			rectTransform.anchoredPosition = new Vector2(TimeTextSize.x / 2 + TimeTextMargin, 0);
			_timeLength.enableAutoSizing = true;
			_timeLength.fontSizeMin = 1;
			_timeLength.alignment = TextAlignmentOptions.Left;
			_timeLength.text = "0:00";
			
			var looperObj = new GameObject("Looper UI");
			looperObj.transform.SetParent(_seekBar.rectTransform, false);
			rectTransform = looperObj.AddComponent<RectTransform>();
			rectTransform.sizeDelta = SeekBarSize;
			rectTransform.anchoredPosition = new Vector2(0, LooperUITopMargin);
			_looperUI = looperObj.AddComponent<LooperUI>();
			_looperUI.Init(this);
			_looperUI.OnDragEndEvent += LooperUIOnOnDragEndEvent;
			
			if (_looperUI.StartTime != 0)
			{
				PlaybackPosition = _looperUI.StartTime;
				Invoke(nameof(ApplyPlaybackPosition), 0.1f);
				ApplyPlaybackPosition();
			}
	
			_mainCamera = Camera.main;
		}
	
		private void LooperUIOnOnDragEndEvent()
		{
			PlaybackPosition = Mathf.Clamp(PlaybackPosition, _looperUI.StartTime, _looperUI.EndTime);
		}
	
		private void OnEnable()
		{
			if (_songAudioSource == null || _songAudioSource.clip == null) return;
			_startTimeSamples = _songAudioSource.timeSamples;
			PlaybackPosition = (float) _songAudioSource.timeSamples / _songAudioSource.clip.samples;
			
			_timeLength.text = FormatTimeSpan(TimeSpan.FromSeconds(_songAudioSource.clip.length));
			UpdateCurrentTimeText(PlaybackPosition);
		}
	
		private void OnDisable()
		{
			if (_songAudioSource == null || _songAudioSource.clip == null) return;
			var newTimeSamples = Mathf.RoundToInt(Mathf.Lerp(0, _songAudioSource.clip.samples, PlaybackPosition));
			if (_startTimeSamples == newTimeSamples) return;
			ApplyPlaybackPosition();
		}
	
		public void OnUpdate()
		{
			if (gameObject.activeInHierarchy || _looperUI == null || _songAudioSource == null || _songAudioSource.clip == null) return;
			var newPos = (_songAudioSource.time + 0.1f) / _songAudioSource.clip.length;
			if (newPos >= _looperUI.EndTime)
			{
				PlaybackPosition = _looperUI.StartTime;
				ApplyPlaybackPosition();
			}
		}
	
		private void LateUpdate()
		{
			var clampedPos = Mathf.Clamp(PlaybackPosition, _looperUI.StartTime, _looperUI.EndTime);
			_seekBar.fillAmount = clampedPos;
			_seekCursor.rectTransform.anchoredPosition =
				new Vector2(Mathf.Lerp(0, SeekBarSize.x, clampedPos), 0);
			UpdateCurrentTimeText(clampedPos);
		}
	
		public void OnDrag(PointerEventData eventData)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, eventData.position,
				_mainCamera, out var pos);
			var posX = pos.x + HalfSeekBarSize;
			PlaybackPosition = Mathf.InverseLerp(0, SeekBarSize.x, posX);
	
			CheckLooperCursorStick();
			UpdateCurrentTimeText(PlaybackPosition);
		}
	
		public void OnPointerDown(PointerEventData eventData)
		{
			eventData.useDragThreshold = false;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, eventData.pressPosition,
				_mainCamera, out var pos);
			
			if (pos.y < 0 || pos.y > SeekBarSize.y) return;
				
			var posX = pos.x + HalfSeekBarSize;
			PlaybackPosition = Mathf.InverseLerp(0, SeekBarSize.x, posX);
			
			CheckLooperCursorStick();
			UpdateCurrentTimeText(PlaybackPosition);
		}

		private void ApplyPlaybackPosition()
		{
			_songAudioSource.timeSamples = Mathf.RoundToInt(Mathf.Lerp(0, _songAudioSource.clip.samples, PlaybackPosition));
			_songAudioSource.time = _songAudioSource.time - Mathf.Min(AheadTime, _songAudioSource.time);
			SongSeekBeatmapHandler.OnSongTimeChanged(_songAudioSource.time, Mathf.Min(AheadTime, _songAudioSource.time));
		}
	
		private void CheckLooperCursorStick()
		{
			if (Mathf.Abs(PlaybackPosition - _looperUI.StartTime) <= StickToLooperCursorDistance)
			{
				PlaybackPosition = _looperUI.StartTime;
			}
			else if (Mathf.Abs(PlaybackPosition - _looperUI.EndTime) <= StickToLooperCursorDistance)
			{
				PlaybackPosition = _looperUI.EndTime;
			}
			
			PlaybackPosition = Mathf.Clamp(PlaybackPosition, _looperUI.StartTime, _looperUI.EndTime);
		}
	
		private void UpdateCurrentTimeText(float playbackPos)
		{
			_currentTime.text = FormatTimeSpan(TimeSpan.FromSeconds(Mathf.Lerp(0, _songAudioSource.clip.length, playbackPos)));
		}
		
		private static string FormatTimeSpan(TimeSpan ts)
		{
			return ts.ToString((int) ts.TotalHours > 0 ? @"h\:m\:ss" : @"m\:ss");
		}
	}
}
