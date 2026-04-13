/*****************************************************************************
 *
 * ReoScript - .NET Script Language Engine
 *
 * https://github.com/jingwood/ReoScript
 *
 * MIT License
 * Copyright 2012-2019 Jingwood
 *
 *****************************************************************************/

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;


using unvell.ReoScript.Runtime;
using unvell.ReoScript.Core;
using unvell.ReoScript.Core.Statement;
using unvell.ReoScript.Reflection;

namespace unvell.ReoScript
{
	namespace Parsers
	{
		#region Parser Interface
		interface INodeParser
		{
			object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx);
		}
		#endregion

		#region Import Statement
		class ImportNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				// Check if the last child is an AS node (alias)
				string alias = null;
				int contentChildCount = t.ChildCount;
				if (t.ChildCount > 1 && t.Children[t.ChildCount - 1].Type == NodeType.AS)
				{
					alias = t.Children[t.ChildCount - 1].ToString();
					contentChildCount--;
				}

				if (t.Children[0].Type == NodeType.STRING_LITERATE)
				{
					string codeFile = t.Children[0].ToString();
					codeFile = codeFile.Substring(1, codeFile.Length - 2);

					string path = Path.GetFullPath(Path.Combine(
						string.IsNullOrEmpty(ctx.SourceFilePath) ? srm.WorkPath
						: Path.GetDirectoryName(ctx.SourceFilePath), codeFile));

					if (alias != null)
					{
						// import "file.reo" as mod; → isolated module import
						ObjectValue module = srm.ImportModuleFile(path);
						ctx[alias] = module;
					}
					else
					{
						// import "file.reo"; → legacy global-scope import
						srm.ImportCodeFile(path);
					}
				}
				else if (srm.AllowImportTypeInScript)
				{
					StringBuilder sb = new StringBuilder();
					for (int i = 0; i < contentChildCount; i++)
					{
						string ns = t.Children[i].ToString();

						if (i == contentChildCount - 1)
						{
							if (ns == "*")
							{
								srm.ImportNamespace(sb.ToString());
								return null;
							}
							else
							{
								string name = sb.ToString();
								if (name.EndsWith(".")) name = name.Substring(0, name.Length - 1);

								Type type = srm.GetTypeFromAssembly(name, ns);
								if (type != null)
								{
									if (alias != null)
									{
										// import System.Collections.Generic.List as List;
										srm.ImportType(type, alias);
									}
									else
									{
										srm.ImportType(type);
									}
								}
								return null;
							}
						}

						sb.Append(ns);
					}
				}

