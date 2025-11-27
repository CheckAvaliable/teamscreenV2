using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace TeamScreenClientPortableWPF.Utils
{
    public static class Extensions
    {
        // Extension methods to handle method name changes
        public static void SendMessage(this object manager, object message)
        {
            // This is a placeholder - the actual implementation depends on the specific type
            // The original code used SendMessage, but it might be named differently
            var method = manager.GetType().GetMethod("SendMessage") ?? manager.GetType().GetMethod("sendMessage");
            method?.Invoke(manager, new[] { message });
        }
        
        public static string GetSymmetricKeyForRemoteId(this object manager, string remoteId)
        {
            var method = manager.GetType().GetMethod("GetSymmetricKeyForRemoteId") ?? manager.GetType().GetMethod("getSymmetricKeyForRemoteId");
            return method?.Invoke(manager, new object[] { remoteId }) as string;
        }
        
        public static object CreateNewKeyPair(this object manager, string systemId, string remoteId)
        {
            var method = manager.GetType().GetMethod("CreateNewKeyPair") ?? manager.GetType().GetMethod("CreateNewKeyPairKey");
            return method?.Invoke(manager, new object[] { remoteId }); // Only remoteId parameter for the original CreateNewKeyPairKey
        }
    }
}