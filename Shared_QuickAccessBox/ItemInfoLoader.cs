﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using BepInEx;
using MessagePack;
using Studio;

namespace KK_QuickAccessBox
{
    internal static class ItemInfoLoader
    {
        private static readonly string _cachePath = Path.Combine(Paths.CachePath, "KK_QuickAccessBox.cache");

        internal static Dictionary<string, TranslationCacheEntry> TranslationCache { get; private set; }

        public static void StartLoadItemsThread(Action<List<ItemInfo>> onFinished)
        {
            QuickAccessBox.Logger.LogDebug("Starting item information load thread");

            var t = new Thread(LoadItemsThread)
            {
                IsBackground = true,
                Name = "Collect item info",
                Priority = ThreadPriority.BelowNormal
            };
            t.Start(onFinished);
        }

        private static void LoadItemsThread(object obj)
        {
            var onFinished = (Action<List<ItemInfo>>)obj;

            var sw = Stopwatch.StartNew();

            LoadTranslationCache();

            var results = new List<ItemInfo>();

            // The instance has to be spawned by now to avoid crashing from crossthreaded FindObjectOfType
            var info = Info.Instance;
            foreach (var group in info.dicItemLoadInfo)
            {
                foreach (var category in @group.Value)
                {
                    foreach (var item in category.Value)
                    {
                        try
                        {
                            results.Add(new ItemInfo(@group.Key, category.Key, item.Key, item.Value));
                        }
                        catch (Exception e)
                        {
                            if (e is TargetInvocationException ie && ie.InnerException != null)
                                e = ie.InnerException;

                            QuickAccessBox.Logger.LogWarning($"Failed to load information about item: name={item.Value.name} group={@group.Key} category={category.Key} itemNo={item.Key} - {e.Message}");
                        }
                    }
                }
            }

            results.Sort((x, y) => string.Compare(x.FullName, y.FullName, StringComparison.Ordinal));

            sw.Stop();
            QuickAccessBox.Logger.LogDebug($"Item information load thread finished in {sw.Elapsed.TotalMilliseconds:F0}ms - {results.Count} valid items found");

            onFinished(results);
        }

        private static void LoadTranslationCache()
        {
            if (File.Exists(_cachePath))
            {
                try
                {
                    var cacheBytes = File.ReadAllBytes(_cachePath);
                    TranslationCache = MessagePackSerializer.Deserialize<Dictionary<string, TranslationCacheEntry>>(cacheBytes);
                }
                catch (Exception ex)
                {
                    QuickAccessBox.Logger.LogWarning("Failed to read cache: " + ex.Message);
                }
            }
            if (TranslationCache == null)
                TranslationCache = new Dictionary<string, TranslationCacheEntry>();
        }

        public static void SaveTranslationCache(IEnumerable<ItemInfo> itemList)
        {
            try
            {
                var newCache = itemList
                    .GroupBy(info => info.CacheId)
                    .Select(infos =>
                    {
                        if (infos.Count() != 1)
                            QuickAccessBox.Logger.LogWarning($"Cache collision on item {infos.Key}, please consider renaming it");

                        // Items have the same full names so translations can be reused for both of them
                        return infos.First();
                    })
                    .ToDictionary(info => info.CacheId, TranslationCacheEntry.FromItemInfo);

                var data = MessagePackSerializer.Serialize(newCache);
                File.WriteAllBytes(_cachePath, data);
            }
            catch (Exception ex)
            {
                QuickAccessBox.Logger.LogWarning("Failed to write cache: " + ex.Message);
            }
        }
    }
}
