using Synonms.Structur.Core.System;
using Xunit;

namespace Synonms.Structur.Core.Tests.Unit.System;

public class DateOnlyExtensionsTests
{
     [Fact]
    public void CalendarDaysTo_InvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        DateOnly fromDate = new(2022, 1, 31);
        DateOnly toDate = new(2022, 1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => fromDate.CalendarDaysTo(toDate));
    }

    [Theory]
    [InlineData("2022-10-01", "2022-10-31", 31)]
    [InlineData("2022-10-01", "2022-10-01", 1)]
    [InlineData("2022-10-31", "2022-11-01", 2)]
    [InlineData("2022-10-01", "2023-09-30", 365)]
    public void CalendarDaysTo_ValidParameters_ReturnsCorrectResult(string from, string to, int expected)
    {
        DateOnly fromDate = DateOnly.Parse(from);
        DateOnly toDate = DateOnly.Parse(to);

        int actual = fromDate.CalendarDaysTo(toDate);
        
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MonthsTo_InvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        DateOnly fromDate = new(2022, 1, 31);
        DateOnly toDate = new(2022, 1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => fromDate.MonthsTo(toDate));
    }
    
    [Theory]
    [InlineData("2022-10-01", "2022-10-01", 0)]
    [InlineData("2022-12-31", "2023-01-01", 0)]
    [InlineData("2022-12-31", "2024-01-01", 12)]
    [InlineData("2022-10-01", "2022-11-30", 1)]
    [InlineData("2022-01-31", "2022-02-28", 0)]
    [InlineData("2022-10-01", "2022-11-01", 1)]
    [InlineData("2022-10-31", "2022-11-30", 0)]
    [InlineData("2022-09-01", "2022-11-11", 2)]
    [InlineData("2022-04-01", "2022-11-11", 7)]
    public void MonthsTo_ValidParameters_ReturnsCorrectResult(string from, string to, int expected)
    {
        DateOnly fromDate = DateOnly.Parse(from);
        DateOnly toDate = DateOnly.Parse(to);

        int actual = fromDate.MonthsTo(toDate);
        
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void YearsTo_InvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        DateOnly fromDate = new(2022, 1, 31);
        DateOnly toDate = new(2022, 1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => fromDate.YearsTo(toDate));
    }
    
    [Theory]
    [InlineData("2022-10-01", "2022-10-01", 0)]
    [InlineData("2022-12-31", "2023-01-01", 0)]
    [InlineData("2022-12-31", "2023-12-31", 1)]
    [InlineData("2022-12-31", "2024-01-01", 1)]
    [InlineData("2022-10-01", "2023-11-30", 1)]
    [InlineData("2022-10-01", "2032-11-01", 10)]
    public void YearsTo_ValidParameters_ReturnsCorrectResult(string from, string to, int expected)
    {
        DateOnly fromDate = DateOnly.Parse(from);
        DateOnly toDate = DateOnly.Parse(to);

        int actual = fromDate.YearsTo(toDate);
        
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Today_ReturnsCorrectDate()
    {
        DateTime expected = DateTime.Today;
        
        DateOnly actual = DateOnlyExtensions.Today;
        
        Assert.Equal(expected.Year, actual.Year);
        Assert.Equal(expected.Month, actual.Month);
        Assert.Equal(expected.Day, actual.Day);
    }
    
    [Fact]
    public void WeekDaysTo_InvalidParameters_ThrowsArgumentOutOfRangeException()
    {
        DateOnly fromDate = new(2022, 1, 31);
        DateOnly toDate = new(2022, 1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => fromDate.WeekDaysTo(toDate));
    }

    [Theory]
    [InlineData("2022-10-01", "2022-10-31", 21)]
    [InlineData("2022-10-01", "2022-10-01", 0)]
    [InlineData("2022-10-03", "2022-10-03", 1)]
    [InlineData("2022-10-31", "2022-11-01", 2)]
    [InlineData("2022-10-01", "2023-09-30", 260)]
    [InlineData("2019-06-04", "2019-07-05", 24)]
    [InlineData("2021-06-08", "2021-07-15", 28)]
    [InlineData("2021-10-22", "2021-12-19", 41)]
    [InlineData("2022-02-03", "2022-02-09", 5)]
    [InlineData("2022-03-31", "2022-05-20", 37)]
    [InlineData("2022-06-30", "2022-07-20", 15)]
    [InlineData("2022-09-02", "2022-11-01", 43)]
    [InlineData("2022-11-08", "2022-11-30", 17)]
    public void WeekDaysTo_ValidParameters_ReturnsCorrectResult(string from, string to, int expected)
    {
        DateOnly fromDate = DateOnly.Parse(from);
        DateOnly toDate = DateOnly.Parse(to);

        int actual = fromDate.WeekDaysTo(toDate);
        
        Assert.Equal(expected, actual);
    }
    
    [Fact]
    public void WorkingDaysTo_InvalidParameters_ThrowsArgumentOutOfRangeException() 
    {
        DateOnly fromDate = new(2022, 1, 31);
        DateOnly toDate = new(2022, 1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => fromDate.WorkingDaysTo(toDate, Enumerable.Empty<DayOfWeek>(), Enumerable.Empty<DateOnly>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => fromDate.WorkingDaysTo(toDate, Enumerable.Empty<DayOfWeek>(), Enumerable.Empty<DateOnly>(), Enumerable.Empty<Tuple<DateOnly, DateOnly>>()));
    }

    [Theory]
    [InlineData("2022-10-01", "2022-10-31", new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, new string[] {}, 21)]
    [InlineData("2022-10-01", "2022-10-31", new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, new [] { "2022-12-26", "2022-12-27" }, 21)]
    [InlineData("2022-10-01", "2022-10-31", new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }, new string[] {}, 17)]
    [InlineData("2022-10-01", "2022-10-31", new [] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, new string[] {}, 16)]
    [InlineData("2022-12-01", "2022-12-31", new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, new [] { "2022-12-26", "2022-12-27" }, 20)]
    [InlineData("2022-12-01", "2022-12-31", new [] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }, new [] { "2022-12-26", "2022-12-27" }, 12)]
    public void WorkingDaysTo_ValidParameters_ReturnsCorrectResult(string from, string to, DayOfWeek[] daysWorked, string[] bankHolidays, int expected)
    {
        DateOnly fromDate = DateOnly.Parse(from);
        DateOnly toDate = DateOnly.Parse(to);

        int actual = fromDate.WorkingDaysTo(toDate, daysWorked, bankHolidays.Select(DateOnly.Parse));
        
        Assert.Equal(expected, actual);
    }
    
    [Theory]
    [InlineData("2022-10-01", "2022-10-31", new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, new string[] {}, "2022-10-01", "2022-10-31", 21)]
    [InlineData("2022-10-01", "2022-10-31", new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, new string[] {}, "2022-09-01", "2022-09-30", 0)]
    [InlineData("2022-10-01", "2022-10-31", new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, new string[] {}, "2022-09-26", "2022-10-07", 5)]
    [InlineData("2022-10-01", "2022-10-31", new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, new [] { "2022-12-26", "2022-12-27" }, "2022-10-01", "2022-10-31", 21)]
    [InlineData("2022-10-01", "2022-10-31", new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, new [] { "2022-12-26", "2022-12-27" }, "2022-09-01", "2022-09-30", 0)]
    [InlineData("2022-10-01", "2022-10-31", new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, new [] { "2022-12-26", "2022-12-27" }, "2022-09-26", "2022-10-07", 5)]
    [InlineData("2022-10-01", "2022-10-31", new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }, new string[] {}, "2022-10-01", "2022-10-31", 17)]
    [InlineData("2022-10-01", "2022-10-31", new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }, new string[] {}, "2022-09-26", "2022-10-07", 4)]
    [InlineData("2022-10-01", "2022-10-31", new [] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, new string[] {}, "2022-10-01", "2022-10-31", 16)]
    [InlineData("2022-10-01", "2022-10-31", new [] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, new string[] {}, "2022-09-26", "2022-10-07", 4)]
    [InlineData("2022-12-01", "2022-12-31", new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, new [] { "2022-12-26", "2022-12-27" }, "2022-12-01", "2022-12-31", 20)]
    [InlineData("2022-12-01", "2022-12-31", new [] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }, new [] { "2022-12-26", "2022-12-27" }, "2022-12-12", "2022-12-31", 13)]
    [InlineData("2022-12-01", "2022-12-31", new [] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }, new [] { "2022-12-26", "2022-12-27" }, "2022-12-01", "2022-12-31", 12)]
    [InlineData("2022-12-01", "2022-12-31", new [] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }, new [] { "2022-12-26", "2022-12-27" }, "2022-12-12", "2022-12-31", 8)]
    [InlineData("2022-12-01", "2022-12-31", new [] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }, new [] { "2022-12-26", "2022-12-27" }, "2022-12-12", "2022-12-16", 3)]
    [InlineData("2022-12-01", "2022-12-31", new [] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }, new [] { "2022-12-26", "2022-12-27" }, "2022-12-12", "2023-02-16", 8)]
    public void WorkingDaysTo_ValidParametersWithPeriods_ReturnsCorrectResult(string from, string to, DayOfWeek[] daysWorked, string[] bankHolidays, string periodToIncludeStartDate, string periodToIncludeEndDate, int expected)
    {
        DateOnly fromDate = DateOnly.Parse(from);
        DateOnly toDate = DateOnly.Parse(to);

        int actual = fromDate.WorkingDaysTo(
            toDate, 
            daysWorked, 
            bankHolidays.Select(DateOnly.Parse), 
            new []
            {
                new Tuple<DateOnly, DateOnly>(DateOnly.Parse(periodToIncludeStartDate), DateOnly.Parse(periodToIncludeEndDate))
            });
        
        Assert.Equal(expected, actual);
    }
}