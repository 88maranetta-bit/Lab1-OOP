using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;

namespace La1
{
    public partial class MainPage : ContentPage
    {
        const int CountColumn = 10;
        const int CountRow = 10;
        private readonly Dictionary<string, string> _cellFormulas = new(StringComparer.OrdinalIgnoreCase);

        public MainPage()
        {
            InitializeComponent();
            CreateGrid();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadSavedFilesList();
        }

        private void LoadSavedFilesList()
        {
            try
            {
                string folderPath = FileSystem.AppDataDirectory;
                var files = Directory.GetFiles(folderPath, "*.json")
                                     .Select(Path.GetFileName)
                                     .ToList();
                savedFilesPicker.ItemsSource = files;
            }
            catch
            {
            }
        }

        private void CreateGrid()
        {
            AddColumnsAndColumnLabels();
            AddRowsAndCellEntries();
        }

        private void AddColumnsAndColumnLabels()
        {
            for (int col = 0; col < CountColumn + 1; col++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = 80 });
                if (col > 0)
                {
                    var label = new Label
                    {
                        Text = GetColumnName(col),
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White,
                        BackgroundColor = Colors.Transparent
                    };
                    Microsoft.Maui.Controls.Grid.SetRow(label, 0);
                    Microsoft.Maui.Controls.Grid.SetColumn(label, col);
                    grid.Children.Add(label);
                }
            }
        }

        private void AddRowsAndCellEntries()
        {
            for (int row = 0; row < CountRow; row++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = 40 });

                var label = new Label
                {
                    Text = (row + 1).ToString(),
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    BackgroundColor = Colors.Transparent
                };
                Microsoft.Maui.Controls.Grid.SetRow(label, row + 1);
                Microsoft.Maui.Controls.Grid.SetColumn(label, 0);
                grid.Children.Add(label);

                for (int col = 0; col < CountColumn; col++)
                {
                    string cellName = GetColumnName(col + 1) + (row + 1);

                    var entry = new Entry
                    {
                        Text = "",
                        StyleId = cellName,
                        WidthRequest = 80,
                        HeightRequest = 40,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        TextColor = Colors.White,
                        BackgroundColor = Color.FromArgb("#2A2A2A")
                    };

                    entry.Focused += Entry_Focused;
                    entry.Unfocused += Entry_Unfocused;
                    Microsoft.Maui.Controls.Grid.SetRow(entry, row + 1);
                    Microsoft.Maui.Controls.Grid.SetColumn(entry, col + 1);
                    grid.Children.Add(entry);
                }
            }
        }

        private string GetColumnName(int colIndex)
        {
            int dividend = colIndex;
            string columnName = string.Empty;
            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }
            return columnName;
        }

        private void RefreshGridIndices()
        {
            var rowLabels = grid.Children
                .OfType<Label>()
                .Where(l => Microsoft.Maui.Controls.Grid.GetColumn(l) == 0 && Microsoft.Maui.Controls.Grid.GetRow(l) > 0)
                .OrderBy(l => Microsoft.Maui.Controls.Grid.GetRow(l))
                .ToList();

            int index = 1;
            foreach (var lbl in rowLabels)
            {
                lbl.Text = index.ToString();
                index++;
            }

            for (int c = 1; c < grid.ColumnDefinitions.Count; c++)
            {
                var label = grid.Children.FirstOrDefault(cld => cld is Label l && Microsoft.Maui.Controls.Grid.GetRow(l) == 0 && Microsoft.Maui.Controls.Grid.GetColumn(l) == c) as Label;
                if (label != null)
                {
                    label.Text = GetColumnName(c);
                }
            }

            foreach (var child in grid.Children)
            {
                if (child is Entry entry)
                {
                    int row = Microsoft.Maui.Controls.Grid.GetRow(entry);
                    int col = Microsoft.Maui.Controls.Grid.GetColumn(entry);
                    if (row > 0 && col > 0)
                    {
                        entry.StyleId = GetColumnName(col) + row;
                    }
                }
            }
        }

        private void ValidateFormula(string formula)
        {
            string lower = formula.ToLower();
            var matches = Regex.Matches(lower, @"[a-zA-Z]+");
            foreach (Match m in matches)
            {
                string w = m.Value;
                if (w != "max" && w != "min" && w != "div" && w != "mod")
                {
                    int index = m.Index + m.Length;
                    bool isCellRef = false;
                    if (index < lower.Length && char.IsDigit(lower[index]))
                    {
                        isCellRef = true;
                    }

                    if (!isCellRef)
                    {
                        throw new Exception("Помилка імені");
                    }
                }
            }
        }

        private void Entry_Focused(object? sender, EventArgs e)
        {
            if (sender is Entry entry && !string.IsNullOrEmpty(entry.StyleId))
            {
                if (showFormulasSwitch != null && showFormulasSwitch.IsToggled)
                {
                    return;
                }

                if (_cellFormulas.TryGetValue(entry.StyleId, out string? formula))
                {
                    entry.Text = formula;
                }
                if (textInput != null)
                {
                    textInput.Text = entry.Text;
                }
            }
        }

        private async void Entry_Unfocused(object? sender, EventArgs e)
        {
            if (sender is Entry entry && !string.IsNullOrEmpty(entry.StyleId))
            {
                if (showFormulasSwitch != null && showFormulasSwitch.IsToggled)
                {
                    return;
                }

                string text = entry.Text?.Trim() ?? "";
                if (text.StartsWith("="))
                {
                    _cellFormulas[entry.StyleId] = text;
                    try
                    {
                        string formula = text.Substring(1);
                        ValidateFormula(formula);
                        var evaluator = new FormulaEvaluatorVisitor(name => GetCellValue(name, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { entry.StyleId }));
                        double result = evaluator.Evaluate(formula);
                        entry.Text = result.ToString(CultureInfo.InvariantCulture);
                        entry.TextColor = Colors.White;
                    }
                    catch (Exception ex)
                    {
                        entry.TextColor = Colors.Red;
                        string msg = ex.Message;
                        if (msg.Contains("циклу"))
                        {
                            entry.Text = "ERR: CYCLE";
                        }
                        else if (msg.Contains("ділення на нуль", StringComparison.OrdinalIgnoreCase) || msg.Contains("Division by zero", StringComparison.OrdinalIgnoreCase))
                        {
                            entry.Text = "#DIV/0!";
                        }
                        else if (msg.Contains("ім'я", StringComparison.OrdinalIgnoreCase) || msg.Contains("name", StringComparison.OrdinalIgnoreCase))
                        {
                            entry.Text = "#NAME?";
                        }
                        else if (msg.Contains("значення", StringComparison.OrdinalIgnoreCase) || msg.Contains("value", StringComparison.OrdinalIgnoreCase))
                        {
                            entry.Text = "#VALUE!";
                        }
                        else
                        {
                            entry.Text = "#REF!";
                        }
                    }
                }
                else
                {
                    _cellFormulas.Remove(entry.StyleId);
                    entry.TextColor = Colors.White;
                }

                if (textInput != null)
                {
                    textInput.Text = text;
                }
            }
            RecalculateAllCells();
        }

        private void ShowFormulasSwitch_Toggled(object? sender, ToggledEventArgs e)
        {
            bool showFormulas = e.Value;

            foreach (var child in grid.Children.ToList())
            {
                if (child is Entry entry && !string.IsNullOrEmpty(entry.StyleId))
                {
                    string cellName = entry.StyleId;

                    if (showFormulas)
                    {
                        if (_cellFormulas.TryGetValue(cellName, out string? formula))
                        {
                            entry.Text = formula;
                            entry.TextColor = Colors.White;
                        }
                    }
                    else
                    {
                        if (!entry.IsFocused)
                        {
                            RecalculateSingleCell(entry);
                        }
                    }
                }
            }

            if (!showFormulas)
            {
                RecalculateAllCells();
            }
        }

        private void RecalculateSingleCell(Entry entry)
        {
            if (_cellFormulas.TryGetValue(entry.StyleId, out string? formulaText))
            {
                try
                {
                    string formula = formulaText.Substring(1);
                    ValidateFormula(formula);
                    var evaluator = new FormulaEvaluatorVisitor(name => GetCellValue(name, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { entry.StyleId }));
                    double result = evaluator.Evaluate(formula);
                    entry.Text = result.ToString(CultureInfo.InvariantCulture);
                    entry.TextColor = Colors.White;
                }
                catch (Exception ex)
                {
                    entry.TextColor = Colors.Red;
                    string msg = ex.Message;
                    if (msg.Contains("ділення на нуль", StringComparison.OrdinalIgnoreCase))
                        entry.Text = "#DIV/0!";
                    else if (msg.Contains("ім'я", StringComparison.OrdinalIgnoreCase))
                        entry.Text = "#NAME?";
                    else if (msg.Contains("значення", StringComparison.OrdinalIgnoreCase))
                        entry.Text = "#VALUE!";
                    else
                        entry.Text = "#REF!";
                }
            }
        }

        private void RecalculateAllCells()
        {
            if (showFormulasSwitch != null && showFormulasSwitch.IsToggled)
            {
                return;
            }

            foreach (var kvp in _cellFormulas)
            {
                string cellName = kvp.Key;
                string formulaText = kvp.Value;

                var entry = grid.Children.OfType<Entry>().FirstOrDefault(e => e.StyleId?.Equals(cellName, StringComparison.OrdinalIgnoreCase) == true);
                if (entry != null && !entry.IsFocused)
                {
                    try
                    {
                        string formula = formulaText.Substring(1);
                        ValidateFormula(formula);
                        var evaluator = new FormulaEvaluatorVisitor(name => GetCellValue(name, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { cellName }));
                        double result = evaluator.Evaluate(formula);

                        entry.Text = result.ToString(CultureInfo.InvariantCulture);
                        entry.TextColor = Colors.White;
                    }
                    catch (Exception ex)
                    {
                        entry.TextColor = Colors.Red;
                        string msg = ex.Message;
                        if (msg.Contains("циклу"))
                        {
                            entry.Text = "ERR: CYCLE";
                        }
                        else if (msg.Contains("ділення на нуль", StringComparison.OrdinalIgnoreCase))
                        {
                            entry.Text = "#DIV/0!";
                        }
                        else if (msg.Contains("ім'я", StringComparison.OrdinalIgnoreCase))
                        {
                            entry.Text = "#NAME?";
                        }
                        else if (msg.Contains("значення", StringComparison.OrdinalIgnoreCase))
                        {
                            entry.Text = "#VALUE!";
                        }
                        else
                        {
                            entry.Text = "#REF!";
                        }
                    }
                }
            }
        }

        private double GetCellValue(string cellName, HashSet<string>? visited = null)
        {
            if (visited == null)
            {
                visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            cellName = cellName.Trim().ToUpper();

            if (visited.Contains(cellName))
            {
                throw new Exception("Помилка циклу: виявлено циклічне посилання!");
            }

            visited.Add(cellName);

            try
            {
                if (_cellFormulas.TryGetValue(cellName, out string? formulaText) && !string.IsNullOrEmpty(formulaText))
                {
                    string formula = formulaText.Substring(1);
                    ValidateFormula(formula);
                    var subEvaluator = new FormulaEvaluatorVisitor(name => GetCellValue(name, new HashSet<string>(visited, StringComparer.OrdinalIgnoreCase)));
                    return subEvaluator.Evaluate(formula);
                }

                foreach (var child in grid.Children)
                {
                    if (child is Entry entry && entry.StyleId?.ToUpper() == cellName)
                    {
                        string text = entry.Text?.Trim() ?? "";
                        if (string.IsNullOrEmpty(text))
                            return 0.0;

                        if (text.StartsWith("="))
                        {
                            text = text.Substring(1);
                        }

                        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                        {
                            return val;
                        }

                        throw new Exception("Помилка значення");
                    }
                }
            }
            finally
            {
                visited.Remove(cellName);
            }

            throw new Exception("Помилка посилання");
        }

        private async void SaveButton_Clicked(object? sender, EventArgs? e)
        {
            try
            {
                string fileName = await DisplayPromptAsync("Збереження", "Введіть назву файлу:", "Зберегти", "Скасувати", "table1.json");
                if (string.IsNullOrWhiteSpace(fileName)) return;

                if (!fileName.EndsWith(".json"))
                    fileName += ".json";

                string folderPath = FileSystem.AppDataDirectory;
                string filePath = Path.Combine(folderPath, fileName);

                var cellData = new Dictionary<string, string>();

                foreach (var kvp in _cellFormulas)
                {
                    cellData[kvp.Key] = kvp.Value;
                }

                foreach (var child in grid.Children)
                {
                    if (child is Entry entry && !string.IsNullOrEmpty(entry.StyleId))
                    {
                        string text = entry.Text?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(text) && !text.StartsWith("=") && !_cellFormulas.ContainsKey(entry.StyleId))
                        {
                            cellData[entry.StyleId] = text;
                        }
                    }
                }

                string jsonString = System.Text.Json.JsonSerializer.Serialize(cellData);
                await File.WriteAllTextAsync(filePath, jsonString);

                LoadSavedFilesList();
                await DisplayAlert("Успіх", $"Файл {fileName} збережено!", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка", $"Не вдалося зберегти: {ex.Message}", "OK");
            }
        }

        private async void ClearButton_Clicked(object? sender, EventArgs? e)
        {
            bool answer = await DisplayAlert("Очищення", "Ви дійсно хочете очистити всю таблицю?", "Так", "Ні");
            if (answer)
            {
                _cellFormulas.Clear();
                foreach (var child in grid.Children)
                {
                    if (child is Entry entry)
                    {
                        entry.Text = "";
                        entry.TextColor = Colors.White;
                    }
                }
                if (textInput != null)
                {
                    textInput.Text = "";
                }
            }
        }

        private async void ReadButton_Clicked(object? sender, EventArgs? e)
        {
            if (savedFilesPicker.SelectedItem is string selectedFile)
            {
                LoadTableFromFile(selectedFile);
            }
            else
            {
                await DisplayAlert("Увага", "Виберіть файл зі списку!", "OK");
            }
        }

        private void SavedFilesPicker_SelectedIndexChanged(object? sender, EventArgs? e)
        {
            if (savedFilesPicker.SelectedItem is string selectedFile)
            {
                LoadTableFromFile(selectedFile);
            }
        }

        private async void LoadTableFromFile(string fileName)
        {
            try
            {
                string folderPath = FileSystem.AppDataDirectory;
                string filePath = Path.Combine(folderPath, fileName);

                if (!File.Exists(filePath)) return;

                string jsonString = await File.ReadAllTextAsync(filePath);
                var cellData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

                if (cellData == null) return;

                _cellFormulas.Clear();
                foreach (var child in grid.Children)
                {
                    if (child is Entry entry)
                    {
                        entry.Text = "";
                        entry.TextColor = Colors.White;
                    }
                }

                foreach (var kvp in cellData)
                {
                    string cellName = kvp.Key;
                    string value = kvp.Value;

                    if (value.StartsWith("="))
                    {
                        _cellFormulas[cellName] = value;
                    }

                    foreach (var child in grid.Children)
                    {
                        if (child is Entry entry && entry.StyleId == cellName)
                        {
                            entry.Text = value;
                        }
                    }
                }

                if (showFormulasSwitch != null)
                {
                    showFormulasSwitch.IsToggled = false;
                }

                RecalculateAllCells();
                await DisplayAlert("Успіх", $"Файл {fileName} завантажено!", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка", $"Помилка завантаження: {ex.Message}", "OK");
            }
        }

        private async void ExitButton_Clicked(object? sender, EventArgs? e)
        {
            bool answer = await DisplayAlert("Підтвердження", "Ви дійсно хочете вийти?", "Так", "Ні");
            if (answer)
            {
                Environment.Exit(0);
            }
        }

        private async void HelpButton_Clicked(object? sender, EventArgs? e)
        {
            await DisplayAlert(
                "Про програму",
                "Лабораторна робота 1\nВаріант 6 (Набір: 1, 2, 4, 6)\n\nОперації: +, -, *, /, mod, div, ^, max, min\n\nВиконала: Мариніна Анна\nГрупа К-27",
                "OK"
            );
        }

        private void DeleteRowButton_Clicked(object? sender, EventArgs? e)
        {
            if (grid.RowDefinitions.Count > 2)
            {
                int targetRow = -1;

                var focusedEntry = grid.Children.OfType<Entry>().FirstOrDefault(entry => entry.IsFocused);
                if (focusedEntry != null)
                {
                    targetRow = Microsoft.Maui.Controls.Grid.GetRow(focusedEntry);
                }

                if (targetRow <= 0)
                {
                    targetRow = grid.RowDefinitions.Count - 1;
                }

                var childrenToRemove = new List<IView>();
                foreach (var child in grid.Children)
                {
                    if (child is BindableObject bindable && Microsoft.Maui.Controls.Grid.GetRow(bindable) == targetRow)
                    {
                        childrenToRemove.Add(child);
                    }
                }

                foreach (var child in childrenToRemove)
                {
                    grid.Children.Remove(child);
                }

                grid.RowDefinitions.RemoveAt(targetRow);

                foreach (var child in grid.Children)
                {
                    if (child is BindableObject bindable)
                    {
                        int r = Microsoft.Maui.Controls.Grid.GetRow(bindable);
                        if (r > targetRow)
                        {
                            Microsoft.Maui.Controls.Grid.SetRow(bindable, r - 1);
                        }
                    }
                }

                RefreshGridIndices();
            }
        }

        private void DeleteColumnButton_Clicked(object? sender, EventArgs? e)
        {
            if (grid.ColumnDefinitions.Count > 2)
            {
                int targetCol = -1;

                var focusedEntry = grid.Children.OfType<Entry>().FirstOrDefault(entry => entry.IsFocused);
                if (focusedEntry != null)
                {
                    targetCol = Microsoft.Maui.Controls.Grid.GetColumn(focusedEntry);
                }

                if (targetCol <= 0)
                {
                    targetCol = grid.ColumnDefinitions.Count - 1;
                }

                var childrenToRemove = new List<IView>();
                foreach (var child in grid.Children)
                {
                    if (child is BindableObject bindable && Microsoft.Maui.Controls.Grid.GetColumn(bindable) == targetCol)
                    {
                        childrenToRemove.Add(child);
                    }
                }

                foreach (var child in childrenToRemove)
                {
                    grid.Children.Remove(child);
                }

                grid.ColumnDefinitions.RemoveAt(targetCol);

                foreach (var child in grid.Children)
                {
                    if (child is BindableObject bindable)
                    {
                        int c = Microsoft.Maui.Controls.Grid.GetColumn(bindable);
                        if (c > targetCol)
                        {
                            Microsoft.Maui.Controls.Grid.SetColumn(bindable, c - 1);
                        }
                    }
                }

                RefreshGridIndices();
            }
        }

        private void AddRowButton_Clicked(object? sender, EventArgs? e)
        {
            int newRow = grid.RowDefinitions.Count;
            grid.RowDefinitions.Add(new RowDefinition { Height = 40 });

            var label = new Label
            {
                Text = newRow.ToString(),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                BackgroundColor = Colors.Transparent
            };
            Microsoft.Maui.Controls.Grid.SetRow(label, newRow);
            Microsoft.Maui.Controls.Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            for (int col = 0; col < grid.ColumnDefinitions.Count - 1; col++)
            {
                string cellName = GetColumnName(col + 1) + newRow;

                var entry = new Entry
                {
                    Text = "",
                    StyleId = cellName,
                    WidthRequest = 80,
                    HeightRequest = 40,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Colors.White,
                    BackgroundColor = Color.FromArgb("#2A2A2A")
                };
                entry.Focused += Entry_Focused;
                entry.Unfocused += Entry_Unfocused;
                Microsoft.Maui.Controls.Grid.SetRow(entry, newRow);
                Microsoft.Maui.Controls.Grid.SetColumn(entry, col + 1);
                grid.Children.Add(entry);
            }
        }

        private void AddColumnButton_Clicked(object? sender, EventArgs? e)
        {
            int newColumn = grid.ColumnDefinitions.Count;
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = 80 });

            var label = new Label
            {
                Text = GetColumnName(newColumn),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                BackgroundColor = Colors.Transparent
            };
            Microsoft.Maui.Controls.Grid.SetRow(label, 0);
            Microsoft.Maui.Controls.Grid.SetColumn(label, newColumn);
            grid.Children.Add(label);

            for (int row = 0; row < grid.RowDefinitions.Count - 1; row++)
            {
                string cellName = GetColumnName(newColumn) + (row + 1);

                var entry = new Entry
                {
                    Text = "",
                    StyleId = cellName,
                    WidthRequest = 80,
                    HeightRequest = 40,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Colors.White,
                    BackgroundColor = Color.FromArgb("#2A2A2A")
                };
                entry.Focused += Entry_Focused;
                entry.Unfocused += Entry_Unfocused;
                Microsoft.Maui.Controls.Grid.SetRow(entry, row + 1);
                Microsoft.Maui.Controls.Grid.SetColumn(entry, newColumn);
                grid.Children.Add(entry);
            }
        }
    }
}