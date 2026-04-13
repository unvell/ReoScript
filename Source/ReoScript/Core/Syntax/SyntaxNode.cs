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

using System.Collections.Generic;

namespace unvell.ReoScript
{
	/// <summary>
	/// Base class for all AST nodes. Replaces ANTLR's CommonTree with a
	/// lightweight, ANTLR-free implementation that keeps the same API surface
	/// so that existing INodeParser dispatch code works unchanged.
	/// </summary>
	public class SyntaxNode
	{
		private List<SyntaxNode> children;

		public int Type { get; set; }

		public string Text { get; set; }

		public int Line { get; set; }

		public int CharPositionInLine { get; set; }

		public IList<SyntaxNode> Children => children;

		public int ChildCount => children?.Count ?? 0;

		public bool IsNil => Type == 0;

		public SyntaxNode() { }

		public SyntaxNode(int type)
		{
			Type = type;
		}

		public SyntaxNode(int type, string text)
		{
			Type = type;
			Text = text;
		}

		public SyntaxNode(int type, string text, int line, int charPos)
		{
			Type = type;
			Text = text;
			Line = line;
			CharPositionInLine = charPos;
		}

		public void AddChild(SyntaxNode child)
		{
			if (child == null) return;
			children ??= new List<SyntaxNode>();
			children.Add(child);
		}

		public void AddChildren(IEnumerable<SyntaxNode> nodes)
		{
			if (nodes == null) return;
			children ??= new List<SyntaxNode>();
			children.AddRange(nodes);
		}

		public void ReplaceChild(int index, SyntaxNode replacement)
		{
			if (children != null && index >= 0 && index < children.Count)
			{
				children[index] = replacement;
			}
		}

		public SyntaxNode DeepClone()
		{
			var clone = new SyntaxNode(Type, Text, Line, CharPositionInLine);
			if (children != null)
			{
				foreach (var child in children)
				{
					clone.AddChild(child.DeepClone());
				}
			}
			return clone;
		}

		public override string ToString()
		{
			return Text ?? base.ToString();
		}
	}

	/// <summary>
	/// Holds a pre-evaluated constant value (number or string) produced during parsing.
	/// </summary>
	class ConstValueNode : SyntaxNode
	{
		public object ConstValue { get; set; }

		public int TokenType { get; set; }

		public ConstValueNode(object constValue, int tokenType, int line, int charPos)
			: base(NodeType.CONST_VALUE, null, line, charPos)
		{
			ConstValue = constValue;
			TokenType = tokenType;
		}
	}

	/// <summary>
	/// Used to replace a node of the syntax tree at runtime.
	/// </summary>
	class ReplacedSyntaxNode : SyntaxNode
	{
		public object Object { get; set; }

		public ReplacedSyntaxNode(object obj)
			: base(NodeType.REPLACED_TREE)
		{
			Object = obj;
		}
	}
}
