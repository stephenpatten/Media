using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaHub.ImdbLoader
{
	public static class WebClientExtensions
	{
		public static async Task<byte[]> DownloadDataTaskAsync(this WebClient client, string url, CancellationToken cancellationToken, IProgress<DownloadDataProgress> progress)
		{
			using (var s = await client.OpenReadTaskAsync(url))
			{
				long totalBytes = 0;
				Int64.TryParse(client.ResponseHeaders[HttpResponseHeader.ContentLength], out totalBytes);

				using (var mso = new MemoryStream())
				{
					var cprogress = new Progress<StreamCopyProgress>();
					cprogress.ProgressChanged += new EventHandler<StreamCopyProgress>((sender, e) =>
					{
						var r = new DownloadDataProgress(e, totalBytes);
						r.UserState = e.UserState;
						progress.Report(r);
					});

					await s.CopyToAsync(mso, cancellationToken, cprogress);

					return mso.ToArray();
				}
			}
		}

		public static async Task DownloadFileTaskAsync(this WebClient client, string url, string filepath, CancellationToken cancellationToken, IProgress<DownloadDataProgress> progress)
		{
			using (var s = await client.OpenReadTaskAsync(url))
			{
				long totalBytes = 0;
				Int64.TryParse(client.ResponseHeaders[HttpResponseHeader.ContentLength], out totalBytes);

				using (var fs = File.Open(filepath, FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					var cprogress = new Progress<StreamCopyProgress>();
					cprogress.ProgressChanged += new EventHandler<StreamCopyProgress>((sender, e) =>
					{
						var r = new DownloadDataProgress(e, totalBytes);
						r.UserState = e.UserState;
						progress.Report(r);
					});

					await s.CopyToAsync(fs, cancellationToken, cprogress);
				}
			}
		}
	}

	public class DownloadDataProgress : StreamCopyProgress
	{
		public DownloadDataProgress() : base() { }

		public DownloadDataProgress(long currentBytes, long totalBytes) : base(currentBytes)
		{
			TotalBytes = totalBytes;
		}

		public DownloadDataProgress(StreamCopyProgress copy, long totalBytes) : base(copy)
		{
			TotalBytes = totalBytes;
		}

		public int ProgressPercent { get { return TotalBytes > 0 ? (int)Math.Floor(((decimal)CurrentBytes / (decimal)TotalBytes) * (decimal)100) : 0; } }
		public long TotalBytes { get; set; }
	}
}
