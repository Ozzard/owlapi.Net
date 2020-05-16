# owlapi.Net
A (presently horribly partial) port of github.com/owlcs/owlapi to .Net Core 3.1+

Beware! This is a work-in-progress port of https://github.com/owlcs/owlapi version 5 (taken from the version5 branch at commit 410dd059f5d8e94886890c11876b77ba8463ca63).
At present, all this does is parse OBO files, because that's all I needed at the time.

This uses C# 8.0 features, notably nullability of reference types. Therefore, you'll need .Net Core 3.1+ (or .Net Standard 2.1+) to compile and use it.
With .Net 5 due out later this year, this was a conscious decision.

Available on NuGet at https://www.nuget.org/packages/Owlapi.Net.Unofficial/

# Example code

```csharp
using org.obolibrary.oboformat.parser;
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var p = new OBOFormatParser();
            var doc = p.Parse(new Uri("https://raw.githubusercontent.com/HUPO-PSI/mzQC/master/cv/qc-cv.obo"));
        }
    }
}
```
