﻿using Kerbalui.Gui;
using LiveRepl.UI.ElementTypes;
using UnityEngine;

namespace LiveRepl.UI.Layout
{
	/// <summary>
	/// The Group that holds the Repl and related functionality.
	/// </summary>
	public class ReplGroup : Group
	{
		public ContentGroup contentGroup;

		public ReplGroup(ContentGroup contentGroup)
		{
			this.contentGroup=contentGroup;
		}

		protected override void SetChildRects()
		{
			//TODO
		}
	}
}