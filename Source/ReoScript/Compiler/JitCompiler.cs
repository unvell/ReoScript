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
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using Antlr.Runtime.Tree;
using unvell.ReoScript.Core;
using unvell.ReoScript.Core.Statement;

namespace unvell.ReoScript.Compiler
{
	/// <summary>
	/// Baseline JIT compiler for ReoScript.
	///
	/// Takes a compiled AST (CommonTree) and emits a DynamicMethod that
	/// executes the script by calling into JitRuntime helper methods.
	/// Unsupported AST nodes fall back to tree-walking via
	/// JitRuntime.InterpretNode, so correctness is always preserved.
	///
	/// Every compiled script has the signature:
	///     object CompiledScript(ScriptContext ctx)
	/// </summary>
	public sealed class JitCompiler
	{
		// ── Cached MethodInfo for JitRuntime helpers ──────────────────

		static readonly MethodInfo RT_GetVariable =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.GetVariable));
		static readonly MethodInfo RT_SetVariable =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.SetVariable));
		static readonly MethodInfo RT_Add =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.Add));
		static readonly MethodInfo RT_Subtract =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.Subtract));
		static readonly MethodInfo RT_Multiply =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.Multiply));
		static readonly MethodInfo RT_Divide =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.Divide));
		static readonly MethodInfo RT_Modulo =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.Modulo));
		static readonly MethodInfo RT_Negate =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.Negate));
		static readonly MethodInfo RT_LessThan =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.LessThan));
		static readonly MethodInfo RT_LessOrEqual =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.LessOrEqual));
		static readonly MethodInfo RT_GreaterThan =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.GreaterThan));
		static readonly MethodInfo RT_GreaterOrEqual =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.GreaterOrEqual));
		static readonly MethodInfo RT_Equals =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.AreEqual));
		static readonly MethodInfo RT_NotEquals =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.NotEquals));
		static readonly MethodInfo RT_IsTrue =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.IsTrue));
		static readonly MethodInfo RT_GetProperty =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.GetProperty));
		static readonly MethodInfo RT_SetProperty =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.SetProperty));
		static readonly MethodInfo RT_CallFunction =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.CallFunction));
		static readonly MethodInfo RT_GetThisObject =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.GetThisObject));
		static readonly MethodInfo RT_PostIncrement =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.PostIncrement));
		static readonly MethodInfo RT_PreIncrement =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.PreIncrement));
		static readonly MethodInfo RT_PostDecrement =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.PostDecrement));
		static readonly MethodInfo RT_InterpretNode =
			typeof(JitRuntime).GetMethod(nameof(JitRuntime.InterpretNode));

		// ── Public API ───────────────────────────────────────────────

		/// <summary>
		/// Compile a parsed AST into a callable delegate.
		/// </summary>
		public static Func<ScriptContext, object> Compile(CommonTree rootNode)
		{
			var dm = new DynamicMethod(
				"ReoScript_JIT",
				typeof(object),
				new[] { typeof(ScriptContext) },
				typeof(JitCompiler).Module,
				skipVisibility: true);

			ILGenerator il = dm.GetILGenerator();
			var compiler = new JitCompiler(il);

			compiler.EmitStatements(rootNode);

			if (rootNode == null || rootNode.Children == null || rootNode.ChildCount == 0)
			{
				// no statements — return null
				il.Emit(OpCodes.Ldnull);
			}
			// else: last statement value is on the stack

			il.Emit(OpCodes.Ret);

			return (Func<ScriptContext, object>)dm.CreateDelegate(
				typeof(Func<ScriptContext, object>));
		}

		// ── Instance state ───────────────────────────────────────────

		private readonly ILGenerator il;
		private int nodeCount;

		private JitCompiler(ILGenerator il)
		{
			this.il = il;
		}

		// ── Statement-level emit ─────────────────────────────────────

		private void EmitStatements(CommonTree node)
		{
			if (node == null || node.Children == null) return;

			for (int i = 0; i < node.ChildCount; i++)
			{
				EmitNode((CommonTree)node.Children[i]);
				// each statement leaves a value on the stack; pop it
				// unless it's the last statement (return value).
				if (i < node.ChildCount - 1)
					il.Emit(OpCodes.Pop);
			}
		}

		// ── Core emit dispatcher ─────────────────────────────────────

		private void EmitNode(CommonTree t)
		{
			if (t == null) { il.Emit(OpCodes.Ldnull); return; }
			nodeCount++;

			switch (t.Type)
			{
				// ── Variable declaration ─────────────────────────
				case ReoScriptLexer.DECLARATION:
					EmitDeclaration(t);
					break;

				case ReoScriptLexer.LOCAL_DECLARE_ASSIGNMENT:
					EmitLocalDeclareAssignment(t);
					break;

				// ── Assignment ────────────────────────────────────
				case ReoScriptLexer.ASSIGNMENT:
					EmitAssignment(t);
					break;

				// ── Control flow ──────────────────────────────────
				case ReoScriptLexer.FOR_STATEMENT:
					EmitFor(t);
					break;

				case ReoScriptLexer.WHILE_STATEMENT:
					EmitWhile(t);
					break;

				case ReoScriptLexer.IF_STATEMENT:
					EmitIf(t);
					break;

				case ReoScriptLexer.BLOCK:
					EmitBlock(t);
					break;

				case ReoScriptLexer.RETURN:
					EmitReturn(t);
					break;

				// ── Expressions ───────────────────────────────────
				case ReoScriptLexer.IDENTIFIER:
					EmitGetVariable(t);
					break;

				case ReoScriptLexer.CONST_VALUE:
					EmitConst(t);
					break;

				case ReoScriptLexer.THIS:
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Call, RT_GetThisObject);
					break;

				case ReoScriptLexer.PROPERTY_ACCESS:
					EmitPropertyAccess(t);
					break;

				case ReoScriptLexer.FUNCTION_CALL:
					EmitFunctionCall(t);
					break;

				// ── Binary arithmetic ─────────────────────────────
				case ReoScriptLexer.PLUS:
					EmitBinaryOp(t, RT_Add);
					break;
				case ReoScriptLexer.MINUS:
					EmitBinaryOp(t, RT_Subtract);
					break;
				case ReoScriptLexer.MUL:
					EmitBinaryOp(t, RT_Multiply);
					break;
				case ReoScriptLexer.DIV:
					EmitBinaryOp(t, RT_Divide);
					break;
				case ReoScriptLexer.MOD:
					EmitBinaryOp(t, RT_Modulo);
					break;

				// ── Comparison ────────────────────────────────────
				case ReoScriptLexer.LESS_THAN:
					EmitBinaryOp(t, RT_LessThan);
					il.Emit(OpCodes.Box, typeof(bool));
					break;
				case ReoScriptLexer.LESS_EQUALS:
					EmitBinaryOp(t, RT_LessOrEqual);
					il.Emit(OpCodes.Box, typeof(bool));
					break;
				case ReoScriptLexer.GREAT_THAN:
					EmitBinaryOp(t, RT_GreaterThan);
					il.Emit(OpCodes.Box, typeof(bool));
					break;
				case ReoScriptLexer.GREAT_EQUALS:
					EmitBinaryOp(t, RT_GreaterOrEqual);
					il.Emit(OpCodes.Box, typeof(bool));
					break;
				case ReoScriptLexer.EQUALS:
					EmitBinaryOp(t, RT_Equals);
					il.Emit(OpCodes.Box, typeof(bool));
					break;
				case ReoScriptLexer.NOT_EQUALS:
					EmitBinaryOp(t, RT_NotEquals);
					il.Emit(OpCodes.Box, typeof(bool));
					break;

				// ── Unary step ────────────────────────────────────
				case ReoScriptLexer.POST_UNARY_STEP:
					EmitPostUnaryStep(t);
					break;

				case ReoScriptLexer.PRE_UNARY_STEP:
					EmitPreUnaryStep(t);
					break;

				// ── Pre-unary (!, -, ~, typeof) ──────────────────
				case ReoScriptLexer.PRE_UNARY:
					EmitPreUnary(t);
					break;

				// ── Fallback to tree-walking ──────────────────────
				default:
					EmitFallback(t);
					break;
			}
		}

		// ── Variable declaration ──────────────────────────────────────

		private void EmitDeclaration(CommonTree t)
		{
			for (int i = 0; i < t.ChildCount; i++)
			{
				EmitNode((CommonTree)t.Children[i]);
				if (i < t.ChildCount - 1) il.Emit(OpCodes.Pop);
			}
		}

		private void EmitLocalDeclareAssignment(CommonTree t)
		{
			if (t is VariableDefineNode vdn && vdn.VariableInfo != null
				&& vdn.VariableInfo.HasInitialValue)
			{
				il.Emit(OpCodes.Ldarg_0); // ctx
				il.Emit(OpCodes.Ldstr, vdn.VariableInfo.Name);
				EmitNode(vdn.VariableInfo.InitialValueTree);
				il.Emit(OpCodes.Call, RT_SetVariable);
				// SetVariable returns void, push null as expression value
				il.Emit(OpCodes.Ldnull);
			}
			else
			{
				il.Emit(OpCodes.Ldnull);
			}
		}

		// ── Assignment ────────────────────────────────────────────────

		private void EmitAssignment(CommonTree t)
		{
			CommonTree target = (CommonTree)t.Children[0];

			switch (target.Type)
			{
				case ReoScriptLexer.IDENTIFIER:
					il.Emit(OpCodes.Ldarg_0); // ctx
					il.Emit(OpCodes.Ldstr, target.Text);
					EmitNode((CommonTree)t.Children[1]); // value
					il.Emit(OpCodes.Dup); // keep value on stack as expression result
					var local = il.DeclareLocal(typeof(object));
					il.Emit(OpCodes.Stloc, local);
					il.Emit(OpCodes.Call, RT_SetVariable);
					il.Emit(OpCodes.Ldloc, local);
					break;

				case ReoScriptLexer.PROPERTY_ACCESS:
					il.Emit(OpCodes.Ldarg_0); // ctx
					EmitNode((CommonTree)target.Children[0]); // owner
					il.Emit(OpCodes.Ldstr, target.Children[1].Text); // name
					EmitNode((CommonTree)t.Children[1]); // value
					var local2 = il.DeclareLocal(typeof(object));
					il.Emit(OpCodes.Dup);
					il.Emit(OpCodes.Stloc, local2);
					il.Emit(OpCodes.Call, RT_SetProperty);
					il.Emit(OpCodes.Ldloc, local2);
					break;

				default:
					// array access etc. — fallback
					EmitFallback(t);
					break;
			}
		}

		// ── Control flow ──────────────────────────────────────────────

		private void EmitFor(CommonTree t)
		{
			// Children: [0]=FOR_INIT, [1]=FOR_CONDITION, [2]=FOR_ITERATOR, [3]=FOR_BODY
			CommonTree forInit = (CommonTree)t.Children[0];
			CommonTree forCond = (CommonTree)t.Children[1];
			CommonTree forIter = (CommonTree)t.Children[2];
			CommonTree forBody = (CommonTree)t.Children[3];

			// init
			for (int i = 0; i < forInit.ChildCount; i++)
			{
				EmitNode((CommonTree)forInit.Children[i]);
				il.Emit(OpCodes.Pop);
			}

			Label loopStart = il.DefineLabel();
			Label loopEnd = il.DefineLabel();

			il.MarkLabel(loopStart);

			// condition
			if (forCond.ChildCount > 0)
			{
				EmitNode((CommonTree)forCond.Children[0]);
				il.Emit(OpCodes.Call, RT_IsTrue);
				il.Emit(OpCodes.Brfalse, loopEnd);
			}

			// body
			if (forBody.ChildCount > 0)
			{
				CommonTree body = (CommonTree)forBody.Children[0];
				EmitBlockBody(body);
				il.Emit(OpCodes.Pop); // discard body result in loop
			}

			// iterator
			for (int i = 0; i < forIter.ChildCount; i++)
			{
				EmitNode((CommonTree)forIter.Children[i]);
				il.Emit(OpCodes.Pop);
			}

			il.Emit(OpCodes.Br, loopStart);
			il.MarkLabel(loopEnd);

			il.Emit(OpCodes.Ldnull); // for loop value = null
		}

		private void EmitWhile(CommonTree t)
		{
			Label loopStart = il.DefineLabel();
			Label loopEnd = il.DefineLabel();

			il.MarkLabel(loopStart);

			// condition
			EmitNode((CommonTree)t.Children[0]);
			il.Emit(OpCodes.Call, RT_IsTrue);
			il.Emit(OpCodes.Brfalse, loopEnd);

			// body
			if (t.ChildCount > 1)
			{
				EmitBlockBody((CommonTree)t.Children[1]);
				il.Emit(OpCodes.Pop); // discard body result in loop
			}

			il.Emit(OpCodes.Br, loopStart);
			il.MarkLabel(loopEnd);

			il.Emit(OpCodes.Ldnull);
		}

		private void EmitIf(CommonTree t)
		{
			// Children: [0]=condition, [1]=thenBody, [2]?=elseBody
			Label elseLabel = il.DefineLabel();
			Label endLabel = il.DefineLabel();

			EmitNode((CommonTree)t.Children[0]);
			il.Emit(OpCodes.Call, RT_IsTrue);
			il.Emit(OpCodes.Brfalse, elseLabel);

			// then
			EmitNode((CommonTree)t.Children[1]);

			if (t.ChildCount > 2)
			{
				il.Emit(OpCodes.Pop);
				il.Emit(OpCodes.Br, endLabel);
				il.MarkLabel(elseLabel);
				EmitNode((CommonTree)t.Children[2]);
				il.MarkLabel(endLabel);
			}
			else
			{
				il.Emit(OpCodes.Br, endLabel);
				il.MarkLabel(elseLabel);
				il.Emit(OpCodes.Ldnull);
				il.MarkLabel(endLabel);
			}
		}

		private void EmitBlock(CommonTree t)
		{
			EmitBlockBody(t);
		}

		private void EmitBlockBody(CommonTree body)
		{
			if (body == null || body.ChildCount == 0)
			{
				il.Emit(OpCodes.Ldnull);
				return;
			}

			for (int i = 0; i < body.ChildCount; i++)
			{
				EmitNode((CommonTree)body.Children[i]);
				if (i < body.ChildCount - 1) il.Emit(OpCodes.Pop);
			}
		}

		private void EmitReturn(CommonTree t)
		{
			if (t.ChildCount > 0)
				EmitNode((CommonTree)t.Children[0]);
			else
				il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ret);
		}

		// ── Expressions ───────────────────────────────────────────────

		private void EmitGetVariable(CommonTree t)
		{
			il.Emit(OpCodes.Ldarg_0); // ctx
			il.Emit(OpCodes.Ldstr, t.Text);
			il.Emit(OpCodes.Call, RT_GetVariable);
		}

		private void EmitConst(CommonTree t)
		{
			if (t.ChildCount == 0) { il.Emit(OpCodes.Ldnull); return; }

			CommonTree child = (CommonTree)t.Children[0];

			switch (child.Type)
			{
				case ReoScriptLexer.CONST_VALUE:
					// Constant folded value (ConstValueNode)
					if (child is ConstValueNode cvn)
					{
						switch (cvn.TokenType)
						{
							case ReoScriptLexer.NUMBER_LITERATE:
								il.Emit(OpCodes.Ldc_R8, (double)cvn.ConstValue);
								il.Emit(OpCodes.Box, typeof(double));
								break;
							case ReoScriptLexer.STRING_LITERATE:
								il.Emit(OpCodes.Ldstr, (string)cvn.ConstValue);
								break;
							default:
								il.Emit(OpCodes.Ldnull);
								break;
						}
					}
					else
					{
						il.Emit(OpCodes.Ldnull);
					}
					break;

				case ReoScriptLexer.LIT_TRUE:
					il.Emit(OpCodes.Ldc_I4_1);
					il.Emit(OpCodes.Box, typeof(bool));
					break;

				case ReoScriptLexer.LIT_FALSE:
					il.Emit(OpCodes.Ldc_I4_0);
					il.Emit(OpCodes.Box, typeof(bool));
					break;

				case ReoScriptLexer.LIT_NULL:
					il.Emit(OpCodes.Ldnull);
					break;

				default:
					// array/object literals etc.
					EmitFallback(t);
					break;
			}
		}

		private void EmitPropertyAccess(CommonTree t)
		{
			il.Emit(OpCodes.Ldarg_0); // ctx
			EmitNode((CommonTree)t.Children[0]); // owner
			il.Emit(OpCodes.Ldstr, t.Children[1].Text); // name
			il.Emit(OpCodes.Call, RT_GetProperty);
		}

		private void EmitFunctionCall(CommonTree t)
		{
			CommonTree funRef = (CommonTree)t.Children[0];
			bool hasArgs = t.ChildCount > 1 && t.Children[1].ChildCount > 0;

			// Arguments: build object[]
			if (hasArgs)
			{
				EmitArgumentArray((CommonTree)t.Children[1]);
			}

			LocalBuilder argsLocal = null;
			if (hasArgs)
			{
				argsLocal = il.DeclareLocal(typeof(object[]));
				il.Emit(OpCodes.Stloc, argsLocal);
			}

			// ctx
			il.Emit(OpCodes.Ldarg_0);

			// owner and function
			if (funRef.Type == ReoScriptLexer.PROPERTY_ACCESS)
			{
				// method call: obj.method(args)
				EmitNode((CommonTree)funRef.Children[0]); // owner (also used as 'this')
				var ownerLocal = il.DeclareLocal(typeof(object));
				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Stloc, ownerLocal);

				// get function from property
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldloc, ownerLocal);
				il.Emit(OpCodes.Ldstr, funRef.Children[1].Text);
				il.Emit(OpCodes.Call, RT_GetProperty);
			}
			else if (funRef.Type == ReoScriptLexer.IDENTIFIER)
			{
				// local function call: fun(args)
				il.Emit(OpCodes.Ldnull); // owner = null
				EmitGetVariable(funRef); // function object
			}
			else
			{
				il.Emit(OpCodes.Ldnull);
				EmitNode(funRef);
			}

			// args
			if (hasArgs)
				il.Emit(OpCodes.Ldloc, argsLocal);
			else
				il.Emit(OpCodes.Ldnull);

			il.Emit(OpCodes.Call, RT_CallFunction);
		}

		private void EmitArgumentArray(CommonTree argList)
		{
			int count = argList.ChildCount;
			il.Emit(OpCodes.Ldc_I4, count);
			il.Emit(OpCodes.Newarr, typeof(object));

			for (int i = 0; i < count; i++)
			{
				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Ldc_I4, i);
				EmitNode((CommonTree)argList.Children[i]);
				il.Emit(OpCodes.Stelem_Ref);
			}
		}

		// ── Binary operations ─────────────────────────────────────────

		private void EmitBinaryOp(CommonTree t, MethodInfo helper)
		{
			EmitNode((CommonTree)t.Children[0]);
			EmitNode((CommonTree)t.Children[1]);
			il.Emit(OpCodes.Call, helper);
		}

		// ── Unary step ────────────────────────────────────────────────

		private void EmitPostUnaryStep(CommonTree t)
		{
			string name = t.Children[0].Text;
			int stepChild = t.ChildCount > 1 ? ((CommonTree)t.Children[1]).Type : 0;

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldstr, name);

			if (stepChild == ReoScriptLexer.DECREMENT)
				il.Emit(OpCodes.Call, RT_PostDecrement);
			else
				il.Emit(OpCodes.Call, RT_PostIncrement);
		}

		private void EmitPreUnaryStep(CommonTree t)
		{
			string name = t.Children[0].Text;
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldstr, name);
			il.Emit(OpCodes.Call, RT_PreIncrement);
		}

		// ── Pre-unary ─────────────────────────────────────────────────

		private void EmitPreUnary(CommonTree t)
		{
			if (t.ChildCount < 2) { EmitFallback(t); return; }

			int op = ((CommonTree)t.Children[0]).Type;
			CommonTree operand = (CommonTree)t.Children[1];

			switch (op)
			{
				case ReoScriptLexer.NOT:
					EmitNode(operand);
					il.Emit(OpCodes.Call, RT_IsTrue);
					il.Emit(OpCodes.Ldc_I4_0);
					il.Emit(OpCodes.Ceq);
					il.Emit(OpCodes.Box, typeof(bool));
					break;

				case ReoScriptLexer.MINUS:
					EmitNode(operand);
					il.Emit(OpCodes.Call, RT_Negate);
					break;

				default:
					EmitFallback(t);
					break;
			}
		}

		// ── Fallback ──────────────────────────────────────────────────

		/// <summary>
		/// For any AST node the JIT doesn't handle yet, fall back to
		/// the tree-walking interpreter. This keeps the JIT correct
		/// while we incrementally add more node types.
		/// </summary>
		private void EmitFallback(CommonTree t)
		{
			il.Emit(OpCodes.Ldarg_0); // ctx
			EmitTreeRef(t); // push the CommonTree node
			il.Emit(OpCodes.Call, RT_InterpretNode);
		}

		/// <summary>
		/// Push a reference to a CommonTree node onto the IL stack.
		///
		/// DynamicMethod can't embed arbitrary object references as IL
		/// constants, so we store the tree node in a static holder and
		/// emit IL that reads it back. This is only used for fallback
		/// paths, so the performance cost is negligible.
		/// </summary>
		private void EmitTreeRef(CommonTree t)
		{
			int index = TreeRefHolder.Store(t);
			il.Emit(OpCodes.Ldc_I4, index);
			il.Emit(OpCodes.Call, typeof(TreeRefHolder).GetMethod(nameof(TreeRefHolder.Load)));
		}
	}

	/// <summary>
	/// Thread-safe holder for CommonTree references used by the fallback path.
	/// JIT-emitted IL stores tree indices and retrieves them at runtime.
	/// </summary>
	public static class TreeRefHolder
	{
		private static readonly List<CommonTree> nodes = new List<CommonTree>();
		private static readonly object syncLock = new object();

		public static int Store(CommonTree node)
		{
			lock (syncLock)
			{
				int idx = nodes.Count;
				nodes.Add(node);
				return idx;
			}
		}

		public static CommonTree Load(int index)
		{
			return nodes[index];
		}
	}
}
