Imports Avalonia.Controls
Imports Avalonia.Interactivity
Imports Avalonia.Media
Imports Avalonia.Layout
Imports System.Diagnostics
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Collections.Generic
Imports System.Linq
Imports Avalonia

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
        AddHandler BrowseApiKeyButton.Click, AddressOf BrowseApiKeyButton_Click
        AddHandler BrowseBaseUrlButton.Click, AddressOf BrowseBaseUrlButton_Click

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
            Cast(Of Collections.DictionaryEntry)().
            Where(Function(kv) kv.Key.ToString().EndsWith("_API_KEY", StringComparison.OrdinalIgnoreCase) AndAlso
                  Not kv.Key.ToString().Equals("ANTHROPIC_API_KEY", StringComparison.OrdinalIgnoreCase)).
            Select(Function(kv) kv.Value.ToString()).
            Distinct().
            ToList()

        For Each key In apiKeys
            ApiKeyComboBox.Items.Add(key)
        Next

        ' Load Base URLs
        Dim baseUrls = Environment.GetEnvironmentVariables().
            Cast(Of Collections.DictionaryEntry)().
            Where(Function(kv) kv.Key.ToString().EndsWith("_BASE_URL", StringComparison.OrdinalIgnoreCase) AndAlso
                  Not kv.Key.ToString().Equals("ANTHROPIC_BASE_URL", StringComparison.OrdinalIgnoreCase)).
            Select(Function(kv) kv.Value.ToString()).
            Distinct().
            ToList()

        For Each url In baseUrls
            BaseUrlComboBox.Items.Add(url)
        Next
    End Sub

    Private Sub BrowseFolderButton_Click(sender As Object, e As RoutedEventArgs)
        Dim dialog = New OpenFolderDialog() With {
            .Title = "Select Working Folder"
        }

        If Not String.IsNullOrEmpty(FolderPathTextBox.Text) Then
            dialog.Directory = FolderPathTextBox.Text
        End If

        Dim result = dialog.ShowAsync(Me).Result
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
            .FileName = GetClaudeExecutable(),
            .WorkingDirectory = FolderPathTextBox.Text,
            .UseShellExecute = False,
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

    Private Function GetClaudeExecutable() As String
        ' Try to find claude executable
        Dim claudeCmd = "claude"

        ' On Windows, check if claude.cmd exists
        If RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then
            Dim fullPath = Environment.ExpandEnvironmentVariables("%PATH%")
            Dim paths = fullPath.Split(";"c)

            For Each p In paths
                Dim claudePath = Path.Combine(p.Trim(), "claude.cmd")
                If File.Exists(claudePath) Then
                    Return claudePath
                End If

                claudePath = Path.Combine(p.Trim(), "claude.exe")
                If File.Exists(claudePath) Then
                    Return claudePath
                End If
            Next
        Else
            ' On Linux/macOS, use which command to find claude
            Dim startInfo = New ProcessStartInfo() With {
                .FileName = "which",
                .Arguments = "claude",
                .RedirectStandardOutput = True,
                .UseShellExecute = False
            }

            Dim proc = Process.Start(startInfo)
            Dim output = proc.StandardOutput.ReadToEnd().Trim()
            proc.WaitForExit()

            If Not String.IsNullOrEmpty(output) AndAlso File.Exists(output) Then
                Return output
            End If
        End If

        Return claudeCmd
    End Function

    Private Sub ShowError(message As String)
        Dim dialog = New Window() With {
            .Title = "Error",
            .Width = 400,
            .Height = 200,
            .Content = New StackPanel() With {
                .Margin = New Thickness(20),
                .Children = {
                    New TextBlock() With {
                        .Text = message,
                        .TextWrapping = TextWrapping.Wrap
                    },
                    New Button() With {
                        .Content = "OK",
                        .HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        .Margin = New Thickness(0, 20, 0, 0)
                    }
                }
            }
        }

        Dim okButton = CType(CType(dialog.Content, StackPanel).Children(1), Button)
        AddHandler okButton.Click, Sub(s, e) dialog.Close()

        dialog.ShowDialog(Me)
    End Sub

    ' Event handlers for ComboBox and TextBox synchronization
    Private Sub ApiKeyComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        ApiKeyTextBox.Text = ApiKeyComboBox.Text
        UpdateEnvironmentPreview(sender, e)
    End Sub

    Private Sub ApiKeyComboBox_TextChanged(sender As Object, e As TextChangedEventArgs)
        ApiKeyTextBox.Text = ApiKeyComboBox.Text
        UpdateEnvironmentPreview(sender, e)
    End Sub

    Private Sub ApiKeyTextBox_TextChanged(sender As Object, e As TextChangedEventArgs)
        ApiKeyComboBox.Text = ApiKeyTextBox.Text
        UpdateEnvironmentPreview(sender, e)
    End Sub

    Private Sub BaseUrlComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        BaseUrlTextBox.Text = BaseUrlComboBox.Text
        UpdateEnvironmentPreview(sender, e)
    End Sub

    Private Sub BaseUrlComboBox_TextChanged(sender As Object, e As TextChangedEventArgs)
        BaseUrlTextBox.Text = BaseUrlComboBox.Text
        UpdateEnvironmentPreview(sender, e)
    End Sub

    Private Sub BaseUrlTextBox_TextChanged(sender As Object, e As TextChangedEventArgs)
        BaseUrlComboBox.Text = BaseUrlTextBox.Text
        UpdateEnvironmentPreview(sender, e)
    End Sub

    Private Sub ModelComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        ModelTextBox.Text = ModelComboBox.Text
        UpdateEnvironmentPreview(sender, e)
    End Sub

    Private Sub ModelComboBox_TextChanged(sender As Object, e As TextChangedEventArgs)
        ModelTextBox.Text = ModelComboBox.Text
        UpdateEnvironmentPreview(sender, e)
    End Sub

    Private Sub ModelTextBox_TextChanged(sender As Object, e As TextChangedEventArgs)
        ModelComboBox.Text = ModelTextBox.Text
        UpdateEnvironmentPreview(sender, e)
    End Sub

    Private Sub BrowseApiKeyButton_Click(sender As Object, e As RoutedEventArgs)
        ' Browse for API key file
        Dim dialog = New OpenFileDialog() With {
            .Title = "Select API Key File",
            .Filters = New List(Of FileDialogFilter) From {
                New FileDialogFilter() With {.Name = "Text Files", .Extensions = New List(Of String) From {"txt"}},
                New FileDialogFilter() With {.Name = "All Files", .Extensions = New List(Of String) From {"*"}}
            },
            .AllowMultiple = False
        }

        Dim result = dialog.ShowAsync(Me).Result
        If result IsNot Nothing AndAlso result.Length > 0 Then
            Try
                Dim apiKey = File.ReadAllText(result(0)).Trim()
                ApiKeyComboBox.Text = apiKey
                ApiKeyTextBox.Text = apiKey
            Catch ex As Exception
                ShowError($"Failed to read API key file: {ex.Message}")
            End Try
        End If
    End Sub

    Private Sub BrowseBaseUrlButton_Click(sender As Object, e As RoutedEventArgs)
        ' Browse for base URL configuration file
        Dim dialog = New OpenFileDialog() With {
            .Title = "Select Base URL Configuration File",
            .Filters = New List(Of FileDialogFilter) From {
                New FileDialogFilter() With {.Name = "Text Files", .Extensions = New List(Of String) From {"txt"}},
                New FileDialogFilter() With {.Name = "All Files", .Extensions = New List(Of String) From {"*"}}
            },
            .AllowMultiple = False
        }

        Dim result = dialog.ShowAsync(Me).Result
        If result IsNot Nothing AndAlso result.Length > 0 Then
            Try
                Dim baseUrl = File.ReadAllText(result(0)).Trim()
                BaseUrlComboBox.Text = baseUrl
                BaseUrlTextBox.Text = baseUrl
            Catch ex As Exception
                ShowError($"Failed to read base URL file: {ex.Message}")
            End Try
        End If
    End Sub
End Class
