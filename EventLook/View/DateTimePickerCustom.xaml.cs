using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using EventLook.Extensions;

namespace EventLook.View;

public sealed partial class DateTimePickerCustom
{
    private bool isUpdating;

    public DateTimePickerCustom()
    {
        InitializeComponent();
        MoveFocusCommand = new RelayCommand(() => PartTimeText.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
    }

    public IRelayCommand MoveFocusCommand { get; }

    #region Value dependency property

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(DateTime),
            typeof(DateTimePickerCustom),
            new FrameworkPropertyMetadata(DateTime.Now, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public DateTime Value
    {
        get => (DateTime)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (DateTimePickerCustom)d;
        ctrl.UpdatePartsFromValue();
    }

    private void UpdatePartsFromValue()
    {
        if (isUpdating) return;
        isUpdating = true;

        SelectedDate = Value;
        SelectedTimeString = TimeOnly.FromTimeSpan(Value.TimeOfDay).ToString(TimeFormat);

        isUpdating = false;
    }

    #endregion Value dependency property

    #region IsReadonly dependency property

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(DateTimePickerCustom), new PropertyMetadata(false));

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    #endregion IsReadonly dependency property

    #region SelectedDate dependency property

    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(
            nameof(SelectedDate),
            typeof(DateTime),
            typeof(DateTimePickerCustom),
            new FrameworkPropertyMetadata(DateTime.Now.Date, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateChanged));

    public DateTime SelectedDate
    {
        get => (DateTime)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (DateTimePickerCustom)d;

        if (ctrl.isUpdating) return;
        ctrl.isUpdating = true;

        ctrl.Value = (DateTime)e.NewValue + ctrl.Value.TimeOfDay;

        ctrl.isUpdating = false;
    }

    #endregion SelectedDate dependency property

    #region SelectedTimeString dependency property

    private const string TimeFormat = "HH:mm";

    public static readonly DependencyProperty SelectedTimeStringProperty =
        DependencyProperty.Register(
            nameof(SelectedTimeString),
            typeof(string),
            typeof(DateTimePickerCustom),
            new FrameworkPropertyMetadata(TimeOnly.MinValue.ToString(TimeFormat), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedTimeStringChanged));

    public string SelectedTimeString
    {
        get => (string)GetValue(SelectedTimeStringProperty);
        set => SetValue(SelectedTimeStringProperty, value);
    }

    private static void OnSelectedTimeStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (DateTimePickerCustom)d;

        if (ctrl.isUpdating) return;
        ctrl.isUpdating = true;

        // Allow HH:mm or HHmm format.
        if (TimeOnly.TryParse((string)e.NewValue, out var time)
            || (e.NewValue is string { Length: 4 } str
                && TimeOnly.TryParse(str[..2] + ":" + str[2..], out time)))
        {
            // Ensure normalized time
            ctrl.SelectedTimeString = time.ToString(TimeFormat);

            ctrl.Value = ctrl.Value.Date + time.ToTimeSpan();
        }

        ctrl.isUpdating = false;
    }

    #endregion

    private void DateTimePickerCustom_Loaded(object sender, RoutedEventArgs e)
    {
        UpdatePartsFromValue();
    }

    private void PartTimeText_OnLoaded(object sender, RoutedEventArgs e)
    {
        ((TextBox)sender).DeleteFluentClearButton();
    }
}