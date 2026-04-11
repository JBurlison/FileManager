using System.Reflection;
using System.Linq;
using System;

class Test { 
    static void Main() {
        var asm = Assembly.LoadFrom(@"C:\Users\JBurl\.nuget\packages\radzen.blazor\8.6.2\lib\net9.0\Radzen.Blazor.dll");
        var t = asm.GetTypes().First(x => x.Name == "RadzenTree");
        Console.WriteLine(string.Join(", ", t.GetMethods().Where(m => m.Name.Contains("Expand")).Select(m=>m.Name)));
    } 
}
