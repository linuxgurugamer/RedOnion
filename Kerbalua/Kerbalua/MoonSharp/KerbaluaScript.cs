using System.Runtime.Remoting.Proxies;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using RedOnion.KSP.Autopilot;
using UnityEngine;
using RedOnion.KSP.MathUtil;
using System;
using KSP.UI.Screens;
using RedOnion.KSP.Lua.Proxies;
using Kerbalua.Parsing;
using RedOnion.UI;

namespace Kerbalua.MoonSharp
{
	public class KspApi
	{
		public FlightControl FlightControl = FlightControl.GetInstance();
		public FlightGlobals FlightGlobals = FlightGlobals.fetch;
		public Time Time = new Time();
		public Mathf Mathf = new Mathf();
		public Scalar Scalar = new Scalar();
		public Vec Vec = new Vec();
		public EditorPanels EditorPanels = EditorPanels.Instance;
		public EditorLogic EditorLogic = EditorLogic.fetch;
		public ShipConstruction ShipConstruction = new ShipConstruction();
		public UnityEngine.Random Random = new UnityEngine.Random();
		public FlightDriver FlightDriver = FlightDriver.fetch;
		public HighLogic HighLogic = HighLogic.fetch;
		public StageManager StageManager = StageManager.Instance;
		public PartLoader PartLoader = PartLoader.Instance;
	}

	public class UI
	{
		public Type Window = typeof(Window);
		public Type Panel = typeof(Panel);
		public Type Layout = typeof(Layout);
		public Type Button = typeof(Button);
		public Type Anchors = typeof(Anchors);
	}

	public class KerbaluaScript : Script
	{
		public KerbaluaScript() : base(CoreModules.Preset_Complete)
		{
			UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;

			GlobalOptions.CustomConverters
				.SetClrToScriptCustomConversion(
					(Script script, ModuleControlSurface m)
						=> DynValue.NewTable(new ModuleControlSurfaceProxyTable(this, m))
					);
			Globals.MetaTable = RedOnion.KSP.API.Globals.Instance;
			//Globals["Vessel"] = FlightGlobals.ActiveVessel;
			Globals["KSP"] = new KspApi();
			Globals["new"] = new Constructor(ConstructorImpl);
			Globals["UI"] = new UI();
			//Globals["import"] = new Importer(ImporterImpl);
		}

		//private Table ImporterImpl(string name)
		//{
		//	var a=Assembly.GetAssembly(GetType());

		//}

		delegate Table Importer(string name);

		object ConstructorImpl(Type t,params DynValue[] dynArgs)
		{
			var constructors = t.GetConstructors();
			foreach(var constructor in constructors)
			{
				var parinfos = constructor.GetParameters();
				if (parinfos.Length == dynArgs.Length)
				{ 
					object[] args = new object[parinfos.Length];

					for(int i = 0; i < args.Length; i++)
					{
						var parinfo = parinfos[i];
						if (parinfo.ParameterType.IsValueType)
						{
							try
							{
								args[i] = Convert.ChangeType(dynArgs[i].ToObject(), parinfo.ParameterType);
							}
							catch(Exception)
							{
								goto nextLoop;
							}
						}
						else
						{
							args[i] = dynArgs[i].ToObject();
						}
					}

					return constructor.Invoke(args);
				}
			nextLoop:;
			}

			if (dynArgs.Length == 0)
			{
				return Activator.CreateInstance(t);
			}
			throw new Exception("Could not find constructor accepting given args for type " + t);
		}
		delegate object Constructor(Type t,params DynValue[] args);

		DynValue coroutine;

		public bool Evaluate(out DynValue result)
		{
			if (coroutine == null)
			{
				throw new System.Exception("Coroutine not set in KerbaluaScript");
			}

			coroutine.Coroutine.AutoYieldCounter = 1000;
			result = coroutine.Coroutine.Resume();


			bool isComplete = false;
			if (coroutine.Coroutine.State == CoroutineState.Dead)
			{
				isComplete = true;
				coroutine = null;
			}

			return isComplete;
		}

		public void SetCoroutine(string source)
		{
			if (IncompleteLuaParsing.IsImplicitReturn(source))
			{
				source = "return " + source;
			}
			DynValue mainFunction = base.DoString("return function () " + source + " end");

			coroutine = CreateCoroutine(mainFunction);
		}

		public void Terminate()
		{
			coroutine = null;
		}
	}
}