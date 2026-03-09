using MauiAbschlussprojekt.ViewModels;

namespace MauiAbschlussprojekt.Views;

public partial class AddSleepEntryPage : ContentPage
{
    private readonly AddSleepEntryViewModel _viewModel;

    public AddSleepEntryPage(AddSleepEntryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // Button-Events verdrahten
        SaveButton.Clicked += OnSaveClicked;
        CancelButton.Clicked += OnCancelClicked;

        // Picker-Optionen befüllen
        FallAsleepPicker.ItemsSource = new List<string>
            { "Schnell (<10 Min)", "Normal (10-20 Min)", "Mittel (20-30 Min)", "Lang (>30 Min)" };
        DreamMoodPicker.ItemsSource = new List<string>
            { "Positiv 😊", "Neutral 😐", "Negativ 😟", "Albtraum 😱" };

        // Schlafdauer-Anzeige aktualisieren wenn Zeiten geändert
        BedTimePicker.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(TimePicker.Time)) UpdateDurationDisplay(); };
        WakeTimePicker.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(TimePicker.Time)) UpdateDurationDisplay(); };
        BedDatePicker.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(DatePicker.Date)) UpdateDurationDisplay(); };
        WakeDatePicker.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(DatePicker.Date)) UpdateDurationDisplay(); };

        // Qualitäts-Slider
        QualitySlider.ValueChanged += (s, e) =>
        {
            int val = (int)Math.Round(e.NewValue);
            QualitySlider.Value = val;
            QualityLabel.Text = val switch
            {
                1 => "1 – Sehr schlecht 😴",
                2 => "2 – Schlecht 😕",
                3 => "3 – Okay 😐",
                4 => "4 – Gut 🙂",
                5 => "5 – Ausgezeichnet 😄",
                _ => val.ToString()
            };
        };

        // Traumtagebuch-Stimmung nur zeigen wenn Text vorhanden
        DreamTextEditor.TextChanged += (s, e) =>
            DreamMoodSection.IsVisible = !string.IsNullOrWhiteSpace(e.NewTextValue);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();

        // Initialwerte in UI schreiben (nach ViewModel-Init)
        BedDatePicker.Date = _viewModel.BedTime.Date;
        BedTimePicker.Time = _viewModel.BedTime.TimeOfDay;
        WakeDatePicker.Date = _viewModel.WakeTime.Date;
        WakeTimePicker.Time = _viewModel.WakeTime.TimeOfDay;

        QualitySlider.Value = _viewModel.InitialSleepQuality;

        if (_viewModel.InitialFallAsleepIndex >= 0 &&
            _viewModel.InitialFallAsleepIndex < FallAsleepPicker.ItemsSource.Count)
            FallAsleepPicker.SelectedIndex = _viewModel.InitialFallAsleepIndex;
        else
            FallAsleepPicker.SelectedIndex = 1; // Standard: Normal

        DreamTextEditor.Text = _viewModel.InitialDreamText;

        if (!string.IsNullOrEmpty(_viewModel.InitialDreamText) &&
            _viewModel.InitialDreamMoodIndex >= 0 &&
            _viewModel.InitialDreamMoodIndex < DreamMoodPicker.ItemsSource.Count)
        {
            DreamMoodSection.IsVisible = true;
            DreamMoodPicker.SelectedIndex = _viewModel.InitialDreamMoodIndex;
        }

        NotesEditor.Text = _viewModel.InitialNotes;
        Title = _viewModel.IsEditMode ? "Nacht bearbeiten" : "Nacht hinzufügen";

        UpdateDurationDisplay();
    }

    // ── Hilfsmethode: Date + Time sicher kombinieren (nullable-safe) ────────
    private static DateTime CombineDateTime(DateTime? date, TimeSpan? time)
        => (date ?? DateTime.Today).Date + (time ?? TimeSpan.Zero);

    // ── Schlafdauer berechnen & anzeigen ────────────────────────────────────
    private void UpdateDurationDisplay()
    {
        var bedTime = CombineDateTime(BedDatePicker.Date, BedTimePicker.Time);
        var wakeTime = CombineDateTime(WakeDatePicker.Date, WakeTimePicker.Time);

        // Wenn Aufwachzeit <= Bettzeit → automatisch nächster Tag
        if (wakeTime <= bedTime)
            wakeTime = wakeTime.AddDays(1);

        var duration = wakeTime - bedTime;
        SleepDurationLabel.Text = $"{(int)duration.TotalHours}h {duration.Minutes:D2}m";

        bool warning = duration.TotalHours < 4 || duration.TotalHours > 16;
        WarningLabel.IsVisible = warning;
        WarningLabel.Text = duration.TotalHours < 4
            ? "⚠️ Sehr kurze Schlafdauer – bitte prüfen"
            : duration.TotalHours > 16
                ? "⚠️ Sehr lange Schlafdauer – bitte prüfen"
                : string.Empty;
    }

    // ── Speichern ────────────────────────────────────────────────────────────
    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var bedTime = CombineDateTime(BedDatePicker.Date, BedTimePicker.Time);
        var wakeTime = CombineDateTime(WakeDatePicker.Date, WakeTimePicker.Time);
        if (wakeTime <= bedTime) wakeTime = wakeTime.AddDays(1);

        int quality = (int)Math.Round(QualitySlider.Value);

        string category = FallAsleepPicker.SelectedIndex switch
        {
            0 => "Fast",
            2 => "Medium",
            3 => "Long",
            _ => "Normal"
        };

        string? dreamText = string.IsNullOrWhiteSpace(DreamTextEditor.Text) ? null : DreamTextEditor.Text;
        string? dreamMood = DreamMoodSection.IsVisible
            ? DreamMoodPicker.SelectedIndex switch
            {
                0 => "Positive",
                2 => "Negative",
                3 => "Nightmare",
                _ => "Neutral"
            }
            : null;
        string? notes = string.IsNullOrWhiteSpace(NotesEditor.Text) ? null : NotesEditor.Text;

        SaveButton.IsEnabled = false;
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        await _viewModel.SaveFromViewAsync(bedTime, wakeTime, quality, category,
            dreamText, dreamMood, notes);

        SaveButton.IsEnabled = true;
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
    }

    // ── Abbrechen ───────────────────────────────────────────────────────────
    private async void OnCancelClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}