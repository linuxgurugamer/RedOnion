using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using MoonSharp.Interpreter;

namespace RedOnion.KSP.Lua.Proxies
{
	public class ProxyException : Exception
	{
		public ProxyException(string message) : base(message)
		{
		}
	}

	/// <summary>
	/// A table that can act as if it were the proxied object.
	/// Works with intellisense via ProxiedObject reference.
	/// </summary>
	public class ProxyTable : Table
	{
		public object ProxiedObject;
		MoonSharp.Interpreter.Script script;
		public ProxyTable(MoonSharp.Interpreter.Script script, object proxied) : base(script)
		{
			this.script = script;
			ProxiedObject = proxied;

			var metatable = new Table(script);
			metatable["__index"] = new Func<Table, DynValue, object>(IndexFunc);
			metatable["__tostring"] = new Func<Table, DynValue>(ToString);
			MetaTable = metatable;

		}

		DynValue Index(Table table,DynValue key)
		{
			return key;
		}


		DynValue ToString(Table table)
		{
			return DynValue.NewString(ProxiedObject.ToString());
		}

		object IndexFunc(Table table, DynValue key)
		{
			if (key.Type != DataType.String)
			{
				throw new ProxyException("Index for proxied object " + ProxiedObject.GetType() + " must be string");
			}
			string memberName = key.String;

			Type t = ProxiedObject.GetType();
			FieldInfo f=t.GetField(memberName);
			if (f != null)
			{
				return f.GetValue(ProxiedObject);
			}

			PropertyInfo p = t.GetProperty(memberName);
			if (p != null)
			{
				return p.GetValue(ProxiedObject, null);
			}

			MethodInfo mi = null;
			try
			{
				mi = t.GetMethod(memberName);
			}
			catch (AmbiguousMatchException)
			{
				MethodInfo[] mis = t.GetMethods();
				foreach (var methodInfo in mis)
				{
					if (methodInfo.Name == memberName)
					{
						mi = methodInfo;
						break;
					}
				}
			}

			InvokerClass invokerObject = new InvokerClass(mi, ProxiedObject);
			object o = new Invoker(invokerObject.HandleInvoker);
			if (mi != null)
			{
				return o;
				//return new ProxyCallTable(script, ProxiedObject, memberName);
			}
			throw new NotImplementedException("Member "+memberName+" was not in proxied object "+ProxiedObject.GetType());
		}

		class InvokerClass
		{
			MethodInfo mi;
			object ProxiedObject;

			public InvokerClass(MethodInfo mi,object ProxiedObject)
			{
				this.mi = mi;
				this.ProxiedObject = ProxiedObject;
			}

			public object HandleInvoker(params object[] args)
			{
				return mi.Invoke(ProxiedObject, args);
			}
		}

		delegate object Invoker(params object[] args);
	}
}
