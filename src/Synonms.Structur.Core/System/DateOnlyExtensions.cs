namespace Synonms.Structur.Core.System;

public static class DateOnlyExtensions
{
    public static int CalendarDaysTo(this DateOnly from, DateOnly to) =>
        from <= to 
            ? to.DayNumber - from.DayNumber + 1
            : throw new ArgumentOutOfRangeException($"Argument {nameof(to)} must be on or after date '{from}'.");

    public static int MonthsTo(this DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            throw new ArgumentOutOfRangeException($"Argument {nameof(to)} must be on or after date '{from}'.");
        }

        int months = 0;

        for (int i = 1;; ++i)
        {
            DateOnly date = from.AddMonths(i);

            if (date > to)
            {
                break;
            }

            if (date == to && date.Day < from.Day)
            {
                // Target month is shorter than From month, e.g. 31 Jan +1 month results in 28 Feb
                break;
            }
            
            months++;
        }

        return months;
    }

    public static int YearsTo(this DateOnly from, DateOnly to) =>
        from.MonthsTo(to) / 12;
    
    public static DateOnly Today =>
        DateOnly.FromDateTime(DateTime.Today);
    
    public static int WeekDaysTo(this DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            throw new ArgumentOutOfRangeException($"Argument {nameof(to)} must be on or after date '{from}'.");
        }

        int weekDays = 0;

        for (int i = 0;; ++i)
        {
            DateOnly date = from.AddDays(i);

            if (date > to)
            {
                break;
            }
            
            weekDays += date.DayOfWeek switch
            {
                DayOfWeek.Saturday => 0,
                DayOfWeek.Sunday => 0,
                _ => 1
            };
        }

        return weekDays;
    }

    public static int WorkingDaysTo(this DateOnly from, DateOnly to, IEnumerable<DayOfWeek> workingDaysOfWeek, IEnumerable<DateOnly> bankHolidays) =>
        from.WorkingDaysTo(to, workingDaysOfWeek, bankHolidays, new[] { new Tuple<DateOnly, DateOnly>(from, to) });
    
    public static int WorkingDaysTo(this DateOnly from, DateOnly to, IEnumerable<DayOfWeek> workingDaysOfWeek, IEnumerable<DateOnly> bankHolidays, IEnumerable<Tuple<DateOnly, DateOnly>> periodsToInclude)
    {
        if (to < from)
        {
            throw new ArgumentOutOfRangeException($"Argument {nameof(to)} must be on or after date '{from}'.");
        }

        List<DayOfWeek> workingDaysOfWeekList = workingDaysOfWeek.ToList();
        List<DateOnly> bankHolidaysList = bankHolidays.ToList();
        List<Tuple<DateOnly, DateOnly>> periodsToIncludeList = periodsToInclude.ToList();
        
        int workingDays = 0;
        
        for(DateOnly date = from; date <= to; date = date.AddDays(1))
        {
            if (workingDaysOfWeekList.Contains(date.DayOfWeek) is true 
                && bankHolidaysList.Contains(date) is false
                && periodsToIncludeList.Any(x => x.Item1 <= date && x.Item2 >= date))
            {
                workingDays++;
            }
        }
        
        return workingDays;
    }
}