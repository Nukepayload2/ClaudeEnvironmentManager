Imports System.IO
Imports System.Xml.Linq

Public NotInheritable Class Settings
    Private Const SettingsFileName As String = "ClaudeEnvironmentManager.settings"

    Public Property AnthropicApiKey As String
    Public Property AnthropicBaseUrl As String
    Public Property AnthropicModel As String
    Public Property LastSelectedFolder As String

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
            </Settings>

        File.WriteAllText(SettingsFileName, settingsXml.ToString())
    End Sub

    Public Shared Function Load() As Settings
        If Not File.Exists(SettingsFileName) Then
            Return New Settings()
        End If

        Try
            Dim xml = XDocument.Load(SettingsFileName)
            Dim settingsElement = xml.Root

            Return New Settings() With {
                .AnthropicApiKey = If(settingsElement.Element("AnthropicApiKey")?.Value, String.Empty),
                .AnthropicBaseUrl = If(settingsElement.Element("AnthropicBaseUrl")?.Value, String.Empty),
                .AnthropicModel = If(settingsElement.Element("AnthropicModel")?.Value, String.Empty),
                .LastSelectedFolder = If(settingsElement.Element("LastSelectedFolder")?.Value, String.Empty)
            }
        Catch
            Return New Settings()
        End Try
    End Function
End Class
