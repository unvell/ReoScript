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

using unvell.ReoScript.Core.Statement;

namespace unvell.ReoScript
{
	#region Extension Objects
	/// <summary>
	/// Dynamic access properties of an object
	/// </summary>
	public class DynamicPropertyObject : ObjectValue
	{
		public Action<string, object> propertySetter { get; set; }
		public Func<string> propertyGetter { get; set; }

		public DynamicPropertyObject(Action<string, object> setter, Func<string> getter)
		{
			this.propertySetter = setter;
			this.propertyGetter = getter;
		}

		public override object this[string name]
		{
			get
			{
				return propertyGetter != null ? propertyGetter() : base[name];
			}
			set
			{
				if (propertySetter != null)
				{
					propertySetter(name, value);
				}
				else
				{
					base[name] = value;
				}
			}
		}
	}

	/// <summary>
	/// ExternalProperty class provides an interface to extend a property to an
	/// object which declared and used in ReoScript context. ExternalProperty
	/// has a getter and setter delegate method that will be invoked automatically
	/// when the property value is accessed in script at runtime.
	/// </summary>
	public class ExternalProperty : ISyntaxTreeReturn
	{
		/// <summary>
		/// Getter method will be invoked when value is required in script
		/// </summary>
		public Func<object> Getter { get; set; }

		/// <summary>
		/// Setter method will be invoked when property is set to a value
		/// in script.
		/// </summary>
		public Action<object> Setter { get; set; }

		/// <summary>
		/// Create extension property for object with getter and setter method
		/// </summary>
		/// <param name="getter">delegate invoked when value be getted</param>
		/// <param name="setter">delegate invoked when value be setted</param>
		public ExternalProperty(Func<object> getter, Action<object> setter)
		{
			this.Getter = getter;
			this.Setter = setter;
		}

		/// <summary>
		/// Create readonly extension property with only getter method
		/// </summary>
		/// <param name="getter">method invoked when value be getted</param>
		public ExternalProperty(Func<object> getter)
			: this(getter, null) { }

		/// <summary>
		/// Invoke getter to retrieve a value from CLR runtime
		/// </summary>
		/// <returns></returns>
		public object GetNativeValue()
		{
			return Getter == null ? null : Getter();
		}

		/// <summary>
		/// Invoke setter to set property value that provided from CLR
		/// </summary>
		/// <param name="value"></param>
		public void SetNativeValue(object value)
		{
			if (Setter != null) Setter(value);
		}

		/// <summary>
		/// Convert value to string
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Getter == null ? "null" : Convert.ToString(Getter());
		}
	}

	/// <summary>
	/// IDirectAccess is a empty interface that allows CLR instance can be
	/// accessed directly from ReoScript.
	/// </summary>
	//public interface IDirectAccess
	//{
	//}

	/// <summary>
	/// DirectAccessAttribute allows CLR instace can be accessed directly
	/// from ReoScript.
	/// </summary>
	//public class DirectAccessAttribute : Attribute
	//{
	//}

	#endregion // Extension Values
}
