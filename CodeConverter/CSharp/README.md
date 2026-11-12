# VB → C# conversion

## ByRef arguments that need a temporary

Sources: [passing arguments by value and by reference](https://learn.microsoft.com/dotnet/visual-basic/programming-guide/language-features/procedures/passing-arguments-by-value-and-by-reference) and Roslyn's VB lowering in [`LocalRewriter_Call.vb`](https://github.com/dotnet/roslyn/blob/main/src/Compilers/VisualBasic/Portable/Lowering/LocalRewriter/LocalRewriter_Call.vb): `RewriteByRefArgumentWithCopyBack` creates the temporary, and `VisitCall` wraps the call in a sequence whose side effects after the value are the copy-backs. Behaviour below is covered by `Tests/TestData/SelfVerifyingTests/VBToCS/ByRefWriteBackPlacementTests.vb`.

### VB

* A ByRef parameter accepts any expression. If it isn't a variable (a property, a method result, a constant, an omitted `Optional` argument), VB passes a temporary.
* For a writable property (or a `ReadOnly` auto-property inside its constructor), VB copies the temporary back **straight after the call returns**, before the rest of the enclosing expression or statement runs.

### C#

* `ref`/`out` need a variable, so the temporary must be declared in a statement before the call.
* There is no expression-level copy-back, so a write-back is a statement too.

### Implementation

`ArgumentConverter.HoistByRefDeclaration` hoists the temporary (`AdditionalDeclaration`) and, when VB copies back, the write-back (`AdditionalAssignment`) into the current `PerScopeState` scope. `PerScopeState.CreateLocalsAsync` emits them before and after the converted statement. The output takes the simplest form that keeps VB's timing:

| Where the call is | C# | Code |
|---|---|---|
| A statement, as the last thing evaluated | temporary before, write-back after the statement | `PerScopeState.CreateLocalsAsync` |
| `Return`, `Throw`, `Yield` | `var ret = call; write-back; return ret;` (only when there is a write-back) | `PerScopeState.WithWriteBacksBefore` |
| Followed by other evaluation, or in a condition or block header (`If`, `ElseIf`, loops, `Select Case`, `Case`, `For`, `For Each`, `Using`, `SyncLock`, `With`, `Catch When`) | a local function that declares, calls and writes back, called in place | `NameExpressionNodeVisitor.RequiresLocalFunction`, `HoistAndCallLocalFunctionAsync` |
| A field initializer | a method that declares, writes back and returns, called from the field (static) or the constructor (instance) | `CommonConversions.WithHoistedNodesInMethodAsync`, `PerScopeState.CreateReturnBlockAsync` |

Known gaps:
* Arguments of `New` (object creation) in a block header still write back after the block.
* A non-constant `For ... Step` is re-evaluated each iteration (not ByRef-specific).
* VB copies back a call's arguments in reverse order (`copyBackArray.ReverseContents()` in `LocalRewriter_Call.vb`); the converter writes back in argument order. Only observable when two arguments write back to aliasing targets, e.g. `Two(p.Value, p.Value)`. A fix needs per-call grouping in `ArgumentConverter`, since nested calls write back before the outer call.
* `Dim a = Inc(P), b = Inc(P)` writes back after all declarators, so `b` reads a stale `P`.
* A lambda parameter inside a ByRef argument (`Function(x) Inc(x.P)`) hoists the local function outside the lambda, where `x` isn't in scope (CS0103).
* An auto-property initializer (`Public Property X As Integer = Inc(P)`) leaves a `ph…` placeholder (CS0103): it doesn't go through the field initializer path.
* Partial-class instance initializers run in part order rather than declaration order.
* A `ReadOnly` auto-property is assignable in its type's constructor and initializers, so VB writes it back there, but `GetRefConversionForExpression` treats every read-only property as not writable. A fix also needs the C# write-back target to be assignable from the field initializer method.
