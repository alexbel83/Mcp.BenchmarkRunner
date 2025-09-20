using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class HashBench
{
    private readonly byte[] _data = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog.");

    [Benchmark] public byte[] Md5()    => MD5.HashData(_data);
    [Benchmark] public byte[] Sha256() => SHA256.HashData(_data);
}
