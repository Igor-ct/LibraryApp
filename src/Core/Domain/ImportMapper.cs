using System;
using System.Collections.Generic;
using Core.Dto;

namespace Core.Domain;

public static class ImportMapper
{
    public static ImportResult<BookCopy> MapBooksToDomain(ImportResult<BookDto> dtoResult)
    {
        var validEntities = new List<BookCopy>();
        var errors = new List<string>(dtoResult.Errors); 

        foreach (var dto in dtoResult.Items)
        {
            try
            {
                var entity = BookCopy.FromDto(dto);
                validEntities.Add(entity);
            }
            catch (Exception ex)
            {
                errors.Add($"Помилка валідації книги {dto.Id}: {ex.Message}");
            }
        }

        return new ImportResult<BookCopy>(validEntities, errors);
    }
}