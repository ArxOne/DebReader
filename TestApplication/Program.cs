
using ArxOne.Debian;

using var deb = File.OpenRead("dotnet-runtime-6.0_6.0.25-1_amd64.deb");
//using var deb = File.OpenRead("arxone-backup_10.0.17913.1222_all.deb");
var reader = new ArReader(deb);
foreach (var (entry, contentStream) in reader.ReadContent())
{
    
}