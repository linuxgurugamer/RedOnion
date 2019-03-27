using System.Runtime.Remoting.Proxies;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using RedOnion.KSP.Autopilot;
using UnityEngine;
using RedOnion.KSP.MathUtil;
using KSP.UI.Screens;
using RedOnion.KSP.Lua.Proxies;

namespace RedOnion.KSP.Lua
{
	public class KspApi
	{
		public FlightControl FlightControl = FlightControl.GetInstance();
		public FlightGlobals FlightGlobals = new FlightGlobals();
		public Time Time = new Time();
		public Mathf Mathf = new Mathf();
		public Vec Vec = new Vec();
		public EditorPanels EditorPanels = EditorPanels.Instance;
		public EditorLogic EditorLogic = EditorLogic.fetch;
		public ShipConstruction ShipConstruction = new ShipConstruction();
		public Random Random = new Random();
		public FlightDriver FlightDriver = new FlightDriver();
		public HighLogic HighLogic = new HighLogic();
		public StageManager StageManager = StageManager.Instance;
		public PartLoader PartLoader = PartLoader.Instance;
	}

	public class KerbaluaScript : MoonSharp.Interpreter.Script
	{
		public KerbaluaScript() : base(CoreModules.Preset_Complete)
		{
			UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;
			//UserData.RegisterProxyType<ModuleProxy, PartModule>(m => new ModuleProxy(m));

			//UserData.RegisterProxyType<ModuleControlSurfaceProxy, ModuleControlSurface>(m => new ModuleControlSurfaceProxy(m));
			GlobalOptions.CustomConverters
				.SetClrToScriptCustomConversion(
					(MoonSharp.Interpreter.Script script, ModuleControlSurface m)
						=> DynValue.NewTable(new ProxyTable(this, m))
					);
			//var a = new Table(this);
			//UserData.
			//var d=UserData.RegisterType<ModuleControlSurface>();
			//Debug.Log(d);
			//descr.RemoveMember("transformName");

			Globals["Ksp"] = new KspApi();
		}

		DynValue coroutine;
		public bool EvaluateWithCoroutine(string source, out DynValue result)
		{
			if (coroutine == null)
			{
				SetCoroutine(source);
			}

			coroutine.Coroutine.AutoYieldCounter = 1000;
			result = coroutine.Coroutine.Resume();
			//Debug.Log("result is " + result);
			//if (coroutine.Coroutine.State == CoroutineState.ForceSuspended) {
			//	Globals["suspended"] = result;
			//}

			bool isComplete = false;
			if (coroutine.Coroutine.State == CoroutineState.Dead)
			{
				//Debug.Log("It's dead jim " + result);

				//Debug.Log("Coroutine state is" + coroutine.Coroutine.State + ", result is " + result);
				isComplete = true;
				coroutine = null;
			}
			else
			{
				//Debug.Log("Coroutine state is" + coroutine.Coroutine.State + ", result is incomplete");
			}

			//DynValue result = base.DoString("return " + str);
			return isComplete;
		}

		void SetCoroutine(string source)
		{
			try
			{
				if (source.StartsWith("="))
				{
					source = "return " + source.Substring(1);
				}
				DynValue mainFunction = base.DoString("return function () " + source + " end");
				//Debug.Log("Main function is "+mainFunction);
				coroutine = CreateCoroutine(mainFunction);
				//Debug.Log("Coroutine is " + coroutine);
			}
			catch (SyntaxErrorException e)
			{
				//Doesn't work in general
				//DynValue mainFunction = base.DoString("return function () return " + source + " end");
				//Debug.Log("Main function is "+mainFunction);
				//coroutine = CreateCoroutine(mainFunction);
				//Debug.Log(e);
				//Debug.Log("Coroutine is " + coroutine);
				throw e;
			}

		}

		public void Terminate()
		{
			coroutine = null;
		}
	}
}