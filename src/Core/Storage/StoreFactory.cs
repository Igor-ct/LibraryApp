using System;
using System.Linq;
using Core.Abstractions;

namespace Core.Storage;

public static class StoreFactory
{
    public static ILibraryStore Create(string[] args, string dataFilePath)
    {
        bool useFile = args.Contains("--file");

        ILibraryStore baseStore = useFile
            ? new FileLibraryStore(dataFilePath)
            : new InMemoryLibraryStore(SampleData.Books());

        return new CachingLibraryStore(baseStore);
    }
}