﻿using System;
using UnityEngine;

namespace PracticePlugin
{
	public class SpeedSettingsCreator : MonoBehaviour
	{
		private GameObject _speedSettings;
		
		private void OnEnable()
		{
			_speedSettings = Instantiate(Plugin.SettingsObject, transform);
			_speedSettings.SetActive(true);
			_speedSettings.GetComponent<SpeedSettingsController>().Init();
			var rectTransform = (RectTransform) _speedSettings.transform;
			rectTransform.anchorMin = Vector2.right * 0.5f;
			rectTransform.anchorMax = Vector2.right * 0.5f;
			rectTransform.anchoredPosition = new Vector2(0, rectTransform.sizeDelta.y * 1.5f);
		}

		private void OnDisable()
		{
			DestroyImmediate(_speedSettings);
		}
	}
}