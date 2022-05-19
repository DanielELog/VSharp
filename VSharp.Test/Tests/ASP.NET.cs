using System;
using VSharp.Test;

namespace IntegrationTests;

using WebApplication_sandbox;

[TestSvmFixture]
public static class AspDotNet
{
    [TestSvm]
    public static int Test1()
    {
        string[] args = Array.Empty<string>();
        Program.Main(args);
        return 0;
    }
}
