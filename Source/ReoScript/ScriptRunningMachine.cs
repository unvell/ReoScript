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
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Net;
// System.Windows.Forms removed for cross-platform .NET 10 support
using System.ComponentModel;
using System.Threading;
using System.Collections.Generic;
using System.Reflection.Emit;


// Resources now loaded via Assembly.GetManifestResourceStream instead of resx
using unvell.ReoScript.Runtime;
using unvell.ReoScript.Parsers;
using unvell.ReoScript.Reflection;
using unvell.ReoScript.Core.Statement;
using unvell.ReoScript.Core;

namespace unvell.ReoScript
{

	#region ScriptRunningMachine
	/// <summary>
	/// A virtual machine to execute ReoScript language. 
	/// </summary>
	public sealed class ScriptRunningMachine
	{
		#region Const & Keywords

		/// <summary>
		/// Keyword of undefined script object
		/// </summary>
		internal static readonly string KEY_UNDEFINED = "undefined";

		/// <summary>
		/// keyword of prototype of constructor function
		/// </summary>
		internal static readonly string KEY_PROTOTYPE = "prototype";

		internal static readonly string KEY___PROTO__ = "__proto__";

		internal static readonly string KEY_CONSTRUCTOR = "constructor";

		internal static readonly string KEY___ARGS__ = "__args__";

		/// <summary>
		/// Global variable name
		/// </summary>
		internal static readonly string GLOBAL_VARIABLE_NAME = "script";

		#endregion

		#region Constructor

		/// <summary>
		/// Specifies what features can be supported by SRM.
		/// After modify this value call Reset method to apply the changes.
		/// </summary>
		public CoreFeatures CoreFeatures { get; set; }

		/// <summary>
		/// Construct SRM with Standard feature support.
		/// </summary>
		public ScriptRunningMachine()
			: this(CoreFeatures.StandardFeatures)
		{
			//this();
		}

		/// <summary>
		/// Construct SRM with specified feature support.
		/// </summary>
		/// <param name="coreFeatures">Specifies what features can be supported by SRM.</param>
		public ScriptRunningMachine(CoreFeatures coreFeatures)
		{
			this.WorkPath = Environment.CurrentDirectory;
			this.CoreFeatures = coreFeatures;

			Reset();
		}

		~ScriptRunningMachine()
		{
			DetachAllEvents();

			try
			{
				if (asyncCallThread != null) asyncCallThread.Abort();
			}
			catch { }
		}

		#endregion

		#region Context
		internal BuiltinConstructors BuiltinConstructors = new BuiltinConstructors();

		//internal ScriptContext CurrentContext { get; set; }

		//internal object RetrieveVariable(string identifier)
		//{
		//  object obj = CurrentContext[identifier];
		//  return obj == null ? this[identifier] : obj;
		//}

		/// <summary>
		/// Reset current context to clear all variables and restart running machine.
		/// </summary>
		public void Reset()
		{
			// force stop current executing
			ForceStop();

			// wait for current executing exiting
			while (IsRunning)
				Thread.Sleep(100);

			// detach all attched CLR events
			DetachAllEvents();

			// reset imported namespace and types
			ImportedNamespace.Clear();
			ImportedTypes.Clear();
			importedCodeFiles.Clear();
			moduleCache.Clear();

			// reset machine status
			isForceStop = false;

			// renew global object
			GlobalObject = new WorldObject();

			// initialize built-in objects
			BuiltinConstructors = new BuiltinConstructors();
			BuiltinConstructors.ApplyToScriptRunningMachine(this);

			// load core library
			LoadCoreLibraries();

			// initialize default context
			defaultContext = new ScriptContext(this, entryFunction);

			if (Resetted != null) Resetted(this, null);
		}

		internal void LoadCoreLibraries()
		{
			LoadEmbeddedScript("unvell.ReoScript.scripts.core.reo");
			LoadEmbeddedScript("unvell.ReoScript.scripts.array.reo");

			if ((this.CoreFeatures & CoreFeatures.ArrayExtension) == CoreFeatures.ArrayExtension)
			{
				LoadEmbeddedScript("unvell.ReoScript.scripts.array_ext.reo");
			}
		}

		private void LoadEmbeddedScript(string resourceName)
		{
			using (Stream stream = typeof(ScriptRunningMachine).Assembly.GetManifestResourceStream(resourceName))
			{
				if (stream != null) Load(stream);
			}
		}

		private ScriptContext defaultContext;

		/// <summary>
		/// Default script context
		/// </summary>
		public ScriptContext DefaultContext { get { return defaultContext; } }

		/// <summary>
		/// Create new script context
		/// </summary>
		/// <returns>created script context</returns>
		public ScriptContext CreateContext()
		{
			return new ScriptContext(this, entryFunction);
		}
		#endregion

		#region Global Variable
		public ObjectValue GlobalObject { get; set; }

		/// <summary>
		/// Set value as a property to the global object. Value name specified by
		/// identifier. After this, the value can be used in script like a normal 
		/// variable.
		/// </summary>
		/// <param name="identifier">name to variable</param>
		/// <param name="obj">value of variable</param>
		public void SetGlobalVariable(string identifier, object obj)
		{
			// if object is function, prepare its prototype 
			if (obj is AbstractFunctionObject)
			{
				AbstractFunctionObject functionObj = (AbstractFunctionObject)obj;

				if (functionObj[KEY_PROTOTYPE] == null)
				{
					functionObj[KEY_PROTOTYPE] = functionObj.CreatePrototype(
						new ScriptContext(this, entryFunction));
				}
			}

			GlobalObject[identifier] = obj;
		}

		/// <summary>
		/// Get a global variable from global object by specified name.
		/// </summary>
		/// <param name="identifier">variable name</param>
		/// <returns>value of global variable</returns>
		public object GetGlobalVariable(string identifier)
		{
			return GlobalObject[identifier];
		}

		/// <summary>
		/// Get a global variable of specified type from global object. If the type convertion is failed, 
		/// a null value will be returned.
		/// </summary>
		/// <typeparam name="T">type to be converted</typeparam>
		/// <param name="identifier">variable name in globla object</param>
		/// <returns>object retrieved from global object</returns>
		public T GetGlobalVariable<T>(string identifier)
		{
			return GetGlobalVariable<T>(identifier, null);
		}

		/// <summary>
		/// Get a global variable of specified type from global object. If the type convertion is failed, 
		/// the defaultValue will be returned.
		/// </summary>
		/// <typeparam name="T">type to be converted</typeparam>
		/// <param name="identifier">variable name in globla object</param>
		/// <param name="defaultValue">if convertion is failed, this value will be returned</param>
		/// <returns>object retrieved from global object</returns>
		public T GetGlobalVariable<T>(string identifier, object defaultValue)
		{
			try
			{
				return (T)Convert.ChangeType(GetGlobalVariable(identifier), typeof(T));
			}
			catch
			{
				return (T)defaultValue;
			}
		}

		/// <summary>
		/// Delete a specified global variable.
		/// </summary>
		/// <param name="identifier">variable name</param>
		/// <returns>true if specified variable does exist and deleting is successed</returns>
		public bool RemoveGlobalVariable(string identifier)
		{
			return GlobalObject.RemoveOwnProperty(identifier);
		}

		/// <summary>
		/// Set or get global variables
		/// </summary>
		/// <param name="identifier">identifier to be used as variable name</param>
		/// <returns>object in global object</returns>
		public object this[string identifier]
		{
			get
			{
				return GetGlobalVariable(identifier);
			}
			set
			{
				SetGlobalVariable(identifier, value);
			}
		}

		/// <summary>
		/// Dummy function object for most outside code scope.
		/// </summary>
		internal static readonly FunctionObject entryFunction = new FunctionObject()
		{
			FunName = "__entry__",
		};

		internal bool IsInGlobalScope(ScriptContext context)
		{
			return context.CurrentCallScope == null;
		}

		#endregion

		#region Object Management

		/// <summary>
		/// Create a new object instance 
		/// </summary>
		/// <returns>object is created</returns>
		public ObjectValue CreateNewObject()
		{
			return CreateNewObject(new ScriptContext(this, entryFunction), BuiltinConstructors.ObjectFunction) as ObjectValue;
		}

		//public ObjectValue CreateNewObject(string name)
		//{
		//  ObjectValue obj = CreateNewObject();
		//  //obj.Name = name;
		//  return obj;
		//}

		public ObjectValue CreateNewObject(Action<ObjectValue> initializer)
		{
			ObjectValue obj = CreateNewObject();
			initializer(obj);
			return obj;
		}

		public ObjectValue CreateNewObject(Dictionary<string, object> properties)
		{
			return CreateNewObject(obj => obj.AddProperties(properties));
		}

		internal ObjectValue CreateNewObject(ScriptContext context)
		{
			return CreateNewObject(context, BuiltinConstructors.ObjectFunction) as ObjectValue;
		}

		internal object CreateNewObject(ScriptContext context, AbstractFunctionObject constructor, bool invokeConstructor = true, object[] constructArguments = null)
		{
			object obj = null;

			if (constructor is NativeFunctionObject)
			{
				obj = ((NativeFunctionObject)constructor).CreateObject(context, constructArguments);
			}

			if (obj == null) obj = new ObjectValue();

			if (obj is ObjectValue)
			{
				ObjectValue objValue = obj as ObjectValue;

				//objValue.Name = constructor.FunName == null ? 
				//  constructor.Name : constructor.FunName;

				// point to constructor
				//objValue[ScriptRunningMachine.KEY_CONSTRUCTOR] = constructor;
				objValue.Constructor = constructor;

				// get prototype from constructor
				object prototype = constructor[KEY_PROTOTYPE];

				// create prototype if not existed
				objValue[KEY___PROTO__] = prototype;
			}

			if (invokeConstructor)
			{
				InvokeFunction(context, obj, constructor, constructArguments);
			}

			if (obj != null && NewObjectCreated != null)
			{
				NewObjectCreated(this, new ReoScriptObjectEventArgs(obj, constructor));
			}

			return obj;
		}

		//internal StringValue CreateNewString(string str)
		//{
		//  StringValue strObj = new StringValue(ConvertEscapeLiterals(str)) { Name = "String" };

		//  strObj[ScriptRunningMachine.KEY___PROTO__]
		//    = BuiltinConstructors.StringFunction[ScriptRunningMachine.KEY_PROTOTYPE];

