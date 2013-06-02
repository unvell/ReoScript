
namespace Unvell.ReoScript.TestCase
{
	[TestSuite]
	class ReoScriptTestSuite
	{
		public ScriptRunningMachine SRM { get; set; }
	}

	class Friut : ObjectValue {
		public new string Name { get; set; }
		public string Color { get; set; }
		public float Price { get; set; }
	}

	[TestSuite("DirectAccess")]
	class DirectAccessTests : ReoScriptTestSuite
	{
		[TestCase("Import and Instance")]
		public void ImportAndCreate()
		{
			SRM.ImportType(typeof(Friut));

			SRM.Run(@"

var t = debug.assert;

var apple = new Friut();

t(typeof apple == 'object');
t(apple instanceof Friut);

");
		}

		[TestCase("Import and Instance (Alias)")]
		public void ImportAndCreate2()
		{
			// import using alias
			SRM.ImportType(typeof(Friut), "MyClass");

			SRM.Run(@"

var t = debug.assert;

var apple = new MyClass();

t(typeof apple == 'object');
t(apple instanceof MyClass);

");
		}

		[TestCase("TestCase Template")]
		public void TestCaseTemplate()
		{
		}
	}
}