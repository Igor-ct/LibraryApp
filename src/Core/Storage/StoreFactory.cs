using System;
using System.IO;
using System.Linq;
using Core.Abstractions;

namespace Core.Storage;

public static class StoreFactory
{
    public static ILibraryStore Create(string[] args, string baseDirectory)
    {
        bool useFile = args.Contains("--file");
        string dataPath = Path.Combine(baseDirectory, "data", "library_catalog.json");

        ILibraryStore baseStore = useFile
            ? new FileLibraryStore(dataPath)
            : new InMemoryLibraryStore(SampleData.Books());

        return new CachingLibraryStore(baseStore);
    }
}