		//  //strObj[ScriptRunningMachine.KEY_CONSTRUCTOR]
		//  //  = BuiltinConstructors.StringFunction;

		//  strObj.Constructor = BuiltinConstructors.StringFunction;

		//  if (NewObjectCreated != null)
		//  {
		//    NewObjectCreated(this, new ReoScriptObjectEventArgs(strObj, BuiltinConstructors.StringFunction));
		//  }

		//  return strObj;
		//}
		#endregion

		#region CLR Type Import
		private List<ScriptRunningMachine.EventHandlerInfo> registeredEventHandlers = new List<ScriptRunningMachine.EventHandlerInfo>();

		internal List<ScriptRunningMachine.EventHandlerInfo> RegisteredEventHandlers
		{
			get { return registeredEventHandlers; }
			set { registeredEventHandlers = value; }
		}

		private List<Type> importedTypes = new List<Type>();

		internal List<Type> ImportedTypes
		{
			get { return importedTypes; }
			set { importedTypes = value; }
		}

		private List<string> importedNamespace = new List<string>();

		internal List<string> ImportedNamespace
		{
			get { return importedNamespace; }
			set { importedNamespace = value; }
		}

		/// <summary>
		/// Import a .NET type into script context. This method will create a constructor function
		/// by type's name and save it as property into global object. Note that if there 
		/// is an object named type's name the object will be overwritten.
		/// </summary>
		/// <param name="type">type to be added into script context</param>
		public void ImportType(Type type)
		{
			ImportType(type, type.Name);
		}

		/// <summary>
		/// Import a .NET type into script context. This method will create a constrcutor function
		/// by given alias name and save it as property into global object. Note that if there
		/// is an object named type's name the object will be overwritten.
		/// </summary>
		/// <param name="type">type to be added into script context</param>
		/// <param name="name">alias name to create constructor function in global object</param>
		public void ImportType(Type type, string name)
		{
			if (ImportedTypes.Contains(type))
			{
				ImportedTypes.Remove(type);
			}

			ImportedTypes.Add(type);

			SetGlobalVariable(name, new TypedNativeFunctionObject(type, name));
		}

		/// <summary>
		/// Import a namespace into script context
		/// </summary>
		/// <param name="name">namespace to be registered into script context</param>
		public void ImportNamespace(string name)
		{
			if (name.EndsWith("*")) name = name.Substring(0, name.Length - 1);
			if (name.EndsWith(".")) name = name.Substring(0, name.Length - 1);

			if (!ImportedNamespace.Contains(name))
			{
				ImportedNamespace.Add(name);
			}
		}

		internal Type GetImportedTypeFromNamespaces(string typeName)
		{
			Type type = null;

			foreach (string ns in ImportedNamespace)
			{
				type = GetTypeFromAssembly(ns, typeName);
				if (type != null) return type;
			}

			return type;
		}

		internal Type GetTypeFromAssembly(string ns, string typeName)
		{
			Type type = null;

			// search assembly which's name starting with specified namespace
			Assembly ass = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(q => q.FullName.StartsWith(ns));

			if (ass != null)
			{
				type = ass.GetType(ns + "." + typeName);
				if (type != null)
				{
					ImportType(type);
					return type;
				}
			}

			return type;
		}

		internal void CombineObject(ScriptContext context, object target, ObjectValue source)
		{
			foreach (string key in source)
			{
				// FIXME: get member by PropertyAccessHelper.GetProperty?
				//        enumerated in only own members
				PropertyAccessHelper.SetProperty(context, target, key, source[key]);
			}
		}

		#endregion

		#region CLR Event

		internal void AttachEvent(ScriptContext context, object obj, EventInfo ei, FunctionObject functionValue)
		{
			// remove last attached event to sample object
			DetachEvent(obj, ei);

			EventHandlerInfo ehi = new EventHandlerInfo(this, context, obj, ei, null, functionValue);
			Action<object> doEvent = (e) =>
			{
				try
				{
					InvokeFunction(context, obj, functionValue, new object[] { e });
				}
				catch (ReoScriptException ex)
				{
					RaiseScriptError(ex);
				}
			};

			Delegate d = null;
			if (ei.EventHandlerType == typeof(EventHandler))
			{
				d = new EventHandler((s, e) => doEvent(e));
			}
			else
			{
				// Generic handler: create a delegate matching the event's signature
				// by wrapping the second parameter (EventArgs-derived) through doEvent.
				MethodInfo invokeMethod = ei.EventHandlerType.GetMethod("Invoke");
				ParameterInfo[] parms = invokeMethod.GetParameters();
				if (parms.Length == 2)
				{
					d = new EventHandler((s, e) => doEvent(e));
					d = Delegate.CreateDelegate(ei.EventHandlerType,
						d.Target, d.Method, false) ?? new EventHandler((s, e) => doEvent(e));
				}
			}

			ehi.ActionMethod = d;
			ei.AddEventHandler(obj, d);

			RegisteredEventHandlers.Add(ehi);
			return;

			// Get an EventInfo representing the Click event, and get the 
			// type of delegate that handles the event. 
			//
			EventInfo evClick = ei;
			Type tDelegate = evClick.EventHandlerType;

			// If you already have a method with the correct signature, 
			// you can simply get a MethodInfo for it.  
			//
			//MethodInfo miHandler =
			//    typeof(Example).GetMethod("LuckyHandler",
			//        BindingFlags.NonPublic | BindingFlags.Instance);

			// Create an instance of the delegate. Using the overloads 
			// of CreateDelegate that take MethodInfo is recommended. 
			//
			//Delegate d = Delegate.CreateDelegate(tDelegate, this, miHandler);

			// Get the "add" accessor of the event and invoke it late-
			// bound, passing in the delegate instance. This is equivalent 
			// to using the += operator in C#, or AddHandler in Visual 
			// Basic. The instance on which the "add" accessor is invoked
			// is the form; the arguments must be passed as an array. 
			//
			MethodInfo addHandler = evClick.GetAddMethod();
			//Object[] addHandlerArgs = { d };
			//addHandler.Invoke(exFormAsObj, addandlerArgs);
			//---------------------------------------------

			// Event handler methods can also be generated at run time, 
			// using lightweight dynamic methods and Reflection.Emit.  
			// To construct an event handler, you need the return type 
			// and parameter types of the delegate. These can be obtained 
			// by examining the delegate's Invoke method.  
			// 
			// It is not necessary to name dynamic methods, so the empty  
			// string can be used. The last argument associates the  
			// dynamic method with the current type, giving the delegate 
			// access to all the public and private members of Example, 
			// as if it were an instance method. 
			//
			Type returnType = GetDelegateReturnType(tDelegate);
			if (returnType != typeof(void))
				throw new ApplicationException("Delegate has a return type.");

			DynamicMethod handler =
					new DynamicMethod(string.Empty,
														null,
														GetDelegateParameterTypes(tDelegate),
														typeof(ScriptRunningMachine));

			// Generate a method body. This method loads a string, calls  
			// the Show method overload that takes a string, pops the  
			// return value off the stack (because the handler has no 
			// return type), and returns. 
			//
			ILGenerator ilgen = handler.GetILGenerator();

			//Type[] showParameters = { typeof(object) };
			//MethodInfo simpleShow = typeof(MessageBox).GetMethod("Show", showParameters);

			MethodInfo mi = GetType().GetMethod("DoEventFunction", BindingFlags.NonPublic | BindingFlags.Static);

			//Type[] showParameters = { typeof(String) };
			//MethodInfo simpleShow =
			//    typeof(MessageBox).GetMethod("Show", showParameters);

			ilgen.Emit(OpCodes.Ldarg_1);
			//ilgen.Emit(OpCodes.Ldobj);
			//ilgen.Emit(OpCodes.Ldobj, 
			//ilgen.Emit(OpCodes.Ldstr, "This event handler was constructed at run time.");
			//ilgen.Emit(OpCodes.Call, evtHandler.Method);
			ilgen.Emit(OpCodes.Call, doEvent.Method);
			//ilgen.Emit(OpCodes.Pop);
			ilgen.Emit(OpCodes.Ret);

			// Complete the dynamic method by calling its CreateDelegate 
			// method. Use the "add" accessor to add the delegate to
			// the invocation list for the event. 
			//
			Delegate dEmitted = handler.CreateDelegate(tDelegate);
			addHandler.Invoke(obj, new Object[] { dEmitted });

			//ehi.ActionMethod = Delegate.CreateDelegate(ei.EventHandlerType, ehi, "DoEvent");
			//ei.AddEventHandler(obj, ehi.ActionMethod);

			RegisteredEventHandlers.Add(ehi);
		}

		private static void DoEventFunction(object e)
		{
			System.Diagnostics.Debug.WriteLine("DoEvent: " + e.ToString());
		}

		private Type[] GetDelegateParameterTypes(Type d)
		{
			if (d.BaseType != typeof(MulticastDelegate))
				throw new ApplicationException("Not a delegate.");

			MethodInfo invoke = d.GetMethod("Invoke");
			if (invoke == null)
				throw new ApplicationException("Not a delegate.");

			ParameterInfo[] parameters = invoke.GetParameters();
			Type[] typeParameters = new Type[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				typeParameters[i] = parameters[i].ParameterType;
			}
			return typeParameters;
		}

		private Type GetDelegateReturnType(Type d)
		{
			if (d.BaseType != typeof(MulticastDelegate))
				throw new ApplicationException("Not a delegate.");

			MethodInfo invoke = d.GetMethod("Invoke");
			if (invoke == null)
				throw new ApplicationException("Not a delegate.");

			return invoke.ReturnType;
		}

		internal void DetachEvent(object obj, EventInfo ei)
		{
			var ehi = RegisteredEventHandlers.FirstOrDefault(reh =>
				reh.EventInfo == ei && reh.Instance == obj);

			if (ehi != null)
			{
				ehi.EventInfo.RemoveEventHandler(obj, ehi.ActionMethod);

				RegisteredEventHandlers.Remove(ehi);
			}
		}

		private void DetachAllEvents()
		{
			foreach (EventHandlerInfo handlerInfo in RegisteredEventHandlers)
			{
				handlerInfo.EventInfo.RemoveEventHandler(handlerInfo.Instance, handlerInfo.ActionMethod);
			}

			RegisteredEventHandlers.Clear();
		}

