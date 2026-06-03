namespace e_student
{
    using Microsoft.Maui.Storage;

    public partial class MainPage : ContentPage
    {
        GradesParser parser = new GradesParser();

        string fnum;
        string egn;

        TuApiService api;

        bool isLoggedIn = false;


        public MainPage()
        {
            InitializeComponent();

            BindingContext = new GradeMonitorViewModel();


            LogoutBtn.IsVisible = isLoggedIn;
            ToggleNotify.IsVisible = isLoggedIn;
            LoadSpinner.IsVisible = isLoggedIn;

            fnum = Preferences.Get("fnum", "");
            egn = Preferences.Get("egn", "");

            if (!string.IsNullOrEmpty(fnum) && !string.IsNullOrEmpty(egn))
            {
                LoginPanel.IsVisible = false;
                LoadBtn.IsVisible = true;

                api = new TuApiService();

                isLoggedIn = true;

                LogoutBtn.IsVisible = true;
                ToggleNotify.IsVisible = true;

                _ = LoadGrades();
            }
        }

        private void HideKeyboard()
        {
            #if ANDROID
                var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                var view = activity?.CurrentFocus ?? activity?.Window?.DecorView;
                if (view != null)
                {
                    var imm = (Android.Views.InputMethods.InputMethodManager?)
                        activity?.GetSystemService(Android.Content.Context.InputMethodService);
                    imm?.HideSoftInputFromWindow(view.WindowToken, 0);
                    view.ClearFocus();
                }
            #endif
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            try
            {
                fnum = FnumEntry.Text;
                egn = EgnEntry.Text;

                HideKeyboard();

                Preferences.Set("fnum", fnum);
                Preferences.Set("egn", egn);

                isLoggedIn = true;
                LogoutBtn.IsVisible = true;
                ToggleNotify.IsVisible = true;

                api = new TuApiService();

                LoginPanel.IsVisible = false;
                LoadBtn.IsVisible = true;

                await LoadGrades();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Login Error", ex.Message, "OK");
            }
        }

        private async void OnLoadClicked(object sender, EventArgs e)
        {
            await LoadGrades();
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            // remove saved login
            Preferences.Remove("fnum");
            Preferences.Remove("egn");
            api = null;

            isLoggedIn = false;
            LogoutBtn.IsVisible = false;
            ToggleNotify.IsVisible = false;

            // clear local variables
            fnum = "";
            egn = "";

            // clear UI
            GradesView.ItemsSource = null;

            // show login again
            LoginPanel.IsVisible = true;
            LoadBtn.IsVisible = false;
        }

        private void OnSwitchToggled(object sender, ToggledEventArgs e)
        {
            var vm = (GradeMonitorViewModel)BindingContext;
            // Only call toggle if the switch state doesn't already match
            if (e.Value != vm.IsMonitoring)
                vm.ToggleCommand.Execute(null);
        }

        private async Task LoadGrades()
        {
            try
            {
                LoadSpinner.IsVisible = true;
                LoadSpinner.IsRunning = true;
                LoadBtn.IsEnabled = false;

                string html = await FetchHtml(fnum, egn);

                var result = parser.Parse(html);

                GradesView.ItemsSource = result;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to load grades: {ex.Message}", "OK");
            }
            finally
            {
                LoadSpinner.IsRunning = false;
                LoadSpinner.IsVisible = false;
                LoadBtn.IsEnabled = true;
            }
        }

        private async Task<string> FetchHtml(string fnum, string egn)
        {

            return await api.GetHtmlAsync(fnum, egn);
        }
    }

}
