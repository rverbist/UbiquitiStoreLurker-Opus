using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace UbiquitiStoreLurker.Web.Data;

// SQLite stores DateTimeOffset as a long (binary ticks + offset).
// This converter ensures round-trip precision without timezone loss.
public sealed class DateTimeOffsetToBinaryConverter : ValueConverter<DateTimeOffset, long>
{
    public DateTimeOffsetToBinaryConverter()
        : base(
            dto => dto.UtcDateTime.Ticks,
            ticks => new DateTimeOffset(new DateTime(ticks, DateTimeKind.Utc), TimeSpan.Zero))
    {
    }
}
