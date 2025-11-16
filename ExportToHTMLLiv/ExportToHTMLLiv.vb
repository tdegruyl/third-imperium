'These aren't usually required Imports within Visual Studio, but have to be included
'here now because the plugin compiler doesn't make these associations automatically.
Imports Microsoft.VisualBasic
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.IO
Imports System.IO.Path
Imports System.Web

'Everything from here and below is normal code and Imports and such, just as it is
'when developing within Visual Studio for VB projects.
Imports GCA5Engine
Imports System.Drawing
Imports System.Reflection

'Any such DLL needs to add References to:
'
'   GCA5Engine
'   GCA5.Interfaces.DLL
'   System.Drawing  (System.Drawing; v4.X) 'for colors and anything drawaing related.
'
'in order to work as a print sheet.
'
Public Class ExportToHTMLLiv
    Implements GCA5.Interfaces.IExportSheet

    Public Event RequestRunSpecificOptions(sender As GCA5.Interfaces.IExportSheet, e As GCA5.Interfaces.DialogOptions_RequestedOptions) Implements GCA5.Interfaces.IExportSheet.RequestRunSpecificOptions

    Private MyOptions As GCA5Engine.SheetOptionsManager
    Private OwnedItemText As String = "* = item is owned by another, its point value and/or cost is included in the other item."
    Private ShowOwnedMessage As Boolean
    ''' <summary>
    ''' If multiple Characters might be printed, this is the value of the Always Ask Me option
    ''' </summary>
    ''' <remarks></remarks>
    Private AlwaysAskMe As Integer

    '******************************************************************************************
    '* All Interface Implementations
    '******************************************************************************************
    Public Sub CreateOptions(Options As GCA5Engine.SheetOptionsManager) Implements GCA5.Interfaces.IExportSheet.CreateOptions
        'This is the routine where all the Options we want to use are created,
        'and where the UI for the Preferences dialog is filled out.
        '
        'This is equivalent to CharacterSheetOptions from previous implementations

        Dim ok As Boolean
        Dim newOption As GCA5Engine.SheetOption

        Dim descFormat As New SheetOptionDisplayFormat
        descFormat.BackColor = SystemColors.Info
        descFormat.CaptionLocalBackColor = SystemColors.Info

        '* Description block at top *
        newOption = New GCA5Engine.SheetOption
        newOption.Name = "Header_Description"
        newOption.Type = GCA5Engine.OptionType.Header
        newOption.UserPrompt = PluginName() & " " & PluginVersion()
        newOption.DisplayFormat = descFormat
        ok = Options.AddOption(newOption)

        newOption = New GCA5Engine.SheetOption
        newOption.Name = "Description"
        newOption.Type = GCA5Engine.OptionType.Caption
        newOption.UserPrompt = PluginDescription()
        newOption.DisplayFormat = descFormat
        ok = Options.AddOption(newOption)

        '******************************
        '* Included Sections 
        '******************************
        newOption = New GCA5Engine.SheetOption
        newOption.Name = "Header_Traits"
        newOption.Type = GCA5Engine.OptionType.Header
        newOption.UserPrompt = "Traits"
        ok = Options.AddOption(newOption)

        newOption = New GCA5Engine.SheetOption
        newOption.Name = "Primary_Attributes"
        newOption.Type = GCA5Engine.OptionType.Text
        newOption.UserPrompt = "Primary Attributes to Include"
        newOption.DefaultValue = "ST,DX,IQ,HT"
        ok = Options.AddOption(newOption)

        newOption = New GCA5Engine.SheetOption
        newOption.Name = "IncludeThrustSwing"
        newOption.Type = GCA5Engine.OptionType.YesNo
        newOption.UserPrompt = "Include Thrust and Swing?"
        newOption.DefaultValue = true
        ok = Options.AddOption(newOption)

        newOption = New GCA5Engine.SheetOption
        newOption.Name = "Secondary_Characteristics"
        newOption.Type = GCA5Engine.OptionType.Text
        newOption.UserPrompt = "Secondary Characteristics to Include"
        newOption.DefaultValue = "Will, Perception, Basic Speed, Basic Move"
        ok = Options.AddOption(newOption)

        newOption = New GCA5Engine.SheetOption
        newOption.Name = "Pools"
        newOption.Type = GCA5Engine.OptionType.Text
        newOption.UserPrompt = "Pools to Include"
        newOption.DefaultValue = "Hit Points, Fatigue Points"
        ok = Options.AddOption(newOption)

        newOption = New GCA5Engine.SheetOption
        newOption.Name = "Header_Injury"
        newOption.Type = GCA5Engine.OptionType.Header
        newOption.UserPrompt = "Injury and Control"
        ok = Options.AddOption(newOption)

        newOption = New GCA5Engine.SheetOption
        newOption.Name = "UseHPOrConditionalInjury"
        newOption.Type = GCA5Engine.OptionType.ListNumber
        newOption.UserPrompt = "How do you want to track injury?"
        newOption.DefaultValue = 0 'first item
        newOption.List = {"Use HP", "Use Conditional Injury (Pyramid 3-120)", "Use Conditional Injury (Mission X)"}
        ok = Options.AddOption(newOption)


        newOption = New GCA5Engine.SheetOption
        newOption.Name = "UseHPOrConditionalInjury"
        newOption.Type = GCA5Engine.OptionType.ListNumber
        newOption.UserPrompt = "How do you want to track injury?"
        newOption.DefaultValue = 0 'first item
        newOption.List = {"Use HP", "Use Conditional Injury (Pyramid 3-120)", "Use Conditional Injury (Mission X)"}
        ok = Options.AddOption(newOption)

        newOption = New GCA5Engine.SheetOption
        newOption.Name = "UseControlPointsOrControlSeverity"
        newOption.Type = GCA5Engine.OptionType.ListNumber
        newOption.UserPrompt = "How do you want to track injury?"
        newOption.DefaultValue = 0 'first item
        newOption.List = {"Do Nothing", "Use Control Points (Fantastic Dungeon Grappling)", "Use Control Severity (Mission X)"}
        ok = Options.AddOption(newOption)
        
        newOption = New GCA5Engine.SheetOption
        newOption.Name = "Header_ItemNotes"
        newOption.Type = GCA5Engine.OptionType.Header
        newOption.UserPrompt = "Item Notes"
        ok = Options.AddOption(newOption)

        newOption = New GCA5Engine.SheetOption
        newOption.Name = "NotesIncludeDescription"
        newOption.Type = GCA5Engine.OptionType.YesNo
        newOption.UserPrompt = "Include a trait's Description in the User Notes and VTT Notes block."
        newOption.DefaultValue = True
        ok = Options.AddOption(newOption)

    End Sub

    Public Sub UpgradeOptions(Options As GCA5Engine.SheetOptionsManager) Implements GCA5.Interfaces.IExportSheet.UpgradeOptions
        'This is called only when a particular plug-in is loaded the first time,
        'and before SetOptions.

        'I don't do anything with this.
    End Sub


    Public Function PreviewOptions(Options As GCA5Engine.SheetOptionsManager) As Boolean Implements GCA5.Interfaces.IExportSheet.PreviewOptions
        'This is called after options are loaded, but before SupportedFileTypeFilter and PreferredFilterIndex are called,
        'to allow for certain specialty sheets to do a little housekeeping if desired.

        'I dont do anything with this.

        'Be sure to return True to avoid the export process being canceled!
        Return True
    End Function

    Public Function PluginName() As String Implements GCA5.Interfaces.IExportSheet.PluginName
        Return "Export To HTML (Liv)"
    End Function
    Public Function PluginDescription() As String Implements GCA5.Interfaces.IExportSheet.PluginDescription
        Return "Exports the currently selected character to an HTML file."
    End Function
    Public Function PluginVersion() As String Implements GCA5.Interfaces.IExportSheet.PluginVersion
        Return AutoFindVersion()
    End Function

    Public Function PreferredFilterIndex() As Integer Implements GCA5.Interfaces.IExportSheet.PreferredFilterIndex
        'Only returns one filter type, so we just use that. Remember this is 0-based!
        Return 0
    End Function

    Public Function SupportedFileTypeFilter() As String Implements GCA5.Interfaces.IExportSheet.SupportedFileTypeFilter
        Return "HTML files (*.html)|*.html"
    End Function

    Public Function GenerateExport(Party As GCA5Engine.Party, TargetFilename As String, Options As GCA5Engine.SheetOptionsManager) As Boolean Implements GCA5.Interfaces.IExportSheet.GenerateExport
        Dim fw As New FileWriter
        MyOptions = Options
        ' Creates a string buffer for the file, but doesn't actually open and
        ' write it until FileClose is called.
        fw.FileOpen(TargetFilename)
        ExportToHTMLLiv(Party.Current, fw)

        'Save all we've written to the file and quit.
        Try
            fw.FileClose()
        Catch ex As Exception
            'problem encountered
            Notify(PluginName() & ": " & Err.Number & ": " & ex.Message & _
                vbCrLf & "Stack Trace: " & vbCrLf & ex.StackTrace, Priority.Red)
            Return False
        End Try

        'all good
        Return True
    End Function

'******************************************************************************************
'* All Internal Routines
'******************************************************************************************
    Public Function AutoFindVersion() As String
        Dim longFormVersion As String = ""

        Dim currentDomain As AppDomain = AppDomain.CurrentDomain
        'Provide the current application domain evidence for the assembly.
        'Load the assembly from the application directory using a simple name.
        currentDomain.Load("ExportToHTMLLiv")

        'Make an array for the list of assemblies.
        Dim assems As [Assembly]() = currentDomain.GetAssemblies()

        'List the assemblies in the current application domain.
        'Echo("List of assemblies loaded in current appdomain:")
        Dim assem As [Assembly]
        'Dim co As New ArrayList
        For Each assem In assems
            If assem.FullName.StartsWith("ExportToHTMLLiv") Then
                Dim parts(0) As String
                parts = assem.FullName.Split(",")
                'name and version are the first two parts
                longFormVersion = parts(1)
                'Version=1.2.3.4
                parts = longFormVersion.Split("=")
                Return parts(1)
            End If
        Next assem

        Return longFormVersion
    End Function

