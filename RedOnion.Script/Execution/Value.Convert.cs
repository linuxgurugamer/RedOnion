using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace RedOnion.Script
{
	public partial struct Value
	{
		/// <summary>
		/// Get native object (string, int, ...)
		/// </summary>
		public object Native
		{
			get
			{
				switch (Type)
				{
				default:
					return null;
				case ValueKind.Object:
					return ptr == null ? null : ((IObject)ptr).Value.Native;
				case ValueKind.Reference:
					return ((IProperties)ptr).Get(str).Native;
				case ValueKind.String:
					return str;
				case ValueKind.Char:
					return data.Char;
				case ValueKind.Bool:
					return data.Bool;
				case ValueKind.Byte:
					return data.Byte;
				case ValueKind.UShort:
					return data.UShort;
				case ValueKind.UInt:
					return data.UInt;
				case ValueKind.ULong:
					return data.ULong;
				case ValueKind.SByte:
					return data.SByte;
				case ValueKind.Short:
					return data.Short;
				case ValueKind.Int:
					return data.Int;
				case ValueKind.Long:
					return data.Long;
				case ValueKind.Float:
					return data.Float;
				case ValueKind.Double:
					return data.Double;
				}
			}
		}

		/// <summary>
		/// Get right-value (unassignable, dereferenced)
		/// </summary>
		public Value RValue => Type == ValueKind.Reference ?
			((IProperties)ptr).Get(str) : this;
		/// <summary>
		/// Get referenced object (if object or reference; null otherwise)
		/// </summary>
		public IObject Deref => Type == ValueKind.Reference ?
			ptr as IObject : Type == ValueKind.Object ? (IObject)ptr : null;
		/// <summary>
		/// Set the value for references
		/// </summary>
		public void Set(Value value)
		{
			if (Type == ValueKind.Reference)
			{
				((IProperties)ptr).Set(str, value.Type == ValueKind.Reference ?
					((IProperties)value.ptr).Get(value.str) : value);
			}
		}

		/// <summary>
		/// Helper for compound assignment operators and increment/decrement
		/// </summary>
		public Value Self
		{
			get => RValue;
			set => Set(value);
		}

		/// <summary>
		/// Convert to number (numeric value) if something else
		/// </summary>
		public Value Number
		{
			get
			{
				switch (Type)
				{
				default:
					return new Value();
				case ValueKind.Object:
					return ptr == null ? new Value() : ((IObject)ptr).Value.Number;
				case ValueKind.Reference:
					return ((IProperties)ptr).Get(str).Number;
				case ValueKind.String:
					if (str == "")
						return new Value();
					return this.Double;
				case ValueKind.Char:
					return this.UShort;
				case ValueKind.Bool:
					return this.Byte;
				case ValueKind.Byte:
				case ValueKind.UShort:
				case ValueKind.UInt:
				case ValueKind.ULong:
				case ValueKind.SByte:
				case ValueKind.Short:
				case ValueKind.Int:
				case ValueKind.Long:
				case ValueKind.Float:
				case ValueKind.Double:
					return this;
				}
			}
		}

		/// <summary>
		/// Culture settings for formatting (invariant by default)
		/// </summary>
		public static CultureInfo Culture = CultureInfo.InvariantCulture;

		public static implicit operator string(Value value)
			=> value.ToString();
		public string String => ToString();
		public override string ToString()
		{
			switch (Type)
			{
			default:
				return "undefined";
			case ValueKind.Object:
				return ptr == null ? "null" : ((IObject)ptr).Value.String;
			case ValueKind.Reference:
				return ((IProperties)ptr).Get(str).String;
			case ValueKind.String:
				return str;
			case ValueKind.Char:
				return data.Char.ToString(Culture);
			case ValueKind.Bool:
				return data.Bool ? "true" : "false";
			case ValueKind.Byte:
				return data.Byte.ToString(Culture);
			case ValueKind.UShort:
				return data.UShort.ToString(Culture);
			case ValueKind.UInt:
				return data.UInt.ToString(Culture);
			case ValueKind.ULong:
				return data.ULong.ToString(Culture);
			case ValueKind.SByte:
				return data.SByte.ToString(Culture);
			case ValueKind.Short:
				return data.Short.ToString(Culture);
			case ValueKind.Int:
				return data.Int.ToString(Culture);
			case ValueKind.Long:
				return data.Long.ToString(Culture);
			case ValueKind.Float:
				return data.Float.ToString(Culture);
			case ValueKind.Double:
				return data.Double.ToString(Culture);
			}
		}

		public ValueKind Type => type;
		public bool IsString => (Type & ValueKind.fNum) != 0;
		public bool IsNumber => (Type & ValueKind.fNum) != 0;
		public bool Is64 => (Type & ValueKind.f64) != 0;
		public byte NumberSize => unchecked((byte)(((ushort)(Type & ValueKind.mSz)) >> 8));
		public bool Signed => (Type & ValueKind.fSig) != 0;
		public bool IsFloatigPoint => (Type & ValueKind.fFp) != 0;
		public bool IsNaN => (Type & ValueKind.fFp) != 0 && double.IsNaN(data.Double);

		public static implicit operator bool(Value value)
			=> value.Bool;
		public bool Bool => IsNumber ? IsFloatigPoint ?
			data.Double != 0 && !double.IsNaN(data.Double) :
			data.Long != 0 :
			Type == ValueKind.Object ? ptr != null :
			Type == ValueKind.String ? ptr != null && ((string)ptr).Length > 0 :
			Type == ValueKind.Reference ? ((IProperties)ptr).Get(str).Bool :
			false;

		public static implicit operator char(Value value)
			=> value.Char;
		public char Char
		{
			get
			{
				if (Type == ValueKind.String)
				{
					var s = ptr as string;
					return s == null || s.Length == 0 ? '\0' : s[0];
				}
				return IsNumber ? IsFloatigPoint ?
					(char)data.Double : (char)data.Long :
					Type == ValueKind.Reference ? ((IProperties)ptr).Get(str).Char :
					'\0';
			}
		}

		public static implicit operator double(Value value)
			=> value.Double;
		public double Double
		{
			get
			{
				if (Type == ValueKind.String)
				{
					if (ptr != null && double.TryParse((string)ptr,
						NumberStyles.Float, CultureInfo.InvariantCulture,
						out var v))
						return v;
					return double.NaN;
				}
				return IsNumber ? IsFloatigPoint ?
					data.Double : data.Long :
					Type == ValueKind.Reference ? ((IProperties)ptr).Get(str).Double :
					double.NaN;
			}
		}

		public static implicit operator long(Value value)
			=> value.Long;
		public long Long
		{
			get
			{
				if (Type == ValueKind.String)
				{
					if (ptr != null && long.TryParse((string)ptr,
						NumberStyles.Number, CultureInfo.InvariantCulture,
						out var v))
						return v;
					return 0;
				}
				return IsNumber ? IsFloatigPoint ?
					(long)data.Double : data.Long :
					Type == ValueKind.Reference ? ((IProperties)ptr).Get(str).Long :
					0;
			}
		}

		public static implicit operator ulong(Value value)
			=> value.ULong;
		public ulong ULong
		{
			get
			{
				if (Type == ValueKind.String)
				{
					if (ptr != null && ulong.TryParse((string)ptr,
						NumberStyles.Number, CultureInfo.InvariantCulture,
						out var v))
						return v;
					return 0;
				}
				return IsNumber ? IsFloatigPoint ?
					(ulong)data.Double : (ulong)data.Long :
					Type == ValueKind.Reference ? ((IProperties)ptr).Get(str).ULong :
					0;
			}
		}

		public float	Float	=> (float)	Double;
		public int		Int		=> (int)	Long;
		public uint		UInt	=> (uint)	ULong;
		public short	Short	=> (short)	Long;
		public ushort	UShort	=> (ushort)	ULong;
		public sbyte	SByte	=> (sbyte)	Long;
		public byte		Byte	=> (byte)	ULong;
	}
}