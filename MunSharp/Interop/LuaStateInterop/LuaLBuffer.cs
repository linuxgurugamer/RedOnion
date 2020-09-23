﻿// Disable warnings about XML documentation
#pragma warning disable 1591

using System.Text;

namespace MunSharp.Interpreter.Interop.LuaStateInterop
{
	public class LuaLBuffer 
	{
		public StringBuilder StringBuilder { get; private set; }
		public LuaState LuaState { get; private set; }

		public LuaLBuffer(LuaState l)
		{
			StringBuilder = new StringBuilder();
			LuaState = l;
		}


	}
}