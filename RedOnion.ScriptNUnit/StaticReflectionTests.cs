using System;
using NUnit.Framework;
using RedOnion.Script;
using RedOnion.Script.ReflectedObjects;

namespace RedOnion.ScriptNUnit
{
	[TestFixture]
	public class ROS_StaticReflectionTests : EngineTestsBase
	{
		[TearDown]
		public void ResetEngine() => Reset();

		public static class StaticClass
		{
			public static bool WasExecuted;
			public static void SimpleAction() => WasExecuted = true;

			public static bool ReturnTrue() => true;
			public static bool ReturnFalse() => false;

			public static object PassThrough(object obj) => obj;
			public static int SumTwoInts(int x, int y) => x + y;

			public static int Overloaded() => 1;
			public static uint Overloaded(uint x) => 2;
			public static long Overloaded(short x, byte y) => 3;

			public static float SomeValue { get; set; }
			public static double PI { get; } = Math.PI;
			public static bool SetOnly { set => WasExecuted = value; }

			public static void DoIt(Action action) => action();

			public static string str;
			public static void Exec(Action<string> action, string value) => action(value);
		}

		[Test]
		public void ROS_SRefl01_SimpleAction()
		{
			var creator = new ReflectedType(this, typeof(StaticClass));
			Assert.IsTrue(creator.Get("simpleAction", out var simpleAction));
			var simple = simpleAction.RefObj as ReflectedFunction;
			Assert.NotNull(simple);
			StaticClass.WasExecuted = false;
			simple.Call(null, 0);
			Assert.IsTrue(StaticClass.WasExecuted);

			Root.Set("testClass", creator);
			StaticClass.WasExecuted = false;
			Test("testClass.simpleAction()");
			Assert.IsTrue(StaticClass.WasExecuted);
		}

		[Test]
		public void ROS_SRefl02_SimpleFunctions()
		{
			Root.AddType(typeof(StaticClass));
			Test(true, "StaticClass.returnTrue()");
			Test(false, "staticClass.ReturnFalse()");
		}

		[Test]
		public void ROS_SRefl03_ComplexFunctions()
		{
			Root.AddType("testClass", typeof(StaticClass));
			Test("hello", "testClass.passThrough(\"hello\")");
			Test(3+4, "testClass.sumTwoInts(3,4)");
			Test(1, "testClass.overloaded()");
			Test(2, "testClass.overloaded(0)");
			Test(3, "testClass.overloaded(0,0)");
		}

		[Test]
		public void ROS_SRefl04_FieldAndProperties()
		{
			Root.AddType("testClass", typeof(StaticClass));
			Test(true, "testClass.wasExecuted = true");
			Assert.IsTrue(StaticClass.WasExecuted);

			// return value is what we pass in (double)
			Test(2.7, "testClass.someValue = 2.7");
			// the value is what the property likes (float)
			Test(2.7f, "testClass.someValue");

			Test(Math.PI, "testClass.pi");
			StaticClass.WasExecuted = false;
			Test(true, "testClass.setOnly = true");
			Test(true, "testClass.wasExecuted");
		}

		[Test]
		public void ROS_SRefl05_Delegate()
		{
			Root.AddType("testClass", typeof(StaticClass));
			StaticClass.WasExecuted = false;
			Test(
				"function action\n" +
				" testClass.wasExecuted = true\n" +
				"testClass.doIt action");
			Assert.IsTrue(StaticClass.WasExecuted);

			Test(
				"function setStr str\n" +
				" testClass.str = str\n" +
				"testClass.exec setStr, \"done\"");
			Assert.AreEqual("done", StaticClass.str);
		}

		public static class GenericTest
		{
			public static T Pass<T>(T value) => value;
		}
		[Test]
		public void ROS_SRefl06_GenericFunction()
		{
			Root.AddType("test", typeof(GenericTest));
			Test(1, "test.pass 1");
			Test(2u, "test.pass.[uint] 2");
		}

		public static class EventTest
		{
			public static event Action action;
			public static void DoAction() => action?.Invoke();
			public static void AddAction(Action a) => action += a;
			public static void RemoveAction(Action a) => action -= a;
			public static int NumberOfActions => action?.GetInvocationList().Length ?? 0;
		}
		[Test]
		public void ROS_SRefl07_Events()
		{
			Root.AddType("test", typeof(EventTest));
			Test("var counter = 0");
			Test("function action\n\tcounter++");
			Test(0, "test.numberOfActions");
			Test("test.addAction action");
			Test(1, "test.numberOfActions");
			Test("test.doAction()");
			Test(1, "counter");
			Test("test.removeAction action"); // see FunctionObj.GetDelegate/DelegateCache
			Test(0, "test.numberOfActions");
			Test("test.action += action");
			Test(1, "test.numberOfActions");
		}
	}
}