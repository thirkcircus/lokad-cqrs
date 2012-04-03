#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System.IO;
using System.Text;

namespace Lokad.Cqrs.StreamingStorage
{
    /// <summary>
    /// Helper extensions for the <see cref="IStreamItem"/>
    /// </summary>
    public static class ExtendStreamItem
    {
        public static void WriteText(this IStreamItem item, string text)
        {
            item.Write(s =>
                {
                    using (var writer = new StreamWriter(s))
                    {
                        writer.Write(text);
                    }
                });
        }

        public static void WriteText(this IStreamItem item, string text, Encoding encoding)
        {
            item.Write(s =>
                {
                    using (var writer = new StreamWriter(s, encoding))
                    {
                        writer.Write(text);
                    }
                });
        }

        public static string ReadText(this IStreamItem item)
        {
            string result = null;
            item.ReadInto((stream) =>
                {
                    using (var reader = new StreamReader(stream))
                    {
                        result = reader.ReadToEnd();
                    }
                });

            return result;
        }
    }
}