﻿using System;
using UnityEngine;

namespace PracticePlugin
{
	public class SpeedSettingsController : ListSettingsController
	{
		public event Action<float> ValueChangedEvent;
		
		private int _indexOffset;

		protected override void GetInitValues(out int idx, out int numberOfElements)
		{
			_indexOffset = Plugin.NoFail ? 1 : 20;
			numberOfElements = Mathf.RoundToInt(Plugin.MaxSize / Plugin.StepSize) - _indexOffset;
			idx = Mathf.RoundToInt(Plugin.TimeScale / Plugin.StepSize) - _indexOffset;
		}

		protected override void ApplyValue(int idx)
		{
		}

		protected override string TextForValue(int idx)
		{
			if (ValueChangedEvent != null)
			{
				ValueChangedEvent(Plugin.StepSize * (idx + _indexOffset));
			}
			return Plugin.StepSize * 100f * (idx + _indexOffset) + "%";
		}
	}
}