		internal FunctionObject GetAttachedEvent(object obj, EventInfo ei)
		{
			var ehi = RegisteredEventHandlers.FirstOrDefault(reh =>
				reh.EventInfo == ei && reh.Instance == obj);

			return ehi == null ? null : ehi.FunctionValue;
		}

		internal class EventHandlerInfo
		{
			public object Instance { get; set; }
			public EventInfo EventInfo { get; set; }
			public Delegate ActionMethod { get; set; }
			public FunctionObject FunctionValue { get; set; }
			public ScriptRunningMachine Srm { get; set; }
			public ScriptContext Context { get; set; }

			internal EventHandlerInfo(ScriptRunningMachine srm, ScriptContext context, object instance,
				EventInfo eventInfo, Delegate delegateMethod, FunctionObject functionValue)
			{
				this.Srm = srm;
				this.Context = context;
				this.Instance = instance;
				this.EventInfo = eventInfo;
				this.ActionMethod = delegateMethod;
				this.FunctionValue = functionValue;
			}

			public void DoEvent(object sender, object arg)
			{
				try
				{
					Srm.InvokeFunction(Context, Instance, FunctionValue, new object[] { arg });
				}
				catch (ReoScriptException ex)
				{
					Srm.RaiseScriptError(ex);
				}
			}
		}

		#endregion

		#region Work Mode
		private MachineWorkMode workMode = MachineWorkMode.Default;

		/// <summary>
		/// Get or set the working mode of script running machine
		/// </summary>
		public MachineWorkMode WorkMode
		{
			get { return workMode; }
			set
			{
				if (workMode != value)
				{
					workMode = value;
					if (WorkModeChanged != null)
					{
						WorkModeChanged(this, null);
					}
				}
			}
		}

		/// <summary>
		/// Event fired when work mode has been changed.
		/// </summary>
		public event EventHandler WorkModeChanged;

		/// <summary>
		/// Get or set whether allowed to access .NET object, type, namespace, etc. directly. (default is false)
		/// </summary>
		public bool AllowDirectAccess
		{
			get { return (workMode & MachineWorkMode.AllowDirectAccess) == MachineWorkMode.AllowDirectAccess; }
			set
			{
				if (value)
					workMode |= MachineWorkMode.AllowDirectAccess;
				else
					workMode &= ~(MachineWorkMode.AllowDirectAccess);
			}
		}

		/// <summary>
		/// Get or set whether allowed to ignore all exceptions that is happened from CLR calling. (default is true)
		/// </summary>
		public bool IgnoreCLRExceptions
		{
			get { return (workMode & MachineWorkMode.IgnoreCLRExceptions) == MachineWorkMode.IgnoreCLRExceptions; }
			set
			{
				if (value)
					workMode |= MachineWorkMode.IgnoreCLRExceptions;
				else
					workMode &= ~(MachineWorkMode.IgnoreCLRExceptions);
			}
		}

		/// <summary>
		/// Get or set whether allowed to auto-import the associated types that is used in other imported types. (default is true)
		/// </summary>
		public bool AutoImportRelationType
		{
			get { return (workMode & MachineWorkMode.AutoImportRelationType) == MachineWorkMode.AutoImportRelationType; }
			set
			{
				if (value)
					workMode |= MachineWorkMode.AutoImportRelationType;
				else
					workMode &= ~(MachineWorkMode.AutoImportRelationType);
			}
		}

		/// <summary>
		/// Get or set whether allowed to import .NET namespaces or classes using 'import' keyword from script. (default is fasle)
		/// </summary>
		public bool AllowImportTypeInScript
		{
			get { return (workMode & MachineWorkMode.AllowImportTypeInScript) == MachineWorkMode.AllowImportTypeInScript; }
			set
			{
				if (value)
					workMode |= MachineWorkMode.AllowImportTypeInScript;
				else
					workMode &= ~(MachineWorkMode.AllowImportTypeInScript);
			}
		}

		/// <summary>
		/// Get or set whether allowed to bind a CLR event. This option works only when AllowDirectAccess is enabled. (default is false)
		/// </summary>
		public bool AllowCLREvent
		{
			get { return (workMode & MachineWorkMode.AllowCLREventBind) == MachineWorkMode.AllowCLREventBind; }
			set
			{
				if (value)
					workMode |= MachineWorkMode.AllowCLREventBind;
				else
					workMode &= ~(MachineWorkMode.AllowCLREventBind);
			}
		}

		#endregion

		#region Invoke Function
		internal object InvokeAbstractFunction(object ownerObject, AbstractFunctionObject funObject, object[] args)
		{
			ScriptContext context = new ScriptContext(this, entryFunction);
			return InvokeFunction(context, ownerObject, funObject, args);
		}

		internal object InvokeFunction(ScriptContext context, object ownerObject, AbstractFunctionObject funObject, object[] args)
		{
			return InvokeFunction(context, ownerObject, funObject, args, 0, 0);
		}

		internal object InvokeFunction(ScriptContext context, object ownerObject, AbstractFunctionObject funObject, object[] args,
			int charIndex, int line)
		{
			if (funObject is NativeFunctionObject)
			{
				NativeFunctionObject nativeFun = funObject as NativeFunctionObject;
				return nativeFun.Invoke(context, ownerObject, args);
			}
			else if (funObject is FunctionObject)
			{
				FunctionObject fun = funObject as FunctionObject;

				CallScope lastScope = context.CallStack.Peek();
				if (lastScope != null)
				{
					lastScope.Line = line;
					lastScope.CharIndex = charIndex;
				}

				CallScope newScope = new CallScope(ownerObject, fun);

				// create inner functions from static local scope
				if (fun.FunctionInfo.InnerScope != null)
				{
					foreach (FunctionInfo fi in fun.FunctionInfo.InnerScope.Functions)
					{
						if (!fi.IsAnonymous)
						{
							Debug.Assert(!newScope.Variables.ContainsKey(fi.Name));
							FunctionObject innerFun = FunctionDefineNodeParser.CreateAndInitFunction(context, fi);
							// Hoisted inner functions lexically belong to the scope
							// being entered, so they capture newScope as their
							// closure environment.
							innerFun.CapturedScope = newScope;
							newScope[fi.Name] = innerFun;
						}
					}
				}

				// prepare arguments
				if (args != null && fun.Args != null)
				{
					for (int i = 0; i < fun.Args.Length && i < args.Length; i++)
					{
						string identifier = fun.Args[i];
						newScope[identifier] = args[i];
					}
				}

				// CapturedScope is now bound at function-creation time (in
				// AnonymousFunctionNodeParser, FunctionDefineNodeParser, and the
				// hoisting loop above), so the closure environment travels with
				// the FunctionObject and is independent of where it is later
				// called from. Nothing to set here, nothing to clear in finally.

				newScope[KEY___ARGS__] = args;

				context.PushCallStack(newScope, fun.FunctionInfo.IsInner || fun.FunctionInfo.IsAnonymous);

				ReturnNode returnValue = null;

				try
				{
					returnValue = ParseNode(fun.Body, context) as ReturnNode;
				}
				finally
				{
					context.PopCallStack();
				}

				return returnValue != null ? returnValue.Value : null;
			}
			else
				throw new ReoScriptRuntimeException(string.Format("{0} is not a function", Convert.ToString(funObject)));
		}

		/// <summary>
		/// Call a function in script specified by function name. 
		/// The function object must can be retrieved from current context or global object.
		/// 
		/// This method equals the evaluation of expression: if (typeof fun == 'function') fun();
		/// </summary>
		/// <param name="funName">function name</param>
		/// <param name="p">parameters if existed</param>
		/// <returns>return from function call</returns>
		public object InvokeFunctionIfExisted(string funName, params object[] p)
		{
			return InvokeFunctionIfExisted(GlobalObject, funName, p);
		}

		/// <summary>
		/// Call a function in script specified by function name.
		/// The function object must can be retrieved from speicifed owner object.
		/// 
		/// This method equals the evaluation of expression: if (obj != null && (typeof obj.fun == 'function')) obj.fun();
		/// </summary>
		/// <param name="owner">owner object (this object after be called)</param>
		/// <param name="funName">function object to execute</param>
		/// <param name="p">parameters if existed</param>
		/// <returns>return from function call</returns>
		public object InvokeFunctionIfExisted(object owner, string funName, params object[] p)
		{
			var ctx = new ScriptContext(this, entryFunction);

			AbstractFunctionObject fun = PropertyAccessHelper.GetProperty(
				ctx, owner, funName) as AbstractFunctionObject;

			return fun != null ? InvokeFunction(ctx, owner, fun, p) : null;
		}
		#endregion

		#region Execution Limits

		/// <summary>
		/// Maximum number of iterations allowed per loop (while/for).
		/// Set to 0 to disable the limit. Default is 10,000,000.
		/// </summary>
		public int MaxIterationsPerLoop { get; set; } = 10_000_000;

		#endregion

		#region Async Calling
		private bool isForceStop = false;

		/// <summary>
		/// Indicate whether the current execution is forced to stop. To force stop execution call ForceStop method.
		/// </summary>
		public bool IsForceStop { get { return isForceStop; } }

		/// <summary>
		/// Indicate whether script is running. To force stop current execution call ForceStop method.
		/// </summary>
		public bool IsRunning
		{
			get
			{
				return timeoutList.Count > 0;
				//return asyncCallThread != null;
				//return timeoutList.Count > 0;
			}
		}

		/// <summary>
		/// Force interrupt current execution.
		/// </summary>
		public void ForceStop()
		{
			isForceStop = true;

			//asyncCallerList.Clear();

			//if (asyncCallThread != null)
			//{
			//  asyncCallThread.Abort();
			//  asyncCallThread = null;
			//}

			lock (timeoutList)
			{
				for (int i = 0; i < timeoutList.Count; i++)
				{
					AsyncBackgroundWorker bw = timeoutList[i];

					try
					{
						if (bw != null)
						{
							timeoutList.Remove(bw);

							bw.CancelAsync();
							bw.Dispose();
							bw = null;
						}
					}
					catch { }
				}
			}
		}

		private List<AsyncBackgroundWorker> timeoutList = new List<AsyncBackgroundWorker>();

		private long asyncCallingCount = 0;

