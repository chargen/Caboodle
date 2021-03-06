﻿using System;
using System.Threading.Tasks;

using Foundation;
using Security;

namespace Xamarin.Essentials
{
    public static partial class SecureStorage
    {
        public static SecAccessible DefaultAccessible { get; set; } =
            SecAccessible.AfterFirstUnlock;

        public static Task<string> GetAsync(string key, SecAccessible accessible)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            var kc = new KeyChain(accessible);

            return Task.FromResult(kc.ValueForKey(key, Alias));
        }

        public static Task SetAsync(string key, string value, SecAccessible accessible)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var kc = new KeyChain(accessible);
            kc.SetValueForKey(value, key, Alias);

            return Task.CompletedTask;
        }

        static Task<string> PlatformGetAsync(string key) =>
            GetAsync(key, DefaultAccessible);

        static Task PlatformSetAsync(string key, string data) =>
            SetAsync(key, data, DefaultAccessible);
    }

    class KeyChain
    {
        SecAccessible accessible;

        internal KeyChain(SecAccessible accessible) =>
            this.accessible = accessible;

        SecRecord ExistingRecordForKey(string key, string service)
        {
            return new SecRecord(SecKind.GenericPassword)
            {
                Account = key,
                Service = service
            };
        }

        internal string ValueForKey(string key, string service)
        {
            var record = ExistingRecordForKey(key, service);
            var match = SecKeyChain.QueryAsRecord(record, out var resultCode);

            if (resultCode == SecStatusCode.Success)
                return NSString.FromData(match.ValueData, NSStringEncoding.UTF8);
            else
                return null;
        }

        internal void SetValueForKey(string value, string key, string service)
        {
            var record = ExistingRecordForKey(key, service);
            if (string.IsNullOrEmpty(value))
            {
                if (!string.IsNullOrEmpty(ValueForKey(key, service)))
                    RemoveRecord(record);

                return;
            }

            // if the key already exists, remove it
            if (!string.IsNullOrEmpty(ValueForKey(key, service)))
                RemoveRecord(record);

            var result = SecKeyChain.Add(CreateRecordForNewKeyValue(key, value, service));
            if (result != SecStatusCode.Success)
                throw new Exception($"Error adding record: {result}");
        }

        SecRecord CreateRecordForNewKeyValue(string key, string value, string service)
        {
            return new SecRecord(SecKind.GenericPassword)
            {
                Account = key,
                Service = service,
                Label = key,
                Accessible = accessible,
                ValueData = NSData.FromString(value, NSStringEncoding.UTF8),
            };
        }

        bool RemoveRecord(SecRecord record)
        {
            var result = SecKeyChain.Remove(record);
            if (result != SecStatusCode.Success)
                throw new Exception(string.Format($"Error removing record: {result}"));

            return true;
        }
    }
}
