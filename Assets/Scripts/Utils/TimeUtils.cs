using System;

public static class TimeUtils
{
    public static int GetTotalMilliseconds(DateTime theDateTime,DateTime startDateTime)
    {
        return ((((theDateTime.Day - startDateTime.Day) * 24 + theDateTime.Hour - startDateTime.Hour) * 60 + theDateTime.Minute - startDateTime.Minute) * 60 + theDateTime.Second - startDateTime.Second) * 1000 + theDateTime.Millisecond - startDateTime.Millisecond;
    }
}

