using System;
using System.Collections.Generic;

namespace RedOnion.ROS
{
	public partial class Descriptor
	{
		public static readonly Descriptor String = new OfString();

		internal class OfString : Descriptor
		{
			public OfString() : base("string", typeof(string), ExCode.String, TypeCode.String) { }

			public override string ToString(ref Value self, string format, IFormatProvider provider, bool debug)
				=> self.obj.ToString();

			public override bool Convert(ref Value self, Descriptor to)
			{
				var str = self.obj.ToString();
				switch (to.Primitive)
				{
				case ExCode.String:
					self = str;
					return true;
				case ExCode.Char:
				case ExCode.WideChar:
					self = str.Length == 0 ? '\0' : str[0];
					return true;
				case ExCode.Byte:
					self = byte.Parse(str, Value.Culture);
					return true;
				case ExCode.UShort:
					self = ushort.Parse(str, Value.Culture);
					return true;
				case ExCode.UInt:
					self = uint.Parse(str, Value.Culture);
					return true;
				case ExCode.ULong:
					self = ulong.Parse(str, Value.Culture);
					return true;
				case ExCode.SByte:
					self = sbyte.Parse(str, Value.Culture);
					return true;
				case ExCode.Short:
					self = short.Parse(str, Value.Culture);
					return true;
				case ExCode.Int:
					self = int.Parse(str, Value.Culture);
					return true;
				case ExCode.Long:
					self = long.Parse(str, Value.Culture);
					return true;
				case ExCode.Float:
					self = float.Parse(str, Value.Culture);
					return true;
				case ExCode.Number:
				case ExCode.Double:
					self = double.Parse(str, Value.Culture);
					return true;
				case ExCode.Bool:
					self = bool.Parse(str);
					return true;
				}
				return false;
			}

			public override bool Binary(ref Value lhs, OpCode op, ref Value rhs)
			{
				if (op != OpCode.Add)
					return false;
				if (lhs.desc.Primitive != ExCode.String)
					lhs.desc.Convert(ref lhs, this);
				if (rhs.desc.Primitive != ExCode.String)
					rhs.desc.Convert(ref rhs, this);
				lhs.obj = lhs.obj.ToString() + rhs.obj.ToString();
				return true;
			}

			public override int Find(object self, string name, bool add)
			{
				if (string.Compare(name, "length", StringComparison.OrdinalIgnoreCase) == 0)
					return 0;
				return -1;
			}
			public override bool Get(ref Value self, int at)
			{
				switch (at)
				{
				case 0:
					self = self.obj.ToString().Length;
					return true;
				}
				return false;
			}
		}
	}
}
