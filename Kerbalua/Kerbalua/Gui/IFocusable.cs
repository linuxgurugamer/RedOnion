﻿namespace Kerbalua.Gui {
    public interface IFocusable {
		string ControlName { get; }
		bool HasFocus();
		void GrabFocus();
	}
}