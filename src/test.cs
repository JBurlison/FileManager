using System;
using System.Reflection;
using Radzen;
using WebFileExplorer.Shared.Models;
public class Program { 
    public static void Main() { 
        var obj = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(DataGridRowMouseEventArgs<FileSystemItem>));
        var prop = typeof(DataGridRowMouseEventArgs<FileSystemItem>).GetProperty("Data");
        Console.WriteLine(prop.CanWrite ? "Can Write" : "Cannot Write");
        var setter = prop.GetSetMethod(true);
        if (setter != null) Console.WriteLine("Has Non-Public Setter");
    }
}
