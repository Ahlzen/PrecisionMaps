using BenchmarkDotNet.Running;

// This is strictly to run launch the benchmark runner
// in a convenient way as a console app.

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
