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

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace unvell.ReoScript.TestCase
{
	public class TestCaseFailureException : Exception
	{
		public TestCaseFailureException(string msg) :
			base(msg) { }
	}

	[XmlRoot("test-suite")]
	public class XmlTestSuite
	{
		[XmlAttribute("id")]
		public string Id { get; set; }

		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("tag")]
		public string Tag { get; set; }

		[XmlElement("test-case")]
		public List<XmlTestCase> TestCases { get; set; }
	}

	public class XmlTestCase
	{
		[XmlAttribute("id")]
		public string Id { get; set; }

		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlElement("script")]
		public string Script { get; set; }

		[XmlText]
		public string TestCode { get; set; }

		[XmlAttribute("disabled")]
		public bool Disabled { get; set; }
	}

	public class TestSuiteAttribute : Attribute
	{
		public string Name { get; set; }

		public TestSuiteAttribute() { }
		public TestSuiteAttribute(string name) : base() { this.Name = name; }
	}

	public class TestCaseAttribute : Attribute
	{
		public string Desc { get; set; }

		public bool Disabled { get; set; }

		public MachineWorkMode WorkMode { get; set; }

		public TestCaseAttribute()
		{
			WorkMode = MachineWorkMode.Default | MachineWorkMode.AllowDirectAccess
				| MachineWorkMode.AllowCLREventBind | MachineWorkMode.AllowImportTypeInScript | MachineWorkMode.AutoImportRelationType
				| MachineWorkMode.AutoUppercaseWhenCLRCalling | MachineWorkMode.IgnoreCLRExceptions;
		}

		public TestCaseAttribute(string desc) : base() { this.Desc = desc; }
	}
}
