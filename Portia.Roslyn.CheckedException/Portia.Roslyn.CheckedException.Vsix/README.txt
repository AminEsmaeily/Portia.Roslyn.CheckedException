# Portia.Roslyn.CheckedException
### Checked Exception in C# Using Roslyn

&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[Checked Exception](https://en.wikibooks.org/wiki/Java_Programming/Checked_Exceptions) is an ability of JAVA language which enables solution designers and programmers to manage exceptions in any part/layer of project. For example, if it is required to handle a custom exception throwed by an _External Library_ in backend of the project and perform an appropriate task for it, the programmer(designer) of the method should introduce that custom exception in method declaration. After that, Users of this method can handle the throwable exception declared in method any where they want.
<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Most important benefit of _Checked Exception_ is handling most unanticipated exceptions may be throwed by a method in another library. In this case, if the callee introduces all exceptions those should be handled in caller, then caller should handle these exceptions or rethrow them to its callers.
<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;There is not any feature like _Exception Handling_ in C# and if the programmer does not know any thing about exceptions may be throwed by the callee and doesn't handle them in code, It can produce bad actions in production.
<br/>
<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;In __CheckedException__ project of the __Portia__ project we created a Visual Studio extention(.VSIX) that can add ability of JAVA's Checked Exception to C#. It makes Programmers and Solution Designers to be able to design layers and methods with less risk of unhandled exceptions. They can handle exceptions in any layer they want and have a good _Stack Trace_ of the throwed exceptions.
<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;To use this extention you should add attribute of type **CheckedException.Base.ThrowsExceptionAttribute** to top of method like this:
<br/>
```csharp
[CheckedException.Base.ThrowsException(typeof(InvalidCastException))]
public static long AddNumbers(int a, bool b)
{
    return a + Convert.ToInt32(b);
}
[CheckedException.Base.ThrowsException(typeof(DivideByZeroException))]
public static double DivideNumbers(int a, int b)
{
    return a / b;
}
```
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;__CheckedExcception.Base.ThrowsExceptionAttribute__ has one constructor parameter that accepts a type. You can give it any type you want, but it won't work for you if the type you gave is not a sub class of __System.Exeption__; So it's better to give it a type that is inherited from __System.Exception__ ;). If developer/designer wants to rethrow the exception throwed from submethod, he/she can add same attribute on top of his/her method; or if he/she wants to handle exception on throwed, he/she can use a try-catch block.
<br/>
<br/>
### How does it work
#### Analyzer
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;In analyzer part, I inherited my analyzer from __DiagnosticAnalyzer__. It helps me to register syntax node action for detecting only invocations using __context.RegisterSyntaxNodeAction(SyntaxNodeAnalyze, SyntaxKind.InvocationExpression);__. In __SyntaxNodeAnalyze__ method I fetch all of the attributes with type of __ThrowsExcepionAttribute__ from the invoked method. After that, for each one of the fetched items, I check state of the exception handling for introduced exception. Inside of the __CheckExceptionHandling__ method I check two types of handling:
* Rethrow using Data Annotation(ThrowsExceptionAttribute)
* Using try-catch block around the method<br/><br/>
If non of desired ways has been found in caller, I report a __Diagnostic__ to inform the programmer of the exception.</br>

#### Code Fixer
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;To refactoring the code and making changes in code to solve the problem, I fetch all of the attributes again just like the analyzer. If an exception is not handled in code, I look after the try-catch block around method invocation. If I find a try-catch block, then I add a new **Catch** block to the try, else, I add a complete try-catch block around the invocation.
