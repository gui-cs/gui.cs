﻿#region

using System.Globalization;

#endregion

namespace Terminal.Gui.ViewsTests;

public class DatePickerTests {
    [Fact]
    public void DatePicker_ChangingCultureChangesFormat () {
        var date = new DateTime (2000, 7, 23);
        var datePicker = new DatePicker (date);

        datePicker.Culture = CultureInfo.GetCultureInfo ("en-GB");
        Assert.Equal ("23/07/2000", datePicker.Text);

        datePicker.Culture = CultureInfo.GetCultureInfo ("pl-PL");
        Assert.Equal ("23.07.2000", datePicker.Text);

        // Deafult date format for en-US is M/d/yyyy but we are using StandardizeDateFormat method
        // to convert it to the format that has 2 digits for month and day.
        datePicker.Culture = CultureInfo.GetCultureInfo ("en-US");
        Assert.Equal ("07/23/2000", datePicker.Text);
    }

    [Fact]
    public void DatePicker_Initialize_ShouldSetCurrentDate () {
        var datePicker = new DatePicker ();
        Assert.Equal (DateTime.Now.Date.Day, datePicker.Date.Day);
        Assert.Equal (DateTime.Now.Date.Month, datePicker.Date.Month);
        Assert.Equal (DateTime.Now.Date.Year, datePicker.Date.Year);
    }

    [Fact]
    public void DatePicker_SetDate_ShouldChangeText () {
        var datePicker = new DatePicker () {
                                               Culture = CultureInfo.GetCultureInfo ("en-GB")
                                           };
        var newDate = new DateTime (2024, 1, 15);
        var format = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;

        datePicker.Date = newDate;
        Assert.Equal (newDate.ToString (format), datePicker.Text);
    }

    [Fact]
    [AutoInitShutdown]
    public void DatePicker_ShouldNot_SetDateOutOfRange_UsingPreviousMonthButton () {
        var date = new DateTime (1, 2, 15);
        var datePicker = new DatePicker (date);

        // Move focus to previous month button
        Application.Top.Add (datePicker);
        Application.Begin (Application.Top);

        // set focus to the previous month button
        datePicker.FocusNext ();
        datePicker.FocusNext ();

        // Change month to January 
        Assert.True (datePicker.NewKeyDownEvent (new (KeyCode.Enter)));
        Assert.Equal (1, datePicker.Date.Month);

        // Date should not change as previous month button is disabled
        Assert.False (datePicker.NewKeyDownEvent (new (KeyCode.Enter)));
        Assert.Equal (1, datePicker.Date.Month);
    }

    [Fact]
    [AutoInitShutdown]
    public void DatePicker_ShouldNot_SetDateOutOfRange_UsingNextMonthButton () {
        var date = new DateTime (9999, 11, 15);
        var datePicker = new DatePicker (date);

        Application.Top.Add (datePicker);
        Application.Begin (Application.Top);

        // Set focus to next month button
        datePicker.FocusNext ();
        datePicker.FocusNext ();
        datePicker.FocusNext ();

        // Change month to December
        Assert.True (datePicker.NewKeyDownEvent (new (KeyCode.Enter)));
        Assert.Equal (12, datePicker.Date.Month);

        // Date should not change as next month button is disabled
        Assert.False (datePicker.NewKeyDownEvent (new (KeyCode.Enter)));
        Assert.Equal (12, datePicker.Date.Month);
    }
}
