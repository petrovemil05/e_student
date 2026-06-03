using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Storage;

namespace e_student
{
    /// <summary>
    /// ViewModel for grade monitoring — retrieves saved credentials from app preferences.
    /// Bind IsMonitoring to a Switch and ToggleCommand to a Button.
    /// </summary>
    public class GradeMonitorViewModel : INotifyPropertyChanged
    {
        private GradeMonitorService? _service;
        private bool _isMonitoring;

        public bool IsMonitoring
        {
            get => _isMonitoring;
            private set { _isMonitoring = value; OnPropertyChanged(); OnPropertyChanged(nameof(ToggleLabel)); }
        }

        public string ToggleLabel => IsMonitoring ? "Спри следенето" : "Следи оценките";

        public ICommand ToggleCommand { get; }

        public GradeMonitorViewModel()
        {
            ToggleCommand = new Command(Toggle);
        }

        private void Toggle()
        {
            if (IsMonitoring)
            {
                StopService();
                IsMonitoring = false;
            }
            else
            {
                StartService();
                IsMonitoring = true;
            }
        }

        private void StartService()
        {
            // Get saved credentials from app preferences
            string fnum = Preferences.Get("fnum", "");
            string egn = Preferences.Get("egn", "");

            if (string.IsNullOrEmpty(fnum) || string.IsNullOrEmpty(egn))
            {
                // No saved credentials — cannot start monitoring
                MainThread.BeginInvokeOnMainThread(() =>
                    Application.Current?.MainPage?.DisplayAlert("Error", "No saved credentials. Please log in first.", "OK"));
                return;
            }

#if ANDROID
            var ctx = Android.App.Application.Context;
            var intent = new Android.Content.Intent(ctx, typeof(GradeMonitorForegroundService));
            intent.SetAction(GradeMonitorForegroundService.ActionStart);
            intent.PutExtra(GradeMonitorForegroundService.ExtraFnum, fnum);
            intent.PutExtra(GradeMonitorForegroundService.ExtraEgn, egn);
            ctx.StartForegroundService(intent);
#endif
        }

        private void StopService()
        {
#if ANDROID
            var ctx = Android.App.Application.Context;
            var intent = new Android.Content.Intent(ctx, typeof(GradeMonitorForegroundService));
            intent.SetAction(GradeMonitorForegroundService.ActionStop);
            ctx.StartService(intent);
#endif
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}