		internal long AsyncCall(object code, int ms, bool forever, object[] args)
		{
			if (IsForceStop) return 0;

			//AddAsyncCall(ms, () =>
			//{
			//  Thread.Sleep(ms);

			//  if (code is FunctionObject)
			//  {
			//    this.ParseNode(((FunctionObject)code).Body);
			//  }
			//  else if (code is SyntaxNode)
			//  {
			//    this.ParseNode(((SyntaxNode)code));
			//  }
			//  else
			//  {
			//    //FIXME: should merge Run and CalcExpression
			//    string codeStr = Convert.ToString(code).TrimEnd();
			//    if (codeStr.EndsWith(";") || codeStr.EndsWith("}"))
			//    {
			//      this.Run(Convert.ToString(codeStr));
			//    }
			//    else
			//    {
			//      this.CalcExpression(Convert.ToString(codeStr));
			//    }
			//  }

			//  return true;
			//});

			AsyncBackgroundWorker bw = new AsyncBackgroundWorker()
			{
				WorkerSupportsCancellation = true,
				ScriptContext = new ScriptContext(this, entryFunction),
			};

			bw.Id = ++asyncCallingCount;

			bw.DoWork += (s, e) =>
			{
				try
				{
					do
					{
						DateTime dt = DateTime.Now.AddMilliseconds(ms);

						while (DateTime.Now < dt)
						{
							if (isForceStop || bw.CancellationPending) break;
							Thread.Sleep(10);
						}

						if (isForceStop || bw.CancellationPending)
						{
							break;
						}
						else
						{
							if (code is FunctionObject)
							{
								this.InvokeFunction(bw.ScriptContext, GlobalObject, code as FunctionObject, args);
							}
							else if (code is SyntaxNode)
							{
								ScriptRunningMachine.ParseNode(((SyntaxNode)code), bw.ScriptContext);
							}
							else
							{
								this.CalcExpression(Convert.ToString(code));
							}
						}

						if (!forever)
						{
							lock (timeoutList)
							{
								timeoutList.Remove(bw);
							}
						}
					} while (forever);
				}
				catch (ReoScriptException ex)
				{
					RaiseScriptError(ex);
				}
			};

			lock (timeoutList)
			{
				timeoutList.Add(bw);
			}
			bw.RunWorkerAsync();

			return bw.Id;
		}

		class AsyncBackgroundWorker : BackgroundWorker
		{
			public long Id { get; set; }

			public ScriptContext ScriptContext { get; set; }
		}

		internal bool CancelAsyncCall(long id)
		{
			lock (timeoutList)
			{
				for (int i = 0; i < timeoutList.Count; i++)
				{
					if (timeoutList[i].Id == id)
					{
						timeoutList[i].CancelAsync();
						timeoutList.RemoveAt(i);
						return true;
					}
				}
			}

			return false;
		}

		private Thread asyncCallThread;

		private List<AsyncCaller> asyncCallerList = new List<AsyncCaller>();

		private int minAsyncCallInterval = 0;

		private void AddAsyncCall(int interval, Func<bool> caller)
		{
			if (minAsyncCallInterval == 0
				|| minAsyncCallInterval > interval)
			{
				minAsyncCallInterval = interval;
			}

			if (minAsyncCallInterval > 10) minAsyncCallInterval = 10;

			asyncCallerList.Add(new AsyncCaller(interval, caller));

			if (asyncCallThread == null)
			{
				asyncCallThread = new Thread(AsyncCallLoop);
				asyncCallThread.Start();
			}
		}

		private void AsyncCallLoop()
		{
			DateTime dt = DateTime.Now;

			while (asyncCallerList.Count > 0)
			{
				// should wait more than 0 ms
				Debug.Assert(minAsyncCallInterval > 0 && minAsyncCallInterval <= 100);

				Thread.Sleep(minAsyncCallInterval);

				for (int i = 0; i < asyncCallerList.Count; i++)
				{
					AsyncCaller caller = asyncCallerList[i];

					double check = (DateTime.Now - caller.LastCalled).TotalMilliseconds;
					Debug.WriteLine("check = " + check);

					if (check > caller.Interval)
					{
						try
						{
							caller.LastCalled = DateTime.Now;
							caller.DoCall();
						}
						catch
						{
							// error caused in this caller, planned to remove it 
							caller.Finished = true;
						}

						Debug.WriteLine((DateTime.Now - dt).Milliseconds);
						dt = DateTime.Now;
					}

					if (caller.Finished)
					{
						asyncCallerList.Remove(caller);
					}
				}
			}

			asyncCallThread = null;
		}

		internal class AsyncCaller
		{
			public AsyncCaller(int interval, Func<bool> caller)
			{
				this.interval = interval;
				this.lastCalled = DateTime.Now;
				this.action = caller;
				this.finished = false;
			}

			private DateTime lastCalled;

			public DateTime LastCalled
			{
				get { return lastCalled; }
				set { lastCalled = value; }
			}

			private int interval;

			public int Interval
			{
				get { return interval; }
				set { interval = value; }
			}

			private Func<bool> action;

			/// <summary>
			/// Do async calling.
			/// If return true this calling will be removed from timer loop.
			/// </summary>
			/// <returns>return true if calling is finished.</returns>
			public void DoCall()
			{
				if (action != null)
				{
					if (action()) finished = true;
				}
			}

			private bool finished;

			public bool Finished
			{
				get { return finished; }
				set { finished = value; }
			}
		}
		#endregion

		#region Standard I/O Interface

		private IStandardInputProvider standardInputProvider = new BuiltinConsoleInputProvider();

		public IStandardInputProvider StandardInputProvider
		{
			get { return standardInputProvider; }
			set { standardInputProvider = value; }
		}

		private List<IStandardOutputListener> outputListeners = new List<IStandardOutputListener>();

		/// <summary>
		/// List of Standard Output Listeners
		/// </summary>
		public List<IStandardOutputListener> OutputListeners
		{
			get { return outputListeners; }
			set { outputListeners = value; }
		}

		/// <summary>
		/// Add a lisenter to get standard output of console.
		/// </summary>
		/// <param name="lisenter">a lisenter to get standard output of console</param>
		public void AddStdOutputListener(IStandardOutputListener lisenter)
		{
			if (outputListeners == null) outputListeners = new List<IStandardOutputListener>();
			outputListeners.Add(lisenter);
		}

		/// <summary>
		/// Check whether specified listener has been added.
		/// </summary>
		/// <param name="listener">a lisenter to get standard output of console</param>
		/// <returns>true if specified listener has already added.</returns>
		public bool HasStdOutputListener(IStandardOutputListener listener)
		{
			return outputListeners == null ? false : outputListeners.Contains(listener);
		}

		/// <summary>
		/// Remove listener from list of lisenters.
		/// </summary>
		/// <param name="lisenter">a lisenter to get standard output of console</param>
		public void RemoveStdOutputListener(IStandardOutputListener lisenter)
		{
			if (outputListeners == null) return;
			outputListeners.Remove(lisenter);
		}

		internal void StandardIOWrite(byte[] buf, int index, int count)
		{
			if (outputListeners != null)
			{
				outputListeners.ForEach(ol => ol.Write(buf, index, count));
			}
		}

		internal void StandardIOWrite(object obj)
		{
			if (outputListeners != null)
			{
				outputListeners.ForEach(ol => ol.Write(obj));
			}
		}

		internal void StandardIOWriteLine(string line)
		{
			if (outputListeners != null)
			{
				outputListeners.ForEach(ol => ol.WriteLine(line));
			}
		}

		#endregion

		#region Load & Run
		/// <summary>
		/// Get or set WorkPath. WorkPath is used when importing an external script file or resource.
		/// </summary>
		public string WorkPath { get; set; }

		/// <summary>
		/// Load script file from specified stream. 
		/// </summary>
		/// <remarks>setTimeout and setInterval function will be disabled when loading script from external stream, uri or file.</remarks>
		/// <param name="s">stream to load script</param>
		public void Load(Stream s)
		{
			using (var reader = new StreamReader(s))
			{
				Load(reader.ReadToEnd(), null);
			}
		}

		/// <summary>
		/// Load script library from a specified uri. Uri can be remote resource on Internet.
		/// </summary>
		/// <param name="uri">uri to load script</param>
		public void Load(Uri uri)
		{
			using (WebClient c = new WebClient())
			{
				using (Stream stream = c.OpenRead(uri))
				{
					Load(stream);
				}
			}
		}

		/// <summary>
		/// Load script from specified file with full path.
		/// </summary>
		/// <param name="path">file to load script</param>
		public void Load(string path)
		{
			FileInfo fi = new FileInfo(path);
			if (!fi.Exists)
			{
				throw new ReoScriptException("File not found: " + fi.FullName);
			}

			Load(File.ReadAllText(path), path);
		}

		private void Load(string script, string filepath)
		{
			Run(script, filepath, true);
		}

		/// <summary>
		/// Run script from specified file and get the last value of execution.
		/// </summary>
		/// <param name="script">file to be executed</param>
		/// <returns>last result returned from script</returns>
		public object Run(FileInfo filePath)
		{
			return Run(filePath, true);
		}

		/// <summary>
		/// Run script from specified file and get the last value of execution.
		/// </summary>
		/// <param name="filePath">file to be executed</param>
		/// <param name="ignoreSyntaxErrors">indicates whether allowed to ignore syntax check. (default is true)</param>
		/// <returns>last result returned from script</returns>
		public object Run(FileInfo filePath, bool ignoreSyntaxErrors)
		{
			return Run(File.ReadAllText(filePath.FullName), filePath.FullName, ignoreSyntaxErrors);
		}

		/// <summary>
		/// Run specified script in text and get the last value of execution.
		/// </summary>
		/// <param name="script">script to be executed</param>
		/// <returns>result of last exected statement</returns>
		public object Run(string script)
		{
			return Run(script, true);
		}

		/// <summary>
		/// Run specified script in text and get the last value from script.
		/// </summary>
		/// <param name="script">script to be executed</param>
		/// <param name="context">current script context</param>
		/// <returns>result of last evaluated statement</returns>
		public object Run(string script, ScriptContext context)
		{
			return Run(script, true, context);
		}

		/// <summary>
		/// Run specified script in text and get the last value from script.
		/// </summary>
		/// <param name="script">script to be executed</param>
		/// <param name="ignoreSyntaxErrors">indicates whether allow to ignore syntax error. (default is true)</param>
		/// <returns>result of last evaluated statement</returns>
		public object Run(string script, bool ignoreSyntaxErrors)
		{
			return Run(script, null, true);
		}

