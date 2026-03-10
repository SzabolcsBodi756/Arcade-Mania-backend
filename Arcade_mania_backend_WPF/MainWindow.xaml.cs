using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Arcade_mania_backend_WPF.Services;
using Arcade_mania_backend_webAPI.Models.Dtos.Scores;
using Arcade_mania_backend_webAPI.Models.Dtos.Users;

namespace Arcade_mania_backend_WPF
{
    public partial class MainWindow : Window
    {
        public bool IsGridPasswordVisible { get; set; } = false;

        private readonly UserApiService _apiService;

        private List<UserDataAdminDto> _users = new();

        private List<GameScoreDto> _currentScores = new();

        public MainWindow()
        {

            InitializeComponent();

            _apiService = new UserApiService();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            SetAdminUiEnabled(false);

        }

        private void SetAdminUiEnabled(bool enabled)

        {
            AdminContentGrid.IsEnabled = enabled;

            AdminContentGrid.Opacity = enabled ? 1.0 : 0.5;

            LoginStatusTextBlock.Text = enabled ? "Bejelentkezve (Admin)" : "Nincs bejelentkezve";
        }

        private async void AdminLoginButton_Click(object sender, RoutedEventArgs e)
        {

            try
            {

                var name = AdminNameTextBox.Text?.Trim() ?? "";

                var password = AdminPasswordBox.Password ?? "";

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
                {

                    LoginStatusTextBlock.Text = "Add meg a nevet és a jelszót.";

                    return;

                }

                LoginStatusTextBlock.Text = "Bejelentkezés...";

                var ok = await _apiService.AdminLoginAsync(name, password);

                if (!ok)
                {

                    SetAdminUiEnabled(false);

                    LoginStatusTextBlock.Text = "Sikertelen bejelentkezés.";

                    return;
                }

                SetAdminUiEnabled(true);

                await LoadUsersAsync();
            }
            catch (Exception ex)
            {

                SetAdminUiEnabled(false);

                LoginStatusTextBlock.Text = $"Hiba: {ex.Message}";

            }
        }

        private void AdminLogoutButton_Click(object sender, RoutedEventArgs e)
        {

            _apiService.Logout();

            UsersGrid.ItemsSource = null;

            _users = new List<UserDataAdminDto>();

            ClearSelectedUserFields();

            SearchUserIdTextBox.Text = "";

            NewUserNameTextBox.Text = "";

            NewUserPasswordTextBox.Text = "";

            NewUserRoleComboBox.SelectedIndex = 0;

            SetAdminUiEnabled(false);
        }

