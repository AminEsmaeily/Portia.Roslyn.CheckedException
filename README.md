## CheckedException
![Logo](https://raw.githubusercontent.com/AminEsmaeily/Portia.Roslyn.CheckedException/master/Attachments/icon/bug64.png)

[Checked Exception](https://en.wikibooks.org/wiki/Java_Programming/Checked_Exceptions "Checked Exception") is a feature that has been supported by some of the languages like JAVA. It helps to inform the method callers of the exceptions that can be thrown inside the method. The exception problem can become more sensitive when we want to use a method from a library and we don't know any think about its business. Then we should guess the exceptions and perform a good reaction against them. But it cannot be a good solution to our problem, because, most of the times we should know the type of the exception and handle it in its way. So the best way is making the method callers aware about the exceptions that this method can throw. In JAVA, we can use `throws` phrase to announce the exceptions that a method can throw, but, unfortunately, in C# we don't have any built-in feature like this. **CheckedException** can handle it to C# developers. It uses the attributes to announce the callers about the callees exceptions, and also it ships a good feature and it is Severity. We can set the severity for the exceptions to tell the callers how important the exception is.

![Sample](https://raw.githubusercontent.com/AminEsmaeily/Portia.Roslyn.CheckedException/master/Attachments/2-2.0.gif)

### Getting started
To use CheckedException, you should install it on your projects. To do this, you can enter the following command inside your Package Manager:

``` bash
PM> Install-Package Portia.Roslyn.CheckedException
```

After installing this package, you can see CheckedException inside the Analyzers and CheckedException.Core inside the Packages part of your projects' references. The first one is an analyzer to check the exception usages and you don't have anything to do with this. But the second one is what actually you need in your classes.

![References](https://raw.githubusercontent.com/AminEsmaeily/Portia.Roslyn.CheckedException/master/Attachments/img_20190626.png)

#### How to use it
First of all, you should include CheckedException.Core namespace in your class. Then you can add ThrowsExceptionAttribute before your class or methods. This attribute comes with two constructors. The first one gets the type of exception that can be thrown from the method, but the second one gets the type of exception and its severity.

#### Types of use
* ***Class***: You can declare the ThrowsException attribute before the class. In this case, you force all of the methods inside the class to inform the users about the exception.
* ***Method***: In addition of the exceptions declared for the class, each method can have its exceptions too. Also, it can override the severity level of an exception that has been declared for the entire class.

In the following code, you can see the usage of this extension:

``` cs

using CheckedException.Core;
using System;

[ThrowsException(typeof(ArithmeticException), DiagnosticSeverity.Error)]
public static class MathHelper
{
    [ThrowsException(typeof(DivideByZeroException))]
    public static double Divide(long nominator, long denominator)
    {
        return nominator / denominator;
    }

    [ThrowsException(typeof(ArgumentOutOfRangeException), DiagnosticSeverity.Warning)]
    [ThrowsException(typeof(ArithmeticException), DiagnosticSeverity.Warning)]
    public static long Add(long number1, long number2)
    {
        return number1 + number2;
    }
}
```

Depends on the severity of the exception, the compiler shows a message about the exception with code **SAE001** whenever you use a method that throws the exception. To fix this error, as the code fixer suggests, you can either wrap the line inside a TryCatch or rethrow the error by adding the *ThrowsExceptionAttribute* on top of the caller method or class.

#### Severity
As discussed [here](https://docs.microsoft.com/en-us/visualstudio/code-quality/use-roslyn-analyzers?view=vs-2019#rule-severity), there are 5 types of severities declared, but here, in Roslyn, we only have 4 of them:
**Error**,
**Warning**,
**Info**,
and **Hidden**.
Then when you want to set the severity of an exception, you can choose one of the above options.
