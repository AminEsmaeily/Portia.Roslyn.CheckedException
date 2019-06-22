namespace CheckedException.Core
{
    public enum DiagnosticSeverity
    {
        //
        // Summary:
        //     Something that is an issue, as determined by some authority, but is not surfaced
        //     through normal means. There may be different mechanisms that act on these issues.
        Hidden = 0,
        //
        // Summary:
        //     Information that does not indicate a problem (i.e. not proscriptive).
        Info = 1,
        //
        // Summary:
        //     Something suspicious but allowed.
        Warning = 2,
        //
        // Summary:
        //     Something not allowed by the rules of the language or other authority.
        Error = 3
    }
}