		/// <summary>
		/// Run specified script in text and get the last value from script.
		/// </summary>
		/// <param name="script">script to be executed</param>
		/// <param name="ignoreSyntaxErrors">indicates whether allow to ignore syntax error. (default is true)</param>
		/// <param name="context">current script context</param>
		/// <returns>result of last evaluated statement</returns>
		public object Run(string script, bool ignoreSyntaxErrors, ScriptContext context)
		{
			return Run(script, null, true, context);
		}

		internal object Run(string script, string filePath, bool ignoreSyntaxErrors)
		{
			return Run(script, filePath, ignoreSyntaxErrors ? (Action<ErrorObject>)(e => { }) : null);
		}

		internal object Run(string script, string filePath, bool ignoreSyntaxErrors, ScriptContext context)
		{
			return Run(script, filePath, ignoreSyntaxErrors ? (Action<ErrorObject>)(e => { }) : null, context);
		}

		internal object Run(string script, string filePath, Action<ErrorObject> compilingErrorHandler)
		{
			ScriptContext context = new ScriptContext(this, entryFunction, filePath);
			return Run(script, filePath, compilingErrorHandler, context);
		}

		internal object Run(string script, string filePath, Action<ErrorObject> compilingErrorHandler, ScriptContext context)
		{
			return RunCompiledScript(Compile(script, compilingErrorHandler), context);
		}

		/// <summary>
		/// Run compiled script
		/// </summary>
		/// <param name="cs">compiled script to run</param>
		/// <returns>last return value from script</returns>
		//public object Run(CompiledScript cs)
		//{
		//  return RunCompiledScript(cs);
		//}

		/// <summary>
		/// Run compiled script. 
		/// </summary>
		/// <see cref="CompiledScript"/>
		/// <param name="script">a compiled script will be executed</param>
		/// <returns>return value from script</returns>
		public object RunCompiledScript(CompiledScript script)
		{
			return RunCompiledScript(script, new ScriptContext(this, entryFunction));
		}

		/// <summary>
		/// Run compiled script.
		/// </summary>
		/// <see cref="CompiledScript"/>
		/// <param name="script">a compiled script will be executed</param>
		/// <param name="context">current executing context</param>
		/// <returns>return value from script</returns>
		public object RunCompiledScript(CompiledScript script, ScriptContext context)
		{
			// clear ForceStop flag
			isForceStop = false;

			if (script.RootNode == null) return null;

			// define global functions 
			if (script.RootScope != null)
			{
				foreach (FunctionInfo fi in script.RootScope.Functions.Where(fi => !fi.IsAnonymous && !fi.IsInner))
				{
					GlobalObject[fi.Name] = FunctionDefineNodeParser.CreateAndInitFunction(context, fi);
				}
			}

			// run syntax tree and return value
			return UnboxAnything(ParseNode(script.RootNode, context));
		}

		/// <summary>
		/// JIT-compile and run the specified script. Parses the source,
		/// compiles the AST to IL via DynamicMethod, then executes.
		/// Unsupported AST nodes automatically fall back to tree-walking.
		/// </summary>
		public object JitRun(string script)
		{
			if (!script.EndsWith(";")) script += ";";

			var cs = Compile(script);
			return JitRun(cs);
		}

		/// <summary>
		/// JIT-compile and run a pre-compiled script (parsed AST).
		/// </summary>
		/// <param name="script">a compiled script to be JIT-executed</param>
		/// <returns>return value from script</returns>
		public object JitRun(CompiledScript script)
		{
			return JitRun(script, new ScriptContext(this, entryFunction));
		}

		/// <summary>
		/// JIT-compile and run a pre-compiled script with the specified context.
		/// </summary>
		/// <param name="script">a compiled script to be JIT-executed</param>
		/// <param name="context">current executing context</param>
		/// <returns>return value from script</returns>
		public object JitRun(CompiledScript script, ScriptContext context)
		{
			if (script == null || script.RootNode == null) return null;

			isForceStop = false;

			// define global functions (same as RunCompiledScript)
			if (script.RootScope != null)
			{
				foreach (var fi in script.RootScope.Functions.Where(fi => !fi.IsAnonymous && !fi.IsInner))
				{
					GlobalObject[fi.Name] = FunctionDefineNodeParser.CreateAndInitFunction(context, fi);
				}
			}

			var compiled = Compiler.JitCompiler.Compile(script.RootNode);
			return UnboxAnything(compiled(context));
		}

		/// <summary>
		/// Unbox anything from script return
		/// </summary>
		/// <param name="o">object returned from script</param>
		/// <returns>object unboxed</returns>
		public static object UnboxAnything(object o)
		{
			// retrieve value from accessors
			if (o is IAccess) o = UnboxAnything(((IAccess)o).Get());

			// retrieve value from ReturnNode
			if (o is ReturnNode) o = UnboxAnything(((ReturnNode)o).Value);

			// retrieve value from const values
			if (o is ConstValueNode) o = UnboxAnything(((ConstValueNode)o).ConstValue);

			// retrieve value from wrapped objects
			if (o is ArrayObject) o = UnboxAnything(((ArrayObject)o).List);
			if (o is StringObject) o = UnboxAnything(((StringObject)o).String);
			if (o is NumberObject) o = UnboxAnything(((NumberObject)o).Number);
			if (o is BooleanObject) o = UnboxAnything(((BooleanObject)o).Boolean);
			if (o is DateObject) o = UnboxAnything(((DateObject)o).DateTime);

			return o;
		}

		/// <summary>
		/// Evaluate specified expression. Only an expression can be properly 
		/// calculated by this method, control statements like if, for, switch, etc.
		/// can not be executed. To execute the statements use Run method.
		/// </summary>
		/// <param name="expression">expression to be calculated</param>
		/// <returns>value of expression</returns>
		public object CalcExpression(string expression)
		{
			return CalcExpression(expression, true);
		}

		/// <summary>
		/// Evaluate specified expression. Only an expression can be properly 
		/// calculated by this method, control statements like if, for, switch, etc.
		/// can not be executed. To execute the statements use Run method.
		/// </summary>
		/// <param name="expression">expression to be calculated</param>
		/// <param name="ignoreErrors">whether allowed to ignore syntax error</param>
		/// <returns>value of expression</returns>
		public object CalcExpression(string expression, bool ignoreErrors)
		{
			return CalcExpression(expression, new ScriptContext(this, entryFunction), ignoreErrors);
		}

		public object CalcExpression(string expression, ScriptContext context)
		{
			return CalcExpression(expression, context, false);
		}

		public object CalcExpression(string expression, ScriptContext context, bool ignoreErrors)
		{
			var parser = new ReoScriptHandwrittenParser();
			if (!ignoreErrors)
			{
				parser.CompilingErrorHandler = e => { throw new ReoScriptCompilingException(e); };
			}

			SyntaxNode t = parser.ParseExpression(expression);
			isForceStop = false;
			object v = ParseNode(t, context);
			while (v is IAccess) v = ((IAccess)v).Get();

			return v;
		}

		private FunctionDefineNodeParser globalFunctionDefineNodeParser = new FunctionDefineNodeParser();

		/// <summary>
		/// Pre-interpret specified script. Pre-interpreting parses the script and tires to construct a 
		/// syntax-tree in memory. Syntax errors will be detected and thrown as ReoScriptCompilingException.
		/// </summary>
		/// <param name="script">script to be compiled</param>
		/// <returns>compiled script instance in memory</returns>
		public CompiledScript Compile(string script)
		{
			return Compile(script, false);
		}

		/// <summary>
		/// Pre-interpret script from specified file. 
		/// Syntax errors will be detected and thrown as ReoScriptCompilingException.
		/// </summary>
		/// <param name="file">file to be compiled</param>
		/// <returns>compiled script instance in memory</returns>
		public CompiledScript Compile(FileInfo file)
		{
			return Compile(file, false);
		}

		/// <summary>
		/// Pre-interpret specified script in text. 
		/// Syntax errors will be detected and stored in the error list of CompiledScript.
		/// </summary>
		/// <param name="script">script to be compiled</param>
		/// <returns>compiled script instance in memory</returns>
		public CompiledScript Compile(string script, bool compilingWithoutException)
		{
			return Compile(script, compilingWithoutException ? (Action<ErrorObject>)((e) => { }) : null);
		}

		/// <summary>
		/// Pre-interpret script from specified file. 
		/// Syntax errors will be detected and stored in the error list of CompiledScript.
		/// </summary>
		/// <param name="script">specified script to be compiled</param>
		/// <returns>compiled script instance in memory</returns>
		public CompiledScript Compile(FileInfo file, bool compilingWithoutException)
		{
			return Compile(file, compilingWithoutException ? (Action<ErrorObject>)((e) => { }) : null);
		}

		/// <summary>
		/// Pre-interpret script from specified file. 
		/// Syntax errors will be detected and passed into CompilingErrorHandler instantly. 
		/// </summary>
		/// <param name="file">file specified to compile</param>
		/// <param name="compilingErrorHandler">error handler to receive a syntax error instantly</param>
		/// <returns>compiled script instance in memory</returns>
		public CompiledScript Compile(FileInfo file, Action<ErrorObject> compilingErrorHandler)
		{
			return Compile(File.ReadAllText(file.FullName), compilingErrorHandler);
		}

		/// <summary>
		/// Pre-interpret specified script in text.
		/// Syntax errors will be detected and passed into CompilingErrorHandler instantly.
		/// </summary>
		/// <param name="script">script specified to compile</param>
		/// <param name="compilingErrorHandler">error handler to receive syntax error</param>
		/// <returns>compiled script instance in memory</returns>
		public CompiledScript Compile(string script, Action<ErrorObject> compilingErrorHandler)
		{
			var parser = new ReoScriptHandwrittenParser();

			if (compilingErrorHandler == null)
			{
				compilingErrorHandler = e => { throw new ReoScriptCompilingException(e); };
			}

			parser.CompilingErrorHandler = compilingErrorHandler;

			// parse script and build AST
			SyntaxNode t = parser.ParseScript(script);

			return new CompiledScript
			{
				RootNode = t,
				CompilingErrors = parser.CompilingErrors,
				RootScope = parser.CurrentStack,
			};
		}

