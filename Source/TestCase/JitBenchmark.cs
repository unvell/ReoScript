/*****************************************************************************
 *
 * ReoScript - .NET Script Language Engine
 *
 * JIT compiler benchmark — compares tree-walking vs JIT execution.
 *
 ****************************************************************************/

using System;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using unvell.ReoScript;
using unvell.ReoScript.Diagnostics;

namespace unvell.ReoScript.TestCase
{
	public class JitBenchmark
	{
		private readonly ITestOutputHelper output;

		public JitBenchmark(ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void SimpleAssignment_Correctness()
		{
			var srm = new ScriptRunningMachine();
			new ScriptDebugger(srm);

			string script = "var a = 42;";

			object treeResult = srm.Run(script);
			srm.Reset();
			object jitResult = srm.JitRun(script);

			output.WriteLine("Tree-walking result: {0}", treeResult);
			output.WriteLine("JIT result:          {0}", jitResult);
		}

		[Fact]
		public void ForLoop_Correctness()
		{
			var srm = new ScriptRunningMachine();
			new ScriptDebugger(srm);

			string script = @"
				var sum = 0;
				for (var i = 0; i < 100; i++) {
					sum = sum + i;
				}
				sum;
			";

			object treeResult = srm.Run(script);
			srm.Reset();
			object jitResult = srm.JitRun(script);

			output.WriteLine("Tree-walking result: {0}", treeResult);
			output.WriteLine("JIT result:          {0}", jitResult);

			Assert.Equal(
				ScriptRunningMachine.GetNumberValue(treeResult),
				ScriptRunningMachine.GetNumberValue(jitResult));
		}

		[Fact]
		public void ForLoop_Benchmark()
		{
			string script = "for(var i=0;i<100000;i++)i=i;";

			var srm = new ScriptRunningMachine();
			new ScriptDebugger(srm);

			// Warmup
			srm.Run(script);
			srm.Reset();
			srm.JitRun(script);
			srm.Reset();

			// Tree-walking
			var sw = Stopwatch.StartNew();
			const int runs = 3;
			for (int r = 0; r < runs; r++)
			{
				srm.Run(script);
				srm.Reset();
			}
			sw.Stop();
			double treeMs = sw.Elapsed.TotalMilliseconds / runs;

			// JIT
			sw.Restart();
			for (int r = 0; r < runs; r++)
			{
				srm.JitRun(script);
				srm.Reset();
			}
			sw.Stop();
			double jitMs = sw.Elapsed.TotalMilliseconds / runs;

			double speedup = treeMs / jitMs;

			output.WriteLine("=== for(i=0;i<100000;i++) i=i ===");
			output.WriteLine("Tree-walking: {0:F1} ms", treeMs);
			output.WriteLine("JIT:          {0:F1} ms", jitMs);
			output.WriteLine("Speedup:      {0:F1}x", speedup);
		}

		[Fact]
		public void Arithmetic_Benchmark()
		{
			string script = @"
				var sum = 0;
				for (var i = 0; i < 100000; i++) {
					sum = sum + i * 2 - 1;
				}
			";

			var srm = new ScriptRunningMachine();
			new ScriptDebugger(srm);

			// Warmup
			srm.Run(script);
			srm.Reset();
			srm.JitRun(script);
			srm.Reset();

			// Tree-walking
			var sw = Stopwatch.StartNew();
			srm.Run(script);
			sw.Stop();
			double treeMs = sw.Elapsed.TotalMilliseconds;
			srm.Reset();

			// JIT
			sw.Restart();
			srm.JitRun(script);
			sw.Stop();
			double jitMs = sw.Elapsed.TotalMilliseconds;

			double speedup = treeMs / jitMs;

			output.WriteLine("=== sum += i*2-1 (100k iterations) ===");
			output.WriteLine("Tree-walking: {0:F1} ms", treeMs);
			output.WriteLine("JIT:          {0:F1} ms", jitMs);
			output.WriteLine("Speedup:      {0:F1}x", speedup);
		}

		[Fact]
		public void FunctionCall_Benchmark()
		{
			string script = @"
				function add(a, b) { return a + b; }
				var sum = 0;
				for (var i = 0; i < 10000; i++) {
					sum = add(sum, i);
				}
			";

			var srm = new ScriptRunningMachine();
			new ScriptDebugger(srm);

			// Warmup
			srm.Run(script);
			srm.Reset();
			srm.JitRun(script);
			srm.Reset();

			// Tree-walking
			var sw = Stopwatch.StartNew();
			srm.Run(script);
			sw.Stop();
			double treeMs = sw.Elapsed.TotalMilliseconds;
			srm.Reset();

			// JIT
			sw.Restart();
			srm.JitRun(script);
			sw.Stop();
			double jitMs = sw.Elapsed.TotalMilliseconds;

			double speedup = treeMs / jitMs;

			output.WriteLine("=== add(sum, i) x 10k ===");
			output.WriteLine("Tree-walking: {0:F1} ms", treeMs);
			output.WriteLine("JIT:          {0:F1} ms", jitMs);
			output.WriteLine("Speedup:      {0:F1}x", speedup);
		}
	}
}
