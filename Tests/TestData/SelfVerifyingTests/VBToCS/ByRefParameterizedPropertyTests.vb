Imports System.Collections.Generic
Imports Xunit

Public Class ByRefParameterizedPropertyTests
    <Fact>
    Public Sub ParameterizedPropertyIsWrittenBackThroughItsSetterIssue909()
        Dim db As New Db
        db.Get_Equal(db.Qr(1, 21))
        Assert.Equal("changed", db.Qr(1, 21))
        Assert.Equal("get 1,21;set 1,21=changed;get 1,21", String.Join(";", db.Log))
    End Sub

    <Fact>
    Public Sub IndexArgumentsAreEvaluatedOnce()
        Dim db As New Db
        Dim calls = 0
        db.Get_Equal(db.Qr(NextIndex(calls), 21))
        Assert.Equal(1, calls)
        Assert.Equal("get 1,21;set 1,21=changed", String.Join(";", db.Log))
    End Sub

    Private Shared Function NextIndex(ByRef calls As Integer) As Integer
        calls += 1
        Return calls
    End Function

    Private Class Db
        Public ReadOnly Log As New List(Of String)
        Private ReadOnly _values As New Dictionary(Of String, String)

        Public Sub Get_Equal(ByRef keyValue As String)
            keyValue = "changed"
        End Sub

        Public Property Qr(tableBuffer As Integer, fieldNumber As Integer) As String
            Get
                Log.Add("get " & tableBuffer & "," & fieldNumber)
                Dim value As String = Nothing
                _values.TryGetValue(tableBuffer & "," & fieldNumber, value)
                Return value
            End Get
            Set(value As String)
                Log.Add("set " & tableBuffer & "," & fieldNumber & "=" & value)
                _values(tableBuffer & "," & fieldNumber) = value
            End Set
        End Property
    End Class
End Class
