using TK_Engine;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
namespace GameLogic
{
	[Window(UILayer.UI)]
	public partial class Widget_HandGuide : UIWidget
	{
		public void Open(Transform target,bool EffHand)
		{
			mHandAnim.gameObject.SetActive(!EffHand);
			mEffHand.SetActive(EffHand);
			transform.position = target.position;
		}

		public void Hide()
		{
		}
	}
}
