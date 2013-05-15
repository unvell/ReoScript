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

	public class FileConstructor
	{

	}

	public class DirectoryConstructor : TypedNativeFunctionObject<DirectoryObject>
	{

	}
}
