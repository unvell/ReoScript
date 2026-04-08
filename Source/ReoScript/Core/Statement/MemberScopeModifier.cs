/*****************************************************************************
 * 
 * ReoScript - .NET Script Language Engine
 * 
 * https://github.com/unvell/ReoScript
 *
 * THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
 * KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
 * PURPOSE.
 *
 * This software released under MIT license.
 * Copyright (c) 2012-2019 Jingwood, unvell.com, all rights reserved.
 * 
 ****************************************************************************/

namespace unvell.ReoScript
{
	/// <summary>
	/// Member scope modifier. (RESERVED FEATURE)
	/// </summary>
	public enum MemberScopeModifier
	{
		/// <summary>
		/// Property or method visible to any scope
		/// </summary>
		Public,

		/// <summary>
		/// Property or method visible to other members defined in same file
		/// </summary>
		Internal,

		/// <summary>
		/// Property or method visible to other members belonging to same instance or prototype
		/// </summary>
		Protected,

		/// <summary>
		/// Property or method visible to other members belonging to same instance
		/// </summary>
		Private,
	}
}
