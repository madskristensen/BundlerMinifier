﻿#if !NETSTANDARD2_0
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography;

// Copied from https://github.com/aspnet/Antiforgery/blob/release/2.1/src/Microsoft.AspNetCore.Antiforgery/Internal/CryptographyAlgorithms.cs
namespace Microsoft.AspNetCore.Antiforgery.Internal
{
    public static class CryptographyAlgorithms
    {
        public static SHA256 CreateSHA256()
        {
            try
            {
                return SHA256.Create();
            }
            // SHA256.Create is documented to throw this exception on FIPS compliant machines.
            // See: https://msdn.microsoft.com/en-us/library/z08hz7ad%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396
            catch (System.Reflection.TargetInvocationException)
            {
                // Fallback to a FIPS compliant SHA256 algorithm.
                return new SHA256CryptoServiceProvider();
            }
        }
    }
}
#endif