using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Logic for unlocking content in the main menu.
/// To unlock tracks and cars.
/// </summary>

public class WindowWithShopLogic :WindowWithShowHideAnimators
{
	[Header("Shop logic")]
	[SerializeField] GameObject LockedHolder;

	[SerializeField] GameObject PriceHolder;
	[SerializeField] TextMeshProUGUI PriceText;

	[SerializeField] GameObject CompleteTrackHolder;
	[SerializeField] TextMeshProUGUI CompleteTrackText;

	[SerializeField] ButtonState UnlockedState;
	[SerializeField] ButtonState MoneyLockState;
	[SerializeField] ButtonState TrackLockState;

	[SerializeField] protected Button SelectButton;
	[SerializeField] TextMeshProUGUI ButtonText;

	protected virtual void OnSelect () { }

	protected void RefreshButtonState (LockedContent lockContent)
	{
		PriceHolder.SetActive (false);
		CompleteTrackHolder.SetActive (false);

		if (lockContent.IsUnlocked)
		{
			LockedHolder.SetActive (false);
			SetButtonState (UnlockedState, OnSelect);
			SelectButton.interactable = true;
			return;
		}

		LockedHolder.SetActive (true);

		if (lockContent.GetUnlockType == LockedContent.UnlockType.UnlockByMoney)
		{
			PriceHolder.SetActive (true);
			PriceText.text = string.Format("${0}", lockContent.GetPrice);

			SelectButton.interactable = lockContent.CanUnlock;
			UnityAction clickAction = () =>
			{
				if (lockContent.TryUnlock ())
				{
					RefreshButtonState (lockContent);
				}
			};
			SetButtonState (MoneyLockState, clickAction);
			return;
		}

		CompleteTrackHolder.SetActive (true);
		CompleteTrackText.text = string.Format("{0}: {1}", lockContent.GetCompleteTrackForUnlock.TrackName, lockContent.GetCompleteTrackForUnlock.RegimeSettings.RegimeCaption);
		SetButtonState (TrackLockState);
		SelectButton.interactable = false;
	}

	void SetButtonState (ButtonState state, UnityAction onClickAction = null)
	{
		SelectButton.onClick.RemoveAllListeners ();
		if (onClickAction != null)
		{
			SelectButton.onClick.AddListener (onClickAction);
		}

		SelectButton.colors = state.ColorBlock;
		ButtonText.text = state.ButtonStr;
	}
}
