using System.Diagnostics.CodeAnalysis;
using Xunit;

[assembly: ExcludeFromCodeCoverage]

// All classes share a single Postgres container. Some handlers run global, side-effecting queries
// (e.g. the converter queue's GetNextVideoToConvertQuery picks and locks *any* eligible video),
// while other classes seed globally-visible rows. Running collections in parallel makes those
// races flaky, so serialize the suite — it completes in a few seconds either way.
[assembly: CollectionBehavior(DisableTestParallelization = true)]