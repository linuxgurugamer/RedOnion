using System;
using UnityEngine;

namespace Kerbalua.Gui {
	public class TextArea:UIElement,ILayoutRenderer {
		public GUIContent content = new GUIContent("");

		public virtual void Render(Rect rect,GUIStyle style=null)
		{
			SetNextControlAsMainControl();
			ClearCharEvent();
			if (style != null) {
				content.text = GUI.TextArea(rect, content.text,style);
			} else {
				content.text = GUI.TextArea(rect, content.text);
			}
		}

		public void Render()
		{
			SetNextControlAsMainControl();
			ClearCharEvent();
			content.text = GUILayout.TextArea(content.text);
		}
	}
}
