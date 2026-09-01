using System;
var t = Type.GetType("Xunit.SkipException, xunit.core");
Console.WriteLine(t == null ? "SkipException NOT found" : "SkipException found: " + t.FullName);
