﻿namespace FlowSynx.Plugins.Google.Cloud.Extensions;

internal static class ByteExtensions
{
    public static string ToHexString(this byte[]? bytes)
    {
        return bytes == null ? string.Empty : Convert.ToHexString(bytes);
    }
}