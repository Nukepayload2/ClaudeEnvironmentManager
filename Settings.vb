Imports System.IO

Public NotInheritable Class Settings
    Private Const SettingsFileName As String = "ClaudeEnvironmentManager.config"
    Private Const DefaultModels As String = "Pro/moonshotai/Kimi-K2-Instruct,moonshotai/Kimi-K2-Instruct,kimi-latest,kimi-k2-0711-preview"

    Public Property AnthropicApiKey As String
    Public Property AnthropicBaseUrl As String
    Public Property AnthropicModel As String
    Public Property LastSelectedFolder As String
    Public Property ModelList As New List(Of String)

    Public Sub New()
        ' Initialize with empty strings
        AnthropicApiKey = String.Empty
        AnthropicBaseUrl = String.Empty
        AnthropicModel = String.Empty
        LastSelectedFolder = String.Empty
    End Sub

    Public Sub Save()
        Dim settingsXml =
            <Settings>
                <AnthropicApiKey><%= AnthropicApiKey %></AnthropicApiKey>
                <AnthropicBaseUrl><%= AnthropicBaseUrl %></AnthropicBaseUrl>
                <AnthropicModel><%= AnthropicModel %></AnthropicModel>
                <LastSelectedFolder><%= LastSelectedFolder %></LastSelectedFolder>
                <ModelList><%= String.Join(",", ModelList) %></ModelList>
            </Settings>

        File.WriteAllText(SettingsFileName, settingsXml.ToString())
    End Sub

    Public Shared Function Load() As Settings
        If Not File.Exists(SettingsFileName) Then
            Dim settings = New Settings With {
                .ModelList = GetDefaultModelList()
            }
            settings.Save()
            Return settings
        End If

        Try
            Dim xml = XDocument.Load(SettingsFileName)
            Dim settingsElement = xml.Root
            Dim modelList = settingsElement.<ModelList>.Value
            Return New Settings() With {
                .AnthropicApiKey = If(settingsElement.<AnthropicApiKey>.Value, String.Empty),
                .AnthropicBaseUrl = If(settingsElement.<AnthropicBaseUrl>.Value, String.Empty),
                .AnthropicModel = If(settingsElement.<AnthropicModel>.Value, String.Empty),
                .LastSelectedFolder = If(settingsElement.<LastSelectedFolder>.Value, String.Empty),
                .ModelList = If(modelList <> Nothing, modelList.Split(","c).ToList(), GetDefaultModelList())
            }
        Catch
            Return New Settings()
        End Try
    End Function

    Private Shared Function GetDefaultModelList() As List(Of String)
        Return DefaultModels.Split(","c).ToList()
    End Function
End Class