		internal static void IterateAST(SyntaxNode parent, Action<SyntaxNode> iterate)
		{
			if (parent != null && parent.ChildCount > 0)
			{
				foreach (SyntaxNode t in parent.Children)
				{
					iterate(t);
				}
			}
		}

		#endregion

		#region Node Parsing
		private IParserAdapter parserAdapter = new AWDLLogicSyntaxParserAdapter();

		internal IParserAdapter ParserAdapter
		{
			get { return parserAdapter; }
		}

		private static readonly AWDLLogicSyntaxParserAdapter parseAdapter = new AWDLLogicSyntaxParserAdapter();

		internal IParserAdapter SelectParserAdapter(IParserAdapter adapter)
		{
			IParserAdapter oldAdapter = this.parserAdapter;
			this.parserAdapter = adapter;
			return oldAdapter;
		}

		public static object ParseNode(SyntaxNode t, ScriptContext ctx)
		{
			if (t == null || ctx.Srm.isForceStop)
			{
				return null;
			}

			INodeParser parser = null;
			if ((parser = AWDLLogicSyntaxParserAdapter.definedParser[t.Type]) != null)
			{
				return parser.Parse(t, ctx.Srm, ctx);
			}
			else
			{
				switch (t.Type)
				{
					case NodeType.IDENTIFIER:
						{
							if (t.Text == ScriptRunningMachine.GLOBAL_VARIABLE_NAME)
								return ctx.GlobalObject;
							else
							{
								return ctx[t.Text];
							}
							//return new VariableAccess(ctx.Srm, ctx, t.Text);
						}

					case NodeType.THIS:
						return ctx.ThisObject;

					case NodeType.CONST_VALUE:
						{
							SyntaxNode child = (SyntaxNode)t.Children[0];

							switch (child.Type)
							{
								case NodeType.CONST_VALUE:
									return ((ConstValueNode)child).ConstValue;

								case NodeType.LIT_TRUE:
									return true;

								case NodeType.LIT_FALSE:
									return false;

								case NodeType.LIT_NULL:
								case NodeType.UNDEFINED:
									return null;
							}
						}
						break;

					//case NodeType.NUMBER_LITERATE:
					//  return Convert.ToDouble(t.Text);

					//case NodeType.HEX_LITERATE:
					//  return (double)Convert.ToInt32(t.Text.Substring(2), 16);

					//case NodeType.BINARY_LITERATE:
					//  return (double)Convert.ToInt32(t.Text.Substring(2), 2);

					//case NodeType.STRING_LITERATE:
					//  string str = t.ToString();
					//  str = str.Substring(1, str.Length - 2);
					//  return ConvertEscapeLiterals(str);


					case NodeType.OBJECT_LITERAL:
						if (t.ChildCount % 2 != 0)
							throw ctx.CreateRuntimeError(t, "object literal should be key/value paired.");

						ObjectValue val = ctx.CreateNewObject();

						for (int i = 0; i < t.ChildCount; i += 2)
						{
							object value = ParseNode((SyntaxNode)t.Children[i + 1], ctx);
							if (value is IAccess) value = ((IAccess)value).Get();

							string identifier = t.Children[i].ToString();
							if (t.Children[i].Type == NodeType.STRING_LITERATE)
								identifier = identifier.Substring(1, identifier.Length - 2);

							val[identifier] = value;
						}

						return val;

					case NodeType.ARRAY_LITERAL:
						ArrayObject arr = ctx.CreateNewObject(ctx.Srm.BuiltinConstructors.ArrayFunction) as ArrayObject;

						if (arr == null) return arr;

						for (int i = 0; i < t.ChildCount; i++)
						{
							object value = ParseNode((SyntaxNode)t.Children[i], ctx);
							if (value is IAccess) value = ((IAccess)value).Get();
							arr.List.Add(value);
						}
						return arr;

					case NodeType.REPLACED_TREE:
						return ((ReplacedSyntaxNode)t).Object;

					case NodeType.NAN:
						return NaNValue.Value;

					case NodeType.BREAK:
						return BreakNode.Value;

					case NodeType.CONTINUE:
						return ContinueNode.Value;
				}

				return ParseChildNodes(t, ctx);
			}
		}
		public static object ParseChildNodes(SyntaxNode t, ScriptContext ctx)
		{
			object childValue = null;
			if (t.ChildCount > 0)
			{
				//foreach (SyntaxNode child in t.Children)
				for (int i = 0; i < t.ChildCount; i++)
				{
					childValue = ParseNode((SyntaxNode)t.Children[i], ctx);

					if (childValue is BreakNode || childValue is ContinueNode || childValue is ReturnNode)
						return childValue;
				}
			}
			return childValue;
		}
		#endregion

		#region Utility
		internal static string ConvertEscapeLiterals(string instr)
		{
			return instr.Replace("\\n", new string(new char[] { '\n' }))
				.Replace("\\t", new string(new char[] { '\t' }))
				.Replace("\\\\", new string(new char[] { '\\' }));
		}

		internal object[] GetParameterList(SyntaxNode paramsTree, ScriptContext ctx)
		{
			int argCount = paramsTree == null ? 0 : paramsTree.ChildCount;
			object[] args = new object[argCount];

			if (argCount > 0)
			{
				for (int i = 0; i < argCount; i++)
				{
					object val = ParseNode((SyntaxNode)paramsTree.Children[i], ctx);
					if (val is IAccess) val = ((IAccess)val).Get();

					args[i] = val;
				}
			}

			return args;
		}

		#region Get primitive data
		/// <summary>
		/// Convert object into integer value.
		/// </summary>
		/// <param name="obj">object to be converted</param>
		/// <returns>converted integer value</returns>
		public static int GetIntValue(object obj)
		{
			return GetIntValue(obj, 0);
		}

		/// <summary>
		/// Convert object into integer value from argument array.
		/// </summary>
		/// <param name="args">argument array to get integer value</param>
		/// <param name="index">zero-based index to get integer value from argument array</param>
		/// <returns>converted integer value</returns>
		public static int GetIntParam(object[] args, int index)
		{
			return GetIntParam(args, index, 0);
		}

		/// <summary>
		/// Convert object into integer value from argument array.
		/// </summary>
		/// <param name="args">argument array to get integer value</param>
		/// <param name="index">zero-based index to get integer value from argument array</param>
		/// <param name="def">default value will be return if converting is failed</param>
		/// <returns>converted integer value</returns>
		public static int GetIntParam(object[] args, int index, int def)
		{
			if (args.Length <= index)
				return def;
			else
				return GetIntValue(args[index], def);
		}

		/// <summary>
		/// Convert object into integer value. If converting is failed, a specified default value will be returned.
		/// </summary>
		/// <param name="obj">object to be converted</param>
		/// <param name="def">default value will be return if converting is failed</param>
		/// <returns>converted integer value</returns>
		public static int GetIntValue(object obj, int def)
		{
			if (obj is int || obj is long)
			{
				return (int)obj;
			}
			else if (obj is double || ScriptRunningMachine.IsPrimitiveNumber(obj))
			{
				return (int)(double)obj;
			}
			else if (obj is string || obj is StringObject)
			{
				double v = def;
				double.TryParse(Convert.ToString(obj), out v);
				return (int)v;
			}
			else if (obj is NumberObject)
			{
				return (int)((NumberObject)obj).Number;
			}
			else
				return def;
		}

		/// <summary>
		/// Convert object into long integer value.
		/// </summary>
		/// <param name="obj">object to be converted</param>
		/// <returns>converted long integer value</returns>
		public static long GetLongValue(object obj)
		{
			return GetLongValue(obj, 0);
		}

		/// <summary>
		/// Convert object into long integer value from argument array.
		/// </summary>
		/// <param name="args">argument array to get long integer value</param>
		/// <param name="index">zero-based index to get long integer value from argument array</param>
		/// <param name="def">default value will be return if converting is failed</param>
		/// <returns>converted long integer value</returns>
		public static long GetLongParam(object[] args, int index, long def)
		{
			if (args.Length <= index)
				return def;
			else
				return GetLongValue(args[index], def);
		}

		/// <summary>
		/// Convert object into long integer value. If converting is failed, 
		/// a specified default value will be returned.
		/// </summary>
		/// <param name="obj">object to be converted</param>
		/// <param name="def">default value will be return if converting is failed</param>
		/// <returns>converted long integer value</returns>
		public static long GetLongValue(object obj, long def)
		{
			if (obj is int || obj is long)
			{
				return (long)obj;
			}
			else if (obj is double || ScriptRunningMachine.IsPrimitiveNumber(obj))
			{
				return (long)(double)obj;
			}
			else if (obj is string || obj is StringObject)
			{
				double v = def;
				double.TryParse(Convert.ToString(obj), out v);
				return (long)v;
			}
			else if (obj is NumberObject)
			{
				return (long)((NumberObject)obj).Number;
			}
			else
				return def;
		}

		public static float GetFloatValue(object obj)
		{
			return GetFloatValue(obj, 0);
		}

		/// <summary>
		/// Convert object into float value. If converting is failed, a specified default value will be returned.
		/// </summary>
		/// <param name="obj">object to be converted</param>
		/// <param name="def">default value will be return if converting is failed</param>
		/// <returns>converted float value</returns>
		public static float GetFloatValue(object obj, float def)
		{
			if (obj is double)
			{
				return (float)(double)obj;
			}
			else if (obj is float)
			{
				return (float)obj;
			}
			else if (ScriptRunningMachine.IsPrimitiveNumber(obj))
			{
				return (float)(double)obj;
			}
			else if (obj is string || obj is StringObject)
			{
				double v = def;
				double.TryParse(Convert.ToString(obj), out v);
				return (float)v;
			}
			else if (obj is NumberObject)
			{
				return (float)((NumberObject)obj).Number;
			}
			else
				return def;
		}

		/// <summary>
		/// Convert object into float value from argument array.
		/// </summary>
		/// <param name="args">argument array to get float value</param>
		/// <param name="index">zero-based index to get float value from argument array</param>
		/// <param name="def">default value will be return if converting is failed</param>
		/// <returns>converted float value</returns>
		public static float GetFloatParam(object[] args, int index, float def)
		{
			if (args.Length <= index)
				return def;
			else
				return GetFloatValue(args[index], def);
		}

