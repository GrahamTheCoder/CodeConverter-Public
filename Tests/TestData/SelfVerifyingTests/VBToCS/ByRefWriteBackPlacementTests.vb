Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports Xunit

Public Class ByRefWriteBackPlacementTests
    <Fact>
    Public Sub ReturnWritesBackBeforeReturningIssue896()
        Dim testClass As New OutParamTestClass
        Assert.Equal(2, testClass.Number)
        Dim result = CallSubWithOutParam(testClass)
        Assert.True(result)
        Assert.Equal(1, testClass.Number)
    End Sub

    Private Shared Function CallSubWithOutParam(testClass As OutParamTestClass) As Boolean
        Return SubWithOutParam(testClass.Number)
    End Function

    Private Shared Function SubWithOutParam(<Out> ByRef out_a As Integer) As Boolean
        out_a = 1
        Return True
    End Function

    Private Class OutParamTestClass
        Friend Property Number As Integer = 2
    End Class

    <Fact>
    Public Sub ReturnWithConversionWritesBackOnce()
        Dim probe As New Probe
        Assert.Equal(1L, ReturnAsLong(probe))
        Assert.Equal("get,set", probe.LogText)
    End Sub

    Private Shared Function ReturnAsLong(probe As Probe) As Long
        Return Increment(probe.Value)
    End Function

    <Fact>
    Public Sub ReturnFromPropertyGetterWritesBack()
        Dim probe As New Probe
        Assert.Equal(1, probe.IncrementedValue)
        Assert.Equal(1, probe.Stored)
    End Sub

    <Fact>
    Public Sub ReturnInMultiLineLambdaWritesBack()
        Dim probe As New Probe
        Dim f As Func(Of Integer) = Function()
                                        Return Increment(probe.Value)
                                    End Function
        Assert.Equal(1, f())
        Assert.Equal(1, probe.Stored)
    End Sub

    <Fact>
    Public Sub ThrowWritesBackBeforeThrowing()
        Dim probe As New Probe
        Dim ex = Assert.Throws(Of InvalidOperationException)(Sub() ThrowCreated(probe))
        Assert.Equal("1", ex.Message)
        Assert.Equal(1, probe.Stored)
        Assert.Equal("get,set", probe.LogText)
    End Sub

    Private Shared Sub ThrowCreated(probe As Probe)
        Throw CreateException(probe.Value)
    End Sub

    Private Shared Function CreateException(ByRef value As Integer) As Exception
        value += 1
        Return New InvalidOperationException(value.ToString())
    End Function

    <Fact>
    Public Sub YieldWritesBackBeforeTheConsumerResumes()
        Dim probe As New Probe
        For Each value In YieldIncremented(probe)
            Assert.Equal(value, probe.Stored)
        Next
        Assert.Equal(2, probe.Stored)
    End Sub

    Private Shared Iterator Function YieldIncremented(probe As Probe) As IEnumerable(Of Integer)
        Yield Increment(probe.Value)
        Yield Increment(probe.Value)
    End Function

    <Fact>
    Public Sub ReturnWritesBackWhenResultNameIsTaken()
        Dim probe As New Probe
        Assert.Equal(42, ReturnWithLocalNamedRet(probe))
        Assert.Equal(42, probe.Stored)
    End Sub

    Private Shared Function ReturnWithLocalNamedRet(probe As Probe) As Integer
        Dim ret = 41
        probe.Stored = ret
        Return Increment(probe.Value)
    End Function

    <Fact>
    Public Sub ReturnToWiderTypeWritesBack()
        Dim probe As New Probe
        Assert.Equal(CObj(1), ReturnAsObject(probe))
        Assert.Equal(1, probe.Stored)
    End Sub

    Private Shared Function ReturnAsObject(probe As Probe) As Object
        Return Increment(probe.Value)
    End Function

    <Fact>
    Public Sub UnchangedStatementShapesStillWriteBack()
        Dim probe As New Probe
        Increment(probe.Value)
        Dim local = Increment(probe.Value)
        local = Increment(probe.Value)
        Assert.Equal(3, local)
        Assert.Equal(3, probe.Stored)
    End Sub

    Private Shared Function Increment(ByRef value As Integer) As Integer
        value += 1
        Return value
    End Function

    Private Class Probe
        Public Stored As Integer
        Public ReadOnly Log As New List(Of String)

        Public ReadOnly Property LogText As String
            Get
                Return String.Join(",", Log)
            End Get
        End Property

        Public Property Value As Integer
            Get
                Log.Add("get")
                Return Stored
            End Get
            Set(value As Integer)
                Log.Add("set")
                Stored = value
            End Set
        End Property

        Public ReadOnly Property IncrementedValue As Integer
            Get
                Return Increment(Value)
            End Get
        End Property
    End Class
End Class
