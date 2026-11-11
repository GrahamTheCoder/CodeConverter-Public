Imports System
Imports System.Collections.Generic
Imports Xunit

Public Class ParameterlessCustomEventSource
    Public Delegate Sub Callback()
    Private ReadOnly _handlers As New List(Of Callback)
    Public ReadOnly Log As New List(Of String)

    Public Custom Event Changed As Callback
        AddHandler(value As Callback)
            _handlers.Add(value)
        End AddHandler
        RemoveHandler(value As Callback)
            _handlers.Remove(value)
        End RemoveHandler
        RaiseEvent
            Log.Add("RaiseEvent")
            For Each handler In _handlers
                handler()
            Next
        End RaiseEvent
    End Event

    Public Sub RaiseChanged()
        RaiseEvent Changed()
    End Sub
End Class

Public Class EventTests
    <Fact>
    Public Sub CustomEventWithParameterlessRaiseEventWithoutParenthesesRunsAccessorAndHandlers()
        Dim source As New ParameterlessCustomEventSource()
        AddHandler source.Changed, Sub() source.Log.Add("Handler")
        source.RaiseChanged()
        Assert.Equal(New String() {"RaiseEvent", "Handler"}, source.Log.ToArray())
    End Sub
End Class
