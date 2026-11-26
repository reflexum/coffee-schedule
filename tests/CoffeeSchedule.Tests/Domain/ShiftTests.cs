using CoffeeSchedule.Domain.Entities;
using CoffeeSchedule.Domain.Enums;
using CoffeeSchedule.Domain.Exceptions;

namespace CoffeeSchedule.Tests.Domain;

public class ShiftTests
{
    [Fact]
    public void Create_ValidData_CreatesDraftShiftInUtc()
    {
        var shopId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var start = new DateTimeOffset(2026, 12, 1, 9, 0, 0, TimeSpan.FromHours(3));
        var end = new DateTimeOffset(2026, 12, 1, 17, 0, 0, TimeSpan.FromHours(3));

        var shift = Shift.Create(shopId, start, end, Position.Barista, employeeId, "  Утренняя смена  ");

        Assert.Equal(ShiftStatus.Draft, shift.Status);
        Assert.Equal(new DateTimeOffset(2026, 12, 1, 6, 0, 0, TimeSpan.Zero), shift.StartsAtUtc);
        Assert.Equal(new DateTimeOffset(2026, 12, 1, 14, 0, 0, TimeSpan.Zero), shift.EndsAtUtc);
        Assert.Equal("Утренняя смена", shift.Notes);
        Assert.Null(shift.PublishedAtUtc);
    }

    [Fact]
    public void Create_EndBeforeOrEqualStart_ThrowsDomainException()
    {
        var start = DateTimeOffset.UtcNow;

        Assert.Throws<DomainException>(() =>
            Shift.Create(Guid.NewGuid(), start, start, Position.Barista));
        Assert.Throws<DomainException>(() =>
            Shift.Create(Guid.NewGuid(), start, start.AddHours(-1), Position.Barista));
    }

    [Fact]
    public void Create_Exceeds24Hours_ThrowsDomainException()
    {
        var start = DateTimeOffset.UtcNow;

        Assert.Throws<DomainException>(() =>
            Shift.Create(Guid.NewGuid(), start, start.AddHours(25), Position.Barista));
    }

    [Fact]
    public void Publish_RequiresAssignedEmployeeAndNonCancelledStatus()
    {
        var start = DateTimeOffset.UtcNow.AddDays(1);
        var unassignedShift = Shift.Create(Guid.NewGuid(), start, start.AddHours(8), Position.Barista);

        Assert.Throws<DomainException>(() => unassignedShift.Publish());

        unassignedShift.UpdateSchedule(start, start.AddHours(8), Position.Barista, Guid.NewGuid(), null);
        unassignedShift.Publish();

        Assert.Equal(ShiftStatus.Published, unassignedShift.Status);
        Assert.NotNull(unassignedShift.PublishedAtUtc);

        unassignedShift.Cancel();
        Assert.Equal(ShiftStatus.Cancelled, unassignedShift.Status);
        Assert.Throws<DomainException>(() => unassignedShift.Publish());
    }

    [Fact]
    public void OverlapsWith_DistinguishesAdjacentFromOverlapping()
    {
        var start = new DateTimeOffset(2026, 12, 1, 9, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 12, 1, 17, 0, 0, TimeSpan.Zero);
        var shift = Shift.Create(Guid.NewGuid(), start, end, Position.Barista);

        Assert.False(shift.OverlapsWith(end, end.AddHours(4)));
        Assert.False(shift.OverlapsWith(start.AddHours(-4), start));
        Assert.True(shift.OverlapsWith(start.AddHours(7), end.AddHours(2)));
        Assert.True(shift.OverlapsWith(start.AddHours(2), end.AddHours(-2)));
    }

    [Theory]
    [InlineData(Position.Manager, Position.Trainee, true)]
    [InlineData(Position.ShiftLead, Position.Barista, true)]
    [InlineData(Position.Barista, Position.Barista, true)]
    [InlineData(Position.Trainee, Position.Barista, false)]
    [InlineData(Position.Barista, Position.Manager, false)]
    public void IsPositionCompatible_FollowsHierarchy(Position employeePos, Position requiredPos, bool expected)
    {
        Assert.Equal(expected, Shift.IsPositionCompatible(employeePos, requiredPos));
    }
}