'****************************************
'* This is where the export to HTML happens. :)
'****************************************
    Private Sub ExportToHTMLLiv(CurChar As GCACharacter, fw As FileWriter)
        ExportHTMLHead(CurChar, fw)
        ExportCharinfoCard(CurChar, fw)
        ExportAttributesCard(CurChar, fw)
        fw.Paragraph("<div class=""navigation"">")
        fw.Paragraph("<button class=""tablinks"" onclick=""openTab(event,'skills-tab')"">Traits and Skills</button>")
        fw.Paragraph("<button class=""tablinks"" onclick=""openTab(event,'combat-tab')"">Combat</button>")
        fw.Paragraph("<button class=""tablinks"" onclick=""openTab(event,'equipment-tab')"">Equipment</button>")
        fw.Paragraph("<button class=""tablinks"" onclick=""openTab(event,'social-tab')"">Social</button>")
        fw.Paragraph("</div>")
        ExportSkillsTab(CurChar, fw)
        ExportEquipmentTab(CurChar, fw)
        ExportCombatTab(CurChar, fw)
        ExportSocialTab(CurChar, fw)

        ExportHTMLFoot(CurChar,fw)
    End Sub

    Private Sub ExportCharinfoCard(CurChar as GCACharacter, fw as FileWriter)
        fw.Paragraph("<div class=""charinfo-card"">")
        ExportDescription(CurChar, fw)
        ExportPointSummary(CurChar, fw)
        fw.Paragraph("</div>")
    End Sub

    Private Sub ExportAttributesCard(CurChar as GCACharacter, fw as FileWriter)
        fw.Paragraph("<div class=""attributes-card"">")
        ExportAttributes(CurChar, fw)
        fw.Paragraph("</div>")
    End Sub

    Private Sub ExportSkillsTab(CurChar as GCACharacter, fw as FileWriter)
        fw.Paragraph("<div id=""skills-tab"" class=""tab"">")
        ExportSkills(CurChar, fw)
        ExportTraits(CurChar, fw)
        ExportSpells(CurChar, fw)
        fw.Paragraph("</div>")
    End Sub

    Private Sub ExportSocialTab(CurChar as GCACharacter, fw as FileWriter)
        fw.Paragraph("<div id=""social-tab"" class=""tab"">")
        ExportLanguages(CurChar, fw)
        ExportCulturalFamiliarity(CurChar, fw)
        ExportReactionModifiers(CurChar, fw)
        ExportNotes(CurChar, fw)
        fw.Paragraph("</div>")
    End Sub

    Private Sub ExportCombatTab(CurChar as GCACharacter, fw as FileWriter)
        fw.Paragraph("<div id=""combat-tab"" class=""tab"">")
        ExportMeleeAttacks(CurChar, fw)
        ExportRangedAttacks(CurChar, fw)
        ExportProtection(CurChar, fw)
        ExportSSRT(CurChar,fw)
        fw.Paragraph("</div>")
    End Sub

    Private Sub ExportEquipmentTab(CurChar as GCACharacter, fw as FileWriter)
        fw.Paragraph("<div id=""equipment-tab"" class=""tab"">")
        ExportEquipment(CurChar, fw)
        ExportLift(CurChar, fw)
        ExportEncumbrance(CurChar, fw)
        fw.Paragraph("</div>")
    End Sub


    Private Sub ExportHTMLHead(CurChar as GCACharacter, fw as FileWriter)
        fw.Paragraph("<!DOCTYPE html>")
        fw.Paragraph("<html lang=""en"">")
        fw.Paragraph("<head>")
        fw.Paragraph("<meta charset=""utf-8""/>")
        fw.Paragraph("<title>" & CurChar.Name & "</title>")
        fw.Paragraph("<link rel=""preconnect"" href=""https://fonts.googleapis.com""/>")
        fw.Paragraph("<link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin />")
        fw.Paragraph("<link href=""https://fonts.googleapis.com/css2?family=Patrick+Hand+SC&display=swap"" rel=""stylesheet"">")
        fw.Paragraph("<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />")
        fw.Paragraph("<style>")
        fw.Paragraph(".navigation { grid-area: navigation; border: thin solid #ccc; background-color:#f1f1f1;}")
        fw.Paragraph(".navigation button { background-color: inherit; float: left; border: none; outline: none; cursor: pointer; padding: 14px 16px; transition: 0.3s; }")
        fw.Paragraph(".navigation button:hover { background-color: #ddd; }")
        fw.Paragraph(".navigation button.active { background-color: #ccc; }" )
        fw.Paragraph(".qty1 { /* visibility: hidden; */ }")
        fw.Paragraph(".title { ")
        fw.Paragraph("    font-weight: bold;")
        fw.Paragraph("    text-align: right;")
        fw.Paragraph("}")
        fw.Paragraph(".field {")
        fw.Paragraph("    padding-left: 3px;")
        fw.Paragraph("    padding-right: 3px;")
        fw.Paragraph("    color: #702963;")
        fw.Paragraph("    font-family: ""Patrick Hand SC"", cursive;")
        fw.Paragraph("    font-weight: 400;")
        fw.Paragraph("    font-style: normal;")
        fw.Paragraph("    font-size:100%;")
        fw.Paragraph("}")
        fw.Paragraph(".underlined {")
        fw.Paragraph(" /*")
        fw.Paragraph("    border-bottom: thin solid black;")
        fw.Paragraph("*/")
        fw.Paragraph("}")
        fw.Paragraph("th { ")
        fw.Paragraph("    text-align: left;")
        fw.Paragraph("    text-decoration: underline;")
        fw.Paragraph("    font-weight:bold;")
        fw.Paragraph("}")
        fw.Paragraph(".tab { display: none; animation: fadeEffect 1s;}")
        fw.Paragraph(".tab div {")
        fw.Paragraph("    box-decoration-break: clone;")
        fw.Paragraph("    -webkit-box-decoration-break: clone;")
        fw.Paragraph("    break-inside: avoid;")
        fw.Paragraph("}")

        fw.Paragraph("#equipment-tab {")
        fw.Paragraph("    display:grid;")
        fw.Paragraph("    grid-area: equipment-tab;")
        fw.Paragraph("    grid-template-columns: 70% 30%;")
        fw.Paragraph("    grid-template-rows: auto;")
        fw.Paragraph("    gap: 3px 3px;")
        fw.Paragraph("    grid-auto-flow: column;")
        fw.Paragraph("    grid-template-areas:")
        fw.Paragraph("      ""carried-equipment lift""")
        fw.Paragraph("      ""carried-equipment encumbrance"";")
        fw.Paragraph("}")

        fw.Paragraph("#combat-tab {")
        fw.Paragraph("    display:grid;")
        fw.Paragraph("    grid-template-rows: auto;")
        fw.Paragraph("    gap: 3px 3px;")
        fw.Paragraph("    grid-auto-flow: column;")
        fw.Paragraph("    grid-area: combat-tab;")
        fw.Paragraph("    grid-template-columns: 75% 25%;")
        fw.Paragraph("    grid-template-areas:")
        fw.Paragraph("      ""melee hit-locations""")
        fw.Paragraph("      ""ranged hit-locations""")
        fw.Paragraph("      ""ssrt ssrt"";")
        fw.Paragraph("}")

        fw.Paragraph("#social-tab {")
        fw.Paragraph("    display:grid;")
        fw.Paragraph("    grid-template-rows: auto;")
        fw.Paragraph("    gap: 3px 3px;")
        fw.Paragraph("    grid-auto-flow: column;")
        fw.Paragraph("    grid-area: social-tab;")
        fw.Paragraph("    grid-template-columns: 25% 50% 25%;")
        fw.Paragraph("    grid-template-areas:")
        fw.Paragraph("      ""languages notes reactions""")
        fw.Paragraph("      ""cultures notes reactions"";")
        fw.Paragraph("}")

        fw.Paragraph(".attributes-card {")
        fw.Paragraph("    grid-area: attributes-card;")
        fw.Paragraph("}")

        fw.Paragraph(".charinfo-card {")
        fw.Paragraph("    display:grid;")
        fw.Paragraph("    grid-area: charinfo-card;")
        fw.Paragraph("    grid-template-columns: 2fr 3fr 1fr;")
        fw.Paragraph("    grid-template-rows: auto;")
        fw.Paragraph("    gap: 3px 3px;")
        fw.Paragraph("    grid-auto-flow: column;")
        fw.Paragraph("    grid-template-areas:")
        fw.Paragraph("      ""char-portrait-field charinfo point-summary"";")
        fw.Paragraph("}")

        fw.Paragraph("#skills-tab {")
        fw.Paragraph("    display:grid;")
        fw.Paragraph("    grid-template-rows: auto;")
        fw.Paragraph("    gap: 3px 3px;")
        fw.Paragraph("    grid-auto-flow: column;")
        fw.Paragraph("    grid-area: skills-tab;")
        fw.Paragraph("    grid-template-columns: 50% 50%;")
        fw.Paragraph("    grid-template-areas:")
        fw.Paragraph("      ""traits skills""")
        If CurChar.Count(Spells) <> 0 Then
            fw.Paragraph("      ""traits spells""")
        End If
        fw.Paragraph("      ;")
        fw.Paragraph("}")

        fw.Paragraph("body {")
        fw.Paragraph("    padding: 1em;")
        fw.Paragraph("    display: grid;")
        fw.Paragraph("    min-width: 700px; ")
        fw.Paragraph("    font-size:10pt;")
        fw.Paragraph("    font-family: verdana;")
        fw.Paragraph("    grid-template-columns: 100%;")
        fw.Paragraph("    grid-template-rows: auto;")
        fw.Paragraph("    gap: 3px 3px;")
        fw.Paragraph("    grid-auto-flow: column;")
        fw.Paragraph("    grid-template-areas:")
        fw.Paragraph("        ""charinfo-card""")
        fw.Paragraph("        ""attributes-card""")
        fw.Paragraph("        ""navigation""")
        fw.Paragraph("        ""skills-tab""")
        fw.Paragraph("        ""combat-tab""")
        fw.Paragraph("        ""equipment-tab""")
        fw.Paragraph("        ""social-tab"";")
        fw.Paragraph("}")
        If CurChar.Count(Spells) = 0 Then
            fw.Paragraph(".spells {display:none; visibility:hidden;}")
        End If
        fw.Paragraph(".attributes {  ")
        fw.Paragraph("    grid-area: attributes;")
        fw.Paragraph("    display: grid;")
        fw.Paragraph("    grid-template-columns: 2fr 3fr 4fr 2fr; ")
        fw.Paragraph("    grid-template-rows: auto;")
        fw.Paragraph("    grid-auto-flow: column;")
        fw.Paragraph("    grid-template-areas:")
        fw.Paragraph("        ""primary-attributes secondary-attributes pools defense""")
        fw.Paragraph("}")
        fw.Paragraph(".charinfo {  ")
        fw.Paragraph("    grid-area: charinfo;")
        fw.Paragraph("    font-size: 100%;")
        fw.Paragraph("    display: grid;")
        fw.Paragraph("    grid-template-columns: 1fr 3fr 1fr 3fr 1fr 3fr; ")
        fw.Paragraph("    grid-template-rows: auto;")
        fw.Paragraph("    gap: 1px 1px;")
        fw.Paragraph("    grid-auto-flow: column;")
        fw.Paragraph("    grid-template-areas:")
        fw.Paragraph("          ""char-name-field char-name-field char-name-field player player-field player-field""")
        fw.Paragraph("          ""height height-field weight weight-field age age-field""")
        fw.Paragraph("          ""ancestry ancestry-field tl tl-field size size-field """)
        fw.Paragraph("          ""appearance appearance-field appearance-field appearance-field appearance-field appearance-field""")
        fw.Paragraph("}")
        fw.Paragraph(".char-portrait-field {grid-area: char-portrait-field; font-size: 20pt; text-align: center;}")
        fw.Paragraph(".portrait {max-height: 200px;}")
        fw.Paragraph(".char-name-field { grid-area: char-name-field; font-size: 200%; text-align: center;}")
        fw.Paragraph(".player-field { grid-area: player-field;  }")
        fw.Paragraph(".player { grid-area: player;}")
        fw.Paragraph(".char-title { grid-area: char-title; }")
        fw.Paragraph(".char-title-field { grid-area: char-title-field;}")
        fw.Paragraph(".org-field {grid-area: org-field; }")
        fw.Paragraph(".org {grid-area: org; }")
        fw.Paragraph(".ancestry-field { grid-area: ancestry-field; }")
        fw.Paragraph(".ancestry { grid-area: ancestry; }")
        fw.Paragraph(".height { grid-area: height; }")
        fw.Paragraph(".height-field { grid-area: height-field;  }")
        fw.Paragraph(".weight { grid-area: weight; }")
        fw.Paragraph(".weight-field { grid-area: weight-field;  }")
        fw.Paragraph(".hair { grid-area: hair; }")
        fw.Paragraph(".hair-field { grid-area: hair-field; }")
        fw.Paragraph(".age-field { grid-area: age-field;  }")
        fw.Paragraph(".age { grid-area: age; }")
        fw.Paragraph(".birthday-field { grid-area: birthday-field; }")
        fw.Paragraph(".birthday { grid-area: birthday; }")
        fw.Paragraph(".eyes-field { grid-area: eyes-field; }")
        fw.Paragraph(".eyes { grid-area: eyes; }")
        fw.Paragraph(".hand-field { grid-area: hand-field; }")
        fw.Paragraph(".hand { grid-area: hand; }")
        fw.Paragraph(".name { grid-area: name; }")
        fw.Paragraph(".size-field { grid-area: size-field;  }")
        fw.Paragraph(".size { grid-area: size; }")
        fw.Paragraph(".skin-field { grid-area: skin-field; }")
        fw.Paragraph(".skin { grid-area: skin; }")
        fw.Paragraph(".appearance-field{grid-area: appearance-field; }")
        fw.Paragraph(".appearance {grid-area: appearance; }")
        fw.Paragraph(".tl-field{grid-area: tl-field; }")
        fw.Paragraph(".tl {grid-area: tl; }")
        fw.Paragraph(".religion {grid-area: religion; }")
        fw.Paragraph(".religion-field {grid-area: religion-field; }")
        fw.Paragraph(".point-summary {grid-area:point-summary;}")
        fw.Paragraph(".primary-attributes {grid-area: primary-attributes; width:100%; font-size: 120%;}")
        fw.Paragraph(".secondary-attributes {grid-area: secondary-attributes; font-size: 120%;  }")
        fw.Paragraph(".pools {grid-area: pools; font-size: 120%;  }")
        fw.Paragraph(".defense {grid-area: defense; font-size: 120%;}")
        fw.Paragraph(".title {vertical-align:middle; }")
        fw.Paragraph(".box {border: 1.5px solid black; padding: 1px;text-align:center; vertical-align: middle;}")
        fw.Paragraph("td.right {text-align:right; vertical-align: middle;}")
        fw.Paragraph(".melee {grid-area: melee;  border: thick solid black; }")
        fw.Paragraph(".ranged {grid-area: ranged;  border: thick solid black;}")
        fw.Paragraph(".carried-equipment { grid-area: carried-equipment;  border: thick solid black;}")
        fw.Paragraph(".other-equipment { grid-area: other-equipment;  border: thick solid black;}")
        fw.Paragraph(".traits {grid-area: traits; min-width: 2.5in; border: thick solid black; }")
        fw.Paragraph(".traits-list { column-count:2; column-gap: 6px; column-rule: 2px solid black; }")
        fw.Paragraph(".skills {grid-area: skills; min-width: 2.5in; border: thick solid black;}")
        fw.Paragraph(".skills-list { column-count:2; column-gap: 6px; column-rule: 2px solid black; }")
        fw.Paragraph(".hanging { display: list-item; list-style-position: inside;}")
        fw.Paragraph(".spells {grid-area: spells; min-width: 2.5in; border: thick solid black;}")
        fw.Paragraph(".spells-list { column-count:1; column-gap: 6px; column-rule: 2px solid black; }")
        fw.Paragraph(".encumbrance {grid-area: encumbrance; font-size: 90%;   border: thick solid black;}")
        fw.Paragraph(".lift {grid-area: lift; font-size: 90%;   border: thick solid black;}")
        fw.Paragraph(".points {font-size:70%; padding-left: 3px; /*display:none;visibility:hidden;*/}")
        fw.Paragraph(".equipment { width: 100%; padding: 1em; border: thick solid black;}")
        fw.Paragraph(".equipment-list { column-count:2; column-gap: 6px; column-rule: 2px solid black; }")
        fw.Paragraph(".reactions { grid-area: reactions; border: thick solid black; }")
        fw.Paragraph(".reactions td { vertical-align: top; } ")
        fw.Paragraph(".cultures { grid-area: cultures; border: thick solid black; }")
        fw.Paragraph(".languages { grid-area: languages; border: thick solid black;}")
        fw.Paragraph(".hit-locations { grid-area: hit-locations; border: thick solid black; }")
        fw.Paragraph(".hit-locations td {text-align: right;}")
        fw.Paragraph(".hit-locations td.field {text-align: left;}")
        fw.Paragraph(".notes {grid-area: notes; border: thick solid black;}")
        fw.Paragraph(".ssrt {grid-area: ssrt; border: thick solid black;}")
        fw.Paragraph(".ssrt td{ text-align: right; }")
        fw.Paragraph("td { padding-left: 1em; padding-right:1em; vertical-align: middle; }")
        fw.Paragraph(".number { text-align: right; }")
        fw.Paragraph(".section-title {text-decoration: none; background-color: black; color: white; padding: 1em; font-size: 120%; font-weight: bold; text-align: center; }")
        fw.Paragraph(".table-title {text-decoration: none; background-color: black; color: white; padding: 1em; font-size: 110%; font-weight: bold; text-align: center; }")
        fw.Paragraph("table { width: 100%; }")
        fw.Paragraph(".alternate tr:nth-child(even) {")
        fw.Paragraph("    background-color: #D3D3D3;")
        fw.Paragraph("}")
        fw.Paragraph(".current {")
        fw.Paragraph("    background-color: #D3D3D3;")
        fw.Paragraph("}")
        fw.Paragraph("#notifications {")
        fw.Paragraph("    color: red;")
        fw.Paragraph("}")
        fw.Paragraph("@keyframes fadeEffect { from {opacity: 0;} to {opacity: 1;}}")
        fw.Paragraph("</style>")
        fw.Paragraph("<style media=""print"" type=""text/css"">")
        fw.Paragraph("        select { display: none !important;}")
        fw.Paragraph("        input { display: none !important;}")
        fw.Paragraph("        .navigation {display: none !important;}")
        fw.Paragraph("        .tab {display: grid !important;break-inside:avoid !important;}")
        fw.Paragraph("</style>")
        fw.Paragraph(" <script>")
        fw.Paragraph(" function openTab(evt, tabName) {")
        fw.Paragraph("   var i, tabcontent, tablinks;")
        fw.Paragraph("   tabcontent = document.getElementsByClassName(""tab"");")
        fw.Paragraph("   for (i = 0; i < tabcontent.length; i++) {")
        fw.Paragraph("     tabcontent[i].style.display = ""none"";")
        fw.Paragraph("   }")
        fw.Paragraph("   tablinks = document.getElementsByClassName(""tablinks"");")
        fw.Paragraph("   for (i = 0; i < tablinks.length; i++) {")
        fw.Paragraph("     tablinks[i].className = tablinks[i].className.replace("" active"", """");")
        fw.Paragraph("   }")
        fw.Paragraph("   document.getElementById(tabName).style.display = ""grid"";")
        fw.Paragraph("   evt.currentTarget.className += "" active"";")
        fw.Paragraph("} ")
        fw.Paragraph("for (i = 1; i < tabcontent.length; i++) {")
        fw.Paragraph("  tabcontent[i].style.display = ""none"";")
        fw.Paragraph("}")
        fw.Paragraph("</script>")
        fw.Paragraph("</head>")
        fw.Paragraph("<body onload=""poolConditionNotifications();openTab(null, 'skills-tab');"">")

    End Sub

    private Sub ExportHTMLFoot(CurChar as GCACharacter, fw as FileWriter)
        fw.Paragraph("</body></html>")
    End Sub

    Private Sub ExportSSRT(CurChar as GCACharacter, fw as FileWriter)
        fw.Paragraph("	    <div class=""ssrt"">")
        fw.Paragraph("		    <h1 class=""section-title"">Range Penalties</h1>")
        fw.Paragraph("		    <table>")
        fw.Paragraph("			    <tr><th>Range</td><td>2</td><td>3</td><td>5</td>")
        fw.Paragraph("				    <td>7</td><td>10</td><td>15</td>")
        fw.Paragraph("				    <td>20</td><td>30</td><td>50</td>")
        fw.Paragraph("				    <td>70</td><td>100</td><td>150</td>")
        fw.Paragraph("			    	    <td>200</td><td>300</td><td>500</td></tr> ")
        fw.Paragraph("			    <tr><th>Penalty</td><td>+0</td><td>-1</td><td>-2</td><td>-3</td>")
        fw.Paragraph("				    <td>-4</td><td>-5</td><td>-6</td><td>-7</td> ")
        fw.Paragraph("				    <td>-8</td><td>-9</td><td>-10</td><td>-11</td>")
        fw.Paragraph("			    	    <td>-12</td><td>-13</td><td>-14</td></tr>")
        fw.Paragraph("		    </table>")
        fw.Paragraph("	    </div>")
        fw.Paragraph("            <script>")
        fw.Paragraph("                const list = document.getElementsByClassName(""qty1"");")
        fw.Paragraph("                for (var item of list) { item.innerHTML = """"; }")
        fw.Paragraph("            </script>")
        fw.Paragraph("</body>")
        fw.Paragraph("</html>")
    End Sub

    Private Sub ExportLift(CurChar as GCACharacter, fw as FileWriter)
        Dim ListLoc As Integer
        Dim lift_types() As String = { "Basic Lift", "One-Handed Lift", "Two-Handed Lift", _
            "Shove/Knock Over", "Carry on Back", "Shift Slightly" }
        fw.Paragraph("<div class=""lift"">")
        fw.Paragraph("<h1 class=""section-title"">Lift and Carry</h1>")
        fw.Paragraph("<table class=""unalternate"">")
        For Each lift_type As String In lift_types
            ListLoc = CurChar.ItemPositionByNameAndExt(lift_type, Stats)
            If ListLoc > 0 Then
                fw.Paragraph("<tr><td class=""title"">" & lift_type & _
                    "</td><td class=""field right"">" & _
                    CurChar.Items(ListLoc).TagItem("score") & "</td></tr>")
            End If
        Next
        fw.Paragraph("</table>")
        fw.Paragraph("</div>")
    End Sub

    Private Sub ExportSkills(CurChar As GCACharacter, fw As FileWriter)

        Dim i As Integer
        Dim j As Integer
        Dim skills_index As Integer
        Dim tmp As String
        Dim relLevel As String
        Dim work As String
        Dim out As String

        fw.Paragraph("            <div class=""skills"">")
        fw.Paragraph("                <h1 class=""section-title"">Skills</h1>")
        fw.Paragraph("                <div class=""skills-list"">")

        For i = 1 To CurChar.Items.Count

            If CurChar.Items(i).ItemType = Skills And CurChar.Items(i).TagItem("hide") = "" Then

                ' it's a skill and not hidden
                skills_index = skills_index + 1
                tmp = CurChar.Items(i).FullNameTL

                If CurChar.Items(i).Mods.Count > 0 Then
                    work = " ("
                    For j = 1 To CurChar.Items(i).Mods.Count
                        If j > 1 Then
                            work = work & "; "
                        End If
                        work = work & CurChar.Items(i).Mods(j).FullName
                        work = work & ", " & CurChar.Items(i).Mods(j).TagItem("value")
                    Next
                    work = work & ")"
                    tmp = tmp & work
                End If

                work = CurChar.Items(i).TagItem("stepoff")
                relLevel = ""
                If work <> "" Then
                    relLevel = work
                    work = CurChar.Items(i).TagItem("step")
                    If work <> "" Then
                        relLevel = relLevel & work
                    Else
                        relLevel = relLevel & "?"
                    End If
                Else
                    relLevel = relLevel & "?+?"
                End If
                out = "<div class=""field hanging"""
                If UserVTTNotes(CurChar.Items(i)) <> "" Or _
                        CurChar.Items(i).TagItem("page") <> "" Then
                    Dim pageTag = CurChar.Items(i).TagItem("page")
                    Dim page As String 
                    If pageTag <> "" Then 
                        page = String.Format(" p. {0}", pageTag)
                    Else
                        page = ""
                    End If
                    out = out & " title=""" & UpdateEscapeChars(tmp) & _
                        "-" & CurChar.Items(i).Level & _
                        ": " & UserVTTNotes(CurChar.Items(i)) & page & """"
                End If 
                out = out & ">" & UpdateEscapeChars(tmp) & " (" & relLevel & ")-" & _ 
                    CurChar.Items(i).Level & "<span class=""points"">[" & _
                    CurChar.Items(i).TagItem("points") & "]</span></div>"
                fw.Paragraph(out)

            End If
        Next
        fw.Paragraph("</div>")
        fw.Paragraph("</div>")

    End Sub


    Private Sub ExportSpells(CurChar As GCACharacter, fw As FileWriter)

        Dim i As Integer
        Dim j As Integer
        Dim spells_index As Integer
        Dim tmp As String
        Dim relLevel As String
        Dim work As String
        Dim out As String


        fw.Paragraph("<div class=""spells""><h1 class=""section-title"">Spells</h1>" & _
                "<div class=""spells-list"">")
        For i = 1 To CurChar.Items.Count

            If CurChar.Items(i).ItemType = Spells And CurChar.Items(i).TagItem("hide") = "" Then

                ' it's a spell and not hidden
                spells_index = spells_index + 1

                tmp = CurChar.Items(i).FullNameTL

                If CurChar.Items(i).Mods.Count > 0 Then
                    work = " ("
                    For j = 1 To CurChar.Items(i).Mods.Count
                        If j > 1 Then
                            work = work & "; "
                        End If
                        work = work & CurChar.Items(i).Mods(j).FullName
                        work = work & ", " & CurChar.Items(i).Mods(j).TagItem("value")
                    Next
                    work = work & ")"
                    tmp = tmp & work
                End If

                work = CurChar.Items(i).TagItem("stepoff")
                relLevel = ""
                If work <> "" Then
                    relLevel = work
                    work = CurChar.Items(i).TagItem("step")
                    If work <> "" Then
                        relLevel = relLevel & work
                    Else
                        relLevel = relLevel & "?"
                    End If
                Else
                    relLevel = relLevel & "?+?"
                End If
                out = "<div class=""field hanging"""
                If UserVTTNotes(CurChar.Items(i)) <> "" Or _
                        CurChar.Items(i).TagItem("page") <> "" Then
                    Dim pageTag = CurChar.Items(i).TagItem("page")
                    Dim page As String 
                    If pageTag <> "" Then 
                        page = String.Format(" p. {0}", pageTag)
                    Else
                        page = ""
                    End If
                    out = out & " title=""" & UpdateEscapeChars(tmp) & _
                        "-" & CurChar.Items(i).Level & _
                        ": " & UserVTTNotes(CurChar.Items(i)) & page & """"
                End If 
                out = out & ">" & UpdateEscapeChars(tmp) & " (" & relLevel & ")-" & _ 
                    CurChar.Items(i).Level & "<span class=""points"">[" & _
                    CurChar.Items(i).TagItem("points") & "]</span></div>"
                fw.Paragraph(out)
            End If
        Next
        fw.Paragraph("</div></div>")
    End Sub


    Private Sub ExportPrimaryAttributes(CurChar As GCACharacter, fw As FileWriter)
        Dim ListLoc As Integer
        Dim primAttrOption = MyOptions.value("Primary_Attributes")
        Dim includeThrustSwing = MyOptions.value("IncludeThrustSwing")
        Dim primaryOptions() As String =primAttrOption.split(","c)
        Dim primaryAttributes As New List(Of String)
        For i As Integer = 0 To primaryOptions.Length -1
            primaryAttributes.Add(primaryOptions(i).Trim)
        Next

        fw.Paragraph("<div class=""primary-attributes"">")
        fw.Paragraph("    <table>")
        For Each primAttr As String In primaryAttributes
            ListLoc = CurChar.ItemPositionByNameAndExt(primAttr, Stats)
            If ListLoc > 0 Then
                fw.Paragraph("        <tr><td class=""title"">" & primAttr & "</td>" & _
                    "<td class=""box field"">" &  CurChar.Items(ListLoc).TagItem("score") & "</td>" & _
                    "<td></td><td class=""points"">[" & CurChar.Items(ListLoc).TagItem("points") & "]</td></tr>")
            End If
        Next

        If includeThrustSwing = true Then
            fw.Paragraph("        <tr><td>&nbsp;</td><td></td><td></td><td></td></tr>")
            fw.Paragraph("        <tr><td class=""title"">Thrust</td><td class=""field"">" & CurChar.BaseTH & "</td><td></td><td></td></tr>")
            fw.Paragraph("        <tr><td class=""title"">Swing</td><td class=""field"">"  & CurChar.BaseSW & "</td><td></td><td></td></tr>")
        End If
        fw.Paragraph("    </table>")
        fw.Paragraph("</div>")

    End Sub

    Private Sub ExportSecondaryAttributes(CurChar as GCACharacter, fw As FileWriter)
        Dim ListLoc as Integer
        Dim callIt as String
        Dim secondaryCharOption = MyOptions.value("Secondary_Characteristics")
        Dim secondaryOptions() As String =secondaryCharOption.split(","c)
        Dim secondaryAttributes As New List(Of String)
        For i As Integer = 0 To secondaryOptions.Length -1
            secondaryAttributes.Add(secondaryOptions(i).Trim)
        Next

        fw.Paragraph("<div class=""secondary-attributes"">")
        fw.Paragraph("<table >")
        For Each secAttr As String In secondaryAttributes
            callIt = secAttr
            ListLoc = CurChar.ItemPositionByNameAndExt(secAttr, Stats)
            If CurChar.Items(ListLoc).TagItem("symbol") <> "" Then
                callIt = CurChar.Items(ListLoc).TagItem("symbol")
            End If
            fw.Paragraph("<tr id=""" & replaceSpaces(secAttr) & """>" & _
                "<td class=""title"">" & callIt & "</td>" & _
                "<td class=""box field"">" & _
                CurChar.Items(ListLoc).TagItem("score") & "</td>" & _
                "<td></td><td class=""points"">[" & _
                CurChar.Items(ListLoc).TagItem("points") & "]</td>" & _
                "<script> let " & replaceSpaces(secAttr) & " =  " & _
                CurChar.Items(ListLoc).TagItem("score") & "; </script></tr>")
        Next
        fw.Paragraph("<tr><td>&nbsp;</td><td></td><td></td><!-- <td></td>--></tr>")
        fw.Paragraph("        </table>")
        fw.Paragraph("    </div>")
    End Sub

    Private Function replaceSpaces( inputString As String) As String
        Dim tmp = Trim(inputString)
        tmp = Replace(tmp, " ", "_")
        return tmp
    End Function

    Private Function ControlCounter(CurChar as GCACharacter) as String
        Dim out As String = ""

        If MyOptions.Value("UseControlPointsOrControlSeverity") = 1 Then ' use CP
            out = "<input class=""noprint"" id=""cur_CP""  type=""number"" " & _
                "name=""Current Control Points"" value=""0"" min=""0"" " & _
                "max=""200"" onchange=""poolConditionNotifications()"">"
        Else If MyOptions.Value("UseControlPointsOrControlSeverity") = 2 Then ' use Severity
            out = "<select class=""noprint"" id=""cur_Control"" name=""Current Control"" " & _
                "value=""-7"" " & _
                "onchange=""poolConditionNotifications()"">" & _
                "<option value=""0"">0: Not Grappled</option>" & _
                "<option value=""1"">1: Grappled</option>" & _
                "<option value=""2"">2: Grappled</option>" & _
                "<option value=""3"">3: Grappled</option>" & _
                "<option value=""4"">4: Grappled</option>" & _
                "<option value=""5"">5: Grappled</option>" & _
                "<option value=""6"">6: Grappled</option>" & _
                "<option value=""7"">7: Grappled</option>" & _
                "<option value=""8"">8: Grappled</option>" & _
                "<option value=""9"">9: Grappled</option>" & _
                "<option value=""10"">10: Grappled</option>" & _
                "</select>"
        End If
        return out
    End Function

    Private Function InjuryCounter(CurChar as GCACharacter) as String
        Dim out As String = ""
        Dim offset As Integer =0
        If MyOptions.Value("UseHPOrConditionalInjury") = 0 Then
            return ""
        Else If MyOptions.Value("UseHPOrConditionalInjury") = 1 Then ' Pyramid CI
            offset = 0
        Else If MyOptions.Value("UseHPOrConditionalInjury") = 2 Then ' Mission X CI
            offset = 7
        End If
        
        out = "<select class=""noprint"" id=""cur_Injury"" name=""Current Injury"" " & _
            "value=""-7"" " & _
            "onchange=""poolConditionNotifications()"">" & _
            "<option value=""-7"">" & (-7 + offset) & ": None</option>" & _
            "<option value=""-6"">" & (-6 + offset) & ": Scratch</option>" & _
            "<option value=""-5"">" & (-5 + offset) & ": Minor Wound</option>" & _
            "<option value=""-4"">" & (-4 + offset) & ": Minor Wound</option>" & _
            "<option value=""-3"">" & (-3 + offset) & ": Minor Wound</option>" & _
            "<option value=""-2"">" & (-2 + offset) & ": Major Wound</option>" & _
            "<option value=""-1"">" & (-1 + offset) & ": Reeling</option>" & _
            "<option value=""0"">" & (0 + offset) & ": Crippled</option>" & _
            "<option value=""1"">" & (1 + offset) & ": Crippled</option>" & _
            "<option value=""2"">" & (2 + offset) & ": Mortal Wound</option>" & _
            "<option value=""3"">" & (3 + offset) & ": Mortal Wound</option>" & _
            "<option value=""4"">" & (4 + offset) & ": Instantly Fatal</option>" & _
            "<option value=""5"">" & (5 + offset) & ": Instantly Fatal</option>" & _
            "<option value=""6"">" & (6 + offset) & ": Destruction</option>" & _
            "</select>"
        return out
    End Function

    Private Sub ExportPools(CurChar as GCACharacter, fw As FileWriter)
        Dim ListLoc as Integer
        Dim callIt as String
        Dim poolsOption = MyOptions.value("Pools")
        Dim poolsOptions() As String = poolsOption.split(","c)
        Dim pools As New List(Of String)
        If MyOptions.Value("UseHPOrConditionalInjury") = 0 Then
            pools.Add("Hit Points")
        End If
        For i As Integer = 0 To poolsOptions.Length -1
            If poolsOptions(i).Trim = "Hit Points" Then
                Continue For
            End If
            pools.Add(poolsOptions(i).Trim)
        Next

        fw.Paragraph("<div class=""pools"">")
        fw.Paragraph("<table >")
        If MyOptions.Value("UseHPOrConditionalInjury") > 0 Then
            fw.Paragraph("<tr><td class=""title"">Injury Severity</td>" & _
                "<td colspan=""3"" class=""box field"" style=""min-width:1em;"">" & _
                InjuryCounter(CurChar) & "</td></tr>")
        End If
        fw.Paragraph("<tr><td class=""title"">Control</td>" & _
            "<td colspan=""3"" class=""box field"" style=""min-width:1em;"">" & _
            ControlCounter(CurChar) & "</td><tr>")
        For Each pool_name As String In pools
            Dim symbol As String
            Dim score As Integer
            callIt = pool_name
            ListLoc = CurChar.ItemPositionByNameAndExt(pool_name, Stats)
            If CurChar.Items(ListLoc).TagItem("symbol") <> "" Then
                callIt = CurChar.Items(ListLoc).TagItem("symbol")
            End If
            symbol = replaceSpaces(callIt)
            score = CurChar.Items(ListLoc).TagItem("score")
            fw.Paragraph("<tr id=""" & pool_name & """>" & _
                "<td class=""title"">" & callIt & "</td>" & _
                "<td class=""box field"">" & score & "</td>" & _
                "<td class=""box field"" style=""min-width:1em;"">" & _
                "<input type=""number"" max=""" & score & _
                """ id=""cur_" & symbol & """ min=""0"" " & _
                " name=""Current " & callIt & """" & _
                """ onchange=""poolConditionNotifications()"" size=""3""></td>" & _
                "<td class=""points"">[" & _
                CurChar.Items(ListLoc).TagItem("points") & "]</td></tr>")
        Next
        fw.Paragraph("<tr><td colspan=""3""><ul id=""notifications""></ul></td></tr>")
        fw.Paragraph("        </table>")
        fw.Paragraph("    </div>")
        fw.Paragraph("<script>")
        If MyOptions.Value("UseHPOrConditionalInjury") = 0 Then
            fw.Paragraph("let storedCur_HP = localStorage.getItem('cur_HP');")
            fw.Paragraph("if(storedCur_HP != """"){" & _
                "document.getElementById(""cur_HP"").value = storedCur_HP;}")
        Else If MyOptions.Value("UseHPOrConditionalInjury") >= 1 Then
            fw.Paragraph("let storedCur_Injury = localStorage.getItem('cur_Injury');")
            fw.Paragraph("if(storedCur_Injury != """"){" & _
                "document.getElementById(""cur_Injury"").value = storedCur_Injury;}")
        End If
        If MyOptions.Value("UseControlPointsOrControlSeverity") = 1 Then ' use CP
            fw.Paragraph("let storedCur_CP = localStorage.getItem('cur_CP');")
            fw.Paragraph("if(storedCur_CP != """"){" & _
                "document.getElementById(""cur_CP"").value = storedCur_CP;}")
        Else If MyOptions.Value("UseControlPointsOrControlSeverity") = 2 Then ' use Severity
            fw.Paragraph("let storedCur_Control = localStorage.getItem('cur_Control');")
            fw.Paragraph("if(storedCur_Control != """"){" & _
                "document.getElementById(""cur_Control"").value = storedCur_Control;}")
        End If
        For Each pool_name As String In pools
            Dim symbol As String
            Dim score As Integer
            callIt = pool_name
            ListLoc = CurChar.ItemPositionByNameAndExt(pool_name, Stats)
            If CurChar.Items(ListLoc).TagItem("symbol") <> "" Then
                callIt = CurChar.Items(ListLoc).TagItem("symbol")
            End If
            symbol = replaceSpaces(callIt)
            score = CurChar.Items(ListLoc).TagItem("score")
            fw.Paragraph("let " & symbol & "= """ & score & """;")
            fw.Paragraph("let storedCur_" & symbol & "=" & "localStorage.getItem('cur_" & _
                symbol & "');")
            fw.Paragraph("if(storedCur_" & symbol & "!= """"){")
            fw.Paragraph("document.getElementById(""cur_" & symbol & _
                """).value = storedCur_" & symbol & ";}")
            fw.Paragraph("else { localStorage.setItem(""cur_" & symbol & """, " & _
                symbol & ");}")
            fw.Paragraph("document.getElementById(""cur_" & symbol & _
                """).value = " & symbol &";")
        Next
        fw.Paragraph("document.getElementById(""cur_FP"").min = (-1 * FP);")
        '
        ' poolConditionNotifications()
        '
        fw.Paragraph("function poolConditionNotifications(){")
        fw.Paragraph("var output = """";")
        If MyOptions.Value("UseControlPointsOrControlSeverity") = 1 Then ' use CP
            ListLoc = CurChar.ItemPositionByNameAndExt("Lifting ST", Stats)
            fw.Paragraph("var CM = " &  CurChar.Items(ListLoc).TagItem("score") & ";")
        End If
        For Each pool_name As String In pools
            Dim symbol As String
            Dim score As Integer
            callIt = pool_name
            ListLoc = CurChar.ItemPositionByNameAndExt(pool_name, Stats)
            If CurChar.Items(ListLoc).TagItem("symbol") <> "" Then
                callIt = CurChar.Items(ListLoc).TagItem("symbol")
            End If
            symbol = replaceSpaces(callIt)
            score = CurChar.Items(ListLoc).TagItem("score")
            fw.Paragraph("var cur_" & symbol & _
                " = document.getElementById(""cur_" & symbol & """).value;")
            fw.Paragraph("localStorage.setItem(""cur_" & symbol & _
                """, cur_" & symbol & ")")
        Next
        fw.Paragraph("var cur_FP = document.getElementById(""cur_FP"").value;")
        If MyOptions.Value("UseControlPointsOrControlSeverity") = 1 Then ' use CP
            fw.Paragraph("var cur_CP = document.getElementById(""cur_CP"").value;")
        Else If MyOptions.Value("UseControlPointsOrControlSeverity") = 1 Then ' use Severity
            fw.Paragraph("var cur_Control = document.getElementById(""cur_Control"").value;")
        End If
        If MyOptions.Value("UseHPOrConditionalInjury") = 0 Then
            fw.Paragraph("var cur_HP = document.getElementById(""cur_HP"").value;")
        Else 
            fw.Paragraph("var cur_Injury = document.getElementById(""cur_Injury"").value;")
        End If
        fw.Paragraph("localStorage.setItem(""cur_FP"", cur_FP)")
        If MyOptions.Value("UseControlPointsOrControlSeverity") = 1 Then ' use CP
            fw.Paragraph("localStorage.setItem(""cur_CP"", cur_CP)")
        Else If MyOptions.Value("UseControlPointsOrControlSeverity") = 1 Then ' use Severity
            fw.Paragraph("localStorage.setItem(""cur_Control"", cur_Control)")
        End If
        If MyOptions.Value("UseHPOrConditionalInjury") = 0 Then
            fw.Paragraph("localStorage.setItem(""cur_HP"", cur_HP)")
        Else
            fw.Paragraph("localStorage.setItem(""cur_Injury"", cur_Injury)")
        End If
        fw.Paragraph("if( cur_FP < Math.ceil(FP/3) && cur_FP >= -1*FP){")
        fw.Paragraph("output += ""<li>Halve your Move, Dodge, and ST (round up).</li>"";")
        fw.Paragraph("} ")
        fw.Paragraph("if ( cur_FP < 0 && cur_FP > -1* FP) {")
        fw.Paragraph("output += ""<li>If you suffer further fatigue, each FP you lose " & _
            "also causes 1 HP of injury. To do anything besides talk or rest, you must " & _
            "make a Will roll On a failure, you collapse, incapacitated, and can do " & _
            "nothing until you recover to posi- tive FP. On a critical failure, make an " & _
            "immediate HT roll. If you fail, you suffer a heart attack.</li>"";")
        fw.Paragraph("} else if ( cur_FP <= -1*FP ) {")
        fw.Paragraph("output += ""<li>You're unconscious.</li>"";")
        fw.Paragraph("}")
        If MyOptions.Value("UseControlPointsOrControlSeverity") = 1 Then ' use CP
            fw.Paragraph("if( cur_CP >= Math.ceil(CM*0.1) && cur_CP < Math.ceil(CM/2)){")
            fw.Paragraph("output += ""<li>-2 to DX. May only move as an Attack " & _
                "maneuver.</li>"";")
            fw.Paragraph("} else if ( cur_CP >= Math.ceil(CM/2) && cur_CP < CM ) {")
            fw.Paragraph("output += ""<li>-4 to DX. May only move as an Attack " & _
                "maneuver.</li>"";")
            fw.Paragraph("} else if ( cur_CP >= CM && cur_CP < Math.ceil(1.5*CM)) {")
            fw.Paragraph("output += ""<li>-6 to DX. May only move as an Attack " & _
                "maneuver.</li>"";")
            fw.Paragraph("} else if ( cur_CP >= Math.ceil(1.5*CM) && cur_CP < 2*CM) {")
            fw.Paragraph("output += ""<li>-8 to DX. May only move as an Attack " & _
                "maneuver.</li>"";")
            fw.Paragraph("} else if ( cur_CP >= 2*CM ) {")
            fw.Paragraph("output += ""<li>-12 to DX. May only move as an Attack " & _
                "maneuver.</li>"";")
            fw.Paragraph("}")
        Else If MyOptions.Value("UseControlPointsOrControlSeverity") = 2 Then ' use Severity
            fw.Paragraph("if (cur_Control == 2) { ")
            fw.Paragraph("output += ""<li>Grappled: -1 DX Penalty</li>"";")
            fw.Paragraph("} else if (cur_Control == 3) { ")
            fw.Paragraph("output += ""<li>Grappled: -2 DX Penalty</li>"";")
            fw.Paragraph("} else if (cur_Control == 4) { ")
            fw.Paragraph("output += ""<li>Grappled: -3 DX Penalty</li>"";")
            fw.Paragraph("} else if (cur_Control == 5) {")
            fw.Paragraph("output += ""<li>Grappled: -4 DX Penalty</li>"";")
            fw.Paragraph("} else if (cur_Control == 6) {")
            fw.Paragraph("output += ""<li>Grappled: -6 DX Penalty</li>"";")
            fw.Paragraph("} else if (cur_Control == 7) {")
            fw.Paragraph("output += ""<li>Grappled: -8 DX Penalty</li>"";")
            fw.Paragraph("} else if (cur_Control == 8) {")
            fw.Paragraph("output += ""<li>Grappled: -12 DX Penalty</li>"";")
            fw.Paragraph("} else if (cur_Control == 9) {")
            fw.Paragraph("output += ""<li>Grappled: -16 DX Penalty</li>"";")
            fw.Paragraph("} else if (cur_Control == 10) {")
            fw.Paragraph("output += ""<li>Grappled: -24 DX Penalty</li>"";")
            fw.Paragraph("}") 
        End If
        If MyOptions.Value("UseHPOrConditionalInjury") = 0 Then
            fw.Paragraph("if ( cur_HP <= 0 && cur_HP > -5* hp) {")
            fw.Paragraph("output += ""<li>Make a HT roll at the start of each of your " & _
                "turns, at -1 per full multiple of HP below zero or fall unconscious. " & _
                "Each time you pass a negative multiple of HP, immediately make a HT " & _
                "roll or die.</li>"";")
            fw.Paragraph("} else if ( cur_HP <= -5*hp && cur_HP > -10* hp) {")
            fw.Paragraph("    output += ""<li>You're dead.</li>"";")
            fw.Paragraph("} else if ( cur_HP <= -10*hp){")
            fw.Paragraph("    output += ""<li>You're dead and your body is destroyed."";")
            fw.Paragraph("}")
        Else If MyOptions.Value("UseHPOrConditionalInjury") >= 1 Then
            fw.Paragraph("if( cur_Injury == -6 ) { // Scratch")
            fw.Paragraph("output += ""<li>Roll HT for pain: <ul><li>Success: Shock " & _
                "(-1)</li><li>Failure: Mild Pain</li></ul></li>"";")
            fw.Paragraph("} else if (cur_Injury == -5) { // Minor Wound")
            fw.Paragraph("output += ""<li>Roll HT for pain: <ul><li>Success: Shock " & _
                "(-1)</li><li>Failure: Mild Pain</li></ul></li>"";")
            fw.Paragraph("} else if (cur_Injury == -4) { // Minor Wound")
            fw.Paragraph("output += ""<li>Roll HT for pain: <ul><li>Success: Shock " & _
                "(-2)</li><li>Failure: Moderate Pain</li></ul></li>"";")
            fw.Paragraph("} else if (cur_Injury == -3) { // Minor Wound")
            fw.Paragraph("output += ""<li>Roll HT for pain: <ul><li>Success: Shock " & _
                "(-3)</li><li>Failure: Moderate Pain</li></ul></li>"";")
            fw.Paragraph("} else if (cur_Injury == -2) { // Major Wound")
            fw.Paragraph("output += ""<li>Roll HT for pain: <ul><li>Success: Shock " & _
                "(-4)</li><li>Failure: Severe Pain</li></ul></li>"";")
            fw.Paragraph("output += ""<li>Roll HT for knockdown and stun</li>"";")
            fw.Paragraph("} else if (cur_Injury == -1) { // Reeling")
            fw.Paragraph("output += ""<li>Roll HT for pain: <ul><li>Success: Shock " & _
                "(-4)</li><li>Failure: Terrible Pain</li></ul></li>"";")
            fw.Paragraph("output += ""<li>Roll HT for knockdown and stun</li>"";")
            fw.Paragraph("output += ""<li><b>Halve move and dodge</b>. Round up</li>"";")
            fw.Paragraph("} else if (cur_Injury == 0) { // Crippled")
            fw.Paragraph("output += ""<li>Roll HT for pain: <ul><li>Success: Shock " & _
                "(-4)</li><li>Failure: Agony</li></ul></li>"";")
            fw.Paragraph("output += ""<li>Roll HT to remain conscious</li>"";")
            fw.Paragraph("output += ""<li>Roll HT for knockdown and stun</li>"";")
            fw.Paragraph("output += ""<li><b>Halve move and dodge</b>. Round up</li>"";")
            fw.Paragraph("} else if (cur_Injury == 1) { // Crippled")
            fw.Paragraph("output += ""<li>Roll HT for pain: <ul><li>Success: Shock " & _
                "(-4)</li><li>Failure: Agony</li></ul></li>"";")
            fw.Paragraph("output += ""<li>Roll HT to remain conscious</li>"";")
            fw.Paragraph("output += ""<li>Roll HT for knockdown and stun</li>"";")
            fw.Paragraph("output += ""<li><b>Halve move and dodge</b>. Round up</li>"";")
            fw.Paragraph("} else if (cur_Injury == 2) { // Mortal Wound")
            fw.Paragraph("output += ""<li>Roll HT to not die</li>"";")
            fw.Paragraph("output += ""<li>Roll HT to remain conscious</li>"";")
            fw.Paragraph("output += ""<li>Roll HT-2 for knockdown and stun</li>"";")
            fw.Paragraph("output += ""<li>Roll HT for pain: <ul><li>Success: Shock " & _
                "(-4)</li><li>Failure: Agony</li></ul></li>"";")
            fw.Paragraph("output += ""<li><b>Incapacitated</b></li>"";")
            fw.Paragraph("} else if (cur_Injury == 3) { // Mortal Wound")
            fw.Paragraph("output += ""<li>Roll HT-1 to not die</li>"";")
            fw.Paragraph("output += ""<li>Roll HT to remain conscious</li>"";")
            fw.Paragraph("output += ""<li>Roll HT-3 for knockdown and stun</li>"";")
            fw.Paragraph("output += ""<li>Roll HT for pain: <ul><li>Success: Shock " & _
                "(-4)</li><li>Failure: Agony</li></ul></li>"";")
            fw.Paragraph("output += ""<li><b>Incapacitated</b></li>"";")
            fw.Paragraph("} else if (cur_Injury == 4) { // Instantly Fatal")
            fw.Paragraph("output += ""<li><b>DEAD</b></li>""")
            fw.Paragraph("} else if (cur_Injury == 5) { // Instantly Fatal")
            fw.Paragraph("output += ""<li><b>DEAD</b></li>""")
            fw.Paragraph("} else if (cur_Injury == 6) { // Destruction")
            fw.Paragraph("output += ""<li><b>DEAD</b> with no chance of resurrection.</li>""")
            fw.Paragraph("}")
        End If
        fw.Paragraph("document.getElementById(""notifications"").innerHTML = output;")
        fw.Paragraph("if(output){")
        fw.Paragraph("document.getElementById(""notifications"").style.display = ""block"";")
        fw.Paragraph("} else {")
        fw.Paragraph("document.getElementById(""notifications"").style.display = ""none"";")
        fw.Paragraph("}")
        fw.Paragraph("}")
        fw.Paragraph("</script>")
    End Sub

    Private Sub ExportDefense(CurChar as GCACharacter, fw As FileWriter)
        Dim ListLoc, EncRow, move As Integer

        EncRow = CurChar.EncumbranceLevel
        If EncRow = 0 Then
            ListLoc = CurChar.ItemPositionByNameAndExt("No Encumbrance Move", Stats)
        ElseIf EncRow = 1 Then
            ListLoc = CurChar.ItemPositionByNameAndExt("Light Encumbrance Move", Stats)
        ElseIf EncRow = 2 Then
            ListLoc = CurChar.ItemPositionByNameAndExt("Medium Encumbrance Move", Stats)
        ElseIf EncRow = 3 Then
            ListLoc = CurChar.ItemPositionByNameAndExt("Heavy Encumbrance Move", Stats)
        ElseIf EncRow = 4 Then
            ListLoc = CurChar.ItemPositionByNameAndExt("X-Heavy Encumbrance Move", Stats)
        End If
        If ListLoc = 0 Then
            ListLoc = CurChar.ItemPositionByNameAndExt("Basic Move", Stats)
        End If
        If ListLoc > 0 Then
            move = CurChar.Items(ListLoc).TagItem("score")
        End If

        ListLoc = CurChar.ItemPositionByNameAndExt("Dodge", Stats)

        fw.Paragraph("<div class=""defense"">")
        fw.Paragraph("    <table>")
        fw.Paragraph("        <tr><td class=""title"">DR</td><td class=""box field"">" & _
            RemoveNoteBrackets(CurChar.Body.Item("Torso").DR) & "</td><td></td></tr>")
        fw.Paragraph("    <tr><td class=""title"">Move</td><td class=""box field"">" & _
            move & "</td><td></td></tr>")
        fw.Paragraph("    <tr><td class=""title"">Dodge</td><td class=""box field"">" & _ 
            CurChar.Items(ListLoc).TagItem("score") & "</td><td></td></tr>")
        fw.Paragraph("    <tr><td class=""title"">Block</td><td class=""box field"">" & _
            CurChar.blockscore & "</td><td></td></tr>")
        fw.Paragraph("    <tr><td class=""title"">Parry</td><td class=""box field"">" & _
            CurChar.parryscore & "</td><td></td></tr>")
        fw.Paragraph("    </table>")
        fw.Paragraph("</div>")
    End Sub

    Private Sub ExportAttributes(CurChar As GCACharacter, fw As FileWriter)
        fw.Paragraph("<div class=""attributes"">")
        ExportPrimaryAttributes(CurChar, fw)
        ExportSecondaryAttributes(CurChar,fw)
        ExportPools(CurChar,fw)
        ExportDefense(CurChar, fw)
        fw.Paragraph("</div>")
    End Sub


    Private Sub ExportMeleeAttacks(CurChar As GCACharacter, fw As FileWriter)
        Dim CurMode As Integer
        Dim DamageText As String
        Dim i As Integer
        Dim item_index As Integer
        Dim db As String
        Dim blk As Long
        Dim okay As Boolean
        Dim tmp As String
        Dim saved_level As String
        Dim block_text As String

        item_index = 0
        fw.Paragraph("<div class=""melee"">")
        fw.Paragraph("<h1 class=""section-title"">Melee Attacks</h1>")
        fw.Paragraph("<div class=""attack-list"">")
        For i = 1 To CurChar.Items.Count

            'we only want to include hand weapons here, so look for items with Reach
            okay = False
            tmp = CurChar.Items(i).TagItem("charreach")

            If tmp = "C" Then
                okay = True
            End If
            ' C,1 1,2 etc.
            If Len(tmp) > 1 Then
                okay = True
            Else
            ' 1, 2, etc.
                If StrToLng(tmp) > 0 Then
                    okay = True
                End If
            End If
            ' Trying to allow non-reach-like values to be ignored (a user uses this for some reason)

            if tmp = "||" then
                okay = False
            End If

            'exclude hidden items
            If CurChar.Items(i).TagItem("hide") <> "" Then
                ' only hide stats & equipment--hidden ads or skills should print
                If CurChar.Items(i).ItemType = Equipment Or CurChar.Items(i).ItemType = Stats Then
                    okay = False
                End If
            End If

            If okay Then
                '* loop round for each reach mode
                CurMode = CurChar.Items(i).DamageModeTagItemAt("charreach")

                Dim qty : qty = 1 
                If StrToLng(CurChar.Items(i).tagitem("count")) <> 0 Then
                    qty = StrToLng(CurChar.Items(i).tagitem("count"))
                End If

                fw.Paragraph("<div class=""field"">" & UpdateEscapeChars(CurChar.Items(i).FullNameTL))

                
                
                db = CurChar.items(i).tagitem("chardb")
                
                Do
                    block_text = "No"
                    saved_level = CurChar.Items(i).DamageModeTagItem(CurMode, "charskillscore")
                    DamageText = CurChar.Items(i).DamageModeTagItem(CurMode, "chardamage")
                    If CurChar.Items(i).DamageModeTagItem(CurMode, "chararmordivisor") <> "" Then
                        DamageText = DamageText & " (" & CurChar.Items(i).DamageModeTagItem(CurMode, "chararmordivisor") & ")"
                    End If
                    DamageText = DamageText & " " & CurChar.Items(i).DamageModeTagItem(CurMode, "chardamtype")
                    if db <> "" Then
                        dim tskill, pos, block
                        tskill = CurChar.Items(i).DamageModeTagItem(CurMode, "charskillused")
                        'remove surrounding quotes, if any
                        if left(tskill,1) = chr(34) then
                            tskill = mid(tskill, 2)
                        end if
                        if right(tskill,1) = chr(34) then
                            tskill = left(tskill, len(tskill)-1)
                        end if
                        'remove prefix tag, if any
                        if left(tskill, 3) = "SK:" then
                            tskill = mid(tskill, 4)
                        end if

                        pos = CurChar.ItemPositionByNameAndExt(tskill, Skills)
                        If pos > 0 Then
                            block = CurChar.Items(pos).TagItem("blocklevel")
                            blk = StrToLng(block)
                            block_text = (blk + db)
                        End If
                    End If
                    fw.Paragraph("<div style=""padding-left:29px"">" & CurChar.Items(i).DamageModeName(CurMode) & _
                        " - " & saved_level & ": " & DamageText & "; Block: " & block_text & "; Parry: " & _
                        CurChar.Items(i).DamageModeTagItem(CurMode, "charparryscore") & "; Reach: " & _
                        CurChar.Items(i).DamageModeTagItem(CurMode, "charreach") & "</div>")

                    CurMode = CurChar.Items(i).DamageModeTagItemAt("charreach", CurMode + 1)

                Loop While CurMode > 0

                fw.Paragraph("</div>")
            End If
        Next
        fw.Paragraph("</div>")
        fw.Paragraph("</div>")
    End Sub


    Private Sub ExportRangedAttacks(CurChar As GCACharacter, fw As FileWriter)
        Dim CurMode As Integer
        Dim DamageText As String
        Dim RangeText As String
        Dim i As Integer
        Dim weapon_mode_index As Integer
        Dim mode_tag_index As String
        Dim okay As Boolean

        fw.Paragraph("<div class=""ranged"">")
        fw.Paragraph("<h1 class=""section-title"">Ranged Attacks</h1>")
        fw.Paragraph("<div class=""attack-list"">")
        For i = 1 To CurChar.Items.Count
            'we only want to include ranged weapons here, so look for items with Range
            okay = False
            If CurChar.Items(i).DamageModeTagItemCount("charrangemax") > 0 Then
                okay = True
            End If

            If okay Then
                If CurChar.Items(i).TagItem("hide") = "" Then 'not hidden

                    ' loop round for each range mode
                    CurMode = CurChar.Items(i).DamageModeTagItemAt("charrangemax")

                    fw.Paragraph("<div class=""field"">" & UpdateEscapeChars(CurChar.Items(i).FullNameTL) )

                    weapon_mode_index = 0
                    Do
                        ' create the opening tag for this weapon item
                        weapon_mode_index = weapon_mode_index + 1
                        mode_tag_index = LeadingZeroes(weapon_mode_index)
                        
                        DamageText = CurChar.Items(i).DamageModeTagItem(CurMode, "chardamage")
                        If CurChar.Items(i).DamageModeTagItem(CurMode, "chararmordivisor") <> "" Then
                            DamageText = DamageText & " (" & CurChar.Items(i).DamageModeTagItem(CurMode, "chararmordivisor") & ")"
                        End If
                        DamageText = DamageText & " " & CurChar.Items(i).DamageModeTagItem(CurMode, "chardamtype")

                        RangeText = CurChar.Items(i).DamageModeTagItem(CurMode, "charrangehalfdam")
                        If RangeText = "" Then
                            RangeText = CurChar.Items(i).DamageModeTagItem(CurMode, "charrangemax")
                        Else
                            RangeText = RangeText & "/" & CurChar.Items(i).DamageModeTagItem(CurMode, "charrangemax")
                        End If
                        
                        fw.Paragraph("<div style=""padding-left: 29px"">" & _
                            CurChar.Items(i).DamageModeTagItem(CurMode, "name") & " - " & _
                            CurChar.Items(i).DamageModeTagItem(CurMode, "charskillscore") & ": " & _
                            DamageText & "; Acc: " & CurChar.Items(i).DamageModeTagItem(CurMode, "characc") & _
                            "; Range: " & RangeText & _
                            "; RoF: " & CurChar.Items(i).DamageModeTagItem(CurMode, "charrof") & _
                            "; Shots: " & CurChar.Items(i).DamageModeTagItem(CurMode, "charshots") & _
                            "; Rcl: " & CurChar.Items(i).DamageModeTagItem(CurMode, "charrcl") & "</div>")

                        CurMode = CurChar.Items(i).DamageModeTagItemAt("charrangemax", CurMode + 1)
                    Loop While CurMode > 0
                    fw.Paragraph("</div>")
                End If
            End If
        Next
        fw.Paragraph("</div>")
        fw.Paragraph("</div>")
    End Sub


