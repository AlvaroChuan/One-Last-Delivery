$code = @"
using System;
public class Test {
    public static int GetStableHashCode(string text) {
        unchecked {
            uint hash = 0x811c9dc5;
            uint prime = 0x1000193;
            for (int i = 0; i < text.Length; ++i) {
                byte value = (byte)text[i];
                hash = hash ^ value;
                hash *= prime;
            }
            return (int)hash;
        }
    }
    public static ushort GetStableHashCode16(string text) {
        int hash = GetStableHashCode(text);
        return (ushort)((hash >> 16) ^ hash);
    }
    public static void Main() {
        string[] texts = new string[] {
            typeof(CustomNetworkManager.SceneTransitionMessage).FullName,
            typeof(CustomNetworkManager.SceneTransitionReceivedMessage).FullName
        };
        foreach(var t in texts) {
            Console.WriteLine(t + \" : \" + GetStableHashCode16(t));
        }
    }
}
public class CustomNetworkManager {
    public struct SceneTransitionMessage {}
    public struct SceneTransitionReceivedMessage {}
}
"@
Add-Type -TypeDefinition $code -Language CSharp
[Test]::Main()
