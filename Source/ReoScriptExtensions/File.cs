/*****************************************************************************
 * 
 * ReoScript - .NET Script Language Engine
 * 
 * http://www.unvell.com/ReoScript
 *
 * THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
 * KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
 * PURPOSE.
 *
 * This software released under LGPLv3 license.
 * Author: Jing Lu <dujid0@gmail.com>
 * 
 * Copyright (c) 2012-2013 unvell.com, all rights reserved.
 * 
 ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Unvell.ReoScript;

namespace Unvell.ReoScript.Extensions
{
	public class FileObject
	{
		public FileInfo FileInfo { get; set; }
	}

	public class DirectoryObject
	{
		public DirectoryInfo DirInfo { get; set; }
	}

	public class FileConstructorFunction : TypedNativeFunctionObject<DirectoryObject>
	{

	}

	public class DirectoryConstructorFunction : TypedNativeFunctionObject<DirectoryObject>
	{

	}

	[ModuleLoader]
	public class FileModuleLoader : IModuleLoader
	{
		public void LoadModule(ScriptRunningMachine srm)
		{
			srm.ImportType(typeof(FileConstructorFunction), "File");
			srm.ImportType(typeof(DirectoryConstructorFunction), "Directory");
		}

		public void UnloadModule(ScriptRunningMachine srm)
		{
		}
	}


}