		public static double GetNumberValue(object obj)
		{
			return GetNumberValue(obj, 0);
		}

		/// <summary>
		/// Convert object into double value. If converting is failed, a specified default value will be returned.
		/// </summary>
		/// <param name="obj">object to be converted</param>
		/// <param name="def">default value will be return if converting is failed</param>
		/// <returns>converted double value</returns>
		public static double GetNumberValue(object obj, double def)
		{
			if (obj is double)
			{
				return (double)obj;
			}
			else if (ScriptRunningMachine.IsPrimitiveNumber(obj))
			{
				return Convert.ToDouble(obj);
			}

			else if (obj is string || obj is StringObject)
			{
				double v = def;
				double.TryParse(Convert.ToString(obj), out v);
				return v;
			}
			else if (obj is NumberObject)
			{
				return ((NumberObject)obj).Number;
			}
			else
				return def;
		}

		public static bool TryGetNumberValue(object obj, out double value)
		{
			if (obj is double doubleVal)
			{
				value = doubleVal;
				return true;
			}
			else if (IsPrimitiveNumber(obj))
			{
				value = Convert.ToDouble(obj);
				return true;
			}
			else if (IsPrimitiveString(obj))
			{
				return double.TryParse(ConvertToString(obj), out value);
			}
			else if (obj is NumberObject numObj)
			{
				value = numObj.Number;
				return true;
			}

			value = 0;
			return false;
		}

		/// <summary>
		/// Convert object into boolean value using truthy/falsy semantics.
		/// Falsy: null, false, 0, NaN, empty string.
		/// Truthy: everything else (non-zero numbers, non-empty strings, objects).
		/// </summary>
		/// <param name="obj">object to be converted</param>
		/// <returns>converted boolean value</returns>
		public static bool GetBoolValue(object obj)
		{
			if (obj == null) return false;
			if (obj is bool) return (bool)obj;
			if (obj is BooleanObject) return ((BooleanObject)obj).Boolean;
			if (obj == NaNValue.Value) return false;
			if (IsPrimitiveNumber(obj)) return GetNumberValue(obj) != 0;
			if (obj is NumberObject) return ((NumberObject)obj).Number != 0;
			if (obj is string) return ((string)obj).Length > 0;
			if (obj is StringObject) return ((StringObject)obj).String.Length > 0;
			// All objects (ObjectValue, arrays, functions, etc.) are truthy
			return true;
		}

		/// <summary>
		/// Convert an object into string.
		/// </summary>
		/// <param name="v">object will be converted</param>
		/// <returns>converted string</returns>
		public static string ConvertToString(object v)
		{
			if (v == null)
			{
				return "null";
			}
			else if (v is bool)
			{
				return ((bool)v) ? "true" : "false";
			}
			else
			{
				return Convert.ToString(v);
			}
		}
		#endregion

		internal static string GetNativeIdentifier(string identifier)
		{
			return string.IsNullOrEmpty(identifier) ? string.Empty :
				(identifier.Substring(0, 1).ToUpper() + identifier.Substring(1));
		}

		internal object ConvertToCLRType(ScriptContext context, object value, Type type)
		{
			if (type == typeof(string))
			{
				return Convert.ToString(value);
			}
			else if (type == typeof(int))
			{
				if (value is double)
				{
					return (int)((double)value);
				}
				else
				{
					return Convert.ToInt32(value);
				}
			}
			else if (type == typeof(long))
			{
				if (value is double)
				{
					return (long)((double)value);
				}
				else
					return Convert.ToInt64(value);
			}
			else if (type == typeof(float))
			{
				if (value is double)
				{
					return (float)(double)value;
				}
				else
					return Convert.ToSingle(value);
			}
			else if (value is ObjectValue)
			{
				if (type == typeof(ObjectValue))
				{
					return value;
				}
				else if (AllowDirectAccess)
				{
					object obj;

					if (type.IsArray && value is ArrayObject)
					{
						ArrayObject arrSource = (ArrayObject)value;
						int count = arrSource.List.Count;

						object[] arrTo = Array.CreateInstance(type.GetElementType(), count) as object[];

						for (int i = 0; i < count; i++)
						{
							arrTo[i] = ConvertToCLRType(context, arrSource.List[i], type.GetElementType());
						}

						obj = arrTo;
					}
					else
					{
						try
						{
							if (type.IsEnum && value is string || value is NumberObject)
							{
								obj = Enum.Parse(type, Convert.ToString(value));
							}
							else
							{
								obj = System.Activator.CreateInstance(type);
							}
						}
						catch (Exception ex)
						{
							throw new ReoScriptException("cannot convert to .NET object from value: " + value, ex);
						}

						CombineObject(context, obj, (ObjectValue)value);
					}
					return obj;
				}
				else
					return value;
			}
			else
			{
				return value;
			}
		}

		/// <summary>
		/// Check whether a specified object is number.
		/// </summary>
		/// <param name="target">object will be checked.</param>
		/// <returns>true if specified object is of number type.</returns>
		public static bool IsPrimitiveNumber(object target)
		{
			return target is double || target is int || target is float || target is char || target is byte
				|| target is short || target is long;
		}

		/// <summary>
		/// Check whether a specified object is string.
		/// </summary>
		/// <param name="target">object will be checked.</param>
		/// <returns>true if specified object is instance of <class>StringObject</class>, <class>string</class> or <class>StringBuilder</class>.</returns>
		public static bool IsPrimitiveString(object target)
		{
			return target is string || target is StringObject || target is StringBuilder;
		}

		internal static bool IsPrimitiveTypes(object obj)
		{
			return obj == null || obj is bool || obj is string || IsPrimitiveNumber(obj);
		}

		internal static MethodInfo FindCLRMethodAmbiguous(object obj, string methodName, object[] args)
		{
			var q = obj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(_q => _q.Name == methodName);

			//MethodInfo method = null;

			foreach (MethodInfo mi in q)
			{
				ParameterInfo[] pi = mi.GetParameters();

				if (pi.Length == args.Length)
				{
					return mi;
				}
				//else
				//{
				//  for (int i = 0; i < args.Length && i < pi.Length; i++)
				//  {
				//    if (pi[i].ParameterType == typeof(string))
				//    {
				//      if(
				//    }
				//  }

				//  // compare every parameters
				//}

			}

			return q == null || q.Count() == 0 ? null : q.First();
		}

		#region JSON Converation
		/// <summary>
		/// Convert an object into JSON string
		/// </summary>
		/// <param name="obj">object will be converted</param>
		/// <param name="handler">handler to process every properties. This parameter can be null.</param>
		/// <returns>converted JSON string</returns>
		public static string ConvertToJSONString(object obj, Func<string, object, object> handler)
		{
			return ConvertToJSONString(obj, handler, true);
		}

		internal static string ConvertToJSONString(object obj, Func<string, object, object> handler, bool allowDotNetObjects)
		{
			StringBuilder sb = new StringBuilder("{");

			if (obj is ISyntaxTreeReturn)
			{
				if (obj is ObjectValue)
				{
					ObjectValue objVal = (ObjectValue)obj;
					foreach (string key in objVal)
					{
						if (sb.Length > 1) sb.Append(',');
						ConvertPropertyToJSONString(sb, key, objVal[key], handler);
					}
				}
			}
			else if (obj is IDictionary<string, object>)
			{
				IDictionary<string, object> objVal = (IDictionary<string, object>)obj;
				foreach (string key in objVal.Keys)
				{
					if (sb.Length > 1) sb.Append(',');
					ConvertPropertyToJSONString(sb, key, objVal[key], handler);
				}
			}
			else if (obj is KeyValuePair<string, object>)
			{
				KeyValuePair<string, object> objVal = (KeyValuePair<string, object>)obj;
				ConvertPropertyToJSONString(sb, objVal.Key, objVal.Value, handler);
			}
			else if (allowDotNetObjects)
			{
				PropertyInfo[] pis = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
				foreach (PropertyInfo pi in pis)
				{
					if (sb.Length > 1) sb.Append(',');
					ConvertPropertyToJSONString(sb, pi.Name, pi.GetValue(obj, null), handler);
				}
			}

			sb.Append('}');
			return sb.ToString();
		}

		private static void ConvertPropertyToJSONString(StringBuilder sb, string key, object value, Func<string, object, object> handler)
		{
			sb.Append(key);
			sb.Append(':');

			if (handler != null) value = handler(key, value);

			if (value == null)
			{
				sb.Append("null");
			}
			else if (value is string || value is StringObject)
			{
				string str = Convert.ToString(value);
				str = str.IndexOf('\"') < 0 ? ("\"" + str + "\"") : ("'" + str + "'");
				sb.Append(str);
			}
			else
			{
				sb.Append(Convert.ToString(value));
			}
		}
		#endregion

		#endregion

		#region Events
		internal event EventHandler<ReoScriptObjectEventArgs> NewObjectCreated;
		//internal event EventHandler<ReoScriptObjectEventArgs> PropertyDeleted;

		/// <summary>
		/// Event fired when script running machine is resetted.
		/// </summary>
		public event EventHandler Resetted;

		/// <summary>
		/// Event fired when a script error occurs during event handler execution
		/// or other contexts where exceptions cannot propagate to the caller.
		/// Subscribe to this event to log or display script errors in the host application.
		/// </summary>
		public event EventHandler<ScriptErrorEventArgs> ScriptError;

		internal void RaiseScriptError(ReoScriptException ex)
		{
			ScriptError?.Invoke(this, new ScriptErrorEventArgs(ex));
		}
		#endregion

		#region Namespace & Class & CodeFile
		private readonly Dictionary<string, AbstractFunctionObject> classDefines
			= new Dictionary<string, AbstractFunctionObject>();

		/// <summary>
		/// Check whether specified class has been registered into script context.
		/// </summary>
		/// <param name="name">class name to be checked</param>
		/// <returns>true if specified class has been registered</returns>
		public bool HasClassRegistered(string name)
		{
			return classDefines.ContainsKey(name);
		}

		internal AbstractFunctionObject GetClass(string name)
		{
			if (classDefines.ContainsKey(name))
				return classDefines[name];
			if (this.GlobalObject[name] is AbstractFunctionObject)
				return this[name] as AbstractFunctionObject;
			else
				return null;
		}