        private async Task LoadUsersAsync()
        {

            try
            {

                var users = await _apiService.GetAllUsersAdminAsync();

                foreach (var u in users)
                {

                    u.Role = string.IsNullOrWhiteSpace(u.Role) ? "User" : u.Role;

                    if (u.Scores != null)
                    {

                        u.Scores = u.Scores
                            .OrderBy(s => s.GameName, StringComparer.CurrentCultureIgnoreCase)
                            .ToList();
                    }
                    else
                    {

                        u.Scores = new List<GameScoreDto>();

                    }
                }

                _users = users
                    .OrderBy(u => u.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                UsersGrid.ItemsSource = _users;
            }
            catch (Exception ex)
            {

                MessageBox.Show(
                    $"Hiba történt az adatok betöltésekor:\n{ex.Message}",
                    "API hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

            }
        }

        private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (UsersGrid.SelectedItem is UserDataAdminDto user)
            {

                FillSelectedUser(user);

            }
        }

        private void FillSelectedUser(UserDataAdminDto user)
        {

            SelectedUserIdTextBox.Text = user.Id.ToString();

            SelectedUserNameTextBox.Text = user.Name;

            SelectedUserPasswordTextBox.Text = user.Password;


            var role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role;
            SelectedUserRoleComboBox.SelectedIndex =
                role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            _currentScores = (user.Scores ?? new List<GameScoreDto>())
                .OrderBy(s => s.GameName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            SetScoreField(Score1NameTextBlock, Score1TextBox, _currentScores, 0);

            SetScoreField(Score2NameTextBlock, Score2TextBox, _currentScores, 1);

            SetScoreField(Score3NameTextBlock, Score3TextBox, _currentScores, 2);

            SearchUserIdTextBox.Text = user.Id.ToString();
        }

        private void SetScoreField(TextBlock nameBlock, TextBox valueBox,
            List<GameScoreDto> scores, int index)
        {

            if (index < scores.Count)
            {

                var s = scores[index];

                nameBlock.Text = $"{s.GameName}:";

                valueBox.Text = s.HighScore.ToString();

                valueBox.IsEnabled = true;
            }
            else
            {

                nameBlock.Text = string.Empty;

                valueBox.Text = string.Empty;

                valueBox.IsEnabled = false;
            }
        }

        private async void SaveUserButton_Click(object sender, RoutedEventArgs e)
        {

            if (string.IsNullOrWhiteSpace(SelectedUserIdTextBox.Text))
            {

                MessageBox.Show("Nincs kijelölt felhasználó.", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }


            if (!Guid.TryParse(SelectedUserIdTextBox.Text, out var id))
            {

                MessageBox.Show("Érvénytelen ID formátum.", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            var name = SelectedUserNameTextBox.Text?.Trim() ?? string.Empty;

            var password = SelectedUserPasswordTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
            {

                MessageBox.Show("Név és jelszó megadása kötelező.", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            var role = (SelectedUserRoleComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "User";

            var updatedScores = new List<GameScoreDto>();

            for (int i = 0; i < _currentScores.Count; i++)
            {

                var original = _currentScores[i];

                int newScore = original.HighScore;

                if (i == 0)
                {

                    if (int.TryParse(Score1TextBox.Text?.Trim(), out int parsed))
                    {

                        newScore = parsed;

                    }
                }
                else if (i == 1)
                {

                    if (int.TryParse(Score2TextBox.Text?.Trim(), out int parsed))
                    {

                        newScore = parsed;

                    }   
                }
                else if (i == 2)
                {

                    if (int.TryParse(Score3TextBox.Text?.Trim(), out int parsed))
                    {

                        newScore = parsed;

                    }
                }

                updatedScores.Add(new GameScoreDto
                {
                    GameId = original.GameId,
                    GameName = original.GameName,
                    HighScore = newScore
                });
            }

            var updateScores = updatedScores
                .Select(s => new UserUpdateScoreAdminDto
                {
                    GameId = s.GameId,
                    HighScore = s.HighScore
                })
                .ToList();

            var dto = new UserUpdateAdminDto
            {
                Name = name,
                Password = password,
                Role = role,
                Scores = updateScores
            };

            try
            {

                await _apiService.UpdateUserAdminAsync(id, dto);

                MessageBox.Show("Felhasználó és pontszámok sikeresen módosítva.",
                    "Siker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await LoadUsersAsync();

                var updatedUser = _users.FirstOrDefault(u => u.Id == id);

                if (updatedUser != null)
                {

                    UsersGrid.SelectedItem = updatedUser;
                    FillSelectedUser(updatedUser);

                }
                else
                {

                    ClearSelectedUserFields();

                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(
                    $"Hiba a módosítás közben:\n{ex.Message}",
                    "API hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ClearSelectedUserFields()
        {

            SelectedUserIdTextBox.Text = string.Empty;

            SelectedUserNameTextBox.Text = string.Empty;

            SelectedUserPasswordTextBox.Text = string.Empty;


            SelectedUserRoleComboBox.SelectedIndex = 0;


            Score1NameTextBlock.Text = string.Empty;

            Score2NameTextBlock.Text = string.Empty;

            Score3NameTextBlock.Text = string.Empty;

            Score1TextBox.Text = string.Empty;

            Score2TextBox.Text = string.Empty;

            Score3TextBox.Text = string.Empty;


            Score1TextBox.IsEnabled = false;

            Score2TextBox.IsEnabled = false;

            Score3TextBox.IsEnabled = false;


            _currentScores = new List<GameScoreDto>();
        }

        private void ClearFieldsButton_Click(object sender, RoutedEventArgs e)
        {

            UsersGrid.SelectedItem = null;

            ClearSelectedUserFields();


            SearchUserIdTextBox.Text = string.Empty;

            NewUserNameTextBox.Text = string.Empty;

            NewUserPasswordTextBox.Text = string.Empty;

            NewUserRoleComboBox.SelectedIndex = 0;
        }

        private async void FetchUserByIdButton_Click(object sender, RoutedEventArgs e)
        {

            var idText = SearchUserIdTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(idText))
            {

                MessageBox.Show("Írd be az ID-t.", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            if (!Guid.TryParse(idText, out var id))
            {

                MessageBox.Show("Érvénytelen ID formátum.", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            try
            {

                var user = await _apiService.GetUserAdminByIdAsync(id);

                if (user == null)
                {

                    MessageBox.Show("Nincs ilyen felhasználó.", "Információ",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    return;
                }

                FillSelectedUser(user);

                var inList = _users.FirstOrDefault(u => u.Id == user.Id);

                if (inList != null)
                {

                    UsersGrid.SelectedItem = inList;

                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(
                    $"Hiba a lekérés közben:\n{ex.Message}",
                    "API hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {

            var idText = SearchUserIdTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(idText))
            {

                MessageBox.Show("Írd be az ID-t törléshez.", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            if (!Guid.TryParse(idText, out var id))
            {

                MessageBox.Show("Érvénytelen ID formátum.", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedUserIdTextBox.Text))
            {

                var result = MessageBox.Show(
                    "Nem tűnik úgy, hogy lekérted volna az adott user adatait.\n" +
                    "Biztosan törölni szeretnéd az ID alapján?",
                    "Megerősítés",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {

                    return;

                }
                    
            }


            var confirm = MessageBox.Show(
                "Biztosan törölni szeretnéd ezt a felhasználót?",
                "Megerősítés",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);


            if (confirm != MessageBoxResult.Yes)
            {

                return;

            }
                

            try
            {

                await _apiService.DeleteUserAdminAsync(id);

                MessageBox.Show("Felhasználó sikeresen törölve.",
                    "Siker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await LoadUsersAsync();
                ClearSelectedUserFields();
                SearchUserIdTextBox.Text = string.Empty;
            }
            catch (Exception ex)
            {

                MessageBox.Show(
                    $"Hiba a törlés közben:\n{ex.Message}",
                    "API hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void CreateUserButton_Click(object sender, RoutedEventArgs e)
        {

            var name = NewUserNameTextBox.Text?.Trim() ?? string.Empty;

            var password = NewUserPasswordTextBox.Text?.Trim() ?? string.Empty;


            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
            {

                MessageBox.Show("Név és jelszó megadása kötelező.", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            var role = (NewUserRoleComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "User";

            var dto = new UserCreateAdminDto
            {
                Name = name,
                Password = password,
                Role = role
            };

            try
            {

                var created = await _apiService.CreateUserAdminAsync(dto);

                MessageBox.Show(
                    $"Új felhasználó létrehozva: {created?.Name}",
                    "Siker",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                NewUserNameTextBox.Text = string.Empty;

                NewUserPasswordTextBox.Text = string.Empty;

                NewUserRoleComboBox.SelectedIndex = 0;

                await LoadUsersAsync();


                if (created != null)
                {
                    var inList = _users.FirstOrDefault(u => u.Id == created.Id);

                    if (inList != null)
                    {

                        UsersGrid.SelectedItem = inList;
                        FillSelectedUser(inList);

                    }
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(
                    $"Hiba az új felhasználó létrehozásakor:\n{ex.Message}",
                    "API hiba",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {

            Guid? selectedId = null;

            if (Guid.TryParse(SelectedUserIdTextBox.Text, out var id))
            {

                selectedId = id;

            }

            await LoadUsersAsync();

            if (selectedId.HasValue)
            {

                var user = _users.FirstOrDefault(u => u.Id == selectedId.Value);

                if (user != null)
                {

                    UsersGrid.SelectedItem = user;

                    FillSelectedUser(user);

                    return;
                }
            }

            ClearSelectedUserFields();
        }

        private void ToggleGridPasswordsButton_Click(object sender, RoutedEventArgs e)
        {

            IsGridPasswordVisible = !IsGridPasswordVisible;

            ToggleGridPasswordsButton.Content = IsGridPasswordVisible
                ? "Jelszavak elrejtése"
                : "Jelszavak megjelenítése";

            UsersGrid.Items.Refresh();
        }
    }
}
