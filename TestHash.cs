using System;

namespace Mirror
{
    public static class Extensions
    {
        public static ushort GetStableHashCode16(this string text)
        {
            int hash = GetStableHashCode(text);
            return (ushort)((hash >> 16) ^ hash);
        }

        public static int GetStableHashCode(this string text)
        {
            unchecked
            {
                uint hash = 0x811c9dc5;
                uint prime = 0x1000193;

                for (int i = 0; i < text.Length; ++i)
                {
                    byte value = (byte)text[i];
                    hash = hash ^ value;
                    hash *= prime;
                }

                return (int)hash;
            }
        }
    }
}

public class CustomNetworkManager
{
    public struct SceneTransitionMessage { }
    public struct SceneTransitionReceivedMessage { }
}

public class Program
{
    public static void Main()
    {
        string t1 = typeof(CustomNetworkManager.SceneTransitionMessage).FullName;
        string t2 = typeof(CustomNetworkManager.SceneTransitionReceivedMessage).FullName;
        
        Console.WriteLine(t1 + ": " + Mirror.Extensions.GetStableHashCode16(t1));
        Console.WriteLine(t2 + ": " + Mirror.Extensions.GetStableHashCode16(t2));
    }
}
