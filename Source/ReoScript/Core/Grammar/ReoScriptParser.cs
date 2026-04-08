using Antlr.Runtime;
using Antlr.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using unvell.ReoScript.Core.Statement;
using unvell.ReoScript.Reflection;

namespace unvell.ReoScript.Core
{
	sealed internal partial class ReoScriptParser
	{
		//internal Preinterpreter Preinterpreter { get; set; }

		private CommonTree ConstLiteral(CommonTree t)
		{
			switch (t.Type)
			{
				case ReoScriptLexer.NUMBER_LITERATE:
					return new ConstValueNode(t, Convert.ToDouble(t.Text, System.Globalization.CultureInfo.InvariantCulture), ReoScriptLexer.NUMBER_LITERATE);

				case ReoScriptLexer.HEX_LITERATE:
					return new ConstValueNode(t, (double)Convert.ToInt32(t.Text.Substring(2), 16), ReoScriptLexer.NUMBER_LITERATE);

				case ReoScriptLexer.BINARY_LITERATE:
					return new ConstValueNode(t, (double)Convert.ToInt32(t.Text.Substring(2), 2), ReoScriptLexer.NUMBER_LITERATE);

				case ReoScriptLexer.STRING_LITERATE:
					string str = t.ToString();
					str = str.Substring(1, str.Length - 2);
					return new ConstValueNode(t, ScriptRunningMachine.ConvertEscapeLiterals(str), ReoScriptLexer.STRING_LITERATE);

					//case ReoScriptLexer.LIT_TRUE:
					//  return new ConstValueNode(t, true);

					//case ReoScriptLexer.LIT_FALSE:
					//  return new ConstValueNode(t, false);

					//case ReoScriptLexer.LIT_NULL:
					//case ReoScriptLexer.UNDEFINED:
					//  return new ConstValueNode(t, null);

					//case ReoScriptLexer.NAN:
					//  return new ConstValueNode(t, NaNValue.Value);
			}

			return t;
		}

		internal Action<ErrorObject> CompilingErrorHandler;

		internal List<ErrorObject> CompilingErrors = new List<ErrorObject>();

		public override void ReportError(RecognitionException re)
		{
			string msg = string.Format("syntax error at char {0} on line {1}", re.CharPositionInLine, re.Line);

			if (re is MissingTokenException)
			{
				MissingTokenException mte = (MissingTokenException)re;
				msg += string.Format(", missing {0}", ReoScriptParser.tokenNames[mte.MissingType]);
			}
			else if (re is MismatchedTokenException)
			{
				MismatchedTokenException mte = (MismatchedTokenException)re;
				msg += string.Format(", expect {0}", ReoScriptParser.tokenNames[mte.Expecting]);
			}
			else if (re is NoViableAltException)
			{
				NoViableAltException nvae = (NoViableAltException)re;
				msg += ", unexpected token " + nvae.Token.Text;
			}

			ErrorObject e = new ErrorObject();
			e.Message = msg;
			e.CharIndex = re.CharPositionInLine;
			e.Line = re.Line;

			CompilingErrors.Add(e);

			if (CompilingErrorHandler != null)
			{
				CompilingErrorHandler(e);
			}
		}

		private Stack<StaticFunctionScope> localStack = new Stack<StaticFunctionScope>();

		internal StaticFunctionScope CurrentStack { get; set; }

		private void PushLocalStack()
		{
			localStack.Push(CurrentStack = new StaticFunctionScope());
		}

		private StaticFunctionScope lastLocalScope;

		private void PopLocalStack()
		{
			if (localStack.Count > 1)
				lastLocalScope = localStack.Pop();

			CurrentStack = localStack.Peek();
		}

		private FunctionDefineNode DefineLocalFunction(string name, CommonTree paramsTree, CommonTree body,
			int modifierToken, int line, int charIndex)
		{
			FunctionInfo fi = new FunctionInfo
			{
				Name = name,
				IsInner = localStack.Count > 1,
				Args = GetArgumentList(paramsTree),
				IsAnonymous = false,
				ScopeModifier = GetScopeModifier(modifierToken),
				BodyTree = body,
				CharIndex = charIndex,
				Line = line,
				InnerScope = lastLocalScope,
				OuterScope = CurrentStack,
			};

			FunctionDefineNode fdn = new FunctionDefineNode
			{
				FunctionInfo = fi,
			};

			CurrentStack.Functions.Add(fi);

			return fdn;
		}

		private AnonymousFunctionDefineNode DefineAnonymousFunction(string arg1, CommonTree argsTree, CommonTree body,
			int modifierToken, int line, int charIndex)
		{
			FunctionInfo fi = new FunctionInfo
			{
				Name = "<anonymous>",
				Args = arg1 == null ? GetArgumentList(argsTree) : new string[] { arg1 },
				IsAnonymous = true,
				ScopeModifier = MemberScopeModifier.Public,
				BodyTree = body,
				CharIndex = charIndex,
				Line = line,
				InnerScope = lastLocalScope,
				OuterScope = CurrentStack,
			};

			AnonymousFunctionDefineNode afdn = new AnonymousFunctionDefineNode
			{
				FunctionInfo = fi,
			};

			if (body.Type != RETURN)
			{
				CurrentStack.Functions.Add(fi);
			}

			return afdn;
		}

		private VariableDefineNode DefineLocalVariable(string name, CommonTree val/*, int modifierToken*/,
			int line, int charIndex)
		{
			VariableInfo vi = new VariableInfo
			{
				Name = name,
				HasInitialValue = val != null,
				IsImplicitDeclaration = false,
				IsLocal = true,
				CharIndex = charIndex,
				Line = line,
				//TODO: ScopeModifier = GetScopeModifier(modifierToken),
				InitialValueTree = val,
			};

			VariableDefineNode vdn = new VariableDefineNode
			{
				VariableInfo = vi
			};

			CurrentStack.Variables.Add(vi);

			return vdn;
		}

		private static string[] GetArgumentList(CommonTree argsTree)
		{
			if (argsTree == null) return new string[0];

			if (argsTree.ChildCount == 0)
			{
				return new string[] { argsTree.Text };
			}
			else
			{
				string[] identifiers = new string[argsTree.ChildCount];

				for (int i = 0; i < identifiers.Length; i++)
				{
					identifiers[i] = argsTree.Children[i].ToString();
				}

				return identifiers;
			}
		}

		private static MemberScopeModifier GetScopeModifier(int tokenType)
		{
			switch (tokenType)
			{
				default:
				case ReoScriptLexer.PUBLIC:
					return MemberScopeModifier.Public;
				case ReoScriptLexer.INTERNAL:
					return MemberScopeModifier.Internal;
				case ReoScriptLexer.PROTECTED:
					return MemberScopeModifier.Protected;
				case ReoScriptLexer.PRIVATE:
					return MemberScopeModifier.Private;
			}
		}
	}
}
