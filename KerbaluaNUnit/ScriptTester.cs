using System;
using MoonSharp.Interpreter;
using NUnit.Framework;

namespace KerbaluaNUnit
{
	public class ScriptTester
	{
		Script script;
		public ScriptTester(Script script)
		{
			this.script=script;
		}

		public DynValue MemberCheck(Table table, string key, Type type)
		{
			var d=GetCheck(table,key);
			TypeCheck(d, type);
			return d;
		}

		public DynValue MethodCheck(Table table, string key)
		{
			var d=GetCheck(table,key);
			TypeCheck(d, typeof(CallbackFunction));
			return d;
		}

		public DynValue StaticCheck(Table table, string key)
		{
			var d=GetCheck(table,key);
			Assert.True(IsStatic(d), key+" should have been a static");
			return d;
		}

		public DynValue GetCheck(Table table, string key)
		{
			script.Globals["t"]=table;
			DynValue d=script.DoString($"return t['{key}']");
			Assert.False(d.IsNil(), key+" should not have been nil");
			return d;
		}

		public Table NamespaceCheck(Table table, string key)
		{
			var d=GetCheck(table,key);
			Assert.NotNull(d.Table, key+" should have been a table");
			return d.Table;
		}

		public DynValue[] CallCheck(DynValue f,object[] args,object[] expectedOutput)
		{
			script.Globals["fun"]=f;
			script.Globals["args"]=new Table(script, ObjectsToDynValues(args));
			DynValue result=script.DoString($"return fun(table.unpack(args))");
			DynValue[] results=result.Tuple;
			if (results==null)
			{
				results=new DynValue[1];
				results[0]=result;
			}
			Assert.AreEqual(results.Length, expectedOutput.Length, "Return values number differs");
			for(int i = 0; i<results.Length; i++)
			{
				Assert.AreEqual(results[i].ToObject(), expectedOutput[i], i.ToString()+"th return value is wrong");
			}
			return results;
		}

		public void TypeCheck(DynValue d,Type type)
		{
			object o=d.ToObject();
			Assert.IsInstanceOf(type, o);
		}

		DynValue[] ObjectsToDynValues(object[] objs)
		{
			var ds=new DynValue[objs.Length];
			for(int i = 0; i<ds.Length; i++)
			{
				ds[i]=DynValue.FromObject(script,objs[i]);
			}
			return ds;
		}

		public bool IsStatic(DynValue d)
		{
			return d.UserData!=null && d.UserData.Object==null;
		}
	}
}
