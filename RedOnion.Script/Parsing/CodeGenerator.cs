﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedOnion.Script.Parsing
{
	partial class Parser
	{
		private byte[] _code = new byte[256];
		private string[] _stringTable = new string[64];
		private Dictionary<string, int> _stringMap = new Dictionary<string, int>();

		/// <summary>
		/// Buffer for final code (statements and expressions)
		/// </summary>
		public byte[] Code
		{
			get => _code;
			private set => _code = value;
		}
		/// <summary>
		/// Write position (top) for code buffer
		/// </summary>
		public int CodeAt { get; protected set; }

		/// <summary>
		/// Strings table for code
		/// </summary>
		protected string[] Strings => _stringTable;
		/// <summary>
		/// Write position (top) for string table
		/// </summary>
		protected int StringsAt { get; set; }

		/// <summary>
		/// Copy code to separate byte array
		/// </summary>
		public byte[] CodeToArray()
		{
			var arr = new byte[CodeAt];
			Array.Copy(Code, 0, arr, 0, arr.Length);
			return arr;
		}
		/// <summary>
		/// Copy string table to separate byte array
		/// </summary>
		public string[] StringsToArray()
		{
			var arr = new string[StringsAt];
			Array.Copy(Strings, 0, arr, 0, arr.Length);
			return arr;
		}

		/// <summary>
		/// Ensure code buffer capacity
		/// </summary>
		protected void Reserve(int size)
		{
			var need = CodeAt + size;
			var cap = Code.Length;
			if (need < cap)
				return;
			do cap <<= 1; while (need > cap);
			Array.Resize(ref _code, cap);
		}

		/// <summary>
		/// Write single byte to code buffer
		/// </summary>
		protected void Write(byte value)
		{
			Reserve(1);
			Code[CodeAt++] = value;
		}

		/// <summary>
		/// Write single byte to code buffer at specified position
		/// </summary>
		protected void Write(byte value, int codeAt)
		{
			Code[codeAt++] = value;
		}

		/// <summary>
		/// Write two bytes to code buffer
		/// </summary>
		protected void Write(ushort value)
		{
			Reserve(2);
			Code[CodeAt++] = (byte)value);
			Code[CodeAt++] = (byte)(value >> 8);
		}

		/// <summary>
		/// Write two bytes to code buffer at specified position
		/// </summary>
		protected void Write(ushort value, int codeAt)
		{
			Code[codeAt++] = (byte)value);
			Code[codeAt++] = (byte)(value >> 8);
		}

		/// <summary>
		/// Write integer (4B LE) to code buffer
		/// </summary>
		protected void Write(int value)
		{
			Reserve(4);
			Code[CodeAt++] = (byte)value;
			Code[CodeAt++] = (byte)(value >> 8);
			Code[CodeAt++] = (byte)(value >> 16);
			Code[CodeAt++] = (byte)(value >> 24);
		}

		/// <summary>
		/// Write integer (4B LE) to code buffer at specified position
		/// </summary>
		protected void Write(int value, int codeAt)
		{
			Code[codeAt++] = (byte)value;
			Code[codeAt++] = (byte)(value >> 8);
			Code[codeAt++] = (byte)(value >> 16);
			Code[codeAt++] = (byte)(value >> 24);
		}

		/// <summary>
		/// Write string (index to string table) to code buffer
		/// </summary>
		protected void Write(string value)
		{
			if (StringValuesAt == StringValues.Length)
				Array.Resize(ref _stringValues, StringValues.Length << 1);
			StringValues[StringValuesAt++] = value;

			Write(Encoding.UTF8.GetBytes(value));
		}

		/// <summary>
		/// write byte array to code buffer
		/// </summary>
		protected void Write(byte[] bytes)
		{
			Reserve(bytes.Length);
			Array.Copy(bytes, 0, Code, CodeAt, bytes.Length);
			CodeAt += bytes.Length;
		}

		/// <summary>
		/// write string literal or identifier to code buffer
		/// @opcode	leading code (type of literal or identifier)
		/// @blen	true for byte-length (e.g. identifier), false for short/int length (string literal)
		/// @string	value to write to code buffer
		/// </summary>
		protected void Write(OpCode opcode, bool blen, string @string)
		{
			Write(opcode, blen, Encoding.UTF8.GetBytes(@string));
		}

		/// <summary>
		/// write string literal or identifier to code buffer
		/// @opcode	leading code (type of literal or identifier)
		/// @blen	true for byte-length (e.g. identifier), false for short/int length (string literal)
		/// @bytes	array of bytes to write to code buffer
		/// </summary>
		protected void Write(Opcode opcode, bool blen, byte[] bytes)
		{
			if (blen)
			{
				if (bytes.Length > 255)
				{
					throw new InvalidOperationException("Byte array too long");
				}
				Reserve(2 + bytes.Length);
				Code[CodeAt++] = unchecked((byte)opcode);
				Code[CodeAt++] = unchecked((byte)bytes.Length);
			}
			else
			{
				Reserve(5 + bytes.Length);
				Code[CodeAt++] = unchecked((byte)opcode);
				Code[CodeAt++] = unchecked((byte)bytes.Length);
				Code[CodeAt++] = unchecked((byte)(bytes.Length >> 8));
				Code[CodeAt++] = unchecked((byte)(bytes.Length >> 16));
				Code[CodeAt++] = unchecked((byte)(bytes.Length >> 24));
			}
			Array.Copy(bytes, 0, Code, CodeAt, bytes.Length);
			CodeAt += bytes.Length;
		}

		/// <summary>
		/// copy string literal or identifier from vals to code buffer
		/// @opcode	leading code (type of literal or identifier)
		/// @blen	true for byte-length (e.g. identifier), false for int length (string literal)
		/// @string	value to write to code buffer
		/// </summary>
		protected void Copy(Opcode opcode, bool blen, int top, int start)
		{
			var len = top - start;
			if (blen)
			{
				if (len > 255)
				{
					throw new InvalidOperationException("Byte array too long");
				}
				Reserve(2 + len);
				Code[CodeAt++] = unchecked((byte)opcode);
				Code[CodeAt++] = unchecked((byte)len);
			}
			else
			{
				Reserve(5 + len);
				Code[CodeAt++] = unchecked((byte)opcode);
				Code[CodeAt++] = unchecked((byte)len);
				Code[CodeAt++] = unchecked((byte)(len >> 8));
				Code[CodeAt++] = unchecked((byte)(len >> 16));
				Code[CodeAt++] = unchecked((byte)(len >> 24));
			}
			Array.Copy(Vals, start, Code, CodeAt, len);
			CodeAt += len;
		}

		/// <summary>
		/// copy block from parsed values to code buffer
		/// </summary>
		protected void Copy(int top, int start)
		{
			var len = top - start;
			Reserve(len);
			Array.Copy(Vals, start, Code, CodeAt, len);
			CodeAt += len;
		}

		/// <summary>
		/// rewrite code from parsed value buffer/stack to code buffer
		/// </summary>
		protected virtual void Rewrite(int top, bool type = false)
		{
		full:
			var start = TopInt(top);
			top -= 4;
			var create = false;
		next:
			var op = ((Opcode)Vals[--top]).Extend();
			Debug.Assert(op.Kind() < Opkind.Statement);
			if (op.Kind() <= Opkind.Number)
			{
				if ((!type) && (!create))
				{
					Literal(op, top, start);
					return;
				}
				if (op == Opcode.Ident)
				{
					Copy(op, true, top, start);
					return;
				}
				Debug.Assert(((op > Opcode.Ident || op == Opcode.Undef) || op == Opcode.This) || op == Opcode.Null);
				Write(unchecked((byte)op));
				return;
			}
			Write(unchecked((byte)op));
			if (op.Unary())
			{
				create |= op == Opcode.Create;
				goto next;
			}
			if (op.Binary())
			{
				Rewrite(TopInt(top), type || create);
				if (op == Opcode.Dot)
				{
					start = TopInt(top);
					top -= 4;
					op = ((Opcode)Vals[--top]).Extend();
					if (op != Opcode.Ident)
					{
						throw new InvalidOperationException();
					}
					var len = top - start;
					if (len > 127)
					{
						throw new InvalidOperationException("Identifier too long");
					}
					Reserve(1 + len);
					Code[CodeAt++] = unchecked((byte)len);
					Array.Copy(Vals, start, Code, CodeAt, len);
					CodeAt += len;
					return;
				}
				if (op != Opcode.LogicAnd && op != Opcode.LogicOr)
				{
					goto full;
				}
				var second = CodeAt;
				Write(0);
				Rewrite(top);
				Write((CodeAt - second) - 4, second);
				return;
			}
			if (op.Ternary())
			{
				var mtop = TopInt(top);
				if (op == Opcode.Var)
				{
					Debug.Assert(!create);
					var varat = --CodeAt;
					Rewrite(TopInt(mtop));
					Code[varat] = unchecked((byte)Opcode.Var);
					Rewrite(mtop, true);
					goto full;
				}
				Rewrite(TopInt(mtop), type || create);
				if (op == Opcode.Ternary)
				{
					var second = CodeAt;
					Write(0);
					Rewrite(mtop);
					Write((CodeAt - second) - 4, second);
					var third = CodeAt;
					Write(0);
					Rewrite(top);
					Write((CodeAt - third) - 4, third);
					return;
				}
				Rewrite(mtop);
				goto full;
			}
			switch (op)
			{
			case Opcode.Array:
				type = false;
				create = true;
				break;
			case Opcode.Generic:
				type = true;
				break;
			default:
				Debug.Assert(op == Opcode.Mcall || op == Opcode.Mindex);
				break;
			}
			var n = Vals[--top];
			Write(unchecked((byte)n));
			Rewrite(top, n, type, create);
		}

		/// <summary>
		/// rewrite n-expressions/values from parsed value buffer/stack to code buffer
		/// </summary>
		protected void Rewrite(int top, int n, bool type = false, bool create = false)
		{
			if (n > 1)
			{
				Rewrite(TopInt(top), n - 1, type, create);
				create = false;
			}
			Rewrite(top, type || create);
		}

		/// <summary>
		/// rewrite literal from parsed value buffer/stack to code buffer
		/// </summary>
		protected virtual void Literal(Opcode op, int top, int start)
		{
			if (op < Opcode.Ident || op == Opcode.Exception)
			{
				Write(unchecked((byte)op));
				return;
			}
			Debug.Assert(((op == Opcode.Ident || op == Opcode.Number) || op == Opcode.String) || op == Opcode.Char);
			Copy(op, op != Opcode.String && op != Opcode.Char, top, start);
		}
	}
}