				return null;
			}

			#endregion
		}
		#endregion
		#region Destructuring Declaration
		class DestructureNodeParser : INodeParser
		{
			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				// Children: [name1, name2, ..., nameN, initExpr]
				// Last child is the initializer expression
				int nameCount = t.ChildCount - 1;
				SyntaxNode initNode = (SyntaxNode)t.Children[nameCount];

				object initVal = ScriptRunningMachine.ParseNode(initNode, ctx);
				if (initVal is IAccess) initVal = ((IAccess)initVal).Get();

				ObjectValue source = initVal as ObjectValue;

				for (int i = 0; i < nameCount; i++)
				{
					string name = t.Children[i].ToString();
					object value = source != null ? source[name] : null;
					ctx[name] = value;
				}

				return null;
			}
		}
		#endregion
		#region Local Variable Declaration
		class DeclarationNodeParser : INodeParser
		{
			private AssignmentNodeParser assignmentParser = new AssignmentNodeParser();

			#region INodeParser Members
			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext context)
			{
				//for (int i = 0; i < t.ChildCount; i++)
				//{
				VariableInfo vi = ((VariableDefineNode)t).VariableInfo;

				string identifier = vi.Name;
				object value = null;

				if (vi.HasInitialValue)
				{
					value = ScriptRunningMachine.ParseNode(vi.InitialValueTree, context);
					if (value is IAccess) value = ((IAccess)value).Get();
				}

				// declare variable in current call stack
				context[identifier] = value;
				//}
				//for (int i = 1; i < t.ChildCount; i++)
				//{
				//  var identifier = t.Children[0].ToString();
				//  var value = srm.ParseNode((SyntaxNode)t.Children[i], context);

				//  if (value is IAccess) value = ((IAccess)value).Get();

				//  // declare variable in current call stack
				//  if (srm.IsInGlobalScope(context))
				//  {
				//    srm[identifier] = value;
				//  }
				//  else
				//  {
				//    context.GetCurrentCallScope()[identifier] = value;
				//  }

				//  lastValue = value;
				//}

				return value;
			}
			#endregion
		}
		#endregion
		#region Assignment =
		class AssignmentNodeParser : INodeParser
		{
			private static readonly ExprPlusNodeParser exprPlusNodeParser = new ExprPlusNodeParser();

			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				if (t.ChildCount == 1)
				{
					return null;
				}

				if (t.Children[0].Type == NodeType.IDENTIFIER)
				{
					string identifier = t.Children[0].Text;

					IVariableContainer container = null;

					CallScope cs = ctx.CurrentCallScope;

					if (cs != null)
					{
						if (cs.Variables.ContainsKey(identifier))
						{
							container = cs;
						}
						else
						{
							CallScope outerScope = cs.CurrentFunction.CapturedScope;
							while (outerScope != null)
							{
								if (outerScope.Variables.ContainsKey(identifier))
								{
									container = outerScope;
									break;
								}

								outerScope = outerScope.CurrentFunction.CapturedScope;
							}
						}
					}

					if (container == null)
					{
						container = ctx.GlobalObject;
					}

					object orginal = null;
					container.TryGetValue(identifier, out orginal);

					object target = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[1], ctx);

					if (orginal is ExternalProperty)
					{
						((ExternalProperty)orginal).Setter(target);
					}
					else
					{
						container[identifier] = target;
					}

					return target;
				}
				else
				{
					IAccess access = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[0], ctx) as IAccess;
					SyntaxNode expr = t.ChildCount > 1 ? (SyntaxNode)t.Children[1] : null;

					object value = null;
					if (expr != null)
					{
						value = ScriptRunningMachine.ParseNode(expr, ctx);
					}

					if (value is IAccess) value = ((IAccess)value).Get();

					if (access != null)
					{
						access.Set(value);
					}
					else if (!srm.IsInGlobalScope(ctx))
					{
						ctx[t.Children[0].Text] = value;
					}

					return value;
				}
			}
			#endregion
		}
		#endregion
		#region Expresion Operator
		abstract class ExpressionOperatorNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				object left = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[0], ctx);
				if (left is IAccess) left = ((IAccess)left).Get();

				object right = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[1], ctx);
				if (right is IAccess) right = ((IAccess)right).Get();

				return Calc(left, right, srm, ctx);
			}

			public abstract object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context);

			#endregion
		}
		abstract class MathExpressionOperatorParser : ExpressionOperatorNodeParser
		{
			public override object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context)
			{
				if (left == NaNValue.Value || right == NaNValue.Value)
					return NaNValue.Value;

				if (!ScriptRunningMachine.IsPrimitiveNumber(left)) left = 0;
				if (!ScriptRunningMachine.IsPrimitiveNumber(right)) right = 0;

				return MathCalc(ScriptRunningMachine.GetNumberValue(left),
					ScriptRunningMachine.GetNumberValue(right));
			}

			public abstract object MathCalc(double left, double right);
		}
		#region Plus +
		class ExprPlusNodeParser : ExpressionOperatorNodeParser
		{
			public override object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context)
			{
				if (left == null && right == null) return null;

				if (left == null) return right;
				else if (right == null) return left;

				if (left == NaNValue.Value && right == NaNValue.Value) return NaNValue.Value;

				if (ScriptRunningMachine.IsPrimitiveNumber(left))
				{
					if (ScriptRunningMachine.IsPrimitiveNumber(right))
					{
						return ScriptRunningMachine.GetNumberValue(left) + ScriptRunningMachine.GetNumberValue(right);
					}
					else if (right == null)
					{
						return left;
					}
					else if (right == NaNValue.Value)
					{
						return NaNValue.Value;
					}
					else if (right is NumberObject)
					{
						return ScriptRunningMachine.GetNumberValue(left) + ((NumberObject)right).Number;
					}
				}
				else if (ScriptRunningMachine.IsPrimitiveNumber(right))
				{
					if (ScriptRunningMachine.IsPrimitiveNumber(left))
					{
						return ScriptRunningMachine.GetNumberValue(left) + ScriptRunningMachine.GetNumberValue(right);
					}
					else if (left == null)
					{
						return right;
					}
					else if (left == NaNValue.Value)
					{
						return NaNValue.Value;
					}
					else if (left is NumberObject)
					{
						return ((NumberObject)left).Number + ScriptRunningMachine.GetNumberValue(right);
					}
				}

				if (left.GetType() == typeof(ObjectValue) && right.GetType() == typeof(ObjectValue))
				{
					ObjectValue obj = srm.CreateNewObject(context);
					srm.CombineObject(context, obj, ((ObjectValue)left));
					srm.CombineObject(context, obj, ((ObjectValue)right));
					return obj;
				}
				else
				{
					return Convert.ToString(left) + Convert.ToString(right);
				}
			}
		}
		#endregion
		#region Minus -
		class ExprMinusNodeParser : ExpressionOperatorNodeParser
		{
			public override object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context)
			{
				if (left == null && right == null)
					return null;

				if (left == null)
					return right;
				else if (right == null)
					return left;

				if (left == NaNValue.Value && right == NaNValue.Value) return NaNValue.Value;

				if (ScriptRunningMachine.IsPrimitiveNumber(left))
				{
					if (right == null)
					{
						return left;
					}
					else if (right == NaNValue.Value)
					{
						return NaNValue.Value;
					}
					else if (ScriptRunningMachine.IsPrimitiveNumber(right))
					{
						return ScriptRunningMachine.GetNumberValue(left) - ScriptRunningMachine.GetNumberValue(right);
					}
					else if (right is NumberObject)
					{
						return ScriptRunningMachine.GetNumberValue(left) - ((NumberObject)right).Number;
					}
					else if (right is DateObject)
					{
						return ScriptRunningMachine.GetNumberValue(left) - ((DateObject)right).Ticks;
					}
				}
				else if (ScriptRunningMachine.IsPrimitiveNumber(right))
				{
					if (left == null)
					{
						return right;
					}
					else if (left == NaNValue.Value)
					{
						return NaNValue.Value;
					}
					else if (ScriptRunningMachine.IsPrimitiveNumber(left))
					{
						return ScriptRunningMachine.GetNumberValue(left) - ScriptRunningMachine.GetNumberValue(right);
					}
					else if (left is NumberObject)
					{
						return ((NumberObject)left).Number - ScriptRunningMachine.GetNumberValue(right);
					}
					else if (left is DateObject)
					{
						return ((DateObject)left).Ticks - ScriptRunningMachine.GetNumberValue(right);
					}
				}
				else if (left is DateObject && right is DateObject)
				{
					return (((DateObject)left).DateTime - ((DateObject)right).DateTime).TotalMilliseconds;
				}

				return double.NaN;
			}
		}
		#endregion
		#region Mul *
		class ExprMultiNodeParser : MathExpressionOperatorParser
		{
			public override object MathCalc(double left, double right)
			{
				double val = left * right;
				return (double.IsNaN(val)) ? NaNValue.Value : (object)val;
			}
		}
		#endregion
		#region Div /
		class ExprDivNodeParser : MathExpressionOperatorParser
		{
			public override object MathCalc(double left, double right)
			{
				double val = left / right;
				return (double.IsNaN(val)) ? NaNValue.Value : (object)val;
			}
		}
		#endregion
		#region Mod %
		class ExprModNodeParser : MathExpressionOperatorParser
		{
			#region INodeParser Members

			public override object MathCalc(double left, double right)
			{
				return (left % right);
			}

			#endregion
		}
		#endregion
		#region And &
		class ExprAndNodeParser : ExpressionOperatorNodeParser
		{
			#region INodeParser Members

			public override object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context)
			{
				if ((left is int || left is double || left is long || left is float)
					&& (right is int || right is double || right is long || right is float))
				{
					return (Convert.ToInt64(left) & Convert.ToInt64(right));
				}
				else
				{
					return NaNValue.Value;
				}
			}

			#endregion
		}
		#endregion
		#region Or |
		class ExprOrNodeParser : ExpressionOperatorNodeParser
		{
			#region INodeParser Members

			public override object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context)
			{
				if ((left is int || left is long || left is float || left is double)
					&& (right is int || right is long || right is float || right is double))
				{
					return (Convert.ToInt64(left) | Convert.ToInt64(right));
				}
				else
				{
					return NaNValue.Value;
				}
			}

			#endregion
		}
		#endregion
		#region Xor ^
		class ExprXorNodeParser : ExpressionOperatorNodeParser
		{
			#region INodeParser Members

			public override object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context)
			{
				if (left is long && right is long)
				{
					return ((long)left ^ (long)right);
				}
				else if (left is ObjectValue && right is ObjectValue)
				{
					ObjectValue leftObj = (ObjectValue)left;
					ObjectValue rightObj = (ObjectValue)right;

					ObjectValue resultObject = srm.CreateNewObject(context);

					foreach (string key in leftObj)
					{
						if (rightObj.HasOwnProperty(key))
						{
							resultObject[key] = leftObj[key];
						}
					}

					return resultObject;
				}
				else
				{
					return NaNValue.Value;
				}
			}

			#endregion
		}
		#endregion
		#region Left Shift <<
		class ExprLeftShiftNodeParser : ExpressionOperatorNodeParser
		{
			#region INodeParser Members

			public override object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context)
			{
				try
				{
					int l = Convert.ToInt32(left);
					int r = Convert.ToInt32(right);

					return l << r;
				}
				catch
				{
					return 0;
				}
			}

			#endregion
		}
		#endregion
		#region Right Shift >>
		class ExprRightShiftNodeParser : ExpressionOperatorNodeParser
		{
			#region INodeParser Members

			public override object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context)
			{
				try
				{
					int l = Convert.ToInt32(left);
					int r = Convert.ToInt32(right);

					return l >> r;
				}
				catch
				{
					return 0;
				}
			}

			#endregion
		}
		#endregion
		#endregion
		#region Unary for Primary Expression
		class ExprUnaryNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				string oprt = t.Children[0].ToString();
				object value = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[1], ctx);

				// get value
				while (value is IAccess) value = ((IAccess)value).Get();

				switch (oprt)
				{
					case "!":
						return !ScriptRunningMachine.GetBoolValue(value);

					default:
						if (value == null) return null;
						break;
				}

				switch (oprt)
				{
					default:
					case "+":
						return value;

					case "-":
						if (value is long)
							return -((long)value);
						else if (value is double)
							return -((double)value);
						else
							return null;

					case "~":
						return (~(Convert.ToInt64(value)));
				}
			}

			#endregion
		}
		#endregion
		#region Post Increment i++
		class ExprPostIncrementNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				if (t.Children[0].Type == NodeType.IDENTIFIER)
				{
					string identifier = t.Children[0].Text;

					IVariableContainer container = null;

					CallScope cs = ctx.CurrentCallScope;

					if (cs != null)
					{
						if (cs.Variables.ContainsKey(identifier))
						{
							container = cs;
						}
						else
						{
							CallScope outerScope = cs.CurrentFunction.CapturedScope;
							while (outerScope != null)
							{
								if (outerScope.Variables.ContainsKey(identifier))
								{
									container = outerScope;
									break;
								}

								outerScope = outerScope.CurrentFunction.CapturedScope;
							}
						}
					}

					if (container == null)
					{
						container = ctx.GlobalObject;
					}

					object orginal = null;
					container.TryGetValue(identifier, out orginal);

					if (!ScriptRunningMachine.IsPrimitiveNumber(orginal))
					{
						throw ctx.CreateRuntimeError(t, "only number can be used as increment or decrement statement.");
					}

					double target = ScriptRunningMachine.GetNumberValue(orginal) + (t.Children[1].Type == NodeType.INCREMENT ? 1 : -1);

					if (orginal is ExternalProperty)
					{
						((ExternalProperty)orginal).Setter(target);
					}
					else
					{
						container[identifier] = target;
					}

					return orginal;
				}
				else
				{

					IAccess access = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[0], ctx) as IAccess;

					if (access == null)
					{
						throw ctx.CreateRuntimeError(t, "only property, indexer, and variable can be used as increment or decrement statement.");
					}

					object oldValue = access.Get();
					if (oldValue == null)
					{
						oldValue = 0;
					}

					if (!ScriptRunningMachine.IsPrimitiveNumber(oldValue))
					{
						throw ctx.CreateRuntimeError(t, "only number can be used as increment or decrement statement.");
					}

					double value = ScriptRunningMachine.GetNumberValue(oldValue);
					double returnValue = value;
					access.Set((value + (t.Children[1].Type == NodeType.INCREMENT ? 1 : -1)));
					return returnValue;
				}
			}

			#endregion
		}
		#endregion
		#region Pre Increment ++i
		class ExprPreIncrementNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				if (t.Children[0].Type == NodeType.IDENTIFIER)
				{
					string identifier = t.Children[0].Text;

					IVariableContainer container = null;

					CallScope cs = ctx.CurrentCallScope;

					if (cs != null)
					{
						if (cs.Variables.ContainsKey(identifier))
						{
							container = cs;
						}
						else
						{
							CallScope outerScope = cs.CurrentFunction.CapturedScope;
							while (outerScope != null)
							{
								if (outerScope.Variables.ContainsKey(identifier))
								{
									container = outerScope;
									break;
								}

								outerScope = outerScope.CurrentFunction.CapturedScope;
							}
						}
					}

					if (container == null)
					{
						container = ctx.GlobalObject;
					}

					object orginal = null;
					container.TryGetValue(identifier, out orginal);

					if (!ScriptRunningMachine.IsPrimitiveNumber(orginal))
					{
						throw ctx.CreateRuntimeError(t, "only number can be used as increment or decrement statement.");
					}

					double target = ScriptRunningMachine.GetNumberValue(orginal) + (t.Children[1].Type == NodeType.INCREMENT ? 1 : -1);

					if (orginal is ExternalProperty)
					{
						((ExternalProperty)orginal).Setter(target);
					}
					else
					{
						container[identifier] = target;
					}

					return target;
				}
				else
				{
					SyntaxNode target = (SyntaxNode)t.Children[0];
					IAccess access = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[0], ctx) as IAccess;
					if (access == null)
					{
						throw ctx.CreateRuntimeError(t, "only property, indexer, and variable can be used as increment or decrement statement.");
					}
					object oldValue = access.Get();
					if (oldValue == null)
					{
						oldValue = 0;
					}

					if (!ScriptRunningMachine.IsPrimitiveNumber(oldValue))
					{
						throw ctx.CreateRuntimeError(t, "only number can be used as increment or decrement statement.");
					}

					double value = ScriptRunningMachine.GetNumberValue(oldValue);

					object v = (value + (t.Children[1].Type == NodeType.INCREMENT ? 1 : -1));
					access.Set(v);
					return v;
				}
			}

			#endregion
		}
		#endregion
		#region Condition ? :
		class ExprConditionNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				object value = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[0], ctx);
				if (value is IAccess) value = ((IAccess)value).Get();

				bool condition = ScriptRunningMachine.GetBoolValue(value);
				return condition ? ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[1], ctx)
					: ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[2], ctx);
			}

			#endregion
		}
		#endregion
		#region Relation Expression Operator
		abstract class RelationExpressionOperatorNodeParser : ExpressionOperatorNodeParser
		{
			#region INodeParser Members
			public override object Calc(object left, object right, ScriptRunningMachine srm, ScriptContext context)
			{
				return Compare(left, right, srm);
			}

			public abstract bool Compare(object left, object right, ScriptRunningMachine srm);
			#endregion
		}

		#region Equals ==
		class ExprEqualsNodeParser : RelationExpressionOperatorNodeParser
		{
			#region INodeParser Members

			public override bool Compare(object left, object right, ScriptRunningMachine srm)
			{
				if (left == null && right == null) return true;
				if (left == null || right == null) return false;
				if (left == NaNValue.Value && right == NaNValue.Value) return true;
				if (left == NaNValue.Value || right == NaNValue.Value) return false;

				if (left is string || left is StringObject)
				{
					return Convert.ToString(left).Equals(Convert.ToString(right));
				}
				else if (right is string || right is StringObject)
				{
					return Convert.ToString(right).Equals(Convert.ToString(left));
				}
				else
				{
					if (ScriptRunningMachine.IsPrimitiveNumber(left) && ScriptRunningMachine.IsPrimitiveNumber(right))
					{
						if (left is float || right is float)
						{
							return Convert.ToSingle(left) == Convert.ToSingle(right);
						}
						else
						{
							return ScriptRunningMachine.GetNumberValue(left) == ScriptRunningMachine.GetNumberValue(right);
						}
					}

					else if (left is NumberObject && ScriptRunningMachine.IsPrimitiveNumber(right))
					{
						return ((NumberObject)left).Number == ScriptRunningMachine.GetNumberValue(right);
					}
					else if (right is NumberObject && ScriptRunningMachine.IsPrimitiveNumber(left))
					{
						return ScriptRunningMachine.GetNumberValue(left) == ((NumberObject)right).Number;
					}
					else if (left is NumberObject && right is NumberObject)
					{
						return ((NumberObject)left).Number == ((NumberObject)right).Number;
					}

					else if (left is BooleanObject && right is bool)
					{
						return ((BooleanObject)left).Boolean == (bool)right;
					}
					else if (right is BooleanObject && left is bool)
					{
						return ((BooleanObject)right).Boolean == (bool)left;
					}
					else if (left is BooleanObject && right is BooleanObject)
					{
						return ((BooleanObject)left).Boolean == ((BooleanObject)right).Boolean;
					}
					else
					{
						return left.Equals(right);
					}
				}
			}

			#endregion
		}
		#endregion
		#region Not Equals !=
		class ExprNotEqualsNodeParser : RelationExpressionOperatorNodeParser
		{
			private static readonly ExprEqualsNodeParser equalsParser = new ExprEqualsNodeParser();
			#region INodeParser Members

			public override bool Compare(object left, object right, ScriptRunningMachine srm)
			{
				return !equalsParser.Compare(left, right, srm);
			}

			#endregion
		}
		#endregion
		#region Strict Equals ===
		class ExprStrictEqualsNodeParser : RelationExpressionOperatorNodeParser
		{
			public override bool Compare(object left, object right, ScriptRunningMachine srm)
			{
				if (left == null && right == null) return true;
				if (left == null || right == null) return false;

				if (ScriptRunningMachine.IsPrimitiveNumber(left) && ScriptRunningMachine.IsPrimitiveNumber(right))
				{
					if (left is float || right is float)
					{
						return Convert.ToSingle(left) == Convert.ToSingle(right);
					}
					else
					{
						return ScriptRunningMachine.GetNumberValue(left) == ScriptRunningMachine.GetNumberValue(right);
					}
				}
				else if ((left is string && right is string) || (left is bool && right is bool))
				{
					return left.Equals(right);
				}
				else
				{
					return left == right;
				}
			}
		}
		#endregion
		#region Strict Not Equals !==
		class ExprStrictNotEqualsNodeParser : RelationExpressionOperatorNodeParser
		{
			private ExprStrictEqualsNodeParser equalsParser = new ExprStrictEqualsNodeParser();

			public override bool Compare(object left, object right, ScriptRunningMachine srm)
			{
				return !equalsParser.Compare(left, right, srm);
				//return left != right;
			}
		}
		#endregion

		#region Greater Than >
		class ExprGreaterThanNodeParser : RelationExpressionOperatorNodeParser
		{
			#region INodeParser Members

			public override bool Compare(object left, object right, ScriptRunningMachine srm)
			{
				if (left is float || right is float)
				{
					return Convert.ToSingle(left) > Convert.ToSingle(right);
				}
				else if (ScriptRunningMachine.IsPrimitiveNumber(left) && ScriptRunningMachine.IsPrimitiveNumber(right))
				{
					return ScriptRunningMachine.GetNumberValue(left) > ScriptRunningMachine.GetNumberValue(right);
				}
				else if (left is NumberObject && ScriptRunningMachine.IsPrimitiveNumber(right))
				{
					return ((NumberObject)left).Number > ScriptRunningMachine.GetNumberValue(right);
				}
				else if (right is NumberObject && ScriptRunningMachine.IsPrimitiveNumber(left))
				{
					return ScriptRunningMachine.GetNumberValue(left) > ((NumberObject)right).Number;
				}
				else if (ScriptRunningMachine.IsPrimitiveString(left) && ScriptRunningMachine.IsPrimitiveString(right))
				{
					return string.Compare(ScriptRunningMachine.ConvertToString(left), ScriptRunningMachine.ConvertToString(right)) > 0;
				}
				else
				{
					return false;
				}
			}

			#endregion
		}
		#endregion
		#region Greater Or Equals >=
		class ExprGreaterOrEqualsNodeParser : RelationExpressionOperatorNodeParser
		{
			#region INodeParser Members

			public override bool Compare(object left, object right, ScriptRunningMachine srm)
			{
				if (left is float || right is float)
				{
					return Convert.ToSingle(left) >= Convert.ToSingle(right);
				}
				else if (ScriptRunningMachine.IsPrimitiveNumber(left) && ScriptRunningMachine.IsPrimitiveNumber(right))
				{
					return ScriptRunningMachine.GetNumberValue(left) >= ScriptRunningMachine.GetNumberValue(right);
				}
				else if (left is NumberObject && ScriptRunningMachine.IsPrimitiveNumber(right))
				{
					return ((NumberObject)left).Number >= ScriptRunningMachine.GetNumberValue(right);
				}
				else if (right is NumberObject && ScriptRunningMachine.IsPrimitiveNumber(left))
				{
					return ScriptRunningMachine.GetNumberValue(left) >= ((NumberObject)right).Number;
				}
				else if (ScriptRunningMachine.IsPrimitiveString(left) && ScriptRunningMachine.IsPrimitiveString(right))
				{
					return string.Compare(ScriptRunningMachine.ConvertToString(left), ScriptRunningMachine.ConvertToString(right)) >= 0;
				}
				else
				{
					return false;
				}
			}

			#endregion
		}
		#endregion
		#region Less Than <
		class ExprLessThanNodeParser : RelationExpressionOperatorNodeParser
		{
			#region INodeParser Members

			public override bool Compare(object left, object right, ScriptRunningMachine srm)
			{
				if (ScriptRunningMachine.IsPrimitiveNumber(left))
				{
					if (ScriptRunningMachine.IsPrimitiveNumber(right))
					{
						if (left is float || right is float)
						{
							return Convert.ToSingle(left) < Convert.ToSingle(right);
						}
						else
						{
							return ScriptRunningMachine.GetNumberValue(left) < ScriptRunningMachine.GetNumberValue(right);
						}
					}
					else if (right is NumberObject)
					{
						return ScriptRunningMachine.GetNumberValue(left) < ((NumberObject)right).Number;
					}
					else if (right is string)
					{
						double d = 0;
						if (double.TryParse(Convert.ToString(right), out d))
							return ScriptRunningMachine.GetNumberValue(left) < d;
						else
							return false;
					}
				}
				else if (ScriptRunningMachine.IsPrimitiveNumber(right))
				{
					if (ScriptRunningMachine.IsPrimitiveNumber(left))
					{
						if (left is float || right is float)
						{
							return Convert.ToSingle(left) < Convert.ToSingle(right);
						}
						else
						{
							return ScriptRunningMachine.GetNumberValue(left) < ScriptRunningMachine.GetNumberValue(right);
						}
					}
					else if (left is NumberObject)
					{
						return ((NumberObject)left).Number < ScriptRunningMachine.GetNumberValue(right);
					}
					else if (ScriptRunningMachine.IsPrimitiveString(left))
					{
						if (double.TryParse(ScriptRunningMachine.ConvertToString(left), out double d))
							return d < ScriptRunningMachine.GetNumberValue(right);
						else
							return false;
					}
				}
				else if (ScriptRunningMachine.IsPrimitiveString(left) && ScriptRunningMachine.IsPrimitiveString(right))
				{
					return string.Compare(ScriptRunningMachine.ConvertToString(left), ScriptRunningMachine.ConvertToString(right)) < 0;
				}

				return false;
			}

			#endregion
		}
		#endregion
		#region Less Or Equals <=
		class ExprLessOrEqualsNodeParser : RelationExpressionOperatorNodeParser
		{
			#region INodeParser Members

			public override bool Compare(object left, object right, ScriptRunningMachine srm)
			{
				if (ScriptRunningMachine.IsPrimitiveNumber(left) && ScriptRunningMachine.IsPrimitiveNumber(right))
				{
					if (left is float || right is float)
						return Convert.ToSingle(left) <= Convert.ToSingle(right);
					else
						return ScriptRunningMachine.GetNumberValue(left) <= ScriptRunningMachine.GetNumberValue(right);
				}
				else if (left is NumberObject && ScriptRunningMachine.IsPrimitiveNumber(right))
				{
					return ((NumberObject)left).Number <= ScriptRunningMachine.GetNumberValue(right);
				}
				else if (right is NumberObject && ScriptRunningMachine.IsPrimitiveNumber(left))
				{
					return ScriptRunningMachine.GetNumberValue(left) <= ((NumberObject)right).Number;
				}
				else if (ScriptRunningMachine.IsPrimitiveString(left) && ScriptRunningMachine.IsPrimitiveString(right))
				{
					return string.Compare(ScriptRunningMachine.ConvertToString(left), ScriptRunningMachine.ConvertToString(right)) <= 0;
				}
				else
				{
					return false;
				}
			}

			#endregion
		}
		#endregion
		#endregion
		#region Boolean &&
		class BooleanAndNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				object left = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[0], ctx);
				if (left is IAccess) left = ((IAccess)left).Get();

				// Short-circuit: if left is falsy, return left; otherwise return right.
				if (!ScriptRunningMachine.GetBoolValue(left))
					return left;

				object right = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[1], ctx);
				if (right is IAccess) right = ((IAccess)right).Get();

				return right;
			}

			#endregion
		}
		#endregion
		#region Boolean ||
		class BooleanOrNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				object left = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[0], ctx);
				if (left is IAccess) left = ((IAccess)left).Get();

				// Short-circuit: if left is truthy, return left; otherwise return right.
				if (ScriptRunningMachine.GetBoolValue(left))
					return left;

				object right = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[1], ctx);
				if (right is IAccess) right = ((IAccess)right).Get();

				return right;
			}

			#endregion
		}
		#endregion
		#region If Else
		class IfStatementNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				object value = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[0], ctx);
				if (value is IAccess) value = ((IAccess)value).Get();

				bool condition = ScriptRunningMachine.GetBoolValue(value);
				if (condition)
				{
					return ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[1], ctx);
				}
				else if (t.ChildCount == 3)
				{
					return ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[2], ctx);
				}
				else
					return null;
			}

			#endregion
		}
		#endregion
		#region Switch Case
		class SwitchCaseStatementNodeParser : INodeParser
		{
			private ExprEqualsNodeParser equalsParser = new ExprEqualsNodeParser();

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				if (t.ChildCount == 0) return null;

				object source = ScriptRunningMachine.ParseNode((SyntaxNode)(t.Children[0]), ctx);
				while (source is IAccess) source = ((IAccess)source).Get();

				if (source == null) return null;

				int defaultCaseLine = 0;
				bool doParse = false;

				int i = 1;

			doDefault:
				while (i < t.ChildCount)
				{
					SyntaxNode caseTree = (SyntaxNode)t.Children[i];

					if (caseTree.Type == NodeType.BREAK)
					{
						if (doParse) return null;
					}
					else if (caseTree.Type == NodeType.RETURN)
					{
						if (doParse) return ScriptRunningMachine.ParseNode(caseTree, ctx);
					}
					else if (caseTree.Type == NodeType.SWITCH_CASE_ELSE)
					{
						defaultCaseLine = i;
					}
					else if (caseTree.Type == NodeType.SWITCH_CASE)
					{
						if (caseTree.ChildCount > 0)
						{
							object target = ScriptRunningMachine.ParseNode((SyntaxNode)caseTree.Children[0], ctx);
							if (target is IAccess) target = ((IAccess)target).Get();

							if ((bool)equalsParser.Calc(source, target, srm, ctx))
							{
								doParse = true;
							}
						}
					}
					else if (doParse)
					{
						ScriptRunningMachine.ParseNode(caseTree, ctx);
					}

					i++;
				}

				if (defaultCaseLine > 0)
				{
					i = defaultCaseLine;
					doParse = true;
					goto doDefault;
				}

				return null;
			}
		}
		#endregion
		#region For
		class ForStatementNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				SyntaxNode forInit = (SyntaxNode)t.Children[0];
				ScriptRunningMachine.ParseChildNodes(forInit, ctx);

				SyntaxNode conditionTree = ((SyntaxNode)t.Children[1]);
				SyntaxNode condition = null;

				if (conditionTree.ChildCount > 0)
				{
					condition = (SyntaxNode)(conditionTree.Children[0]);
				}

				SyntaxNode iteratorTree = ((SyntaxNode)t.Children[2]);
				SyntaxNode body = (SyntaxNode)(((SyntaxNode)t.Children[3]).Children[0]);

				bool hasCondition = condition != null;
				bool hasBody = body.ChildCount > 0;

				int maxIterations = srm.MaxIterationsPerLoop;
				int iterations = 0;

				while (true)
				{
					if (maxIterations > 0 && ++iterations > maxIterations)
					{
						throw new ScriptExecutionTimeoutException(
							ctx.CreateErrorObject(t, string.Format(
								"Loop exceeded maximum iteration limit ({0}). " +
								"Possible infinite loop at line {1}.",
								maxIterations, t.Line)));
					}

					if (srm.IsForceStop) return null;

					if (hasCondition)
					{
						object conditionValue = ScriptRunningMachine.ParseNode(condition, ctx);

						if (!ScriptRunningMachine.GetBoolValue(conditionValue))
							return null;
					}

					if (hasBody)
					{
						object result = ScriptRunningMachine.ParseNode(body, ctx);

						if (result is BreakNode)
						{
							return null;
						}
						else if (result is ReturnNode)
						{
							return result;
						}
					}

					ScriptRunningMachine.ParseChildNodes(iteratorTree, ctx);
				}
			}

			#endregion
		}
		public class JITForStatementNodeParser : INodeParser
		{
			#region INodeParser Members
			private static long count = 0;

			//private struct ForSession
			//{
			//  public SyntaxNode Condition;
			//  public SyntaxNode Body;
			//  public SyntaxNode Iterators;
			//  public ScriptContext ctx;
			//}

			//public static object ParseCondition(ForSession ses)
			//{
			//  return ScriptRunningMachine.ParseNode(ses.Condition, ses.ctx);
			//}

			private static readonly MethodInfo _ParseNode = typeof(ScriptRunningMachine).GetMethod("ParseNode");
			private static readonly MethodInfo _ParseChildNodes = typeof(ScriptRunningMachine).GetMethod("ParseChildNodes");

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				SyntaxNode forInit = (SyntaxNode)t.Children[0];
				ScriptRunningMachine.ParseChildNodes(forInit, ctx);

				SyntaxNode condition = ((SyntaxNode)t.Children[1]);

				bool test = ScriptRunningMachine.GetBoolValue(ScriptRunningMachine.ParseNode(condition, ctx));
				if (!test) return null;

				DynamicMethod dm = new DynamicMethod("__for$" + count++, typeof(object),
					new Type[] { typeof(ScriptContext), typeof(SyntaxNode), typeof(SyntaxNode), typeof(SyntaxNode) });
				ILGenerator il = dm.GetILGenerator();

				SyntaxNode body = (SyntaxNode)(((SyntaxNode)t.Children[3]).Children[0]);
				SyntaxNode iterators = ((SyntaxNode)t.Children[2]);

				System.Reflection.Emit.Label start = il.DefineLabel();
				System.Reflection.Emit.Label end = il.DefineLabel();
				//LocalBuilder local = il.DeclareLocal(typeof(object[]));

				//ForSession ses=  new ForSession(){
				//  Condition = condition,
				//  Body= body,
				//  Iterators=iterators,};

				//object[] forses = new object[]{ ctx, condition, body, iterators};

				il.MarkLabel(start);

				// condition
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Call, _ParseNode);

				il.Emit(OpCodes.Unbox_Any, typeof(bool));
				il.Emit(OpCodes.Brfalse_S, end);

				// body
				il.Emit(OpCodes.Ldarg_2);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Call, _ParseNode);
				il.Emit(OpCodes.Pop);

				// iterators
				il.Emit(OpCodes.Ldarg_3);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Call, _ParseChildNodes);
				il.Emit(OpCodes.Pop);

				//// loop
				il.Emit(OpCodes.Br_S, start);

				il.MarkLabel(end);
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Ret);

				dm.Invoke(null, new object[] { ctx, condition, body, iterators });

				return null;
			}

			#endregion
		}

		#endregion
		#region Foreach
		class ForEachStatementNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				string varName = t.Children[0].ToString();

				CallScope scope = null;

				if (t.ChildCount > 3 && t.Children[3].Type == NodeType.TYPE
					&& !srm.IsInGlobalScope(ctx))
				{
					scope = ctx.CurrentCallScope;
				}

				SyntaxNode body = (SyntaxNode)t.Children[2];

				// retrieve target object
				object target = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[1], ctx);
				if (target is IAccess) target = ((IAccess)target).Get();

				if (target is IDictionary<string, object>)
				{
					IDictionary<string, object> dict = (IDictionary<string, object>)target;

					foreach (string key in dict.Keys)
					{
						if (scope == null)
						{
							srm[varName] = key;
						}
						else
						{
							scope[varName] = key;
						}

						// call iterator and terminal loop if break
						object result = ScriptRunningMachine.ParseNode(body, ctx);
						if (result is BreakNode)
						{
							return null;
						}
						else if (result is ReturnNode)
						{
							return result;
						}
					}
				}
				else if (target is IEnumerable)
				{
					IEnumerator iterator = ((IEnumerable)target).GetEnumerator();

					while (iterator.MoveNext())
					{
						// prepare key
						object value = iterator.Current;

						if (scope == null)
						{
							ctx.Srm[varName] = value;
						}
						else
						{
							scope[varName] = value;
						}

						// call iterator and terminal loop if break
						object result = ScriptRunningMachine.ParseNode(body, ctx);
						if (result is BreakNode)
						{
							return null;
						}
						else if (result is ReturnNode)
						{
							return result;
						}
					}
				}

				return null;
			}

			#endregion

		}

		#endregion
		#region Return
		class ReturnNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				object v = null;

				if (t.ChildCount > 0)
				{
					v = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[0], ctx);
					if (v is IAccess) v = ((IAccess)v).Get();
				}

				// TODO: make ReturnNode single instance
				return new ReturnNode(v);
			}

			#endregion
		}
		#endregion
		#region Try Catch
		class TryCatchNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				SyntaxNode tryBody = t.Children[0] as SyntaxNode;

				string errorObjIdentifier = null;

				SyntaxNode catchNode = t.Children[1] as SyntaxNode;
				SyntaxNode catchBody = catchNode.Children[0] as SyntaxNode;

				if (catchNode.ChildCount > 1)
				{
					errorObjIdentifier = catchNode.Children[1].Text;
				}

				SyntaxNode finallyNode = t.Children[2] as SyntaxNode;
				SyntaxNode finallyBody = finallyNode.ChildCount > 0 ? finallyNode.Children[0] as SyntaxNode : null;

				object ret = null;

				try
				{
					ret = ScriptRunningMachine.ParseNode(tryBody, ctx);
				}
				catch (ReoScriptException ex)
				{
					if (catchBody != null)
					{
						if (!string.IsNullOrEmpty(errorObjIdentifier))
						{
							ctx[errorObjIdentifier] = ex.ErrorObject.CustomeErrorObject == null ? ex.ErrorObject : ex.ErrorObject.CustomeErrorObject;

							try
							{
								ret = ScriptRunningMachine.ParseNode(catchBody, ctx);
							}
							finally
							{
								ctx.RemoveVariable(errorObjIdentifier);
							}
						}
						else
						{
							ret = ScriptRunningMachine.ParseNode(catchBody, ctx);
						}
					}
				}
				finally
				{
					if (finallyBody != null) ret = ScriptRunningMachine.ParseNode(finallyBody, ctx);
				}

				return ret;
			}

			#endregion
		}
		#endregion
		#region Throw
		class ThrowNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				object obj = ScriptRunningMachine.ParseNode(t.Children[0] as SyntaxNode, ctx);
				if (obj is IAccess) obj = ((IAccess)obj).Get();

				ErrorObject err = ctx.CreateErrorObject(t, Convert.ToString(obj));
				err.CustomeErrorObject = obj;
				throw new ReoScriptRuntimeException(err);
			}

			#endregion
		}
		#endregion
		#region Function Define
		class FunctionDefineNodeParser : INodeParser
		{
			private AssignmentNodeParser assignParser = new AssignmentNodeParser();

			#region INodeParser Members
			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext context)
			{
				FunctionObject fun = CreateAndInitFunction(context, ((FunctionDefineNode)t).FunctionInfo);
				// Capture the lexical scope this declaration is being walked in.
				// For top-level declarations CurrentCallScope is null and lookups
				// fall through to the global object.
				fun.CapturedScope = context.CurrentCallScope;
				srm[fun.FunName] = fun;
				return fun;
			}
			#endregion

			internal static FunctionObject CreateAndInitFunction(ScriptContext context, FunctionInfo fi)
			{
				ScriptRunningMachine srm = context.Srm;

				FunctionObject fun = srm.CreateNewObject(context,
					srm.BuiltinConstructors.FunctionFunction) as FunctionObject;

				if (fun == null) return null;

				ObjectValue prototype = srm.CreateNewObject(context, srm.BuiltinConstructors.ObjectFunction) as ObjectValue;
				prototype[ScriptRunningMachine.KEY___PROTO__] = fun[ScriptRunningMachine.KEY___PROTO__];

				fun[ScriptRunningMachine.KEY_PROTOTYPE] = prototype;

				if (!fi.IsAnonymous) fun.FunName = fi.Name;
				fun.Args = fi.Args;
				fun.Body = fi.BodyTree;
				fun.FunctionInfo = fi;
				//fun.OuterStack = context.GetCurrentCallScope();

				//AbstractFunctionObject inFun = context.GetCurrentCallScope().CurrentFunction;
				//if(inFun is FunctionObject){
				//  FunctionObject calledInFunction = (FunctionObject)inFun;
				//  fun.DeclaredFilePath = calledInFunction.DeclaredFilePath;
				return fun;
			}
		}
		#endregion
		#region Anonymous Function
		class AnonymousFunctionNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				FunctionObject fun = FunctionDefineNodeParser.CreateAndInitFunction(ctx,
					((AnonymousFunctionDefineNode)t).FunctionInfo);
				fun.CapturedScope = ctx.CurrentCallScope;
				return fun;
			}

			#endregion
		}

		#endregion
		#region Function Call
		class FunctionCallNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext context)
			{
				object funObj = null;
				object ownerObj = null;

				// local function call
				if (t.Children[0].Type == NodeType.IDENTIFIER)
				{
					string funName = t.Children[0].ToString();
					funObj = context[funName];

					if (funObj == null && context.CurrentCallScope != null)
					{
						CallScope outerScope = context.CurrentCallScope.CurrentFunction.CapturedScope;
						while (outerScope != null)
						{
							if (outerScope.Variables.TryGetValue(funName, out funObj))
							{
								break;
							}

							outerScope = outerScope.CurrentFunction.CapturedScope;
						}
					}
				}
				else
				{
					// object method call
					funObj = ScriptRunningMachine.ParseNode(t.Children[0] as SyntaxNode, context);

					if (funObj is PropertyAccess)
					{
						ownerObj = ((PropertyAccess)funObj).Object;
						string methodName = ((PropertyAccess)funObj).Identifier;

						if (!ScriptRunningMachine.IsPrimitiveTypes(ownerObj) && !(ownerObj is ISyntaxTreeReturn))
						{
							if (!srm.AllowDirectAccess)
							{
								// owner object is not ReoScript object and DirectAccess is disabled.
								// there is nothing can do so just return undefined.
								return null;
							}
							else
							{
								if (srm.AllowDirectAccess && !(ownerObj is ISyntaxTreeReturn))
								{
									object[] args = srm.GetParameterList(
											(t.ChildCount <= 1 ? null : t.Children[1] as SyntaxNode), context);

									methodName = ((srm.WorkMode & MachineWorkMode.AutoUppercaseWhenCLRCalling)
										== MachineWorkMode.AutoUppercaseWhenCLRCalling)
										? ScriptRunningMachine.GetNativeIdentifier(methodName) : methodName;

									MethodInfo mi = ScriptRunningMachine.FindCLRMethodAmbiguous(ownerObj, methodName, args);

									if (mi != null)
									{
										ParameterInfo[] paramTypeList = mi.GetParameters();

										try
										{
											object[] convertedArgs = new object[args.Length];
											for (int i = 0; i < convertedArgs.Length; i++)
											{
												convertedArgs[i] = srm.ConvertToCLRType(context, args[i], paramTypeList[i].ParameterType);
											}
											return mi.Invoke(ownerObj, convertedArgs);
										}
										catch (Exception ex)
										{
											if (srm.IgnoreCLRExceptions)
											{
												// call error, return undefined
												return null;
											}
											else
												throw ex;
										}
									}
								}
							}
						}

						funObj = ((IAccess)funObj).Get();

						if (funObj == null)
						{
							throw context.CreateRuntimeError(t, string.Format("{0} has no method '{1}'", ownerObj, methodName));
						}
					}
					else
					{
						if (funObj is IAccess) funObj = ((IAccess)funObj).Get();
					}
				}

				if (funObj == null)
				{
					throw context.CreateRuntimeError(t, "Function is not defined: " + t.Children[0].ToString());
				}

				AbstractFunctionObject fun = funObj as AbstractFunctionObject;

				if (fun == null)
				{
					throw context.CreateRuntimeError(t, "Object is not a function: " + Convert.ToString(funObj));
				}

				if (ownerObj == null) ownerObj = context.ThisObject;

				SyntaxNode argTree = t.ChildCount < 2 ? null : t.Children[1] as SyntaxNode;

				try
				{
					return srm.InvokeFunction(context, ownerObj, fun, srm.GetParameterList(argTree, context), t.CharPositionInLine, t.Line);
				}
				catch (Exception ex)
				{
					ReoScriptException rex = ex as ReoScriptException;

					if (rex != null && rex.ErrorObject == null)
					{
						rex.ErrorObject = context.CreateErrorObject(t, ex.Message);
					}
					else if (rex == null)
					{
						ex = new ReoScriptRuntimeException(context.CreateErrorObject(t, ex.Message), ex);
					}

					throw ex;
				}
			}

			#endregion
		}
		#endregion
		#region Create
		class CreateObjectNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext context)
			{
				// combiled construct
				if (t.ChildCount > 0 && t.Children[0].Type == NodeType.COMBINE_OBJECT)
				{
					SyntaxNode combileTree = ((SyntaxNode)t.Children[0]);
					ObjectValue combileObj = ScriptRunningMachine.ParseNode(combileTree.Children[1] as SyntaxNode, context) as ObjectValue;

					object createdObj = Parse(combileTree.Children[0] as SyntaxNode, srm, context);
					srm.CombineObject(context, createdObj, combileObj);
					return createdObj;
				}

				SyntaxNode tempTree = t.Children[0] as SyntaxNode;
				SyntaxNode constructTree = t;

				// need a depth variable to remember the depth of construct calling
				int committedDepth = 0, depth = 0;

				// find construct calling
				while (tempTree != null && tempTree.Children != null)
				{
					if (tempTree.Type == NodeType.FUNCTION_CALL)
					{
						constructTree = tempTree;
						committedDepth += depth;
					}

					tempTree = tempTree.Children[0] as SyntaxNode;
					depth++;
				}

				if (constructTree == null) throw context.CreateRuntimeError(t, "unexpected end to new operator.");

				// get constructor if it is need to retrieve from other Accessors
				object constructorValue = ScriptRunningMachine.ParseNode((SyntaxNode)constructTree.Children[0], context);

				// get identifier of constructor
				string constructorName = constructTree.Children[0].Type == NodeType.IDENTIFIER
					? constructTree.Children[0].Text : ScriptRunningMachine.KEY_UNDEFINED;

				if (constructorValue is IAccess) constructorValue = ((IAccess)constructorValue).Get();

				if (constructorValue == null)
				{
					constructorValue = srm.GetClass(constructorName);
				}

				if (constructorValue == null && srm.AllowDirectAccess)
				{
					Type type = srm.GetImportedTypeFromNamespaces(constructorName);
					if (type != null)
					{
						constructorValue = new TypedNativeFunctionObject(type, type.Name);
					}
				}

				if (constructorValue == null)
				{
					throw context.CreateRuntimeError(t, "Constructor not found: " + constructorName);
				}

				if (!(constructorValue is AbstractFunctionObject))
				{
					throw context.CreateRuntimeError(t, "Constructor is not of function: " + constructorName);
				}
				else
				{
					// call constructor
					AbstractFunctionObject funObj = (AbstractFunctionObject)constructorValue;

					SyntaxNode argTree = (constructTree == null || constructTree.ChildCount < 2) ? null : constructTree.Children[1] as SyntaxNode;
					object[] args = srm.GetParameterList(argTree, context);

					object obj = srm.CreateNewObject(context, funObj, constructArguments: args);

					// committedDepth > 0 means there is some primaryExpressions are remaining.
					// replace current construction tree and call srm to execute the remaining.
					if (committedDepth > 0)
					{
						SyntaxNode newTree = t.DeepClone();

						int d = 0;
						SyntaxNode ct = newTree.Children[0] as SyntaxNode;
						while (d++ < committedDepth - 1)
						{
							ct = ct.Children[0] as SyntaxNode;
						}

						// replace construction tree with created object
						ct.ReplaceChild(0, new ReplacedSyntaxNode(obj));

						// drop the construction tree topmost node [CREATE]
						newTree = newTree.Children[0] as SyntaxNode;

						// execute remained nodes than construction tree
						return ScriptRunningMachine.ParseNode(newTree, context);
					}
					else
						return obj;
				}
			}

			#endregion
		}
		#endregion
		#region ArrayAccess
		class IndexAccessNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext context)
			{
				object value = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[0], context);
				if (value is IAccess) value = ((IAccess)value).Get();

				if (value == null)
				{
					throw context.CreateRuntimeError(t, "Attempt to access an array or object that is null or undefined.");
				}

				object indexValue = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[1], context);
				if (indexValue is IAccess) indexValue = ((IAccess)indexValue).Get();

				if (value is IList list)
				{
					// index access for array
					return new ArrayAccess(srm, context, list, ScriptRunningMachine.GetIntValue(indexValue));
				}
				else if (value is ObjectValue)
				{
					// index access for object
					return new PropertyAccess(srm, context, value, ScriptRunningMachine.ConvertToString(indexValue));
				}
				else if (value is string || value is StringObject || value is StringBuilder)
				{
					// index access for string
					return new StringAccess(srm, context, value, ScriptRunningMachine.GetIntValue(indexValue));
				}
				else if (indexValue is string indexStr)
				{
					// index access for object
					return new PropertyAccess(srm, context, value, indexStr);
				}
				else
				{
					return null;
				}
			}

			#endregion
		}
		#endregion
		#region PropertyAccess
		class PropertyAccessNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				object value = null;

				value = ScriptRunningMachine.ParseNode((SyntaxNode)t.Children[0], ctx);
				if (value is IAccess) value = ((IAccess)value).Get();

				if (value == null) throw ctx.CreateRuntimeError(t,
					"Attempt to access property of null or undefined object" +
					((t.Children[0].Type == NodeType.IDENTIFIER)
					? (": " + t.Children[0].ToString()) : "."));

				string identifier = t.Children[1].Text;

				if (ScriptRunningMachine.IsPrimitiveNumber(value) || value is string || value is bool || value is IList)
				{
					// no need check
				}
				else if (!(value is ObjectValue))
				{
					if (value is ISyntaxTreeReturn)
					{
						throw ctx.CreateRuntimeError(t,
							string.Format("Attempt to access an object '{0}' that is not of Object type.", value.ToString()));
					}
					else if (!srm.AllowDirectAccess)
					{
						throw ctx.CreateRuntimeError(t, string.Format(
							"Attempt to access an object '{0}' that is not of Object type. To access .NET object, set the WorkMode to allow DirectAccess.",
							value.ToString()));
					}
				}

				return new PropertyAccess(srm, ctx, value, identifier);
			}

			#endregion
		}
		#endregion
		#region Delete Property
		class DeletePropertyNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				t = t.Children[0] as SyntaxNode;
				if (t == null) return false;

				if (t.Type == NodeType.IDENTIFIER)
				{
					string identifier = t.Text;
					if (ctx.GlobalObject.HasOwnProperty(identifier))
					{
						ctx.GlobalObject.RemoveOwnProperty(identifier);
						return true;
					}
				}
				else if (t.Type == NodeType.PROPERTY_ACCESS)
				{
					if (t.Children[1].Type != NodeType.IDENTIFIER)
					{
						throw ctx.CreateRuntimeError(t, "delete keyword requires an identifier to delete property from object.");
					}

					object owner = ScriptRunningMachine.ParseNode(t.Children[0] as SyntaxNode, ctx);
					if (owner is IAccess) owner = ((IAccess)owner).Get();

					if (owner == null)
					{
						string msg = "Attmpt to delete property from null or undefined object";

						if (t.Children[0].Type == NodeType.IDENTIFIER)
						{
							msg += ": " + t.Text;
						}
						else
						{
							msg += ".";
						}

						throw ctx.CreateRuntimeError(t, msg);
					}

					ObjectValue ownerObject = (ObjectValue)owner;

					if (ownerObject != null)
					{
						ownerObject.RemoveOwnProperty(t.Children[1].Text);
						return true;
					}
				}

				return false;
			}

			#endregion
		}

		#endregion
		#region Typeof
		class TypeofNodeParser : INodeParser
		{
			public static string Typeof(ScriptRunningMachine srm, object obj)
			{
				if (obj == null)
				{
					return "null";
				}
				else if (obj is bool)
				{
					return srm.BuiltinConstructors.BooleanFunction.FunName.ToLower();
				}
				else if (ScriptRunningMachine.IsPrimitiveNumber(obj)
					|| obj == NaNValue.Value || obj == InfinityValue.Value || obj == MinusInfinityValue.Value)
				{
					return srm.BuiltinConstructors.NumberFunction.FunName.ToLower();
				}
				else if (obj is string)
				{
					return srm.BuiltinConstructors.StringFunction.FunName.ToLower();
				}
				else if (obj is AbstractFunctionObject)
				{
					return srm.BuiltinConstructors.FunctionFunction.FunName.ToLower();
				}
				else if (obj is ObjectValue)
				{
					return srm.BuiltinConstructors.ObjectFunction.FunName.ToLower();
				}
				else if ((srm.WorkMode & MachineWorkMode.AllowDirectAccess) == MachineWorkMode.AllowDirectAccess)
				{
					return "native object";
				}
				else
				{
					return "unknown";
				}
			}

			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				object obj = ScriptRunningMachine.ParseNode(t.Children[0] as SyntaxNode, ctx);
				if (obj is IAccess) obj = ((IAccess)obj).Get();
				return Typeof(srm, obj);
			}

			#endregion
		}
		#endregion
		#region InstanceOf
		class InstanceOfNodeParser : INodeParser
		{
			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				object obj = ScriptRunningMachine.ParseNode(t.Children[0] as SyntaxNode, ctx);
				if (obj is IAccess) obj = ((IAccess)obj).Get();

				object constructor = ScriptRunningMachine.ParseNode(t.Children[1] as SyntaxNode, ctx);
				if (constructor is IAccess) constructor = ((IAccess)constructor).Get();

				if (!(constructor is AbstractFunctionObject)) return false;

				if (obj is string)
				{
					return constructor == srm.BuiltinConstructors.StringFunction;
				}
				else if (ScriptRunningMachine.IsPrimitiveNumber(obj))
				{
					return constructor == srm.BuiltinConstructors.NumberFunction;
				}
				else if (obj is bool)
				{
					return constructor == srm.BuiltinConstructors.BooleanFunction;
				}
				else if (srm.AllowDirectAccess && !(obj is ISyntaxTreeReturn)
					&& constructor is TypedNativeFunctionObject)
				{
					return obj.GetType().IsAssignableFrom(((TypedNativeFunctionObject)constructor).Type);
				}

				ObjectValue objValue = ((ObjectValue)obj);

				while (objValue != null)
				{
					bool instanceof = objValue.Constructor == constructor;
					if (instanceof) return true;

					objValue = objValue[ScriptRunningMachine.KEY___PROTO__] as ObjectValue;
				}

				return false;
			}
		}

		#endregion
		#region Tag
		class TagNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				string tagName = (((SyntaxNode)t.Children[0]).Children[0].ToString());

				tagName = ((srm.WorkMode & MachineWorkMode.AutoUppercaseWhenCLRCalling)
					== MachineWorkMode.AutoUppercaseWhenCLRCalling)
					? ScriptRunningMachine.GetNativeIdentifier(tagName) : tagName;

				AbstractFunctionObject classConstructor = srm.GetClass(tagName) as AbstractFunctionObject;
				if (classConstructor == null) throw ctx.CreateRuntimeError(t, "Class not found: " + tagName);

				ObjectValue obj = srm.CreateNewObject(ctx, classConstructor) as ObjectValue;

				if (obj != null)
				{
					if (classConstructor is TemplateConstructorObject)
					{
						// start of constructing tag from template

						TemplateConstructorObject templateConstructor = classConstructor as TemplateConstructorObject;

						Dictionary<string, object> mergedPropertes = new Dictionary<string, object>();

						// instance property setter
						SyntaxNode instAttrTree = t.Children[1] as SyntaxNode;

						CallScope scope = null;

						if (instAttrTree.ChildCount > 0)
						{
							scope = new CallScope(obj, classConstructor)
							{
								CharIndex = t.CharPositionInLine,
								Line = t.Line,
							};

							for (int i = 0; i < instAttrTree.ChildCount; i++)
							{
								SyntaxNode attr = instAttrTree.Children[i] as SyntaxNode;

								object val = ScriptRunningMachine.ParseNode(attr.Children[1] as SyntaxNode, ctx);
								if (val is IAccess) val = ((IAccess)val).Get();

								string propertyName = attr.Children[0].ToString();
								mergedPropertes[propertyName] = val;

								if (templateConstructor.Args.Contains(propertyName))
								{
									scope[propertyName] = val;
								}
							}
						}

						// template default setter
						SyntaxNode templateTagAttrTree = templateConstructor.TemplateTag.Children[1] as SyntaxNode;
						for (int i = 0; i < templateTagAttrTree.ChildCount; i++)
						{
							SyntaxNode attr = templateTagAttrTree.Children[i] as SyntaxNode;

							object val = ScriptRunningMachine.ParseNode(attr.Children[1] as SyntaxNode, ctx);
							if (val is IAccess) val = ((IAccess)val).Get();

							string propertyName = attr.Children[0].ToString();

							// properties that not need to set if value be setted from user-side
							if (!mergedPropertes.ContainsKey(propertyName))
							{
								mergedPropertes[propertyName] = val;
							}
						}

						// copy values into object
						foreach (string propertyName in mergedPropertes.Keys)
						{
							PropertyAccessHelper.SetProperty(ctx, obj, propertyName, mergedPropertes[propertyName]);
						}

						if (scope == null && templateConstructor.TemplateTag.ChildCount > 2)
						{
							scope = new CallScope(obj, classConstructor)
							{
								CharIndex = t.CharPositionInLine,
								Line = t.Line,
							};
						}

						if (scope != null)
						{
							ctx.PushCallStack(scope, true);

							try
							{
								for (int i = 2; i < templateConstructor.TemplateTag.ChildCount; i++)
								{
									SyntaxNode tagStmt = templateConstructor.TemplateTag.Children[i] as SyntaxNode;

									if (tagStmt.Type == NodeType.TAG)
									{
										srm.InvokeFunctionIfExisted(obj, "appendChild", ScriptRunningMachine.ParseNode(tagStmt, ctx));
									}
									else
									{
										ScriptRunningMachine.ParseNode(tagStmt, ctx);
									}
								}
							}
							finally
							{
								ctx.PopCallStack();
							}
						}

						// end of constructing tag from template
					}
					else
					{
						// start of constructing tag from class
						SyntaxNode attrTree = t.Children[1] as SyntaxNode;
						for (int i = 0; i < attrTree.ChildCount; i++)
						{
							SyntaxNode attr = attrTree.Children[i] as SyntaxNode;

							object val = ScriptRunningMachine.ParseNode(attr.Children[1] as SyntaxNode, ctx);
							if (val is IAccess) val = ((IAccess)val).Get();

							PropertyAccessHelper.SetProperty(ctx, obj, attr.Children[0].ToString(), val);
						}
						// end of constructing tag from class
					}

					if (t.ChildCount > 2)
					{
						CallScope scope = new CallScope(obj, classConstructor)
						{
							CharIndex = t.CharPositionInLine,
							Line = t.Line,
						};
						ctx.PushCallStack(scope, true);

						try
						{
							for (int i = 2; i < t.ChildCount; i++)
							{
								SyntaxNode tagStmt = t.Children[i] as SyntaxNode;

								if (tagStmt.Type == NodeType.TAG)
								{
									srm.InvokeFunctionIfExisted(obj, "appendChild", ScriptRunningMachine.ParseNode(tagStmt, ctx));
								}
								else
								{
									ScriptRunningMachine.ParseNode(tagStmt, ctx);
								}
							}
						}
						finally
						{
							ctx.PopCallStack();
						}
					}
				}

				return obj;
			}

			static void RunInTag(SyntaxNode tagTree, ScriptContext context)
			{

			}
			#endregion
		}

		class TemplateDefineNodeParser : INodeParser
		{
			#region INodeParser Members

			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				string typeName = t.Children[0].Text;

				SyntaxNode paramsTree = t.Children[1] as SyntaxNode;
				SyntaxNode rootTag = t.Children[2] as SyntaxNode;

				string tagName = (rootTag.Children[0] as SyntaxNode).Children[0].Text;

				TypedNativeFunctionObject typedConstructor = srm.GetClass(typeName) as TypedNativeFunctionObject;

				if (typedConstructor == null)
					throw ctx.CreateRuntimeError(t, "Class not found: " + typeName);

				TemplateConstructorObject templateDefine = new TemplateConstructorObject(tagName)
				{
					TemplateTag = rootTag,
					TypedConstructor = typedConstructor,
				};

				SyntaxNode rootAttrTree = rootTag.Children[1] as SyntaxNode;

				templateDefine[ScriptRunningMachine.KEY_PROTOTYPE] = templateDefine.CreatePrototype(
					new ScriptContext(srm, ScriptRunningMachine.entryFunction));

				string[] identifiers = new string[paramsTree.ChildCount];

				for (int i = 0; i < identifiers.Length; i++)
				{
					identifiers[i] = paramsTree.Children[i].ToString();
				}

				templateDefine.Args = identifiers;

				srm.RegisterClass(templateDefine, tagName);
				return null;
			}
			#endregion
		}
		class TemplateConstructorObject : TypedNativeFunctionObject
		{
			public TemplateConstructorObject(string name) : base(name) { }

			public SyntaxNode TemplateTag { get; set; }

			public string[] Args { get; set; }

			public TypedNativeFunctionObject TypedConstructor { get; set; }

			public override object CreateObject(ScriptContext context, object[] args)
			{
				return TypedConstructor.CreateObject(context, args);
			}

			public override object CreatePrototype(ScriptContext context)
			{
				return TypedConstructor.CreatePrototype(context);
			}

			public override object Invoke(ScriptContext context, object owner, object[] args)
			{
				return TypedConstructor.Invoke(context, owner, args);
			}
		}
		#endregion
		#region RangeGenerator
