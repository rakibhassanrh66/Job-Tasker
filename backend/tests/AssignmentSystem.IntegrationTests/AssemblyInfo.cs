// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using Xunit;

// These tests share a database and configure the host through process-wide environment
// variables, both of which are global state. Running collections concurrently would let
// one test's configuration land in another's host. Serial execution is the honest choice.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
