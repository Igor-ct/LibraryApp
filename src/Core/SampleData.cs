using System.Collections.Generic;
using Core.Domain;

namespace Core;

public static class SampleData
{
    public static IEnumerable<BookCopy> Books()
    {
        var books = new List<BookCopy>
        {
            BookCopy.Create("C-101", "978-0131103627", "The C Programming Language", 1988, "Brian W. Kernighan"),
            BookCopy.Create("C-102", "978-0201835953", "The Mythical Man-Month", 1995, "Frederick P. Brooks Jr."),
            BookCopy.Create("C-103", "978-0132350884", "Clean Code", 2008, "Robert C. Martin"),
            BookCopy.Create("C-104", "978-0201633610", "Design Patterns", 1994, "Gang of Four"),
            BookCopy.Create("C-105", "978-1449331818", "Learning Python", 2013, "Mark Lutz"),
            BookCopy.Create("C-106", "978-0596009205", "Head First Java", 2005, "Kathy Sierra"),
            BookCopy.Create("C-107", "978-0134685991", "Effective Java", 2017, "Joshua Bloch"),
            BookCopy.Create("C-108", "978-1491950296", "Programming Rust", 2017, "Jim Blandy"),
            BookCopy.Create("C-109", "978-0135957059", "The Pragmatic Programmer", 2019, "Andrew Hunt"),
            BookCopy.Create("C-110", "978-1491904244", "You Don't Know JS", 2015, "Kyle Simpson"),
            BookCopy.Create("C-111", "978-0321751041", "Domain-Driven Design", 2003, "Eric Evans"),
            BookCopy.Create("C-112", "978-1492051961", "Concurrency in C#", 2019, "Stephen Cleary"),
            BookCopy.Create("C-113", "978-0134494166", "Clean Architecture", 2017, "Robert C. Martin"),
            BookCopy.Create("C-114", "978-0321125217", "Refactoring", 1999, "Martin Fowler"),
            BookCopy.Create("C-115", "978-1449373320", "Designing Data-Intensive Apps", 2017, "Martin Kleppmann"),
            BookCopy.Create("C-116", "978-0137081073", "C# in Depth", 2019, "Jon Skeet"),
            BookCopy.Create("C-117", "978-1617294532", "Microservices Patterns", 2018, "Chris Richardson"),
            BookCopy.Create("C-118", "978-1098101227", "Cloud Native Go", 2021, "Matthew Titmus"),
            BookCopy.Create("C-119", "978-1492078005", "Learning Go", 2021, "Jon Bodner"),
            BookCopy.Create("C-120", "978-0321563842", "The C++ Programming Language", 2013, "Bjarne Stroustrup")
        };

        books[0].Issue();
        books[3].Issue();
        books[15].Issue();
        books[19].Issue();

        return books;
    }
}