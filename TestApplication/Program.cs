
using ArxOne.Debian;
using ArxOne.Debian.Formats.Ar;

using var deb = File.OpenRead("dotnet-runtime-6.0_6.0.25-1_amd64.deb");
//using var deb = File.OpenRead("arxone-backup_10.0.17913.1222_all.deb");
var reader = new DebReader(deb);
var (control, files) = reader.Read();

Console.WriteLine();

//var reader = new ArReader(deb);
//for (; ; )
//{
//    var entry = reader.GetNextEntry();
//    if (entry is null)
//        break;
//}
