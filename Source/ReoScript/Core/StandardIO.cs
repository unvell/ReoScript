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
using System.Text;

namespace unvell.ReoScript
{
	/// <summary>
	/// Interface to provide standard input data for script.
	/// The method of implementation of this interface will be invoked when data is requested
	/// to input from script by __stdout__ and __stdoutln__ built-in functions.
	/// </summary>
	public interface IStandardInputProvider
	{
		/// <summary>
		/// Read a byte from provider.
		/// </summary>
		/// <returns>byte be read</returns>
		byte Read();

		/// <summary>
		/// Read a whole line string from provider.
		/// </summary>
		/// <returns>string line</returns>
		string ReadLine();
	}

	/// <summary>
	/// An interface to listen what data has been outputed by __stdout__ and __stdoutln__
	/// built-in functions in script.
	/// </summary>
	public interface IStandardOutputListener
	{
		/// <summary>
		/// Write a byte array to listener.
		/// </summary>
		/// <param name="buf">buffer where the byte array is saved</param>
		/// <param name="index">index in buffer read from</param>
		/// <param name="count">count in buffer to read</param>
		void Write(byte[] buf, int index, int count);

		/// <summary>
		/// Write a line to listener.
		/// </summary>
		/// <param name="line">line to be output</param>
		void WriteLine(string line);

		/// <summary>
		/// write a object to listener.
		/// </summary>
		/// <param name="obj">object to be output</param>
		void Write(object obj);
	}

	/// <summary>
	/// Default built-in console input provider for Standard I/O Interface.
	/// </summary>
	public class BuiltinConsoleInputProvider : IStandardInputProvider
	{
		/// <summary>
		/// Read a byte from console.
		/// </summary>
		/// <returns></returns>
		public byte Read()
		{
			return (byte)Console.ReadKey().KeyChar;
		}

		/// <summary>
		/// Read a string line from console.
		/// </summary>
		/// <returns></returns>
		public string ReadLine()
		{
			return Console.ReadLine();
		}
	}

	/// <summary>
	/// Default built-in
	/// </summary>
	public class BuiltinConsoleOutputListener : IStandardOutputListener
	{
		/// <summary>
		/// Print byte array in console.
		/// </summary>
		/// <param name="buf">buffer where the byte array saved</param>
		/// <param name="index">byte index from array</param>
		/// <param name="count">byte count in array</param>
		public void Write(byte[] buf, int index, int count)
		{
			Console.Write(Encoding.ASCII.GetString(buf, index, count));
		}

		/// <summary>
		/// Output string line passed from script.
		/// </summary>
		/// <param name="line">string line</param>
		public void WriteLine(string line)
		{
			Console.WriteLine(line);
		}

		/// <summary>
		/// Output serialized string of specified object from script.
		/// </summary>
		/// <param name="obj">the object to output</param>
		public void Write(object obj)
		{
			Console.Write(Convert.ToString(obj));
		}
	}
}
