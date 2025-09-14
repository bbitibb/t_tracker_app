using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace t_tracker_ui.State;

public class UiState : INotifyPropertyChanged
{
    public UiState() {}

    private DateTimeOffset _selectedDate = DateTimeOffset.Now;
    public DateTimeOffset SelectedDate
    {
        get => _selectedDate;
        set { if (_selectedDate != value) { _selectedDate = value; OnPropertyChanged(); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}