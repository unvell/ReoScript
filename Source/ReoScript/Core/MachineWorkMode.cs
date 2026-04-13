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
	/// Defines and represents the working mode of script running machine
	/// </summary>
	public enum MachineWorkMode
	{
		/// <summary>
		/// Default working mode
		/// </summary>
		Default = 0 | MachineWorkMode.IgnoreCLRExceptions | MachineWorkMode.AutoImportRelationType |
			MachineWorkMode.AutoUppercaseWhenCLRCalling,

		/// <summary>
		/// Allows to access .NET object, type, namespace, etc. directly.
		/// </summary>
		AllowDirectAccess = 0x1,

		/// <summary>
		/// Allows to auto-bind CLR event. This option works with AllowDirectAccess.
		/// </summary>
		AllowCLREventBind = 0x2,

		/// <summary>
		/// Allows import .NET namespaces and classes in script using 'import' keyword.
		/// </summary>
		AllowImportTypeInScript = 0x4,

		/// <summary>
		/// Ignore all exception in CLR invoking.
		/// </summary>
		IgnoreCLRExceptions = 0x8,

		/// <summary>
		/// Allows ReoScript to auto-import the relation types that may used in other imported type.
		/// </summary>
		AutoImportRelationType = 0x10,

		/// <summary>
		/// Allows to auto-convert the name into uppercase to find member or property during CLR calling.
		/// </summary>
		AutoUppercaseWhenCLRCalling = 0x20,
	}

	/// <summary>
	/// Specifies what features can be supported by ScriptRunningMachine.
	/// </summary>
	public enum CoreFeatures
	{
		/// <summary>
		/// A set of full features will be supported, both StandardFeatures and ExtendedFeatures.
		/// </summary>
		FullFeatures = StandardFeatures | ExtendedFeatures,

		/// <summary>
		/// A set of standard features will be supported. (Compatible with ECMAScript)
		/// Contains the alert, eval, setTimeout, setInterval and console object.
		/// </summary>
		StandardFeatures = Alert | Console | Eval | AsyncCalling | JSON,

		/// <summary>
		/// A set of featuers supported by default. (equals StandardFeatures)
		/// </summary>
		Default = StandardFeatures,

		/// <summary>
		/// Only minimum features will be supported
		/// </summary>
		None = 0x0,

		/// <summary>
		/// alert function support
		/// </summary>
		Alert = 0x1,

		/// <summary>
		/// eval function support
		/// </summary>
		Eval = 0x2,

		/// <summary>
		/// setTimeout and setInterval function support
		/// </summary>
		AsyncCalling = 0x4,

		/// <summary>
		/// console object support
		/// </summary>
		Console = 0x8,

		/// <summary>
		/// JSON object with 'parse' and 'stringify' method will be provided for script
		/// </summary>
		JSON = 0x10,

		/// <summary>
		/// Extended Feature supported by ReoScript (Non-compatible with ECMAScript)
		/// </summary>
		ExtendedFeatures = ArrayExtension,

		/// <summary>
		/// Array extension feature support
		/// </summary>
		ArrayExtension = 0x10000,
	}
}
