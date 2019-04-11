using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RedOnion.Script.BasicObjects
{
	/// <summary>
	/// String function (used to create new string objects)
	/// </summary>
	public class StringFun : BasicObject
	{
		/// <summary>
		/// Prototype of all string objects
		/// </summary>
		public StringObj Prototype { get; }

		public override ObjectFeatures Features
			=> ObjectFeatures.Function | ObjectFeatures.Constructor
			| ObjectFeatures.Converter | ObjectFeatures.TypeReference;
		public override Type Type => typeof(string);

		public StringFun(IEngine engine, IObject baseClass, StringObj prototype)
			: base(engine, baseClass, new Properties("prototype", prototype))
			=> Prototype = prototype;

		public override Value Call(IObject self, Arguments args)
			=> args.Length == 0 ? new Value("") : new Value(args[0].String);

		public override IObject Create(Arguments args)
			=> new StringObj(Engine, Prototype, args.Length == 0 ? "" : args[0].String);

		public override IObject Convert(object value)
			=> new StringObj(Engine, Prototype, value.ToString());
	}

	/// <summary>
	/// String object (string box)
	/// </summary>
	[DebuggerDisplay("{Name}: {String}")]
	[Creator(typeof(StringFun))]
	public class StringObj : BasicObject, IListObject
	{
		/// <summary>
		/// Boxed value
		/// </summary>
		public string String { get; protected set; }
		public override Value Value => new Value(String);

		/// <summary>
		/// Create String.prototype
		/// </summary>
		public StringObj(IEngine engine, IObject baseClass)
			: base(engine, baseClass)
			=> String = "";

		/// <summary>
		/// Create new string object boxing the string
		/// </summary>
		public StringObj(IEngine engine, StringObj baseClass, string value)
			: base(engine, baseClass, new Properties(StdProps))
			=> String = value;

		public override Value Index(Arguments args)
		{
			if (args.Length == 1)
			{
				var i = args[0];
				if (i.IsNumber)
					return String[i.Int];
			}
			return base.Index(args);
		}

		public int Count => String.Length;
		bool ICollection<Value>.IsReadOnly => true;
		bool IListObject.IsWritable => false;
		bool IListObject.IsFixedSize => true;

		public bool Contains(char c)
			=> String.Contains(c);
		public bool Contains(Value item)
			=> String.Contains(item.Char);

		void ICollection<Value>.CopyTo(Value[] array, int arrayIndex)
		{
			foreach (char c in String)
				array[arrayIndex++] = c;
		}

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();
		public IEnumerator<Value> GetEnumerator()
		{
			foreach (char c in String)
				yield return c;
		}

		public int IndexOf(char c)
			=> String.IndexOf(c);
		public int IndexOf(Value item)
			=> String.IndexOf(item.Char);
		Value IList<Value>.this[int index]
		{
			get => String[index];
			set => throw new NotImplementedException();
		}
		public char this[int index] => String[index];

		void ICollection<Value>.Add(Value item) => throw new NotImplementedException();
		void ICollection<Value>.Clear() => throw new NotImplementedException();
		bool ICollection<Value>.Remove(Value item) => throw new NotImplementedException();
		void IList<Value>.Insert(int index, Value item) => throw new NotImplementedException();
		void IList<Value>.RemoveAt(int index) => throw new NotImplementedException();

		public static IDictionary<string, Value> StdProps { get; } = new Dictionary<string, Value>()
		{
			{ "length", ArrayObj.LengthProp.Value },
			{ "substring", new Value(e => new SubstringMtd(e)) }
		};
		public class SubstringMtd : BasicObject
		{
			public SubstringMtd(IEngine engine)
				: base(engine) { }
			public override ObjectFeatures Features => ObjectFeatures.Function;
			public override Value Call(IObject self, Arguments args)
			{
				var str = ((StringObj)self).String;
				if (args.Length == 0) return str;
				var arg = args[0].Int;
				if (args.Length == 1) return str.Substring(arg);
				var arg2 = args[1].Int;
				return str.Substring(arg, arg2);
			}
		}
	}
}
