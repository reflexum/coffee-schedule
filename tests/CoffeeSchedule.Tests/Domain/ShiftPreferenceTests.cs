using CoffeeSchedule.Domain.Entities;
using CoffeeSchedule.Domain.Enums;
using CoffeeSchedule.Domain.Exceptions;

namespace CoffeeSchedule.Tests.Domain;

public class ShiftPreferenceTests
{
    [Fact]
    public void Create_ValidFullDayAndTimedPreferences_SetsProperties()
    {
        var today = new DateOnly(2026, 12, 1);
        var employeeId = Guid.NewGuid();

        var fullDay = ShiftPreference.Create(
            employeeId,
            today.AddDays(1),
            null,
            null,
            PreferenceType.Unavailable,
            "  Учёба в вузе  ",
            today);

        Assert.Null(fullDay.StartTime);
        Assert.Null(fullDay.EndTime);
        Assert.Equal("Учёба в вузе", fullDay.Comment);
        Assert.True(fullDay.ConflictsWithShift(new TimeOnly(8, 0), new TimeOnly(14, 0)));

        var timed = ShiftPreference.Create(
            employeeId,
            today.AddDays(2),
            new TimeOnly(14, 0),
            new TimeOnly(22, 0),
            PreferenceType.Preferred,
            null,
            today);

        Assert.False(timed.ConflictsWithShift(new TimeOnly(14, 0), new TimeOnly(22, 0)));
    }

    [Fact]
    public void Create_PastDateOrInvalidInterval_ThrowsDomainException()
    {
        var today = new DateOnly(2026, 12, 1);
        var employeeId = Guid.NewGuid();

        Assert.Throws<DomainException>(() => ShiftPreference.Create(
            employeeId,
            today.AddDays(-1),
            null,
            null,
            PreferenceType.Unavailable,
            null,
            today));

        Assert.Throws<DomainException>(() => ShiftPreference.Create(
            employeeId,
            today.AddDays(1),
            new TimeOnly(10, 0),
            null,
            PreferenceType.Preferred,
            null,
            today));

        Assert.Throws<DomainException>(() => ShiftPreference.Create(
            employeeId,
            today.AddDays(1),
            new TimeOnly(14, 0),
            new TimeOnly(10, 0),
            PreferenceType.Preferred,
            null,
            today));
    }

    [Fact]
    public void ConflictsWithShift_ChecksPartialUnavailableTimeWindows()
    {
        var today = new DateOnly(2026, 12, 1);
        var unavailableMorning = ShiftPreference.Create(
            Guid.NewGuid(),
            today.AddDays(1),
            new TimeOnly(9, 0),
            new TimeOnly(13, 0),
            PreferenceType.Unavailable,
            "Пары",
            today);

        Assert.True(unavailableMorning.ConflictsWithShift(new TimeOnly(8, 0), new TimeOnly(12, 0)));
        Assert.False(unavailableMorning.ConflictsWithShift(new TimeOnly(13, 0), new TimeOnly(21, 0)));
    }
}
