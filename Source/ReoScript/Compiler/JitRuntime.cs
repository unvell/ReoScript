/*****************************************************************************
 *
 * ReoScript - .NET Script Language Engine
 *
 * https://github.com/unvell/ReoScript
 *
 * This software released under MIT license.
 * Copyright (c) 2012-2026 Jingwood, unvell.com, all rights reserved.
 *
 ****************************************************************************/

using System;
using Antlr.Runtime.Tree;
using unvell.ReoScript.Core.Statement;

namespace unvell.ReoScript.Compiler
{
	/// <summary>
	/// Static helper methods called from JIT-emitted IL code.
	///
	/// Every method here is public static so that DynamicMethod IL can
	/// emit Call instructions to them. They bridge the gap between the
	/// typed IL world and the loosely-typed ReoScript runtime.
	///
	/// Naming convention: the IL emitter refers to these via
	/// typeof(JitRuntime).GetMethod("MethodName").
	/// </summary>
	public static class JitRuntime
	{
		// ── Variable access ──────────────────────────────────────────

		public static object GetVariable(ScriptContext ctx, string name)
		{
			return ctx[name];
		}

		public static void SetVariable(ScriptContext ctx, string name, object value)
		{
			ctx[name] = value;
		}

		// ── Arithmetic ───────────────────────────────────────────────

		public static object Add(object left, object right)
		{
			if (left == null && right == null) return null;
			if (left == null) return right;
			if (right == null) return left;

			if (ScriptRunningMachine.IsPrimitiveNumber(left)
				&& ScriptRunningMachine.IsPrimitiveNumber(right))
			{
				return ScriptRunningMachine.GetNumberValue(left)
					 + ScriptRunningMachine.GetNumberValue(right);
			}

			return Convert.ToString(left) + Convert.ToString(right);
		}

		public static object Subtract(object left, object right)
		{
			return ScriptRunningMachine.GetNumberValue(left)
				 - ScriptRunningMachine.GetNumberValue(right);
		}

		public static object Multiply(object left, object right)
		{
			return ScriptRunningMachine.GetNumberValue(left)
				 * ScriptRunningMachine.GetNumberValue(right);
		}

		public static object Divide(object left, object right)
		{
			double r = ScriptRunningMachine.GetNumberValue(right);
			if (r == 0) return double.PositiveInfinity;
			return ScriptRunningMachine.GetNumberValue(left) / r;
		}

		public static object Modulo(object left, object right)
		{
			return ScriptRunningMachine.GetNumberValue(left)
				 % ScriptRunningMachine.GetNumberValue(right);
		}

		public static object Negate(object val)
		{
			return -ScriptRunningMachine.GetNumberValue(val);
		}

		// ── Comparison ───────────────────────────────────────────────

		public static bool LessThan(object left, object right)
		{
			if (ScriptRunningMachine.IsPrimitiveNumber(left)
				&& ScriptRunningMachine.IsPrimitiveNumber(right))
			{
				return ScriptRunningMachine.GetNumberValue(left)
					 < ScriptRunningMachine.GetNumberValue(right);
			}
			return false;
		}

		public static bool LessOrEqual(object left, object right)
		{
			if (ScriptRunningMachine.IsPrimitiveNumber(left)
				&& ScriptRunningMachine.IsPrimitiveNumber(right))
			{
				return ScriptRunningMachine.GetNumberValue(left)
					<= ScriptRunningMachine.GetNumberValue(right);
			}
			return false;
		}

		public static bool GreaterThan(object left, object right)
		{
			if (ScriptRunningMachine.IsPrimitiveNumber(left)
				&& ScriptRunningMachine.IsPrimitiveNumber(right))
			{
				return ScriptRunningMachine.GetNumberValue(left)
					 > ScriptRunningMachine.GetNumberValue(right);
			}
			return false;
		}

		public static bool GreaterOrEqual(object left, object right)
		{
			if (ScriptRunningMachine.IsPrimitiveNumber(left)
				&& ScriptRunningMachine.IsPrimitiveNumber(right))
			{
				return ScriptRunningMachine.GetNumberValue(left)
					>= ScriptRunningMachine.GetNumberValue(right);
			}
			return false;
		}

		public static bool AreEqual(object left, object right)
		{
			if (left == null && right == null) return true;
			if (left == null || right == null) return false;

			if (ScriptRunningMachine.IsPrimitiveNumber(left)
				&& ScriptRunningMachine.IsPrimitiveNumber(right))
			{
				return ScriptRunningMachine.GetNumberValue(left)
					== ScriptRunningMachine.GetNumberValue(right);
			}

			return left.Equals(right);
		}

		public static bool NotEquals(object left, object right)
		{
			return !AreEqual(left, right);
		}

		// ── Logical ──────────────────────────────────────────────────

		public static bool IsTrue(object val)
		{
			if (val == null) return false;
			if (val is bool b) return b;
			if (val is double d) return d != 0;
			if (val is int i) return i != 0;
			if (val is string s) return s.Length > 0;
			return true;
		}

		// ── Property access ──────────────────────────────────────────

		public static object GetProperty(ScriptContext ctx, object owner, string name)
		{
			return PropertyAccessHelper.GetProperty(ctx, owner, name);
		}

		public static void SetProperty(ScriptContext ctx, object owner, string name, object value)
		{
			PropertyAccessHelper.SetProperty(ctx, owner, name, value);
		}

		// ── Object / Array creation ──────────────────────────────────

		public static object CreateNewObject(ScriptContext ctx)
		{
			return ctx.Srm.CreateNewObject(ctx);
		}

		// ── Function calls ───────────────────────────────────────────

		public static object CallFunction(ScriptContext ctx, object owner, object funObj, object[] args)
		{
			return ctx.Srm.InvokeFunction(ctx, owner, funObj as AbstractFunctionObject, args);
		}

		public static object GetThisObject(ScriptContext ctx)
		{
			return ctx.ThisObject;
		}

		// ── Post-increment / decrement ───────────────────────────────

		public static object PostIncrement(ScriptContext ctx, string name)
		{
			object val = ctx[name];
			double d = ScriptRunningMachine.GetNumberValue(val);
			ctx[name] = d + 1;
			return d; // returns value before increment
		}

		public static object PreIncrement(ScriptContext ctx, string name)
		{
			object val = ctx[name];
			double d = ScriptRunningMachine.GetNumberValue(val) + 1;
			ctx[name] = d;
			return d;
		}

		public static object PostDecrement(ScriptContext ctx, string name)
		{
			object val = ctx[name];
			double d = ScriptRunningMachine.GetNumberValue(val);
			ctx[name] = d - 1;
			return d;
		}

		// ── Fallback to tree-walking ─────────────────────────────────

		/// <summary>
		/// When the JIT compiler encounters an AST node it doesn't yet
		/// support, it falls back to tree-walking by calling this method.
		/// This ensures correctness: the JIT is never "wrong", it may
		/// just be slower for unsupported constructs.
		/// </summary>
		public static object InterpretNode(ScriptContext ctx, CommonTree node)
		{
			return ScriptRunningMachine.ParseNode(node, ctx);
		}
	}
}
