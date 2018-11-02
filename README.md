# Portia.Roslyn.CheckedException

##   
Checked Exception in C# Using Roslyn

    [Checked Exception](https://en.wikibooks.org/wiki/Java_Programming/Checked_Exceptions "Checked Exception") is an ability of JAVA language which enables solution designers and programmers to manage exceptions in any part/layer of the project. For example, if it is required to handle a custom exception thrown by an **External Library** in the backend of the project and perform an appropriate task for it, the programmer(designer) of the method should introduce that custom exception in the method declaration. After that, Users of this method can handle the throwable exception declared in method anywhere they want.

![Alt Text](https://raw.githubusercontent.com/AminEsmaeily/Portia.Roslyn.CheckedException/master/Attachments/2-2.0.gif)

    The most important benefit of **Checked Exception** is handling most unanticipated exceptions may be thrown by a method in another library. In this case, if the callee introduces all exceptions those should be handled by the caller, then the caller should handle these exceptions or rethrow them to its callers.  

    There is not any feature like **Exception Handling** in C# and if the programmer does not know anything about exceptions may be thrown by the callee and doesn't handle them in code, It can produce bad actions in production.

    In _**CheckedException**_ project of the **_Portia_** project, we created a Visual Studio extension(.VSIX) that can add the ability of JAVA's Checked Exception to C#. It makes Programmers and Solution Designers be able to design layers and methods with less risk of unhandled exceptions. They can handle exceptions in any layer they want and have a good _Stack Trace_ of the thrown exceptions. To use this extension you should add a reference to **Portia.Roslyn.Base** library and after that, add _CheckedException.Base.ThrowsExceptionAttribute_ annotation to top of the method like this:  

**C#**  
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

    _CheckedException.Base.ThrowsExceptionAttribute_ has one constructor parameter that accepts a type. You can give it any type you want, but it won't work for you if the type you gave is not a subclass of **System.Exeption**; So it's better to give it a type that is inherited from **System.Exception** ;). If developer/designer wants to rethrow the exception thrown from submethod, he/she can add same attribute on top of his/her method; or if he/she wants to handle the exception on threw, he/she can use a try-catch block.

###   
How does it work

####   
Analyzer

    In analyzer part, I inherited my analyzer from **DiagnosticAnalyzer**. It helps me to register syntax node action for detecting only invocations using _context.RegisterSyntaxNodeAction(SyntaxNodeAnalyze, SyntaxKind.InvocationExpression);_. In **SyntaxNodeAnalyze** method I fetch all of the attributes with the type of **ThrowsExcepionAttribute** from the invoked method. After that, for each one of the fetched items, I check the state of the exception handling for an introduced exception. Inside of the **CheckExceptionHandling** method, I check two types of handling:

*   Rethrow using Data Annotation(ThrowsExceptionAttribute)
*   Using try-catch block around the method

If non of desired ways has been found in the caller, I report a _Diagnostic_ to inform the programmer of the exception.

#### Code Fixer

    To refactor the code and making changes in code to solve the problem, I fetch all of the attributes again just like the analyzer. If an exception is not handled in code, I look after the try-catch block around method invocation. If I find a try-catch block, then I add a new **Catch** block to the try, else, I add a complete try-catch block around the invocation.
