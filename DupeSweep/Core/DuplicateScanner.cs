using DupeSweep.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DupeSweep.Core
{
    public class DuplicateScanner
    {
        /// <summary>
        /// ハッシュ関数
        /// </summary>
        /// <param name="fileName">調べたいファイル</param>
        /// <returns></returns>
        private string GetHash(string fileName)
        {
            using var stream = File.OpenRead(fileName);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(stream);
            return Convert.ToHexString(hash);
        }

        public List<FileItem> Scan(string folder, Action<int> progress)
        {
            var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);

            var items = files
                .Select(file =>
                {
                    var info = new FileInfo(file);
                    return new FileItem
                    {
                        FileName = info.Name,
                        FullPath = info.FullName,
                        Size = info.Length,
                    };
                })
                .ToList();

            // まずサイズで絞る
            var candidates = items
                .GroupBy(x => x.Size)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .ToList();

            // 候補だけハッシュ計算

            int completed = 0;
            int total = candidates.Count;
            int lastPercent = 0;

            Parallel.ForEach(
                candidates,
                item =>
                {
                    item.Hash = GetHash(item.FullPath);

                    int current = Interlocked.Increment(ref completed);
                    int percent = current * 100 / total;
                    if (percent != lastPercent)
                    {
                        lastPercent = percent;
                        progress(percent);
                    }
                });

            // ハッシュ一致のみ返す
            return candidates
                .GroupBy(x => x.Hash)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .ToList();
        }
    }
}
