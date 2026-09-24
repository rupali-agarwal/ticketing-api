using Xunit;

// Integration tests share a single SQL Server database, so they must run sequentially to avoid tests clearing each other's data.
[assembly: CollectionBehavior(DisableTestParallelization = true)]