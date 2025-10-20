using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace t_tracker_ui.State;

public class UiState : INotifyPropertyChanged
{
    public UiState() { }

    private DateTimeOffset _selectedDate = DateTimeOffset.Now;
    private int _displayNamesVersion;
    private int _limit = 10;
    public DateTimeOffset SelectedDate
    {
        get => _selectedDate;
        set { if (_selectedDate != value) { _selectedDate = value; OnPropertyChanged(); } }
    }
    public int DisplayNamesVersion
    {
        get => _displayNamesVersion;
        private set { if (_displayNamesVersion != value) { _displayNamesVersion = value; OnPropertyChanged(); } }
    }
    public int Limit
    {
        get => _limit;
        set { if (_limit != value) { _limit = value; OnPropertyChanged(); } }
    }
    
    public void NotifyDisplayNamesChanged() => DisplayNamesVersion++;
    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}