using Xunit;

// Disable parallel test collection execution to avoid font rendering and Skia/QuestPDF race conditions in property tests
[assembly: CollectionBehavior(DisableTestParallelization = true)]