'****************************************
'* Export Protection
'****************************************
    Private Sub ExportProtection(CurChar As GCACharacter, fw As FileWriter)

        Dim ListLoc As Integer
        Dim BonusDR As Integer
        Dim i As Integer
        Dim locDR As String

        ListLoc = CurChar.ItemPositionByNameAndExt("DR", Stats)
        If ListLoc > 0 Then
            If CurChar.Items(ListLoc).TagItem("score") <> 0 Then
                BonusDR = CurChar.Items(ListLoc).TagItem("score")
            End If
        End If
        If Not BonusDR Then BonusDR = 0

        fw.Paragraph("<div class=""hit-locations"">")
        fw.Paragraph("<h1 class=""section-title"">DR by Hit Location</h1>")
        fw.Paragraph("<table class=""hit-locations-list"">")
        
        For i = 1 To CurChar.Body.Count
            If CurChar.Body.Item(i).Display Then

                locDR = CurChar.Body.Item(i).DR
                If locDR.Length = 0 Then
                    locDR = 0
                End If
                If BonusDR > 0 Then
                    locDR = locDR & " +" & BonusDR
                End If

                locDR = RemoveNoteBrackets(locDR)

                fw.Paragraph("<tr>" & _
                    "<td class=""title"">" & CurChar.Body.Item(i).Name & "</td>" & _
                    "<td class=""field right"">" & locDR & "</td>" & _
                    "</tr>")
            End If
        Next
        fw.Paragraph("</table>")
        fw.Paragraph("</div>")

    End Sub


    Private Sub ExportEncumbrance(CurChar As GCACharacter, fw As FileWriter)
        Dim ListLoc As Integer
        Dim enc_levels As String() = {"No Encumbrance", "Light Encumbrance", _
            "Medium Encumbrance", "Heavy Encumbrance", "X-Heavy Encumbrance"}


        fw.Paragraph("<div class=""encumbrance"">")
        fw.Paragraph("    <h1 class=""section-title"">Encumbrance</h1>")
        fw.Paragraph("    <table class=""unalternate"">")
        For Each eLevel As String In enc_levels
            ListLoc = CurChar.ItemPositionByNameAndExt(eLevel, Stats)
            If ListLoc > 0 Then
                fw.Paragraph("<tr><td class=""title"">" & _
                    eLevel & "</td><td class=""field right"">" & _
                    CurChar.Items(ListLoc).TagItem("score") & "</td></tr>")
            End If
        Next

        fw.Paragraph("</table>")
        fw.Paragraph("</div>")
    End Sub

    Private Function SizeModifier(CurChar As GCACharacter) As String
        Dim ListLoc As Integer

        ListLoc = CurChar.ItemPositionByNameAndExt("Size Modifier", Stats)
        If ListLoc > 0 Then
            Return CurChar.Items(ListLoc).TagItem("score")
        End If
        Return "0"
    End Function

