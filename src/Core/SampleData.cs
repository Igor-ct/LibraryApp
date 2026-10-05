using System.Collections.Generic;
using Core.Domain;

namespace Core;

public static class SampleData
{
    public static IEnumerable<BookCopy> Books() =>
    [
        BookCopy.Create("C-101", "978-0131103627", "The C Programming Language", 1988, "Brian W. Kernighan"),
        BookCopy.Create("C-102", "978-0201835953", "The Mythical Man-Month", 1995, "Frederick P. Brooks Jr.")
    ];
}