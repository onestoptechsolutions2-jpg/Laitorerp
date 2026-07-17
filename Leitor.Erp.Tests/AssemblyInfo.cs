using Xunit;

// Each test class boots its own full ABP host + in-memory Sqlite connection (see ErpTestBase).
// Running them in parallel hits a known thread-safety issue in Microsoft.Data.Sqlite's internal
// aggregate-function registration (a shared static dictionary gets corrupted under concurrent
// SqliteConnection initialization) - not a bug in this project's code, just not safe to
// parallelize across hosts like this.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
