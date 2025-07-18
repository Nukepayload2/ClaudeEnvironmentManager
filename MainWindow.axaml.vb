Imports Avalonia.Controls
Imports Avalonia.Interactivity
Imports Avalonia.Media
Imports System.IO
Imports System.Runtime.InteropServices
Imports Avalonia
Imports Avalonia.Platform.Storage

Partial Class MainWindow
    Inherits Window

    Private ReadOnly _settings As Settings
    Private Const DefaultModels As String = "Pro/moonshotai/Kimi-K2-Instruct,moonshotai/Kimi-K2-Instruct,kimi-latest,kimi-k2-0711-preview"

    Public Sub New()
        InitializeComponent()
        _settings = Settings.Load()
        InitializeControls()
        LoadEnvironmentVariables()
    End Sub

    Private Sub InitializeControls()
        ' Setup Model ComboBox with predefined models
        Dim models = DefaultModels.Split(","c)
        For Each model In models
            ModelComboBox.Items.Add(model.Trim())
        Next

        ' Load saved settings
        ApiKeyTextBox.Text = _settings.AnthropicApiKey
        BaseUrlTextBox.Text = _settings.AnthropicBaseUrl
        ModelTextBox.Text = _settings.AnthropicModel
        FolderPathTextBox.Text = _settings.LastSelectedFolder

        ' Attach event handlers
        AddHandler BrowseFolderButton.Click, AddressOf BrowseFolderButton_Click
        AddHandler LaunchButton.Click, AddressOf LaunchButton_Click

        ' Update preview when values change
        AddHandler ApiKeyTextBox.TextChanged, AddressOf UpdateEnvironmentPreview
        AddHandler BaseUrlTextBox.TextChanged, AddressOf UpdateEnvironmentPreview
        AddHandler ModelTextBox.TextChanged, AddressOf UpdateEnvironmentPreview
        AddHandler FolderPathTextBox.TextChanged, AddressOf UpdateEnvironmentPreview

        ' Initial preview update
        UpdateEnvironmentPreview(Nothing, Nothing)
    End Sub

    Private Sub LoadEnvironmentVariables()
        ' Load API Keys
        Dim apiKeys = Environment.GetEnvironmentVariables().
            Cast(Of DictionaryEntry)().
            Where(Function(kv) kv.Key.ToString().EndsWith("_API_KEY", StringComparison.OrdinalIgnoreCase) AndAlso
                  Not kv.Key.ToString().Equals("ANTHROPIC_API_KEY", StringComparison.OrdinalIgnoreCase)).
            ToList()

        For Each kv In apiKeys
            Dim item = New ComboBoxItem() With {
                .Content = kv.Key.ToString(),
                .Tag = kv.Value.ToString()
            }
            ApiKeyComboBox.Items.Add(item)
        Next

        ' Load Base URLs
        Dim baseUrls = Environment.GetEnvironmentVariables().
            Cast(Of DictionaryEntry)().
            Where(Function(kv) kv.Key.ToString().EndsWith("_BASE_URL", StringComparison.OrdinalIgnoreCase) AndAlso
                  Not kv.Key.ToString().Equals("ANTHROPIC_BASE_URL", StringComparison.OrdinalIgnoreCase)).
            ToList()

        For Each kv In baseUrls
            Dim item = New ComboBoxItem() With {
                .Content = kv.Key.ToString(),
                .Tag = kv.Value.ToString()
            }
            BaseUrlComboBox.Items.Add(item)
        Next
    End Sub

    Private Async Sub BrowseFolderButton_Click(sender As Object, e As RoutedEventArgs)
        Dim dialog = New FolderPickerOpenOptions With {.Title = "Select Working Folder"}

        If Not String.IsNullOrEmpty(FolderPathTextBox.Text) Then
            dialog.SuggestedStartLocation = StorageProvider.TryGetFolderFromPathAsync(FolderPathTextBox.Text)
        End If

        Dim results = Await StorageProvider.OpenFolderPickerAsync(
            New FolderPickerOpenOptions With {.Title = "Select Working Folder"})
        Dim result = results.FirstOrDefault?.TryGetLocalPath()
        If Not String.IsNullOrEmpty(result) Then
            FolderPathTextBox.Text = result
            _settings.LastSelectedFolder = result
        End If
    End Sub

    Private Sub UpdateEnvironmentPreview(sender As Object, e As EventArgs)
        Dim apiKey = If(ApiKeyTextBox.Text, String.Empty)
        Dim baseUrl = If(BaseUrlTextBox.Text, String.Empty)
        Dim model = If(ModelTextBox.Text, String.Empty)
        Dim folder = If(FolderPathTextBox.Text, String.Empty)

        EnvironmentPreviewTextBox.Text = $"ANTHROPIC_API_KEY={apiKey}
