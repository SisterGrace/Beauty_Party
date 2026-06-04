using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using GameBase;

namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_Waiting : UIWidget
	{
		private float mRotateSpeed = -360f;
		bool mIsLoading = false;
		public void Open(string message="")
		{
			gameObject.SetActive(true);
			mIsLoading = true;
			if (string.IsNullOrEmpty(message))
			{
				mLoadingTxt.gameObject.Hide();
			}
			else
			{
				mLoadingTxt.gameObject.Show();
				mLoadingTxt.SetLanguage(message);
			}
		}

		protected override void OnUpdate()
		{
			if (mIsLoading)
			{
				mloadIcon.Rotate(0, 0, mRotateSpeed * Time.unscaledDeltaTime);
			}
			
		}
		public void Hide()
		{
			gameObject.SetActive(false);
			mIsLoading = false;
			mLoadingTxt.text = "";
		}
	}
}
