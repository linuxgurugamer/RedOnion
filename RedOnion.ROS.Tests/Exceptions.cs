using System;
using System.Collections;
using NUnit.Framework;
using RedOnion.ROS.Objects;

namespace RedOnion.ROS.Tests
{
	[TestFixture]
	public class ROS_Exceptions : StatementTests
	{
		[TearDown]
		public override void Reset() => base.Reset();

		[Test]
		public void ROS_Catch01_NoThrow()
		{
			Lines(ExitCode.Return, true,
				"try",
				"return true");
			Lines(ExitCode.Return, true,
				"var ok = false",
				"try",
				"  ok = true",
				"return ok");
			Lines(ExitCode.Return, true,
				"try",
				"finally",
				"  return true",
				"return false");
			Lines(ExitCode.Return, true,
				"var ok = false",
				"try",
				"  ok = true",
				"catch string",
				"  ok = false",
				"finally",
				"  return ok");
		}

		public static void ThrowError()
			=> throw new InvalidOperationException("error");
		[Test]
		public void ROS_Catch02_Finally()
		{
			Lines(ExitCode.Exception, "thrown",
				"throw \"thrown\"");
			Lines(ExitCode.Exception, "thrown",
				"global.done = false",
				"throw \"thrown\"",
				"done = true");
			Assert.IsFalse(Globals["done"].ToBool());

			Lines(ExitCode.Exception, "catch",
				"global.done = false",
				"global.test = false",
				"try",
				"  throw \"catch\"",
				"  test = true",
				"  return false",
				"finally",
				"  done = true",
				"  return true", // must not override active exception
				"test = true",
				"return false");
			Assert.IsTrue(Globals["done"].ToBool());
			Assert.IsFalse(Globals["test"].ToBool());

			Lines(ExitCode.Exception, 3.14,
				"global.counter = 0",
				"global.test = false",
				"try",
				"  counter++",
				"  try",
				"    counter++",
				"    raise 3.14",
				"    test = true",
				"  finally",
				"    counter++",
				"  test = true",
				"  return 0",
				"finally",
				"  counter++",
				"test = true");
			Assert.AreEqual(4, Globals["counter"].ToInt());
			Assert.IsFalse(Globals["test"].ToBool());

			Globals["throwError"] = new Value(ThrowError);
			Expect<RuntimeError>("throwError");
			Assert.IsTrue((error.obj as RuntimeError)?.InnerException is InvalidOperationException);
			Assert.AreEqual("error", ((error.obj as RuntimeError)?.InnerException as InvalidOperationException)?.Message);

			Expect<RuntimeError>(
				"global.done = false",
				"global.test = false",
				"try",
				"  throwError",
				"  test = true",
				"finally",
				"  done = true",
				"test = true");
			Assert.IsTrue(Globals["done"].ToBool());
			Assert.IsFalse(Globals["test"].ToBool());
		}

		[Test]
		public void ROS_Catch03_FinallyWithDef()
		{
			Globals["throwError"] = new Value(ThrowError);
			Expect<RuntimeError>(
				"global.done = false",
				"global.test = false",
				"def throwIt",
				"  throwError",
				"  test = true",
				"try",
				"  throwIt",
				"  test = true",
				"finally",
				"  done = true",
				"test = true");
			Assert.IsTrue(Globals["done"].ToBool());
			Assert.IsFalse(Globals["test"].ToBool());
			Assert.IsTrue((error.obj as RuntimeError)?.InnerException is InvalidOperationException);
			Assert.AreEqual("error", ((error.obj as RuntimeError)?.InnerException as InvalidOperationException)?.Message);

			Expect<RuntimeError>(
				"global.counter = 0",
				"global.test = false",
				"def throwIt",
				"  throwError",
				"  test = true",
				"try",
				"  counter++",
				"  try",
				"    counter++",
				"    throwIt",
				"    test = true",
				"  finally",
				"    counter++",
				"  test = true",
				"  return 0",
				"finally",
				"  counter++",
				"test = true");
			Assert.IsFalse(Globals["test"].ToBool());
			Assert.AreEqual(4, Globals["counter"].ToInt());
			Assert.IsTrue((error.obj as RuntimeError)?.InnerException is InvalidOperationException);
			Assert.AreEqual("error", ((error.obj as RuntimeError)?.InnerException as InvalidOperationException)?.Message);

			Lines(ExitCode.Exception, 3.14,
				"global.counter = 0",
				"global.test = false",
				"def throwIt",
				"  raise 3.14",
				"  test = true",
				"try",
				"  counter++",
				"  try",
				"    counter++",
				"    throwIt",
				"    test = true",
				"  finally",
				"    counter++",
				"  test = true",
				"  return 0",
				"finally",
				"  counter++",
				"test = true");
			Assert.IsFalse(Globals["test"].ToBool());
			Assert.AreEqual(4, Globals["counter"].ToInt());
		}

		[Test]
		public void ROS_Catch04_SimpleCatch()
		{
			Lines(ExitCode.Return, true,
				"var result = false",
				"try",
				"  throw 1",
				"else",
				"  result = true",
				"return result");
			Lines(ExitCode.Return, true,
				"var result = false",
				"try",
				"  throw 1",
				"catch",
				"  result = true",
				"return result");
		}
	}
}