'****************************************
'* Export Ads, Disads, Perks, and Quirks
'****************************************
    Private Sub ExportTraits(CurChar As GCACharacter, fw As FileWriter)

        If CurChar.Count(Ads) <= 0 And CurChar.Count(Packages) <= 0 And CurChar.Count(Perks) <= 0 Then Exit Sub

        Dim i As Integer
        Dim ads_index As Integer
        Dim tmp As String
        Dim work As String
        Dim mods_text as String
        Dim out As String
        Dim types_list() as Double = {Packages, Ads, Perks, Disads, Quirks}

        fw.Paragraph("<div class=""traits"">")
        fw.Paragraph("<h1 class=""section-title"">Traits</h1>")
        fw.Paragraph("<div class=""traits-list"">")

        For Each trait_type As Double In types_list

            For i = 1 To CurChar.Items.Count
                If CurChar.Items(i).ItemType = trait_type And CurChar.Items(i).TagItem("hide") = "" Then
                    ' it's an advantage and not hidden
                    ads_index = ads_index + 1

                    tmp = CurChar.Items(i).FullName

                    work = CurChar.Items(i).LevelName
                    If work <> "" Then
                        tmp = tmp & " " & work 
                    End If

                    mods_text = CurChar.Items(i).ExpandedModCaptions(False)
                    if mods_text <> "" Then
                        tmp = tmp & UpdateEscapeChars(mods_text)
                    End If

                    ' get the points cost
                    work = CInt(CurChar.Items(i).TagItem("points"))
                    ' if the item is a parent, subtract the points value of its children
                    If CurChar.Items(i).TagItem("childpoints") <> "" Then
                        work = work - CInt(CurChar.Items(i).TagItem("childpoints"))
                    End If

                    out = "<div class=""field hanging " & CurChar.Items(i).ItemType & _
                        """"
                    If UserVTTNotes(CurChar.Items(i)) <> "" Or _
                            CurChar.Items(i).TagItem("page") <> "" Then
                        Dim pageTag = CurChar.Items(i).TagItem("page")
                        Dim page As String 
                        If pageTag <> "" Then 
                            page = String.Format(" p. {0}", pageTag)
                        Else
                            page = ""
                        End If
                        out = out & " title=""" & UpdateEscapeChars(tmp) & ": " & _
                            UserVTTNotes(CurChar.Items(i)) & page & """"
                    End If 
                    out = out & " >" & UpdateEscapeChars(tmp) & _
                        "<span class=""points"">[" & UpdateEscapeChars(work) & "]</span>" & _
                        "</div>"
                    fw.Paragraph(out)
                End If
            Next

        Next
        fw.Paragraph("</div>")
        fw.Paragraph("</div>")
    End Sub


'****************************************
'* Export Cultural Familiarity
'****************************************
    Private Sub ExportCulturalFamiliarity(CurChar As GCACharacter, fw As FileWriter)
        Dim i As Integer

        fw.Paragraph("<div class=""cultures"">")
        fw.Paragraph("<h1 class=""section-title"">Cultures</h1>")
        For i = 1 To CurChar.Items.Count
            If CurChar.Items(i).ItemType = Cultures Then
                If CurChar.Items(i).TagItem("hide") = "" Then 'not hidden
                    fw.Paragraph("<div class=""field"">" & CurChar.Items(i).FullNameTL & _
                        "<span class=""points"">[" & CurChar.Items(i).TagItem("points") & _
                        "]</span></div>")
                End If
            End If
        Next
        fw.Paragraph("</div>")
    End Sub


'****************************************
'* Export Languages
'****************************************
    Private Sub ExportLanguages(CurChar As GCACharacter, fw As FileWriter)

        Dim i As Integer
'
        fw.Paragraph("<div class=""languages"">")
        fw.Paragraph("<h1 class=""section-title"">Languages</h1>")

        For i = 1 To CurChar.Items.Count
            If CurChar.Items(i).ItemType = Languages Then
                If CurChar.Items(i).TagItem("hide") = "" Then 'not hidden
                    Dim langName = CurChar.Items(i).FullName
                    Dim levelName = CurChar.Items(i).LevelName
                    Dim native = ""
                    Dim points = CurChar.Items(i).TagItem("points")

                    If IsNativeLang(CurChar, i) Then
                        native = "*"
                    End If

                    fw.Paragraph("<div class=""field"">" & langName & _
                        " (" & levelName & ")" &  _
                        "<span class=""points"">[" & points & native & "]</span></div>")
                End If
            End If
        Next
        fw.Paragraph("</div>")
    End Sub

    Private Sub ExportConditionalModifiers(CurChar As GCACharacter, fw As FileWriter)
        Dim l1 As Integer
        Dim reaction_types() As String = {"Unappealing", "Appealing", "Status", "Reaction"}

        fw.Paragraph("<div class=""field"">")
        For Each rType As String In reaction_types 
            l1 = CurChar.ItemPositionByNameAndExt(rType, Stats)
            If l1 > 0 Then
                fw.Paragraph(CurChar.Items(l1).TagItem("conditionallist") & "<br>")
            End If
        Next
        fw.Paragraph("</div>")
    End Sub

    Private Sub ExportReactionModifiers(CurChar As GCACharacter, fw As FileWriter)
        Dim l1, l2 As Integer
        Dim appearance_types() As String = {"Appealing", "Unappealing"} 
        Dim reaction_types() As String = {"Status", "Reaction"}
        Dim appearance_mod As String = ""

        fw.Paragraph("<div class=""reactions"">")
        fw.Paragraph("<h1 class=""section-title"">Reaction Modifiers</h1>")
        fw.Paragraph("<table>")
        fw.Paragraph("<tr><td class=""title"">Appearance</td><td class=""field"">")

        l1 = CurChar.ItemPositionByNameAndExt(appearance_types(0), Stats)
        l2 = CurChar.ItemPositionByNameAndExt(appearance_types(1), Stats)
        
        If l1 > 0 Then
            appearance_mod = appearance_mod & CurChar.Items(l1).TagItem("bonuslist")
        End If
        If l1 > 0 And l2 > 0 And _
            CurChar.Items(l1).TagItem("bonuslist") <> "" And _
            CurChar.Items(l2).TagItem("bonuslist") <> "" And _
            CurChar.Items(l1).TagItem("bonuslist") <> _
                CurChar.Items(l2).TagItem("bonuslist") Then
            appearance_mod = appearance_mod & " / " & CurChar.Items(l2).TagItem("bonuslist")
        End If
        fw.Paragraph(appearance_mod & "</td></tr>")

        For Each rType As String In reaction_types 
            l1 = CurChar.ItemPositionByNameAndExt(rType, Stats)
            If l1 > 0 Then
                fw.Paragraph("<tr><td class=""title"">" & _
                    rType & "</td><td class=""field"">" & _
                    CurChar.Items(l1).TagItem("bonuslist") & "</td></tr>")
            End If
        Next
        fw.Paragraph("</table>")
        ExportConditionalModifiers(CurChar, fw)
        fw.Paragraph("</div>")
    End Sub


    Private Function TechLevel(CurChar As GCACharacter) As String

        Dim ListLoc As Integer

        ListLoc = CurChar.ItemPositionByNameAndExt("Tech Level", Stats)
        If ListLoc > 0 Then
            Return CurChar.Items(ListLoc).TagItem("score")
        End If
        Return ""
    End Function

    Private sub ExportEquipmentItem (CurChar As GCACharacter, fw as FileWriter, theItem As GCATrait, level as Integer)
        Dim qty : qty = 1 
        Dim out As String

        If StrToLng(theItem.tagitem("count")) <> 0 Then
            qty = StrToLng(theItem.tagitem("count"))
        End If
        out = "<div class =""field"" style=""padding-left:" & ((level * 20) + 4 ) & "px;""" 
        If UserVTTNotes(theItem) <> "" Or _
                theItem.TagItem("page") <> "" Then
            Dim pageTag = theItem.TagItem("page")
            Dim page As String 
            If pageTag <> "" Then 
                page = String.Format(" p. {0}", theItem)
            Else
                page = ""
            End If
            out = out & " title=""" & UpdateEscapeChars(theItem.FullNameTL) & ": " & _
                UserVTTNotes(theItem) & page & """"
        End If 
        out = out & ">" & UpdateEscapeChars(theItem.FullNameTL) & _
            "<span class=""qty" & theItem.tagitem("count") & """> (" & _
            theItem.tagitem("count") & ")</span> (" & _
            theItem.tagitem("weight") & "; " & _
            Format(theItem.tagitem("cost"), "Currency") & ")</div>"
        fw.Paragraph(out)
    End Sub

    Private sub ExportEquipmentItemWithChildren (CurChar As GCACharacter, fw as FileWriter, theItem As GCATrait, level as Integer)
        Dim i As Integer
        Dim okay as Boolean

        ExportEquipmentItem(CurChar, fw, theItem, level)

        For i = 1 To CurChar.Items.Count
            If CurChar.Items(i).ItemType = Equipment Then
                okay = True
                If CurChar.Items(i).ParentKey <> ("k" & theItem.IDKey) Then
                    okay = False
                End If

                If CurChar.Items(i).TagItem("hide") <> "" Then
                    okay = False
                End If

                If okay Then
                    ExportEquipmentItemWithChildren(CurChar, fw, CurChar.Items(i), level + 1 )
                End If
            End If
        Next
    End Sub

    Private Sub ExportEquipment(CurChar As GCACharacter, fw As FileWriter)

        Dim i As Integer
        Dim okay as Boolean

        fw.Paragraph("<div class=""carried-equipment"">")
        fw.Paragraph("    <h1 class=""section-title"">Equipment</h1>")
        fw.Paragraph("    <div class=""equipment-list"">")

        For i = 1 To CurChar.Items.Count
            If CurChar.Items(i).ItemType = Equipment Then
                okay = True
                If CurChar.Items(i).ParentKey <> "" Then
                    okay = False
                End If

                If CurChar.Items(i).TagItem("hide") <> "" Then
                    okay = False
                End If

                If okay Then
                    ExportEquipmentItemWithChildren(CurChar, fw, CurChar.Items(i), 0)
                End If
            End If
        Next
        fw.Paragraph("    </div>")
        fw.Paragraph("</div>")
    End Sub


'****************************************
'* Export Description Note
'****************************************
    Private Sub ExportDescription(CurChar As GCACharacter, fw As FileWriter)
        fw.Paragraph("<div class=""char-portrait-field"">")
        If CurChar.Portrait<> "" Then
            Dim portraitFile = Path.GetFileName(CurChar.Portrait)
            Dim extension = Path.GetExtension(CurChar.Portrait)
            Dim portImgData = File.ReadAllBytes(CurChar.Portrait)
            Dim mimeType = ""
            If extension = ".png" Then
                mimeType = "image/png"
            Else If extension = ".jpg" Or extension = ".jpeg" Then
                mimeType = "image/jpeg"
            End If

            fw.Paragraph("<img class=""portrait"" src=""data:" & mimeType & ";base64, " & _
                Convert.ToBase64String(portImgData) & """ >")
        End If
        fw.Paragraph("</div>")
        fw.Paragraph("  <div class=""charinfo"">")
        fw.Paragraph("  <div class=""char-name-field field"">" & CurChar.Name & "</div>")
        fw.Paragraph("  <div class=""player title"">Player</div>")
        fw.Paragraph("  <div class=""player-field field"">" & CurChar.Player & "</div>")
        fw.Paragraph("  <div class=""ancestry title"">Race</div>")
        fw.Paragraph("  <div class=""ancestry-field field underlined"">" & CurChar.Race & "</div>")
        fw.Paragraph("  <div class=""height title"">Height</div>")
        fw.Paragraph("  <div class=""height-field field underlined"">" & CurChar.Height & "</div>")
        fw.Paragraph("  <div class=""weight title"">Weight</div>")
        fw.Paragraph("  <div class=""weight-field field underlined"">" & CurChar.Weight & "</div>")
        fw.Paragraph("  <div class=""tl title"">TL</div>")
        fw.Paragraph("  <div class=""tl-field field underlined"">" & TechLevel(CurChar) & "</div>")
        fw.Paragraph("  <div class=""age title"">Age</div>")
        fw.Paragraph("  <div class=""age-field field underlined"">" & CurChar.Age & "</div>")
        fw.Paragraph("  <div class=""size title"">Size</div>")
        fw.Paragraph("  <div class=""size-field field underlined"">" & SizeModifier(CurChar) _
            & "</div>")
        fw.Paragraph("  <div class=""appearance title"">Appearance</div>")
        fw.Paragraph("  <div class=""appearance-field field underlined"">" & CurChar.Appearance _
            & "</div>")
        fw.Paragraph("</div>")
    End Sub


'****************************************
'* Export Notes
'****************************************
    Private Sub ExportNotes(CurChar As GCACharacter, fw As FileWriter)
        fw.Paragraph("<div class=""notes"">")
        fw.Paragraph("<h1 class=""section-title"">Notes</h1>")
        fw.Paragraph("<div class=""field"">" & _
            UpdateEscapeChars(RTFtoPlainText(CurChar.Notes)) & "</div>")
        fw.Paragraph("</div>")
    End Sub


'****************************************
'* Export Point Summary
'****************************************
    Private Sub ExportPointSummary(CurChar As GCACharacter, fw As FileWriter)
        fw.Paragraph("<div class=""point-summary"">")
        fw.Paragraph("<table>")
        

        fw.Paragraph("<tr><td class=""title"">Total</td><td class=""field box right"">" & _
            CurChar.TotalPoints & "</td></tr>")
        fw.Paragraph("<tr><td class=""title"">Unspent</td><td class=""field right"">" & _
            CurChar.UnspentPoints & "</td></tr>")
        fw.Paragraph("<tr><td class=""title"">Stats</td><td class=""field right"">" & _
            CurChar.Cost(Stats) & "</td></tr>")
        fw.Paragraph("<tr><td class=""title"">Advantages</td><td class=""field right"">" & _
            CStr(CInt(CurChar.Cost(Ads)) + CInt(CurChar.Cost(Packages)) + _
            CInt(CurChar.Cost(Cultures)) + CInt(CurChar.Cost(Languages))) & "</td></tr>")
        fw.Paragraph("<tr><td class=""title"">Disadvantages</td><td class=""field right"">" & _
            CurChar.Cost(Disads) & "</td></tr>")
        fw.Paragraph("<tr><td class=""title"">Quirks</td><td class=""field right"">" & _
            CurChar.Cost(Quirks) & "</td></tr>")
        fw.Paragraph("<tr><td class=""title"">Skills</td><td class=""field right"">" & _
            CurChar.Cost(Skills) & "</td></tr>")
        If CurChar.Count(Spells) <> 0 Then
            fw.Paragraph("<tr><td class=""title"">Spells</td><td class=""field right"">" & _
                CurChar.Cost(Spells) & "</td></tr>")
        End If
        fw.Paragraph("</table></div>")
    End Sub



'****************************************
'* Function Remove after basic damage
'****************************************
    Public Function RemoveAfterBasicDamage(ByVal damage)
        Dim correctDamage As Array
        Dim MyString As String

        correctDamage = Split(damage, "+ ")
        MyString = correctDamage(0).Replace(" ", "")

        Return MyString
        
    End Function


'****************************************
'* Function Create Control Roll
'****************************************
    Public Function CreateControlRoll(ByVal MyString)
        Dim regexCR = New Regex("CR: \d{1,2}", RegexOptions.IgnoreCase)
        Dim regexLess = New Regex("\d{1,2} or less", RegexOptions.IgnoreCase)

        MyString = MyString.Replace("[", "")
        MyString = MyString.Replace("]", "")

        Dim collectionCR As MatchCollection = regexCR.Matches(MyString)
        For Each matchCR As Match In collectionCR
            MyString = MyString.Replace(matchCR.value, "[" & matchCR.value & " or less] ")
        Next

        Dim collectionLess As MatchCollection = regexLess.Matches(MyString)
        For Each matchLess As Match In collectionLess
            MyString = MyString.Replace(matchLess.value, "[CR: " & matchLess.value & "] ")
        Next

        MyString = MyString.Replace("] or less", "]")

        Return MyString

    End Function


'****************************************
'* Function Remove Note Brackets
'****************************************
    Public Function RemoveNoteBrackets(ByVal MyString)
        MyString = Replace(MyString, "[note]", "")

        Return MyString

    End Function


'****************************************
'* Function Update Escape Chars
'****************************************
    Public Function UpdateEscapeChars(ByVal MyString)
		'2023-10-07 - moved these two up here from below
        MyString = Replace(MyString, "<", "&lt;")
        MyString = Replace(MyString, ">", "&gt;")
		
        MyString = Replace(MyString, Chr(10), "<br>")
        MyString = Replace(MyString, Chr(13), "<br>")
        MyString = Replace(MyString, "&", "&amp;")
        MyString = Replace(MyString, " \par", "<br>")
        MyString = Replace(MyString, Chr(134), "&#8224;") 'Single Dagger
        MyString = Replace(MyString, Chr(135), "&#8225;") 'Double Dagger

        Return MyString  

    End Function


'****************************************
'* Function Leading Zeroes
'****************************************
    Public Function LeadingZeroes(myInteger)
        Return String.Format("{0:00000}", myInteger)
    End Function


'****************************************
'* Function Is Native Lang
'****************************************
    Public Function IsNativeLang(CurChar, index)

        Dim j As Integer

        For j = 1 To CurChar.Items(index).Mods.count
            If CurChar.Items(index).Mods(j).FullName = "Native Language" Then
                Return True
            End If
        Next
        Return False
    End Function


'****************************************
'* Function StrToDbl
'****************************************
    Public Function StrToDbl(ByVal aNumStr)
        'trim leading/trailing whitespace from aNumStr
        aNumStr = Trim(aNumStr)
        'handle signs and decimals w/o initial zero
        Dim Sign : Sign = Left(aNumStr, 1)
        If Sign = "-" Or Sign = "+" Then
            StrToDbl = Sign & "0" & Mid(aNumStr, 2)
        Else
            StrToDbl = "0" & aNumStr
        End If
        'format with period: d.dd (e.g. 0.01)
         StrToDbl = Replace(Replace(StrToDbl, " ", ""), ",", ".")
        'attempt to convert value to double
        On Error Resume Next

        'enable error handling
        Return CDbl(StrToDbl)
        'attempt the conversion
        If Err.Number <> 0 Then

            'if an error was thrown
            Err.Clear
            Return 0.0
        End If
        On Error GoTo 0
        'disable error handling

    End Function


'****************************************
'* Function StrToLng
'****************************************
    Function StrToLng(ByVal aNumStr)

        'trim leading/trailing whitespace from aNumStr
        aNumStr = Trim(aNumStr)

        'remove any digit grouping characters
        StrToLng = Replace(Replace(Replace(aNumStr, " ", ""), ".", ""), ",", "")

        'attempt to convert value to long
        On Error Resume Next

        'enable error handling
            Return CLng(StrToLng)
        'attempt the conversion
        If Err.Number <> 0 Then

        'if an error was thrown
            Err.Clear
            Return 0
        End If
        On Error GoTo 0
        'disable error handling

    End Function

'****************************************
'* Function
'* Convenient way to get UserNotes and VTTNotes combined
'* ~ ADS
'****************************************
    Function UserVTTNotes(theItem as GCATrait) As String
        Dim ret As String = ""
        Dim desc as string = RTFtoPlainText(theItem.TagItem("description")).Trim
        Dim user As String = RTFtoPlainText(theItem.TagItem("usernotes")).Trim
        Dim vtt As String = theItem.TagItem("vttnotes").Trim

        If MyOptions.Value("NotesIncludeDescription") = True Then
        	If desc <> "" Then ret = desc
        End If

        If user <> "" Then
            If ret = "" Then
                ret = user
            Else
                ret = ret & vblf & user
            End If
        End If
		
        If vtt <> "" Then
            If ret = "" Then
                ret = vtt
            Else
                ret = ret & vblf & vtt
            End If
        End If

        Return ret
    End Function

'    Function BaseDamageFromDmg(dmg As Integer) as String
'        Dim baseDmg As String
'        baseDmg = BaseDamageFromST(dmg)
'        ' Add 5B
'        return baseDmg
'    End Function
'
'    Function BaseDamageFromST(st As Integer) as String
'        If st = 1 Then
'            return "-4B+0"
'        Else If st = 2 Then
'            return "-2B+0"
'        Else If st = 3 Then
'            return "-1B+0"
'        Else If st = 4 Then
'            return "-1B+2"
'        Else If st = 5 Then
'            return "0B+0"
'        Else If st = 6 Then
'            return "0B+2"
'        Else If st = 7 Then
'            return "1B+0"
'        Else If st = 8 Then
'            return "1B+1"
'        Else If st = 9 Then
'            return "1B+1"
'        Else If st = 10 Then
'            return "2B+0"
'        Else If st = 11 Then
'            return "2B+1"
'        Else If st = 12 Then
'            return "2B+2"
'        Else If st = 13 Then
'            return "2B+3"
'        Else If st = 14 Then
'            return "2B+4"
'        Else If st = 15 Then
'            return "3B+0"
'        Else If st = 16 Then
'            return "3B+1"
'        Else If st = 17 Then
'            return "3B+2"
'        Else If st = 18 Then
'            return "3B+3"
'        Else If st = 19 Then
'            return "3B+4"
'	Else If st >= 20 And st <= 21 Then
'            return "4B+0"
'	Else If st >= 22 And st <= 23 Then
'            return "4B+1"
'	Else If st >= 24 And st <= 25 Then
'            return "4B+2"
'	Else If st >= 26 And st <= 27 Then
'            return "4B+3"
'	Else If st >= 28 And st <= 29 Then
'            return "4B+4"
'	Else If st >= 30 And st <= 32 Then
'            return "5B+0"
'	Else If st >= 33 And st <= 36 Then
'            return "5B+1"
'	Else If st >= 37 And st <= 41 Then
'            return "5B+2"
'	Else If st >= 42 And st <= 45 Then
'            return "5B+3"
'	Else If st >= 46 And st <= 49 Then
'            return "5B+4"
'	Else If st >= 50 And st <= 52 Then
'            return "6B+0"
'	Else If st >= 53 And st <= 56 Then
'            return "6B+1"
'	Else If st >= 57 And st <= 61 Then
'            return "6B+2"
'	Else If st >= 62 And st <= 65 Then
'            return "6B+3"
'	Else If st >= 66 And st <= 69 Then
'            return "6B+4"
'	Else If st >= 70 And st <= 74 Then
'            return "7B+0"
'	Else If st >= 75 And st <= 80 Then
'            return "7B+1"
'	Else If st >= 81 And st <= 87 Then
'            return "7B+2"
'	Else If st >= 88 And st <= 94 Then
'            return "7B+3"
'	Else If st >= 95 And st <= 99 Then
'            return "7B+4"
'        Else If st >= 100 And st <= 109 Then 
'            return "8B+0"
'        Else If st >= 110 And st <= 119 Then 
'            return "8B+1"
'        Else If st >= 120 And st <= 129 Then 
'            return "8B+2"
'        Else If st >= 130 And st <= 139 Then 
'            return "8B+3"
'        Else If st >= 140 And st <= 149 Then 
'            return "8B+4"
'        Else If st >= 150 And st <= 159 Then 
'            return "9B+0"
'        Else If st >= 160 And st <= 169 Then 
'            return "9B+1"
'        Else If st >= 170 And st <= 179 Then 
'            return "9B+2"
'        Else If st >= 180 And st <= 189 Then 
'            return "9B+3"
'        Else If st >= 190 And st <= 199 Then 
'            return "9B+4"
'        Else If st >= 200 And st <= 219 Then 
'            return "10B+0"
'        Else If st >= 220 And st <= 239 Then 
'            return "10B+1"
'        Else If st >= 240 And st <= 259 Then 
'            return "10B+2"
'        Else If st >= 260 And st <= 279 Then 
'            return "10B+3"
'        Else If st >= 280 And st <= 299 Then 
'            return "10B+4"
'        Else If st >= 300 And st <= 329 Then 
'            return "11B+0"
'        Else If st >= 330 And st <= 369 Then 
'            return "11B+1"
'        Else If st >= 370 And st <= 419 Then 
'            return "11B+2"
'        Else If st >= 420 And st <= 459 Then 
'            return "11B+3"
'        Else If st >= 460 And st <= 499 Then 
'            return "11B+4"
'        Else If st >= 500 And st <= 529 Then 
'            return "12B+0"
'        Else If st >= 530 And st <= 569 Then 
'            return "12B+1"
'        Else If st >= 570 And st <= 619 Then 
'            return "12B+2"
'        Else If st >= 620 And st <= 659 Then 
'            return "12B+3"
'        Else If st >= 660 And st <= 699 Then 
'            return "12B+4"
'        Else If st >= 700 And st <= 749 Then 
'            return "13B+0"
'        Else If st >= 750 And st <= 809 Then 
'            return "13B+1"
'        Else If st >= 810 And st <= 879 Then 
'            return "13B+2"
'        Else If st >= 880 And st <= 949 Then 
'            return "13B+3"
'        Else If st >= 950 And st <= 999 Then 
'            return "13B+4"
'        Else If st >= 1000 And st <= 1099 Then 
'            return "14B+0"
'        Else If st >= 1100 And st <= 1199 Then 
'            return "14B+1"
'        Else If st >= 1200 And st <= 1299 Then 
'            return "14B+2"
'        Else If st >= 1300 And st <= 1399 Then 
'            return "14B+3"
'        Else If st >= 1400 And st <= 1499 Then 
'            return "14B+4"
'        Else If st >= 1500 And st <= 1599 Then 
'            return "15B+0"
'        Else If st >= 1600 And st <= 1699 Then 
'            return "15B+1"
'        Else If st >= 1700 And st <= 1799 Then 
'            return "15B+2"
'        Else If st >= 1800 And st <= 1899 Then 
'            return "15B+3"
'        Else If st >= 1900 And st <= 1999 Then 
'            return "15B+4"
'        Else If st >= 2000 And st <= 2199 Then 
'            return "16B+0"
'        Else If st >= 2200 And st <= 2399 Then 
'            return "16B+1"
'        Else If st >= 2400 And st <= 2599 Then 
'            return "16B+2"
'        Else If st >= 2600 And st <= 2799 Then 
'            return "16B+3"
'        Else If st >= 2800 And st <= 2999 Then 
'            return "16B+4"
'        Else If st >= 3000 And st <= 3299 Then 
'            return "17B+0"
'        Else If st >= 3300 And st <= 3699 Then 
'            return "17B+1"
'        Else If st >= 3700 And st <= 4199 Then 
'            return "17B+2"
'        Else If st >= 4200 And st <= 4599 Then 
'            return "17B+3"
'        Else If st >= 4600 And st <= 4999 Then 
'            return "17B+4"
'        Else If st >= 5000 And st <= 5299 Then 
'            return "18B+0"
'        Else If st >= 5300 And st <= 5699 Then 
'            return "18B+1"
'        Else If st >= 5700 And st <= 6199 Then 
'            return "18B+2"
'        Else If st >= 6200 And st <= 6599 Then 
'            return "18B+3"
'        Else If st >= 6600 And st <= 6999 Then 
'            return "18B+4"
'        Else If st >= 7000 And st <= 7499 Then 
'            return "19B+0"
'        Else If st >= 7500 And st <= 8099 Then 
'            return "19B+1"
'        Else If st >= 8100 And st <= 8799 Then 
'            return "19B+2"
'        Else If st >= 8800 And st <= 9499 Then 
'            return "19B+3"
'        Else If st >= 9500 And st <= 9999 Then 
'            return "19B+4"
'        Else If st >= 10000 And st <= 10999 Then 
'            return "20B+0"
'        Else If st >= 11000 And st <= 11999 Then 
'            return "20B+1"
'        Else If st >= 12000 And st <= 12999 Then 
'            return "20B+2"
'        Else If st >= 13000 And st <= 13999 Then 
'            return "20B+3"
'        Else If st >= 14000 And st <= 14999 Then 
'            return "20B+4"
'        Else If st >= 15000 And st <= 15999 Then 
'            return "21B+0"
'        Else If st >= 16000 And st <= 16999 Then 
'            return "21B+1"
'        Else If st >= 17000 And st <= 17999 Then 
'            return "21B+2"
'        Else If st >= 18000 And st <= 18999 Then 
'            return "21B+3"
'        Else If st >= 19000 And st <= 19999 Then 
'            return "21B+4"
'        Else If st >= 20000 And st <= 21999 Then 
'            return "22B+0"
'        Else If st >= 22000 And st <= 23999 Then 
'            return "22B+1"
'        Else If st >= 24000 And st <= 25999 Then 
'            return "22B+2"
'        Else If st >= 26000 And st <= 27999 Then 
'            return "22B+3"
'        Else If st >= 28000 And st <= 29999 Then 
'            return "22B+4"
'        Else If st >= 30000 And st <= 32999 Then 
'            return "23B+0"
'        Else If st >= 33000 And st <= 36999 Then 
'            return "23B+1"
'        Else If st >= 37000 And st <= 41999 Then 
'            return "23B+2"
'        Else If st >= 42000 And st <= 45999 Then 
'            return "23B+3"
'        Else If st >= 46000 And st <= 49999 Then 
'            return "23B+4"
'        Else If st >= 50000 And st <= 52999 Then 
'            return "24B+0"
'        Else If st >= 53000 And st <= 56999 Then 
'            return "24B+1"
'        Else If st >= 57000 And st <= 61999 Then 
'            return "24B+2"
'        Else If st >= 62000 And st <= 65999 Then 
'            return "24B+3"
'        Else If st >= 66000 And st <= 69999 Then 
'            return "24B+4"
'        Else If st >= 70000 And st <= 74999 Then 
'            return "25B+0"
'        Else If st >= 75000 And st <= 80999 Then 
'            return "25B+1"
'        Else If st >= 81000 And st <= 87999 Then 
'            return "25B+2"
'        Else If st >= 88000 And st <= 94999 Then 
'            return "25B+3"
'        Else If st >= 95000 And st <= 99999 Then 
'            return "25B+4"
'        End If
'        return "(error)"
'    End Function
'
'    Public Function ConvertDiceDamageToMX(damage as String) as String
'        Dim diceRegex As New Regex("(\d+)d")
'        Dim addsRegex As New Regex("d([+-]\d*)")
'        Dim multsRegex As New Regex("x(\d+)")
'        Dim adRegex As New Regex("\((\d+)\)")
'        Dim typeRegex As New Regex("(tb|bur|cor|cr|cut|imp|pi-|pi|pi+|pi++|tox)")
'        Dim mxDamageSplit As New Regex("B+")
'        Dim mc As MatchCollection
'        Dim dice As Integer
'        Dim adds As Integer
'        Dim mults As Integer
'        Dim ad As Integer
'        Dim dmgType As String
'        Dim avgDamage As Integer
'        Dim baseDamage As String
'
'        ' TODO: Matches
'        ' 
'
'        avgDamage = Floor((3.5 * dice + adds) * mults)
'        baseDamage = BaseDamageFromDmg(avgDamage)
'        
'        If ad <> 0 Then
'            ' TODO: Add AD
'        End If
'
'        return baseDamage & " " & dmgType
'    End Function

End Class