ANTHROPIC_BASE_URL={baseUrl}
ANTHROPIC_MODEL={model}
WORKING_FOLDER={folder}"
    End Sub

    Private Sub LaunchButton_Click(sender As Object, e As RoutedEventArgs)
        ' Save settings
        _settings.AnthropicApiKey = ApiKeyTextBox.Text
        _settings.AnthropicBaseUrl = BaseUrlTextBox.Text
        _settings.AnthropicModel = ModelTextBox.Text
        _settings.LastSelectedFolder = FolderPathTextBox.Text
        _settings.Save()

        ' Validate inputs
        If String.IsNullOrWhiteSpace(FolderPathTextBox.Text) Then
            ShowError("Please select a working folder.")
            Return
        End If

        If Not Directory.Exists(FolderPathTextBox.Text) Then
            ShowError("The selected folder does not exist.")
            Return
        End If

        If String.IsNullOrWhiteSpace(ApiKeyTextBox.Text) Then
            ShowError("Please provide an API key.")
            Return
        End If

        Try
            LaunchClaude()
        Catch ex As Exception
            ShowError($"Failed to launch Claude: {ex.Message}")
        End Try
    End Sub

    Private Sub LaunchClaude()
        Dim startInfo = New ProcessStartInfo() With {
            .FileName = "claude",
            .WorkingDirectory = FolderPathTextBox.Text,
            .UseShellExecute = True,
            .CreateNoWindow = False
        }

        ' Set environment variables
        startInfo.EnvironmentVariables("ANTHROPIC_API_KEY") = ApiKeyTextBox.Text
        If Not String.IsNullOrWhiteSpace(BaseUrlTextBox.Text) Then
            startInfo.EnvironmentVariables("ANTHROPIC_BASE_URL") = BaseUrlTextBox.Text
        End If
        If Not String.IsNullOrWhiteSpace(ModelTextBox.Text) Then
            startInfo.EnvironmentVariables("ANTHROPIC_MODEL") = ModelTextBox.Text
        End If

        Process.Start(startInfo)
    End Sub

    Private Sub ShowError(message As String)
        Dim dialog = New Window() With {
            .Title = "Error",
            .Width = 400,
            .Height = 200
        }

        Dim stackPanel = New StackPanel() With {
            .Margin = New Thickness(20)
        }

        Dim textBlock = New TextBlock() With {
            .Text = message,
            .TextWrapping = TextWrapping.Wrap
        }

        Dim okButton = New Button() With {
            .Content = "OK",
            .HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            .Margin = New Thickness(0, 20, 0, 0)
        }

        AddHandler okButton.Click, Sub(s, e) dialog.Close()

        stackPanel.Children.Add(textBlock)
        stackPanel.Children.Add(okButton)

        dialog.Content = stackPanel
        dialog.ShowDialog(Me)
    End Sub

    ' Event handlers for ComboBox and TextBox synchronization
    Private Sub ApiKeyComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        ApiKeyTextBox.Text = If(ApiKeyComboBox.SelectedItem IsNot Nothing, ApiKeyComboBox.SelectedItem.ToString(), String.Empty)
        UpdateEnvironmentPreview(sender, e)
    End Sub

    Private Sub ApiKeyComboBox_TextChanged(sender As Object, e As TextChangedEventArgs)
        ApiKeyTextBox.Text = If(ApiKeyComboBox.SelectedItem IsNot Nothing, ApiKeyComboBox.SelectedItem.ToString(), String.Empty)
        UpdateEnvironmentPreview(sender, e)
    End Sub

    Private Sub ApiKeyTextBox_TextChanged(sender As Object, e As TextChangedEventArgs)
        Dim text = ApiKeyTextBox.Text
        For Each it In ApiKeyComboBox.Items
            If it.ToString() = text Then
                ApiKeyComboBox.SelectedItem = it
                Exit For
            End If
        Next
        UpdateEnvironmentPreview(sender, e)
    End Sub

    Private Sub BaseUrlComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        BaseUrlTextBox.Text = If(BaseUrlComboBox.SelectedItem IsNot Nothing, BaseUrlComboBox.SelectedItem.ToString(), String.Empty)
        UpdateEnvironmentPreview(sender, e)
    End Sub

    Private Sub BaseUrlComboBox_TextChanged(sender As Object, e As TextChangedEventArgs)
        BaseUrlTextBox.Text = If(BaseUrlComboBox.SelectedItem IsNot Nothing, BaseUrlComboBox.SelectedItem.ToString(), String.Empty)
        UpdateEnvironmentPreview(sender, e)
    End Sub

    Private Sub BaseUrlTextBox_TextChanged(sender As Object, e As TextChangedEventArgs)
        Dim text = BaseUrlTextBox.Text
        For Each it In BaseUrlComboBox.Items
            If it.ToString() = text Then
                BaseUrlComboBox.SelectedItem = it
                Exit For
            End If
        Next
        UpdateEnvironmentPreview(sender, e)
    End Sub

    Private Sub ModelComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        ModelTextBox.Text = If(ModelComboBox.SelectedItem IsNot Nothing, ModelComboBox.SelectedItem.ToString(), String.Empty)
        UpdateEnvironmentPreview(sender, e)
    End Sub

    Private Sub ModelComboBox_TextChanged(sender As Object, e As TextChangedEventArgs)
        ModelTextBox.Text = If(ModelComboBox.SelectedItem IsNot Nothing, ModelComboBox.SelectedItem.ToString(), String.Empty)
        UpdateEnvironmentPreview(sender, e)
    End Sub

    Private Sub ModelTextBox_TextChanged(sender As Object, e As TextChangedEventArgs)
        Dim text = ModelTextBox.Text
        For Each it In ModelComboBox.Items
            If it.ToString() = text Then
                ModelComboBox.SelectedItem = it
                Exit For
            End If
        Next
        UpdateEnvironmentPreview(sender, e)
    End Sub

End Class