		/// <summary>
		/// Register a class into script running machine.
		/// </summary>
		/// <param name="constructor">constructor of class to be registered</param>
		public void RegisterClass(AbstractFunctionObject constructor)
		{
			RegisterClass(constructor, constructor.FunName);
		}

		/// <summary>
		/// Register a class into script running machine.
		/// </summary>
		/// <param name="constructor">constructor of class to be registered</param>
		/// <param name="name"></param>
		public void RegisterClass(AbstractFunctionObject constructor, string name)
		{
			if (string.IsNullOrEmpty(name))
				name = constructor.FunName;

			if (string.IsNullOrEmpty(name))
			{
				throw new ReoScriptException("Class should has a name to register: " + constructor.ToString());
			}

			if (classDefines.ContainsKey(name))
			{
				throw new ReoScriptException(string.Format("Class named '{0}' has been defined.", name));
			}

			// if object is function, prepare its prototype 
			if (constructor[KEY_PROTOTYPE] == null)
			{
				constructor[KEY_PROTOTYPE] = constructor.CreatePrototype(new ScriptContext(this, entryFunction));
			}

			constructor.FunName = name;
			classDefines[name] = constructor;
		}

		private static readonly List<string> importedCodeFiles = new List<string>();

		internal void ImportCodeFile(string fullPath)
		{
			if (!importedCodeFiles.Contains(fullPath))
			{
				importedCodeFiles.Add(fullPath);

				Load(fullPath);
			}
		}

		private readonly Dictionary<string, ObjectValue> moduleCache = new Dictionary<string, ObjectValue>();

		/// <summary>
		/// Import a script file as an isolated module. The file is compiled and
		/// executed in a separate scope; top-level functions and variables defined
		/// in the file become properties of the returned module object. Results
		/// are cached so each file is executed at most once.
		/// </summary>
		/// <param name="fullPath">Absolute path to the script file</param>
		/// <returns>Module object containing the file's exported definitions</returns>
		public ObjectValue ImportModuleFile(string fullPath)
		{
			if (moduleCache.TryGetValue(fullPath, out ObjectValue cached))
			{
				return cached;
			}

			FileInfo fi = new FileInfo(fullPath);
			if (!fi.Exists)
			{
				throw new ReoScriptException("Module file not found: " + fi.FullName);
			}

			// Compile the module source
			string moduleSource = File.ReadAllText(fullPath);
			CompiledScript script = Compile(moduleSource, e => { });
			if (script == null || script.RootNode == null)
			{
				throw new ReoScriptException("Failed to compile module: " + fi.FullName);
			}

			// Create an isolated module scope: a fresh ObjectValue acts as the
			// module's "global" so that its definitions don't leak into the real
			// global scope.
			ObjectValue moduleGlobal = new ObjectValue();

			// Create a context whose GlobalObject points to the module scope
			ScriptContext moduleCtx = new ScriptContext(this, entryFunction, fullPath);
			moduleCtx.GlobalObject = moduleGlobal;

			// Hoist top-level function definitions into the module scope
			if (script.RootScope != null)
			{
				foreach (FunctionInfo funcInfo in script.RootScope.Functions.Where(f => !f.IsAnonymous && !f.IsInner))
				{
					moduleGlobal[funcInfo.Name] = FunctionDefineNodeParser.CreateAndInitFunction(moduleCtx, funcInfo);
				}
			}

			// Execute the module body (variables land in moduleGlobal)
			ParseNode(script.RootNode, moduleCtx);

			// Build a CallScope containing all module-level definitions so that
			// module functions can resolve their free variables through the
			// CapturedScope chain when called from an external context.
			CallScope moduleScope = new CallScope(moduleGlobal, entryFunction);
			foreach (string key in moduleGlobal)
			{
				moduleScope[key] = moduleGlobal[key];
			}

			// Bind every FunctionObject defined in the module to this scope
			foreach (string key in moduleGlobal)
			{
				if (moduleGlobal[key] is FunctionObject fun)
				{
					fun.CapturedScope = moduleScope;
				}
			}

			// Cache and return
			moduleCache[fullPath] = moduleGlobal;
			return moduleGlobal;
		}

		#endregion

		//public void Test()
		//{
		//	string script = @"c = 10; function xx(a,b,c){ return {aa:'bb',result:a+b+c}} arr=[1,2,3]; xx(10,11,12) ";
		//	ReoScriptLexer l = new ReoScriptLexer(new ANTLRStringStream(script));
		//	CommonTokenStream t = new CommonTokenStream(l);
		//	ReoScriptParser p = new ReoScriptParser(t);

		//	SyntaxNode tree = p.script().Tree;


		//}
	}


	namespace Diagnostics
	{
		/// <summary>
		/// Provides ability to debug script
		/// </summary>
		public sealed class ScriptDebugger
		{
			public static readonly string DEBUG_OBJECT_NAME = "debug";

			private ObjectValue debugObject;

			/// <summary>
			/// Debug object named 'debug' will be added into script context
			/// </summary>
			public ObjectValue DebugObject { get { return debugObject; } }

			/// <summary>
			/// Current SRM instance to be monitored
			/// </summary>
			public ScriptRunningMachine Srm { get; set; }

			/// <summary>
			/// Current context used to script executing
			/// </summary>
			public ScriptContext Context { get; set; }

			private int totalObjectCreated = 0;

			/// <summary>
			/// Total counter of object created in script
			/// </summary>
			public int TotalObjectCreated
			{
				get { return totalObjectCreated; }
				set { totalObjectCreated = value; }
			}

			ExprEqualsNodeParser comparer = new ExprEqualsNodeParser();

			/// <summary>
			/// Construct debugger to monitor specified script running machine 
			/// </summary>
			/// <param name="srm">instance of script running machine</param>
			public ScriptDebugger(ScriptRunningMachine srm)
			{
				this.Srm = srm;
				this.Context = new ScriptContext(srm, ScriptRunningMachine.entryFunction);

				srm.NewObjectCreated += new EventHandler<ReoScriptObjectEventArgs>(srm_NewObjectCreated);
				srm.Resetted += (s, e) => RestoreDebugger();

				debugObject = srm.CreateNewObject(Context) as ObjectValue;

				if (DebugObject != null)
				{
					DebugObject["assert"] = new NativeFunctionObject("assert", (ctx, owner, args) =>
					{
						if (args.Length == 0)
						{
							throw new ReoScriptAssertionException("assertion failed.");
						}
						else if (args.Length == 1)
						{
							if (!ScriptRunningMachine.GetBoolValue(args[0]))
							{
								throw new ReoScriptAssertionException("assertion failed.");
							}
						}
						else if (args.Length == 2)
						{
							if (!comparer.Compare(args[0], args[1], srm))
							{
								throw new ReoScriptAssertionException(string.Format("expect '{0}' but '{1}'",
									ScriptRunningMachine.ConvertToString(args[1]),
									ScriptRunningMachine.ConvertToString(args[0])));
							}
						}
						else if (args.Length == 3)
						{
							if (!comparer.Compare(args[0], args[1], srm))
							{
								throw new ReoScriptAssertionException(ScriptRunningMachine.ConvertToString(args[2]));
							}
						}
						return null;
					});

					DebugObject["total_created_objects"] = new ExternalProperty(() => totalObjectCreated, null);
				}

				RestoreDebugger();
			}

			/// <summary>
			/// Restore a debugger after monitoring SRM resetting
			/// </summary>
			public void RestoreDebugger()
			{
				if (DebugObject != null)
				{
					Srm[DEBUG_OBJECT_NAME] = DebugObject;
				}

				// debug script library
				using (Stream ms = typeof(ScriptRunningMachine).Assembly
					.GetManifestResourceStream("unvell.ReoScript.scripts.debug.reo"))
				{
					if (ms != null) Srm.Load(ms);
				}
			}

			void srm_NewObjectCreated(object sender, ReoScriptObjectEventArgs e)
			{
				totalObjectCreated++;
			}

			private List<Breakpoint> breakpoints = new List<Breakpoint>();

			/// <summary>
			/// Add a breakpoint at specified position. (NOT AVAILABLE YET!)
			/// </summary>
			/// <param name="breakpoint"></param>
			public void AddBreakpoint(Breakpoint breakpoint)
			{
				this.breakpoints.Add(breakpoint);
			}
		}

		/// <summary>
		/// Breakpoint to add at specified position.
		/// </summary>
		public class Breakpoint
		{
			public string FilePath { get; set; }
			public int Line { get; set; }
			public int Index { get; set; }
			public int Length { get; set; }
			public string TestCode { get; set; }
		}

	}

	#endregion


	namespace Runtime
	{
		public static class RSRuntimeInterface
		{
			public static object GetProperty(ScriptContext ctx, object owner, string identifier)
			{
				return PropertyAccessHelper.GetProperty(ctx, owner, identifier);
			}
			public static object SetProperty(ScriptContext ctx, object owner, string identifier, object value)
			{
				PropertyAccessHelper.SetProperty(ctx, owner, identifier, value);
				return value;
			}
			public static object GetThisObject(ScriptContext ctx)
			{
				return ctx.ThisObject;
			}
			public static object Plus(ScriptContext ctx, object left, object right)
			{
				return ((ExprPlusNodeParser)AWDLLogicSyntaxParserAdapter.definedParser[NodeType.PLUS]).Calc(left, right, ctx.Srm, ctx);
			}
			public static bool LessThan(ScriptContext ctx, object left, object right)
			{
				return ((ExprLessThanNodeParser)AWDLLogicSyntaxParserAdapter.definedParser[NodeType.LESS_THAN]).Compare(left, right, ctx.Srm);
			}
			public static bool LessEquals(ScriptContext ctx, object left, object right)
			{
				return ((ExprLessOrEqualsNodeParser)AWDLLogicSyntaxParserAdapter.definedParser[NodeType.LESS_EQUALS]).Compare(left, right, ctx.Srm);
			}
			public static object CallFunction(ScriptContext ctx, object owner, object funObj, object[] args)
			{
				AbstractFunctionObject fun = funObj as AbstractFunctionObject;

				if (fun == null)
				{
					// FIXME: throw an error info object
					throw new ReoScriptRuntimeException("Object is not of function.");
				}

				if (owner == null) owner = ctx.ThisObject;

				return ctx.Srm.InvokeAbstractFunction(owner, fun, args);
			}
		}

		public class RuntimeContext
		{

		}
	}
}