#if EXTERNAL_GETTER_SETTER
		class RangeLiteralParser : INodeParser
		{
			public object Parse(SyntaxNode t, ScriptRunningMachine srm, ScriptContext ctx)
			{
				string fromText = ((SyntaxNode)t.Children[0]).Text;
				string toText = ((SyntaxNode)t.Children[0]).Text;

				//object from = ScriptRunningMachine.ParseNode(t.Children[0] as SyntaxNode, ctx);
				//object to = ScriptRunningMachine.ParseNode(t.Children[1] as SyntaxNode, ctx);

				//if (from is IAccess) from = ((IAccess)from).Get();
				//if (to is IAccess) to = ((IAccess)to).Get();

				if (ctx.ExternalRangeGenerator != null)
				{
					ctx.ExternalRangeGenerator(fromText, toText);
				}

				return null;
			}
		}
#endif
		#endregion

	#region Parser Adapter
	#region Define Interface
	interface IParserAdapter
	{
		INodeParser MatchParser(SyntaxNode t);
	}
	#endregion

	#region AWDLDefaultParserAdapter
	class AWDLLogicSyntaxParserAdapter : IParserAdapter
	{
		internal static readonly INodeParser[] definedParser = new INodeParser[NodeType.MAX_TOKENS];

		static AWDLLogicSyntaxParserAdapter()
		{
			#region Generic Parsers
			definedParser[NodeType.IMPORT] = new ImportNodeParser();
			definedParser[NodeType.DESTRUCTURE] = new DestructureNodeParser();
			definedParser[NodeType.LOCAL_DECLARE_ASSIGNMENT] = new DeclarationNodeParser();
			definedParser[NodeType.ASSIGNMENT] = new AssignmentNodeParser();
			definedParser[NodeType.IF_STATEMENT] = new IfStatementNodeParser();
			definedParser[NodeType.FOR_STATEMENT] = new ForStatementNodeParser();
			//definedParser[NodeType.FOR_STATEMENT] = new JITForStatementNodeParser();
			definedParser[NodeType.FOREACH_STATEMENT] = new ForEachStatementNodeParser();
			definedParser[NodeType.SWITCH] = new SwitchCaseStatementNodeParser();
			definedParser[NodeType.FUNCTION_CALL] = new FunctionCallNodeParser();
			//definedParser[NodeType.FUNCTION_DEFINE] = new FunctionDefineNodeParser();
			definedParser[NodeType.ANONYMOUS_FUNCTION] = new AnonymousFunctionNodeParser();
			//definedParser[NodeType.BREAK] = new BreakNodeParser();
			//definedParser[NodeType.CONTINUE] = new ContinueNodeParser();
			definedParser[NodeType.RETURN] = new ReturnNodeParser();
			definedParser[NodeType.CREATE] = new CreateObjectNodeParser();
			definedParser[NodeType.TRY_CATCH] = new TryCatchNodeParser();
			definedParser[NodeType.TRY_CATCH_TRHOW] = new ThrowNodeParser();
			definedParser[NodeType.ARRAY_ACCESS] = new IndexAccessNodeParser();
			definedParser[NodeType.PROPERTY_ACCESS] = new PropertyAccessNodeParser();
			definedParser[NodeType.DELETE_PROP] = new DeletePropertyNodeParser();
			definedParser[NodeType.TYPEOF] = new TypeofNodeParser();
			definedParser[NodeType.INSTANCEOF] = new InstanceOfNodeParser();

			#endregion

			#region Operators
			definedParser[NodeType.PLUS] = new ExprPlusNodeParser();
			definedParser[NodeType.MINUS] = new ExprMinusNodeParser();
			definedParser[NodeType.MUL] = new ExprMultiNodeParser();
			definedParser[NodeType.DIV] = new ExprDivNodeParser();
			definedParser[NodeType.MOD] = new ExprModNodeParser();
			definedParser[NodeType.AND] = new ExprAndNodeParser();
			definedParser[NodeType.OR] = new ExprOrNodeParser();
			definedParser[NodeType.XOR] = new ExprXorNodeParser();
			definedParser[NodeType.LSHIFT] = new ExprLeftShiftNodeParser();
			definedParser[NodeType.RSHIFT] = new ExprRightShiftNodeParser();
			#endregion

			#region Unary Operators
			definedParser[NodeType.PRE_UNARY] = new ExprUnaryNodeParser();
			definedParser[NodeType.PRE_UNARY_STEP] = new ExprPreIncrementNodeParser();
			definedParser[NodeType.POST_UNARY_STEP] = new ExprPostIncrementNodeParser();
			#endregion

			#region Relation Operators
			definedParser[NodeType.CONDITION] = new ExprConditionNodeParser();
			definedParser[NodeType.EQUALS] = new ExprEqualsNodeParser();
			definedParser[NodeType.NOT_EQUALS] = new ExprNotEqualsNodeParser();
			definedParser[NodeType.STRICT_EQUALS] = new ExprStrictEqualsNodeParser();
			definedParser[NodeType.STRICT_NOT_EQUALS] = new ExprStrictNotEqualsNodeParser();
			definedParser[NodeType.GREAT_THAN] = new ExprGreaterThanNodeParser();
			definedParser[NodeType.GREAT_EQUALS] = new ExprGreaterOrEqualsNodeParser();
			definedParser[NodeType.LESS_THAN] = new ExprLessThanNodeParser();
			definedParser[NodeType.LESS_EQUALS] = new ExprLessOrEqualsNodeParser();
			#endregion

			#region Boolean Operations
			definedParser[NodeType.LOGICAL_AND] = new BooleanAndNodeParser();
			definedParser[NodeType.LOGICAL_OR] = new BooleanOrNodeParser();
			#endregion

			#region External Statements
			definedParser[NodeType.TAG] = new TagNodeParser();
			definedParser[NodeType.TEMPLATE_DEFINE] = new TemplateDefineNodeParser();
			//definedParser[NodeType.RANGE_EXP] = new RangeLiteralParser();
			#endregion
		}

		static object Parse(SyntaxNode t, ScriptContext ctx)
		{
			Parsers.INodeParser parser = definedParser[t.Type];
			return (parser != null) ? parser.Parse(t, ctx.Srm, ctx) : null;
		}


		#region IParserAdapter Members

		public virtual INodeParser MatchParser(SyntaxNode t)
		{
			return definedParser[t.Type];
		}

		#endregion
	}
	#endregion

	#endregion
	}
}
