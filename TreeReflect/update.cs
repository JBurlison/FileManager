using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

string[] testFiles = Directory.GetFiles(@"C:\source\JBurlison\FileManager\src\WebFileExplorer.Tests", "*.cs", SearchOption.AllDirectories);

foreach (var file in testFiles)
{
    var codeOrig = File.ReadAllText(file);
    var code = codeOrig;

    code = Regex.Replace(code, @"\.ListDirectoriesAsync\(([^,]+),\s*([^,]+)\)", ".ListDirectoriesAsync(, , null, null, default)");
    code = Regex.Replace(code, @"\.ListDirectoriesAsync\(([^,]+)\)", ".ListDirectoriesAsync(, false, null, null, default)");

    code = Regex.Replace(code, @"\.SearchAsync\(([^,]+),\s*([^,]+),\s*([^,]+),\s*([^,]+),\s*([^,)]+)\)", ".SearchAsync(, , , , null, null, )");
    
    code = code.Replace("controller.ListDirectories(path)", "controller.ListDirectories(path, false, null, null, default)");
    code = code.Replace("controller.ListDirectories(\"/auth\")", "controller.ListDirectories(\"/auth\", false, null, null, default)");
    code = code.Replace("controller.ListDirectories(path, showHidden)", "controller.ListDirectories(path, showHidden, null, null, default)");

    code = code.Replace("new List<FileSystemItem>", "new WebFileExplorer.Shared.Models.PagedResult<WebFileExplorer.Shared.Models.FileSystemItem> { Items = new System.Collections.Generic.List<WebFileExplorer.Shared.Models.FileSystemItem>() }");

    if (codeOrig != code)
        File.WriteAllText(file, code);
}
