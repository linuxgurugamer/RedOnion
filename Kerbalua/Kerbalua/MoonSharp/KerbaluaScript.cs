using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using RedOnion.KSP.Autopilot;
using UnityEngine;
using RedOnion.KSP.MathUtil;
using System;
using KSP.UI.Screens;
using RedOnion.KSP.MoonSharp.Proxies;
using Kerbalua.Parsing;
using RedOnion.UI;
using System.Reflection;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using API = RedOnion.KSP.API;
using RedOnion.KSP.ReflectionUtil;
using System.Diagnostics;
using RedOnion.KSP.API;
using System.Linq.Expressions;

namespace Kerbalua.MoonSharp
{
	public class KerbaluaScript : Script
	{
		public Action<string> PrintErrorAction { get; set; }

		public KerbaluaScript() : base(CoreModules.Preset_Complete)
		{
			UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;
			
			GlobalOptions.CustomConverters
				.SetClrToScriptCustomConversion(
					(Script script, ModuleControlSurface m)
						=> DynValue.FromObject(script, new LuaProxy(m)) //DynValue.NewTable(new ModuleControlSurfaceProxyTable(this, m))
					);
			GlobalOptions.CustomConverters
				.SetScriptToClrCustomConversion(DataType.Function
					, typeof(Func<object,object>), (f) => new Func<object, object>((item) =>
					{
						var co = CreateCoroutine(f);
						co.Coroutine.AutoYieldCounter = 10000;
						object retval=co.Coroutine.Resume(item);
						if (co.Coroutine.State == CoroutineState.ForceSuspended)
						{
							PrintErrorAction?.Invoke("Action<object> callback unable to finish");
							return null;
						}
						return retval;
					}));
			GlobalOptions.CustomConverters
				.SetScriptToClrCustomConversion(DataType.Function
					, typeof(System.Action<object>), (f) => new Action<object>((item) =>
				{

					var co = CreateCoroutine(f);
					co.Coroutine.AutoYieldCounter = 10000;
					co.Coroutine.Resume(item);
					if (co.Coroutine.State == CoroutineState.ForceSuspended)
					{
						PrintErrorAction?.Invoke("Action<object> callback unable to finish");
					}
				}));
			GlobalOptions.CustomConverters
				.SetScriptToClrCustomConversion(DataType.Function
					, typeof(UnityAction), (f) => new UnityAction(() =>
				{
					var co = CreateCoroutine(f);
					co.Coroutine.AutoYieldCounter = 10000;
					co.Coroutine.Resume();
					if (co.Coroutine.State == CoroutineState.ForceSuspended)
					{
						PrintErrorAction?.Invoke("UnityAction callback unable to finish");
					}
				}));
			Globals.MetaTable = API.LuaGlobals.Instance;
			//Globals["Vessel"] = FlightGlobals.ActiveVessel;
			var defaultMappings = NamespaceMappings.DefaultAssemblies;
			Globals["new"] = new DelegateTypeNew(New);
			Globals["static"] = new Func<object, DynValue>((o) =>
			{
				if (o is Type t)
				{
					return UserData.CreateStatic(t);
				}
				return UserData.CreateStatic(o.GetType());
			});

			Globals["gettype"] = new Func<object, DynValue>((o) =>
			{
				if (o is DynValue d && d.Type==DataType.UserData)
				{
					return DynValue.FromObject(this,d.UserData.Descriptor.Type);
				}
				if (o is Type t)
				{
					return DynValue.FromObject(this,t);
				}
				return DynValue.FromObject(this,o.GetType());
			});
			Globals["assemblies"] = new Func<List<Assembly>>(() =>
			{
				return AppDomain.CurrentDomain.GetAssemblies().ToList();
			});

			Globals["printall"] = DoString(
			@"
				return function(lst) for i=0,lst.Count-1 do print(i..' '..lst[i].ToString()) end end
				");
			//Globals["Assembly"] = typeof(Assembly);
			//Assembly blah;
			Globals["import"] = defaultMappings.GetNamespace("");
			//Globals["Coll"] = allMappings.GetNamespace("System.Collections.Generic");
			Globals["reldir"] = new Func<double, double, RelativeDirection>((heading, pitch) => new RelativeDirection(heading, pitch));
			//Globals["AppDomain"] = UserData.CreateStatic(typeof(AppDomain));
			//Globals["AssemblyStatic"] = UserData.CreateStatic(typeof(Assembly));
			//UserData.RegisterExtensionType(typeof(System.Linq.Enumerable));
			//UserData.RegisterAssembly(Assembly.GetAssembly(typeof(System.Linq.Enumerable)),true);
			var coroutines=Globals["coroutine"] as Table;
			var coroYield=coroutines["yield"];
			Globals["globals"] = UserData.CreateStatic(typeof(Globals));
			
			Globals["sleep"] = new Action<double>((double waittimeSeconds) =>
			{
				//PrintErrorAction("start");
				//UnityEngine.Debug.Log("start");
				sleeptimeMillis=waittimeSeconds*1000;
				sleepwatch.Start();
				coroutine.Coroutine.AutoYieldCounter=0;
				//UnityEngine.Debug.Log("end");
				//PrintErrorAction("end");
			});
			Globals["coroutine"]=null;

			//coroutines.Clear();


		}

		public int TestFunc(int a,double b,out string outstring)
		{
			outstring=a+","+b;
			return (int)(a + b);
		}

		//private Table ImporterImpl(string name)
		//{
		//	var a=Assembly.GetAssembly(GetType());

		//}
		double sleeptimeMillis=0;
		Stopwatch sleepwatch=new Stopwatch();
		delegate Table Importer(string name);

		//Stack<DynValue> coroutineStack=new Stack<DynValue>();
		DynValue coroutine;

		public bool Evaluate(out DynValue result)
		{
			if (coroutine == null)
			{
				throw new System.Exception("Coroutine not set in KerbaluaScript");
			}

			if (sleepwatch.IsRunning)
			{
				if (sleepwatch.ElapsedMilliseconds>sleeptimeMillis)
				{
					sleeptimeMillis=0;
					sleepwatch.Reset();
				}
				else
				{
					result=null;
					return false;
				}
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
			sleepwatch.Reset();
			sleeptimeMillis=0;
		}

		delegate object DelegateTypeNew(object obj, params DynValue[] args);
		object New(object obj, params DynValue[] dynArgs)
		{
			Type type=obj as Type;
			var constructors = type.GetConstructors();
			foreach (var constructor in constructors)
			{
				var parinfos = constructor.GetParameters();
				if (parinfos.Length >= dynArgs.Length)
				{
					object[] args = new object[parinfos.Length];

					for (int i = 0; i < args.Length; i++)
					{
						var parinfo = parinfos[i];
						if (i>= dynArgs.Length)
						{
							if (!parinfo.IsOptional)
							{
								goto nextConstructor;
							}
							args[i] = parinfo.DefaultValue;
						}
						else
						{
							if (parinfo.ParameterType.IsValueType)
							{
								try
								{
									args[i] = System.Convert.ChangeType(dynArgs[i].ToObject(), parinfo.ParameterType);
								}
								catch (Exception)
								{
									goto nextConstructor;
								}
							}
							else
							{
								args[i] = dynArgs[i].ToObject();
							}
						}

					}

					return constructor.Invoke(args);
				}
			nextConstructor:;
			}

			if (dynArgs.Length == 0)
			{
				return Activator.CreateInstance(type);
			}
			throw new Exception("Could not find constructor accepting given args for type " + type);
		}
	}
}
