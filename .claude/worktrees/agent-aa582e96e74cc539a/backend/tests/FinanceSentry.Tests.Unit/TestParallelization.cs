// Some units under test hold process-wide static state (e.g. CurrencyConverter's
// rate table). Running test classes in parallel would let a mutating test race a
// reader in another class, so parallelization is disabled for this assembly.
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
